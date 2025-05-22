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

        // Create a new assembly and module for the dynamic type
        var assemblyName = new AssemblyName($"DynamicBuilder_{sliceFragmentBuilderInterfaceType.Name}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Create the type
        var typeBuilder = moduleBuilder.DefineType(
            $"DynamicBuilder_{sliceFragmentBuilderInterfaceType.Name}",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(SliceFragmentBuilderBase),
            [sliceFragmentBuilderInterfaceType]);

        // Create constructor
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(Dictionary<Type, ElementType>), typeof(Func<ISlice, SliceFragmentBase>)]);

        var ilGenerator = constructorBuilder.GetILGenerator();

        // Call base constructor
        var baseConstructor =
            typeof(SliceFragmentBuilderBase).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(Dictionary<Type, ElementType>), typeof(Func<ISlice, SliceFragmentBase>)],
                modifiers: null)
            ?? throw new InvalidOperationException("No constructor found for SliceFragmentBuilderBase.");
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load Dictionary<Type, ElementType> parameter
        ilGenerator.Emit(OpCodes.Ldarg_2); // Load Func<ISlice, SliceFragmentBase> parameter
        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Ret);

        // Implement members declared in interfaces
        var members = sliceFragmentBuilderInterfaceType
            .GetInterfaces()
            .Concat([sliceFragmentBuilderInterfaceType])
            .SelectMany(i => i.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            .Distinct();

        var elementTypeMap = new Dictionary<Type, ElementType>();

        foreach (var member in members)
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

        // Create the type
        var type = typeBuilder.CreateTypeInfo()
            ?? throw new InvalidOperationException("Failed to create type.");

        return () => (SliceFragmentBuilderBase)Activator.CreateInstance(type, elementTypeMap, sliceFragmentFactory)!;
    }

    private static void ImplementBuildAsyncMethod(MethodInfo method, TypeBuilder typeBuilder, Type sliceFragmentInterfaceType)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            method.ReturnType,
            null);

        var methodIlGenerator = methodBuilder.GetILGenerator();

        // Call base BuildAsync<TSliceFragment>
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBuilderBase).GetMethod(nameof(BuildAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(sliceFragmentInterfaceType));
        methodIlGenerator.Emit(OpCodes.Ret);

        // Override the interface method
        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }

    private static void ImplementRootElementMethod(
        MethodInfo method,
        TypeBuilder typeBuilder,
        RootElementAttribute rootElementAttr,
        Dictionary<Type, ElementType> elementTypeMap,
        ITypeSystem typeSystem)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            method.ReturnType,
            [typeof(ElementId)]);

        var methodIlGenerator = methodBuilder.GetILGenerator();

        // Call base AddRootElement<TElement>
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Ldarg_1); // Load ElementId parameter
        methodIlGenerator.Emit(OpCodes.Call, typeof(SliceFragmentBuilderBase).GetMethod(nameof(AddRootElement), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(rootElementAttr.ElementType));
        methodIlGenerator.Emit(OpCodes.Ldarg_0); // Load this
        methodIlGenerator.Emit(OpCodes.Ret);

        elementTypeMap[rootElementAttr.ElementType] = typeSystem.GetElementTypeFromInterface(rootElementAttr.ElementType);

        // Override the interface method
        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }
}
