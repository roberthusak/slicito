
using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.Abstractions.Models;

namespace Slicito.DotNet.AspNetCore;

public class ApiEndpointCatalog(ILazySlice slice, DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes, ICodeNavigator? navigator) : IController
{
    private const string _navigateToActionName = "NavigateTo";

    private const string _idActionParameterName = "Id";

    public async Task<IModel> InitAsync()
    {
        var endpoints = await new ApiEndpointList.Builder(slice, dotnetContext, dotnetTypes).BuildAsync();

        var items = endpoints.Endpoints.Select(e =>
            new TreeItem($"{e.Method} {e.Path}", [], CreateNavigateToCommand(e.HandlerElement)));

        return new Tree([.. items]);
    }

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _navigateToActionName &&
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            await TryNavigateToAsync(new(id));
        }

        return null;
    }

    private async Task TryNavigateToAsync(ElementId elementId)
    {
        if (navigator is null || !dotnetTypes.HasCodeLocation(dotnetTypes.Method))
        {
            return;
        }

        var codeLocationProvider = slice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.CodeLocation);
        var codeLocationString = await codeLocationProvider(elementId);
        var codeLocation = CodeLocation.Parse(codeLocationString);

        if (codeLocation is null)
        {
            return;
        }

        await navigator.NavigateToAsync(codeLocation);
    }

    private static Command CreateNavigateToCommand(ElementInfo element)
    {
        return new Command(
            _navigateToActionName,
            ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }
}
