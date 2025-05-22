using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public partial class ElementBase
{
    internal static Type GenerateInheritedElementType(Type elementType)
    {
        if (!elementType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.");
        }

        if (!typeof(IElement).IsAssignableFrom(elementType))
        {
            throw new ArgumentException("Type must inherit from IElement.");
        }

        // Check if the interface or any of its inherited interfaces have members we can't implement
        var allInterfaces = new HashSet<Type> { elementType };
        allInterfaces.UnionWith(elementType.GetInterfaces());
        allInterfaces.Remove(typeof(IElement)); // IElement is covered by the base class

        foreach (var iface in allInterfaces)
        {
            var members = iface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            if (members.Any())
            {
                throw new ArgumentException($"Interface {iface.Name} contains members that cannot be implemented.");
            }
        }

        // Create a new assembly and module for the dynamic type
        var assemblyName = new AssemblyName($"DynamicElement_{elementType.Name}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Create the type
        var typeBuilder = moduleBuilder.DefineType(
            $"DynamicElement_{elementType.Name}",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(ElementBase),
            [elementType]);

        // Create constructor
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(ElementId)]);

        var ilGenerator = constructorBuilder.GetILGenerator();

        // Call base constructor
        var baseConstructor =
            typeof(ElementBase).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(ElementId)],
                modifiers: null)
            ?? throw new InvalidOperationException("No constructor found for ElementBase.");
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load this
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load ElementId parameter
        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Ret);

        return typeBuilder.CreateTypeInfo()
            ?? throw new InvalidOperationException("Failed to create type info.");
    }    
}
