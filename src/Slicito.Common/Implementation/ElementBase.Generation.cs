using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation.Reflection;

namespace Slicito.Common.Implementation;

public partial class ElementBase
{
    internal record InheritedElementTypeInfo(Type Type, ImmutableArray<string> ConstructorAttributeNames);

    internal static InheritedElementTypeInfo GenerateInheritedElementType(Type elementInterfaceType)
    {
        if (!elementInterfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.");
        }

        if (!typeof(IElement).IsAssignableFrom(elementInterfaceType))
        {
            throw new ArgumentException("Type must inherit from IElement.");
        }

        var typeBuilder = new DynamicTypeBuilder(typeof(ElementBase), elementInterfaceType);
        
        var fields = new List<FieldBuilder>();
        var attributeNames = new List<string>();
        
        foreach (var member in typeBuilder.GetUnimplementedInterfaceMembers())
        {
            ImplementMember(member, typeBuilder, fields, attributeNames);
        }

        ImplementConstructor(typeBuilder, fields);
        
        return new InheritedElementTypeInfo(typeBuilder.CreateType(), [.. attributeNames]);
    }

    private static void ImplementMember(MemberInfo member, DynamicTypeBuilder typeBuilder, List<FieldBuilder> fields, List<string> attributeNames)
    {
        if (TryMatchStringProperty(member, out var property))
        {
            var field = ImplementStringProperty(property, typeBuilder);
            fields.Add(field);
            attributeNames.Add(property.Name);
            return;
        }

        if (IsPropertyGetMethod(member))
        {
            // Handled by ImplementStringProperty
            return;
        }

        throw new ArgumentException(
            $"Member {MemberSignatureFormatter.Format(member)} doesn't match any expected pattern.");
    }

    private static bool TryMatchStringProperty(MemberInfo member, out PropertyInfo property)
    {
        property = null!;
        
        if (member is not PropertyInfo prop)
        {
            return false;
        }
        
        if (prop.PropertyType != typeof(string))
        {
            return false;
        }
        
        property = prop;
        return true;
    }

    private static bool IsPropertyGetMethod(MemberInfo member)
    {
        if (member is not MethodInfo method)
        {
            return false;
        }

        return method.IsSpecialName && method.Name.StartsWith("get_");
    }
    
    private static FieldBuilder ImplementStringProperty(PropertyInfo property, DynamicTypeBuilder typeBuilder)
    {
        // Define backing field
        var fieldName = $"_{property.Name.ToLowerInvariant()}";
        var fieldBuilder = typeBuilder.TypeBuilder.DefineField(fieldName, typeof(string), FieldAttributes.Private);
        
        // Define property
        var propertyBuilder = typeBuilder.TypeBuilder.DefineProperty(
            property.Name,
            PropertyAttributes.None,
            typeof(string),
            null);
        
        // Define getter method
        var getterBuilder = typeBuilder.TypeBuilder.DefineMethod(
            $"get_{property.Name}",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(string),
            Type.EmptyTypes);
        
        var getterIL = getterBuilder.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);  // this
        getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getterIL.Emit(OpCodes.Ret);
        
        propertyBuilder.SetGetMethod(getterBuilder);
        
        // Implement the interface method
        typeBuilder.TypeBuilder.DefineMethodOverride(getterBuilder, property.GetMethod!);
        
        return fieldBuilder;
    }
    
    private static void ImplementConstructor(DynamicTypeBuilder typeBuilder, List<FieldBuilder> fields)
    {
        // Create constructor that takes ElementId and string properties
        var constructorBuilder = typeBuilder.TypeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(ElementId), .. Enumerable.Repeat(typeof(string), fields.Count)]);
        
        var ilGenerator = constructorBuilder.GetILGenerator();
        
        // Call base constructor with ElementId only
        ilGenerator.Emit(OpCodes.Ldarg_0);  // this
        ilGenerator.Emit(OpCodes.Ldarg_1);  // ElementId parameter
        ilGenerator.Emit(OpCodes.Call, typeof(ElementBase).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [typeof(ElementId)], 
            null)!);
        
        // Set field values from constructor parameters
        for (var i = 0; i < fields.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);  // this
            ilGenerator.Emit(OpCodes.Ldarg, i + 2);  // string parameter (i+2 because 0 is this, 1 is ElementId)
            ilGenerator.Emit(OpCodes.Stfld, fields[i]);
        }
        
        ilGenerator.Emit(OpCodes.Ret);
    }
}
