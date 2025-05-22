using System.Reflection;
using System.Reflection.Emit;

namespace Slicito.Common.Implementation.Reflection;

internal class DynamicTypeBuilder
{
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TypeBuilder _typeBuilder;
    private static int _typeCounter;

    public DynamicTypeBuilder(Type baseType, Type interfaceType)
    {
        var typeName = $"Dynamic_{baseType.Name}_{Interlocked.Increment(ref _typeCounter)}";
        var assemblyName = new AssemblyName(typeName);
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Indirect interfaces would have been eventually included when creating the type but we have to include them manually
        // so that the later call to TypeBuilder.GetInterfaces() returns them.
        var interfaces = interfaceType.GetInterfaces().Concat([interfaceType]).ToArray();

        _typeBuilder = _moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class,
            baseType,
            interfaces);
    }

    public IEnumerable<MemberInfo> GetUnimplementedInterfaceMembers()
    {
        var baseType = _typeBuilder.BaseType!;
        var allInterfaces = _typeBuilder.GetInterfaces();

        // Get all members from interfaces
        var interfaceMembers = allInterfaces
            .SelectMany(i => i.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            .Distinct();

        // Get all members from base type
        var baseMembers = new HashSet<string>(
            baseType
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name));

        // Return only members that are not implemented in base type
        return interfaceMembers
            .Where(m => !baseMembers.Contains(m.Name));
    }

    public ConstructorBuilder CreateConstructor(params Type[] parameterTypes)
    {
        var constructorBuilder = _typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            parameterTypes);

        var ilGenerator = constructorBuilder.GetILGenerator();

        // Load this and all parameters
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i + 1); // Load parameter i+1 (0 is this)
        }

        // Call base constructor
        var baseConstructor = _typeBuilder.BaseType!.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            parameterTypes,
            modifiers: null)
            ?? throw new InvalidOperationException($"No constructor found for {_typeBuilder.BaseType.Name} with the specified parameters.");

        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Ret);

        return constructorBuilder;
    }

    public MethodBuilder CreateMethodImplementation(MethodInfo methodInfo, MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual)
    {
        var methodBuilder = _typeBuilder.DefineMethod(
            methodInfo.Name,
            methodAttributes,
            methodInfo.ReturnType,
            methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

        // Override the interface method
        _typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);

        return methodBuilder;
    }

    public MethodBuilder CreateHelperMethod(
        string name,
        MethodAttributes attributes,
        Type returnType,
        params Type[] parameterTypes)
    {
        return _typeBuilder.DefineMethod(
            name,
            attributes,
            returnType,
            parameterTypes);
    }

    public TypeInfo CreateType()
    {
        return _typeBuilder.CreateTypeInfo()
            ?? throw new InvalidOperationException("Failed to create type info.");
    }
}
