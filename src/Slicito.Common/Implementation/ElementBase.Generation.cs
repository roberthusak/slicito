using System.Reflection;
using System.Reflection.Emit;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public partial class ElementBase
{
    internal static Type GenerateInheritedElementType(Type elementInterfaceType)
    {
        if (!elementInterfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.");
        }

        if (!typeof(IElement).IsAssignableFrom(elementInterfaceType))
        {
            throw new ArgumentException("Type must inherit from IElement.");
        }

        // Check if the interface or any of its inherited interfaces have members we can't implement
        var allInterfaces = new HashSet<Type> { elementInterfaceType };
        allInterfaces.UnionWith(elementInterfaceType.GetInterfaces());
        allInterfaces.Remove(typeof(IElement)); // IElement is covered by the base class

        foreach (var iface in allInterfaces)
        {
            var members = iface.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            if (members.Any())
            {
                throw new ArgumentException($"Interface {iface.Name} contains members that cannot be implemented.");
            }
        }

        var typeBuilder = new DynamicTypeBuilder(typeof(ElementBase), elementInterfaceType);
        typeBuilder.CreateConstructor(typeof(ElementId));

        return typeBuilder.CreateType();
    }    
}
