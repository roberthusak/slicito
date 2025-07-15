using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Common.Controllers;


namespace Slicito.Common.Extensibility;

public static class SlicitoContextExtensions
{
    [return: NotNullIfNotNull(nameof(value))]
    public static IController? ConvertToController(this ISlicitoContext context, object? value)
    {
        if (value is null)
        {
            return null;
        }
        else if (value is IController controller)
        {
            return controller;
        }
        else if (value is IModel model)
        {
            return new ModelDisplayer(model);
        }
        else if (context.TryCreateModel(value, out var createdModel))
        {
            return new ModelDisplayer(createdModel);
        }
        else
        {
            var textModel = new Tree([new(value.ToString(), [])]);
            return new ModelDisplayer(textModel);
        }
    }

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
