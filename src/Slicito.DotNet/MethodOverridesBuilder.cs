using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.DotNet.Facts;

namespace Slicito.DotNet;

/// <remarks>
/// Cannot be used concurrently.
/// </remarks>
public class MethodOverridesBuilder(IDotNetSliceFragment sliceFragment)
{
    private readonly Dictionary<ElementId, HashSet<ElementId>> _overrides = [];

    public async Task AddOverridenMethodsRecursivelyAsync(IDotNetMethodElement method)
    {
        var overridenMethods = await sliceFragment.GetOverridenMethodsAsync(method);

        foreach (var overridenMethod in overridenMethods)
        {
            if (_overrides.TryGetValue(overridenMethod.Id, out var overrides))
            {
                if (!overrides.Add(method.Id))
                {
                    // Already visited from different path in the overriding tree
                    continue;
                }
            }
            else
            {
                _overrides[overridenMethod.Id] = [method.Id];
            }

            await AddOverridenMethodsRecursivelyAsync(overridenMethod);
        }
    }

    public IReadOnlyDictionary<ElementId, ImmutableArray<ElementId>> Build() =>
        _overrides.ToImmutableDictionary(
            keySelector: kvp => kvp.Key,
            elementSelector: kvp => kvp.Value.ToImmutableArray());
}
