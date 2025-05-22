using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public partial class SliceFragmentBase
{
    internal static Func<ISlice, SliceFragmentBase> GenerateInheritedFragmentFactory(
        Type sliceFragmentType,
        ITypeSystem typeSystem,
        Func<Type, Type> elementTypeFactory)
    {
        if (!sliceFragmentType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.", nameof(sliceFragmentType));
        }

        if (!typeof(ITypedSliceFragment).IsAssignableFrom(sliceFragmentType))
        {
            throw new ArgumentException("Type must inherit from ITypedSliceFragment.", nameof(sliceFragmentType));
        }

        // Create a new assembly and module for the dynamic type
        var assemblyName = new AssemblyName($"DynamicElement_{sliceFragmentType.Name}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Create the type
        var typeBuilder = moduleBuilder.DefineType(
            $"DynamicElement_{sliceFragmentType.Name}",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(SliceFragmentBase),
            [sliceFragmentType]);

        // Create constructor
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(ISlice), typeof(Dictionary<Type, ElementType>)]);

        var ilGenerator = constructorBuilder.GetILGenerator();

        // Call base constructor
        var baseConstructor =
            typeof(SliceFragmentBase).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(ISlice), typeof(Dictionary<Type, ElementType>)],
                modifiers: null)
            ?? throw new InvalidOperationException("No constructor found for SliceFragmentBase.");
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load ISlice parameter
        ilGenerator.Emit(OpCodes.Ldarg_2); // Load Dictionary<Type, ElementType> parameter
        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Ret);

        var members = sliceFragmentType
            .GetInterfaces()
            .Where(i => i != typeof(ITypedSliceFragment))   // Already implemented in base class
            .Concat([sliceFragmentType])
            .SelectMany(i => i.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            .Distinct();

        var elementTypeMap = new Dictionary<Type, ElementType>();

        foreach (var member in members)
        {
            ImplementMember(member, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem);
        }

        // Create the type
        var type = typeBuilder.CreateTypeInfo()
            ?? throw new InvalidOperationException("Failed to create type.");

        return slice => (SliceFragmentBase)Activator.CreateInstance(type, slice, elementTypeMap)!;
    }

    private static void ImplementMember(
        MemberInfo member,
        TypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        Func<Type, Type> elementTypeFactory,
        ITypeSystem typeSystem)
    {
        if (member is not MethodInfo method)
        {
            throw new ArgumentException($"Member {member.Name} is not a method.");
        }

        // Check if the method matches the pattern: ValueTask<IEnumerable<ISomeElement>> MethodName()
        if (!method.ReturnType.IsGenericType ||
            method.ReturnType.GetGenericTypeDefinition() != typeof(ValueTask<>) ||
            !method.ReturnType.GetGenericArguments()[0].IsGenericType ||
            method.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition() != typeof(IEnumerable<>))
        {
            throw new ArgumentException($"Method {method.Name} does not return ValueTask<IEnumerable<T>>.");
        }

        var elementType = method.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0];
        if (!typeof(IElement).IsAssignableFrom(elementType))
        {
            throw new ArgumentException($"Method {method.Name} does not return a collection of IElement.");
        }

        if (method.GetParameters().Length > 0)
        {
            throw new ArgumentException($"Method {method.Name} must not have any parameters.");
        }

        ImplementAsyncLoaderOfRootElements(elementType, method, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem);
    }

    private static void ImplementAsyncLoaderOfRootElements(
        Type elementType,
        MethodInfo method,
        TypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        Func<Type, Type> elementTypeFactory,
        ITypeSystem typeSystem)
    {
        // Create the element type if not already in the map
        if (!elementTypeMap.ContainsKey(elementType))
        {
            elementTypeMap[elementType] = typeSystem.GetElementTypeFromInterface(elementType);
        }

        // Create the helper method for element construction
        var helperMethodBuilder = typeBuilder.DefineMethod(
            $"Create_{elementType.Name}",
            MethodAttributes.Private | MethodAttributes.Static,
            elementType,
            [typeof(ElementId)]);

        var helperIlGenerator = helperMethodBuilder.GetILGenerator();
        var concreteElementType = elementTypeFactory(elementType);
        var elementConstructor = concreteElementType.GetConstructor([typeof(ElementId)])
            ?? throw new InvalidOperationException($"No constructor found for {concreteElementType.Name} that takes ElementId.");

        // Implement the helper method to call the constructor
        helperIlGenerator.Emit(OpCodes.Ldarg_0); // Load ElementId parameter
        helperIlGenerator.Emit(OpCodes.Newobj, elementConstructor); // Call constructor
        helperIlGenerator.Emit(OpCodes.Ret);

        // Create the method implementation
        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            method.ReturnType,
            null);

        var ilGenerator = methodBuilder.GetILGenerator();

        // Create a local variable for the element factory
        var elementFactoryLocal = ilGenerator.DeclareLocal(typeof(Func<,>).MakeGenericType(typeof(ElementId), elementType));

        // Create a delegate that calls the helper method
        ilGenerator.Emit(OpCodes.Ldnull); // Load null for the target object (static method)
        ilGenerator.Emit(OpCodes.Ldftn, helperMethodBuilder); // Load helper method pointer
        ilGenerator.Emit(OpCodes.Newobj, typeof(Func<,>).MakeGenericType(typeof(ElementId), elementType).GetConstructor([typeof(object), typeof(IntPtr)])!);
        ilGenerator.Emit(OpCodes.Stloc, elementFactoryLocal);

        // Call GetRootElementsAsync
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldloc, elementFactoryLocal); // Load element factory
        ilGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBase).GetMethod(nameof(GetRootElementsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(elementType));
        ilGenerator.Emit(OpCodes.Ret);

        // Override the interface method
        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }
}
