using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;

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
            if (member is not MethodInfo method)
            {
                throw new ArgumentException($"Member {member.Name} is not a method.");
            }

            // Handle BuildAsync method
            if (method.Name == "BuildAsync" && method.ReturnType == typeof(ValueTask<>).MakeGenericType(sliceFragmentInterfaceType))
            {
                if (method.GetParameters().Length > 0)
                {
                    throw new ArgumentException($"Method {method.Name} must not have any parameters.");
                }

                ImplementBuildAsyncMethod(method, typeBuilder, sliceFragmentInterfaceType);
                continue;
            }

            // Handle Add*Element methods with [RootElement] attribute
            var rootElementAttr = method.GetCustomAttribute<RootElementAttribute>();
            if (rootElementAttr != null && method.ReturnType == sliceFragmentBuilderInterfaceType)
            {
                if (method.GetParameters().Length != 1 || method.GetParameters()[0].ParameterType != typeof(ElementId))
                {
                    throw new ArgumentException($"Method {method.Name} must take exactly one ElementId parameter.");
                }

                ImplementRootElementMethod(method, typeBuilder, rootElementAttr, elementTypeMap, typeSystem);
                continue;
            }

            throw new ArgumentException($"Method {method.Name} does not match any expected pattern.");
        }

        var type = typeBuilder.CreateType();
        return () => (SliceFragmentBuilderBase)Activator.CreateInstance(type, elementTypeMap, sliceFragmentFactory)!;
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

    private static void ImplementRootElementMethod(
        MethodInfo method,
        DynamicTypeBuilder typeBuilder,
        RootElementAttribute rootElementAttr,
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
