using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;


namespace Slicito.Common.Extensibility;

public static class SlicitoContextExtensions
{
    public static bool TryCreateModel(this ISlicitoContext context, object value, [NotNullWhen(true)] out IModel? model)
    {
        var genericCreatorType = typeof(IModelCreator<>);

        for (var type = value.GetType(); type is not null; type = type.BaseType)
        {
            var creatorType = genericCreatorType.MakeGenericType(type);
            if (context.TryGetService(creatorType, out var creator))
            {
                var createModelMethod = creatorType.GetMethod(nameof(IModelCreator<object>.CreateModel));

                model = (IModel) createModelMethod!.Invoke(creator, [value])!;
                return true;
            }
        }

        model = default;
        return false;
    }
}
