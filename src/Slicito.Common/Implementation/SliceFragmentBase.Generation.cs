using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation.Reflection;

namespace Slicito.Common.Implementation;

public partial class SliceFragmentBase
{
    internal static Func<ISlice, SliceFragmentBase> GenerateInheritedFragmentFactory(
        Type sliceFragmentInterfaceType,
        ITypeSystem typeSystem,
        Func<Type, Type> elementTypeFactory)
    {
        if (!sliceFragmentInterfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.", nameof(sliceFragmentInterfaceType));
        }

        if (!typeof(ITypedSliceFragment).IsAssignableFrom(sliceFragmentInterfaceType))
        {
            throw new ArgumentException("Type must inherit from ITypedSliceFragment.", nameof(sliceFragmentInterfaceType));
        }

        var typeBuilder = new DynamicTypeBuilder(typeof(SliceFragmentBase), sliceFragmentInterfaceType);
        typeBuilder.CreateConstructor(typeof(ISlice), typeof(Dictionary<Type, ElementType>));

        var elementTypeMap = new Dictionary<Type, ElementType>();

        foreach (var member in typeBuilder.GetUnimplementedInterfaceMembers())
        {
            ImplementMember(member, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem);
        }

        var type = typeBuilder.CreateType();
        return slice => (SliceFragmentBase)Activator.CreateInstance(type, slice, elementTypeMap)!;
    }

    private static void ImplementMember(
        MemberInfo member,
        DynamicTypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        Func<Type, Type> elementTypeFactory,
        ITypeSystem typeSystem)
    {
        if (TryMatchRootElementLoaderMethod(member, out var method, out var elementInterfaceType))
        {
            ImplementAsyncLoaderOfRootElements(elementInterfaceType, method, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem);
            return;
        }

        throw new ArgumentException(
            $"Method {MemberSignatureFormatter.Format(member)} doesn't match any expected pattern.");
    }

    private static bool TryMatchRootElementLoaderMethod(
        MemberInfo member,
        out MethodInfo matchedMethod,
        out Type elementInterfaceType)
    {
        matchedMethod = null!;
        elementInterfaceType = null!;

        if (member is not MethodInfo method)
        {
            return false;
        }

        if (!method.ReturnType.IsGenericType ||
            method.ReturnType.GetGenericTypeDefinition() != typeof(ValueTask<>) ||
            !method.ReturnType.GetGenericArguments()[0].IsGenericType ||
            method.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition() != typeof(IEnumerable<>) ||
            method.GetParameters().Length > 0)
        {
            return false;
        }

        elementInterfaceType = method.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0];
        if (!typeof(IElement).IsAssignableFrom(elementInterfaceType))
        {
            return false;
        }

        matchedMethod = method;
        return true;
    }

    private static void ImplementAsyncLoaderOfRootElements(
        Type elementInterfaceType,
        MethodInfo method,
        DynamicTypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        Func<Type, Type> elementTypeFactory,
        ITypeSystem typeSystem)
    {
        // Create the element type if not already in the map
        if (!elementTypeMap.ContainsKey(elementInterfaceType))
        {
            elementTypeMap[elementInterfaceType] = typeSystem.GetElementTypeFromInterface(elementInterfaceType);
        }

        // Create the helper method for element construction
        var helperMethodBuilder = typeBuilder.CreateHelperMethod(
            $"Create_{elementInterfaceType.Name}",
            MethodAttributes.Private | MethodAttributes.Static,
            elementInterfaceType,
            typeof(ElementId));

        var helperIlGenerator = helperMethodBuilder.GetILGenerator();
        var concreteElementType = elementTypeFactory(elementInterfaceType);
        var elementConstructor = concreteElementType.GetConstructor([typeof(ElementId)])
            ?? throw new InvalidOperationException($"No constructor found for {concreteElementType.Name} that takes ElementId.");

        // Implement the helper method to call the constructor
        helperIlGenerator.Emit(OpCodes.Ldarg_0); // Load ElementId parameter
        helperIlGenerator.Emit(OpCodes.Newobj, elementConstructor); // Call constructor
        helperIlGenerator.Emit(OpCodes.Ret);

        // Create the method implementation
        var methodBuilder = typeBuilder.CreateMethodImplementation(method);
        var ilGenerator = methodBuilder.GetILGenerator();

        // Create a local variable for the element factory
        var elementFactoryLocal = ilGenerator.DeclareLocal(typeof(Func<,>).MakeGenericType(typeof(ElementId), elementInterfaceType));

        // Create a delegate that calls the helper method
        ilGenerator.Emit(OpCodes.Ldnull); // Load null for the target object (static method)
        ilGenerator.Emit(OpCodes.Ldftn, helperMethodBuilder); // Load helper method pointer
        ilGenerator.Emit(OpCodes.Newobj, typeof(Func<,>).MakeGenericType(typeof(ElementId), elementInterfaceType).GetConstructor([typeof(object), typeof(IntPtr)])!);
        ilGenerator.Emit(OpCodes.Stloc, elementFactoryLocal);

        // Call GetRootElementsAsync
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldloc, elementFactoryLocal); // Load element factory
        ilGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBase).GetMethod(nameof(GetRootElementsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(elementInterfaceType));
        ilGenerator.Emit(OpCodes.Ret);
    }
}
