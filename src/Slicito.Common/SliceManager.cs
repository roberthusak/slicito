using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;

namespace Slicito.Common;

public class SliceManager(ITypeSystem typeSystem) : ISliceManager
{
    private readonly ConcurrentDictionary<Type, Type> _elementInterfaceToElementType = new();
    private readonly ConcurrentDictionary<Type, Func<ISlice, SliceFragmentBase>> _sliceFragmentFactories = new();
    private readonly ConcurrentDictionary<Type, Func<SliceFragmentBuilderBase>> _sliceFragmentBuilderFactories = new();

    public ISliceBuilder CreateBuilder() => new SliceBuilder();

    public TSliceFragmentBuilder CreateTypedBuilder<TSliceFragmentBuilder>()
    {
        var fragmentBuilderType = typeof(TSliceFragmentBuilder);

        if (!fragmentBuilderType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.");
        }

        if (!TryFindFragmentBuilderInterfaceInstantiation(fragmentBuilderType, out var fragmentBuilderInterfaceType))
        {
            throw new ArgumentException("Type must implement ITypedSliceFragmentBuilder<>.");
        }

        Debug.Assert(fragmentBuilderInterfaceType.GetGenericTypeDefinition() == typeof(ITypedSliceFragmentBuilder<>));

        var fragmentType = fragmentBuilderInterfaceType.GetGenericArguments()[0];

        Debug.Assert(typeof(ITypedSliceFragment).IsAssignableFrom(fragmentType));

        var fragmentFactory = _sliceFragmentFactories.GetOrAdd(
            fragmentType,
            _ => SliceFragmentBase.GenerateInheritedFragmentFactory(
                fragmentType,
                typeSystem,
                elementType => _elementInterfaceToElementType.GetOrAdd(elementType, ElementBase.GenerateInheritedElementType)));

        var builderFactory = _sliceFragmentBuilderFactories.GetOrAdd(
            fragmentBuilderType,
            _ => SliceFragmentBuilderBase.GenerateInheritedFragmentBuilderFactory(fragmentBuilderType, fragmentType, typeSystem, fragmentFactory));

        return (TSliceFragmentBuilder)(object)builderFactory();
    }

    private bool TryFindFragmentBuilderInterfaceInstantiation(Type fragmentBuilderType, [NotNullWhen(true)] out Type? fragmentBuilderInterfaceType)
    {
        var interfaces = fragmentBuilderType.GetInterfaces();
        Type? foundInterface = null;

        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ITypedSliceFragmentBuilder<>))
            {
                if (foundInterface != null && foundInterface != interfaceType)
                {
                    throw new ArgumentException($"Type {fragmentBuilderType.Name} implements multiple instantiations of ITypedSliceFragmentBuilder<>.");
                }

                foundInterface = interfaceType;
            }
        }

        fragmentBuilderInterfaceType = foundInterface;
        return foundInterface != null;
    }
}
