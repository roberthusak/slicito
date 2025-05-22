using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.Common.Implementation.Reflection;

namespace Slicito.Common.Implementation;

public partial class SliceFragmentBuilderBase
{
    internal static Func<SliceFragmentBuilderBase> GenerateInheritedFragmentBuilderFactory(
        Type sliceFragmentBuilderInterfaceType,
        Type sliceFragmentInterfaceType,
        ITypeSystem typeSystem,
        Func<ISlice, SliceFragmentBase> sliceFragmentFactory)
    {
        if (!sliceFragmentBuilderInterfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.", nameof(sliceFragmentBuilderInterfaceType));
        }

        var typeBuilder = new DynamicTypeBuilder(typeof(SliceFragmentBuilderBase), sliceFragmentBuilderInterfaceType);
        typeBuilder.CreateConstructor(typeof(Dictionary<Type, ElementType>), typeof(Func<ISlice, SliceFragmentBase>));

        var elementTypeMap = new Dictionary<Type, ElementType>();

        foreach (var member in typeBuilder.GetUnimplementedInterfaceMembers())
        {
            ImplementMember(member, typeBuilder, sliceFragmentBuilderInterfaceType, sliceFragmentInterfaceType, elementTypeMap, typeSystem);
        }

        var type = typeBuilder.CreateType();
        return () => (SliceFragmentBuilderBase)Activator.CreateInstance(type, elementTypeMap, sliceFragmentFactory)!;
    }

    private static void ImplementMember(
        MemberInfo member,
        DynamicTypeBuilder typeBuilder,
        Type sliceFragmentBuilderInterfaceType,
        Type sliceFragmentInterfaceType,
        Dictionary<Type, ElementType> elementTypeMap,
        ITypeSystem typeSystem)
    {
        if (TryMatchBuildAsyncMethod(member, sliceFragmentInterfaceType, out var buildAsyncMethod))
        {
            ImplementBuildAsyncMethod(buildAsyncMethod, typeBuilder, sliceFragmentInterfaceType);
            return;
        }

        if (TryMatchRootElementMethod(member, sliceFragmentBuilderInterfaceType, out var rootElementMethod, out var rootElementAttr))
        {
            ImplementRootElementMethod(rootElementMethod, rootElementAttr, typeBuilder, elementTypeMap, typeSystem);
            return;
        }

        throw new ArgumentException(
            $"Method {MemberSignatureFormatter.Format(member)} doesn't match any expected pattern.");
    }

    private static bool TryMatchBuildAsyncMethod(
        MemberInfo member,
        Type sliceFragmentInterfaceType,
        out MethodInfo matchedMethod)
    {
        matchedMethod = null!;
        
        if (member is not MethodInfo method)
        {
            return false;
        }
        
        if (method.Name != "BuildAsync" || 
            method.ReturnType != typeof(ValueTask<>).MakeGenericType(sliceFragmentInterfaceType) ||
            method.GetParameters().Length > 0)
        {
            return false;
        }

        matchedMethod = method;
        return true;
    }
    
    private static void ImplementBuildAsyncMethod(MethodInfo method, DynamicTypeBuilder typeBuilder, Type sliceFragmentInterfaceType)
    {
        var methodBuilder = typeBuilder.CreateMethodImplementation(method);
        var methodIlGenerator = methodBuilder.GetILGenerator();

        // Call base BuildAsync<TSliceFragment>
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBuilderBase).GetMethod(nameof(BuildAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(sliceFragmentInterfaceType));
        methodIlGenerator.Emit(OpCodes.Ret);
    }

    private static bool TryMatchRootElementMethod(
        MemberInfo member,
        Type sliceFragmentBuilderInterfaceType,
        out MethodInfo matchedMethod,
        out RootElementAttribute rootElementAttr)
    {
        matchedMethod = null!;
        rootElementAttr = null!;
        
        if (member is not MethodInfo method)
        {
            return false;
        }
        
        rootElementAttr = method.GetCustomAttribute<RootElementAttribute>();
        if (rootElementAttr == null || 
            method.ReturnType != sliceFragmentBuilderInterfaceType ||
            method.GetParameters().Length != 1 || 
            method.GetParameters()[0].ParameterType != typeof(ElementId))
        {
            return false;
        }

        matchedMethod = method;
        return true;
    }

    private static void ImplementRootElementMethod(
        MethodInfo method,
        RootElementAttribute rootElementAttr,
        DynamicTypeBuilder typeBuilder,
        Dictionary<Type, ElementType> elementTypeMap,
        ITypeSystem typeSystem)
    {
        var methodBuilder = typeBuilder.CreateMethodImplementation(method);
        var methodIlGenerator = methodBuilder.GetILGenerator();

        // Call base AddRootElement<TElement>
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Ldarg_1); // Load ElementId parameter
        methodIlGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBuilderBase).GetMethod(nameof(AddRootElement), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(rootElementAttr.ElementType));
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Ret);

        elementTypeMap[rootElementAttr.ElementType] = typeSystem.GetElementTypeFromInterface(rootElementAttr.ElementType);
    }
}
