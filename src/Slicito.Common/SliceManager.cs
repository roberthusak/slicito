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
        var fragmentBuilderInterfaceType = typeof(TSliceFragmentBuilder);

        if (!fragmentBuilderInterfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface.");
        }

        if (!TryFindFragmentBuilderInterfaceInstantiation(fragmentBuilderInterfaceType, out var fragmentBuilderGenericInterfaceType))
        {
            throw new ArgumentException("Type must implement ITypedSliceFragmentBuilder<>.");
        }

        Debug.Assert(fragmentBuilderGenericInterfaceType.GetGenericTypeDefinition() == typeof(ITypedSliceFragmentBuilder<>));

        var fragmentInterfaceType = fragmentBuilderGenericInterfaceType.GetGenericArguments()[0];

        Debug.Assert(typeof(ITypedSliceFragment).IsAssignableFrom(fragmentInterfaceType));

        var fragmentFactory = _sliceFragmentFactories.GetOrAdd(
            fragmentInterfaceType,
            _ => SliceFragmentBase.GenerateInheritedFragmentFactory(
                fragmentInterfaceType,
                typeSystem,
                elementInterfaceType => _elementInterfaceToElementType.GetOrAdd(elementInterfaceType, ElementBase.GenerateInheritedElementType)));

        var builderFactory = _sliceFragmentBuilderFactories.GetOrAdd(
            fragmentBuilderInterfaceType,
            _ => SliceFragmentBuilderBase.GenerateInheritedFragmentBuilderFactory(fragmentBuilderInterfaceType, fragmentInterfaceType, typeSystem, fragmentFactory));

        return (TSliceFragmentBuilder)(object)builderFactory();
    }

    private bool TryFindFragmentBuilderInterfaceInstantiation(Type fragmentBuilderInterfaceType, [NotNullWhen(true)] out Type? fragmentBuilderGenericInterfaceType)
    {
        var interfaces = fragmentBuilderInterfaceType.GetInterfaces();
        Type? foundInterface = null;

        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ITypedSliceFragmentBuilder<>))
            {
                if (foundInterface != null && foundInterface != interfaceType)
                {
                    throw new ArgumentException($"Type {fragmentBuilderInterfaceType.Name} implements multiple instantiations of ITypedSliceFragmentBuilder<>.");
                }

                foundInterface = interfaceType;
            }
        }

        fragmentBuilderGenericInterfaceType = foundInterface;
        return foundInterface != null;
    }
}
