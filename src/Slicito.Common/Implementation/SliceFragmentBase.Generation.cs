using System.Collections.Immutable;
using System.Diagnostics;
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
        Func<Type, ElementBase.InheritedElementTypeInfo> elementTypeFactory)
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
        
        // Create a dictionary to store element type to attribute names mapping
        var elementTypeToAttributeNames = new Dictionary<ElementType, ImmutableArray<string>>();
        
        // Create constructor with additional Dictionary parameter
        typeBuilder.CreateConstructor(typeof(ISlice), typeof(Dictionary<Type, ElementType>), typeof(Dictionary<ElementType, ImmutableArray<string>>));

        var elementTypeMap = new Dictionary<Type, ElementType>();

        foreach (var member in typeBuilder.GetUnimplementedInterfaceMembers())
        {
            ImplementMember(member, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem, elementTypeToAttributeNames);
        }

        var type = typeBuilder.CreateType();
        return slice => (SliceFragmentBase)Activator.CreateInstance(type, slice, elementTypeMap, elementTypeToAttributeNames)!;
    }

    private static void ImplementMember(
        MemberInfo member,
        DynamicTypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        Func<Type, ElementBase.InheritedElementTypeInfo> elementTypeFactory,
        ITypeSystem typeSystem,
        Dictionary<ElementType, ImmutableArray<string>> elementTypeToAttributeNames)
    {
        if (TryMatchRootElementLoaderMethod(member, out var method, out var elementInterfaceType))
        {
            ImplementAsyncLoaderOfRootElements(elementInterfaceType, method, typeBuilder, elementTypeMap, elementTypeFactory, typeSystem, elementTypeToAttributeNames);
            return;
        }

        throw new ArgumentException(
            $"Member {MemberSignatureFormatter.Format(member)} doesn't match any expected pattern.");
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
        Func<Type, ElementBase.InheritedElementTypeInfo> elementTypeFactory,
        ITypeSystem typeSystem,
        Dictionary<ElementType, ImmutableArray<string>> elementTypeToAttributeNames)
    {
        var (concreteElementType, constructorAttributeNames) = elementTypeFactory(elementInterfaceType);

        // Create the element type if not already in the map
        if (!elementTypeMap.ContainsKey(elementInterfaceType))
        {
            var elementType = typeSystem.GetElementTypeFromInterface(elementInterfaceType);
            elementTypeMap[elementInterfaceType] = elementType;

            Debug.Assert(!elementTypeToAttributeNames.ContainsKey(elementType));

            elementTypeToAttributeNames[elementType] = constructorAttributeNames;
        }

        // Create the helper method for element construction
        var helperMethodBuilder = typeBuilder.CreateHelperMethod(
            $"Create_{elementInterfaceType.Name}",
            MethodAttributes.Private | MethodAttributes.Static,
            elementInterfaceType,
            typeof(ElementId),
            typeof(string[]));

        var helperIlGenerator = helperMethodBuilder.GetILGenerator();
        
        // Get constructor that takes ElementId and strings
        var constructorParameterTypes = new List<Type> { typeof(ElementId) };
        constructorParameterTypes.AddRange(Enumerable.Repeat(typeof(string), constructorAttributeNames.Length));
        
        var elementConstructor = concreteElementType.GetConstructor([.. constructorParameterTypes])
            ?? throw new InvalidOperationException($"No constructor found for {concreteElementType.Name} that takes ElementId and {constructorAttributeNames.Length} string parameters.");

        // Implement the helper method to call the constructor
        helperIlGenerator.Emit(OpCodes.Ldarg_0); // Load ElementId parameter
        
        // Load string arguments from the array
        for (var i = 0; i < constructorAttributeNames.Length; i++)
        {
            helperIlGenerator.Emit(OpCodes.Ldarg_1); // Load string[] parameter
            helperIlGenerator.Emit(OpCodes.Ldc_I4, i); // Load index
            helperIlGenerator.Emit(OpCodes.Ldelem_Ref); // Get string from array
        }
        
        helperIlGenerator.Emit(OpCodes.Newobj, elementConstructor); // Call constructor
        helperIlGenerator.Emit(OpCodes.Ret);

        // Create the method implementation
        var methodBuilder = typeBuilder.CreateMethodImplementation(method);
        var ilGenerator = methodBuilder.GetILGenerator();

        // Create a local variable for the element factory
        var elementFactoryLocal = ilGenerator.DeclareLocal(typeof(Func<,,>).MakeGenericType(typeof(ElementId), typeof(string[]), elementInterfaceType));

        // Create a delegate that calls the helper method
        ilGenerator.Emit(OpCodes.Ldnull); // Load null for the target object (static method)
        ilGenerator.Emit(OpCodes.Ldftn, helperMethodBuilder); // Load helper method pointer
        ilGenerator.Emit(OpCodes.Newobj, typeof(Func<,,>).MakeGenericType(typeof(ElementId), typeof(string[]), elementInterfaceType).GetConstructor([typeof(object), typeof(IntPtr)])!);
        ilGenerator.Emit(OpCodes.Stloc, elementFactoryLocal);

        // Call GetRootElementsAsync
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldloc, elementFactoryLocal); // Load element factory
        ilGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBase).GetMethod(nameof(GetRootElementsAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(elementInterfaceType));
        ilGenerator.Emit(OpCodes.Ret);
    }
}
