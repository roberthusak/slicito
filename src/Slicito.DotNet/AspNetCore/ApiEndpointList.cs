using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet.AspNetCore;

public class ApiEndpointList
{
    public ImmutableArray<ApiEndpoint> Endpoints { get; }

    private ApiEndpointList(ImmutableArray<ApiEndpoint> endpoints)
    {
        Endpoints = endpoints;
    }

    public class Builder(ILazySlice slice, DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        public async Task<ApiEndpointList> BuildAsync()
        {
            var arrayBuilder = ImmutableArray.CreateBuilder<ApiEndpoint>();

            var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(slice, dotnetTypes);

            foreach (var method in methods)
            {
                var methodElement = method.Method;

                var methodSymbol = (IMethodSymbol) dotnetContext.GetSymbol(methodElement);

                var methodAttribute = methodSymbol.GetAttributes().FirstOrDefault(IsHttpMethodAttribute);
                if (methodAttribute is null)
                {
                    continue;
                }

                if (!TryGetControllerRoute(methodSymbol.ContainingType, out var controllerRoute))
                {
                    continue;
                }

                var (httpMethod, endpointRoute) = GetEndpointHttpMethodAndRoute(methodAttribute);

                var route = CombineRoutes(controllerRoute, endpointRoute);

                arrayBuilder.Add(new ApiEndpoint(httpMethod, route, methodElement));
            }

            return new ApiEndpointList(arrayBuilder.ToImmutable());
        }

        private bool IsHttpMethodAttribute(AttributeData data)
        {
            if (data.AttributeClass is null ||
                !data.AttributeClass.Name.StartsWith("Http") ||
                !data.AttributeClass.Name.EndsWith("Attribute"))
            {
                return false;
            }

            var baseType = data.AttributeClass.BaseType;

            return baseType?.Name == "HttpMethodAttribute" &&
                RoslynHelper.GetFullName(baseType) == "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute";
        }

        private bool TryGetControllerRoute(ITypeSymbol type, [NotNullWhen(true)] out string? route)
        {
            // Check if type is a controller
            if (!type.Name.EndsWith("Controller") || 
                !type.GetAttributes().Any(attr => attr.AttributeClass?.Name is "ControllerAttribute" or "ApiControllerAttribute"))
            {
                route = null;
                return false;
            }

            // Get route from [Route] attribute
            var routeAttribute = type.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "RouteAttribute");

            if (routeAttribute == null)
            {
                route = "";
                return true;
            }

            var routeConstant = routeAttribute.ConstructorArguments.FirstOrDefault();
            route = routeConstant.Kind == TypedConstantKind.Error ? "" : (string?)routeConstant.Value ?? "";
            return true;
        }

        private (HttpMethod method, string route) GetEndpointHttpMethodAndRoute(AttributeData data)
        {
            var methodString = data.AttributeClass!.Name["Http".Length..^"Attribute".Length];
            var method = new HttpMethod(methodString.ToUpperInvariant());

            var routeConstant = data.ConstructorArguments.FirstOrDefault();
            var route = routeConstant.Kind == TypedConstantKind.Error ? null : (string?) routeConstant.Value;

            return (method, route ?? "");
        }

        private string CombineRoutes(string controllerRoute, string endpointRoute)
        {
            // Clean up routes by removing leading/trailing slashes and combining with single slash
            var segments = new[]
            {
                controllerRoute.Trim('/'),
                endpointRoute.Trim('/')
            }
            .Where(s => !string.IsNullOrEmpty(s));

            var combinedRoute = string.Join("/", segments);
            
            // Ensure route starts with / and doesn't end with /
            return combinedRoute.Length == 0 ? "/" : $"/{combinedRoute}";
        }
    }
}

