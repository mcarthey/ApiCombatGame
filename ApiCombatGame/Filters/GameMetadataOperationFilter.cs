using ApiCombatGame.Filters.Attributes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiCombatGame.Filters;

/// <summary>
/// Reads custom game attributes from controller methods and injects them
/// as x-game-* extensions into the OpenAPI spec.
/// </summary>
public class GameMetadataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;
        var controllerType = methodInfo.DeclaringType;

        // Game tips
        var tips = methodInfo.GetCustomAttributes(typeof(ApiGameTipAttribute), false)
            .Cast<ApiGameTipAttribute>()
            .Select(a => a.Tip)
            .ToList();

        if (tips.Count > 0)
        {
            var arr = new OpenApiArray();
            arr.AddRange(tips.Select(t => (IOpenApiAny)new OpenApiString(t)));
            operation.Extensions["x-game-tips"] = arr;
        }

        // Examples
        var examples = methodInfo.GetCustomAttributes(typeof(ApiExampleAttribute), false)
            .Cast<ApiExampleAttribute>()
            .ToList();

        if (examples.Count > 0)
        {
            var arr = new OpenApiArray();
            arr.AddRange(examples.Select(e =>
            {
                var obj = new OpenApiObject
                {
                    ["name"] = new OpenApiString(e.Name)
                };
                if (e.Request != null)
                    obj["request"] = new OpenApiString(e.Request);
                if (e.Response != null)
                    obj["response"] = new OpenApiString(e.Response);
                return (IOpenApiAny)obj;
            }));
            operation.Extensions["x-game-examples"] = arr;
        }

        // Prerequisites
        var prereq = methodInfo.GetCustomAttributes(typeof(ApiPrerequisiteAttribute), false)
            .Cast<ApiPrerequisiteAttribute>()
            .FirstOrDefault();

        if (prereq != null)
        {
            var arr = new OpenApiArray();
            arr.AddRange(prereq.Steps.Select(s => (IOpenApiAny)new OpenApiString(s)));
            operation.Extensions["x-game-prerequisites"] = arr;
        }

        // Difficulty
        var difficulty = methodInfo.GetCustomAttributes(typeof(ApiDifficultyAttribute), false)
            .Cast<ApiDifficultyAttribute>()
            .FirstOrDefault();

        if (difficulty != null)
        {
            operation.Extensions["x-game-difficulty"] = new OpenApiString(difficulty.Level);
        }

        // Category metadata from controller-level attribute
        if (controllerType != null)
        {
            var categoryMeta = controllerType.GetCustomAttributes(typeof(ApiCategoryMetaAttribute), false)
                .Cast<ApiCategoryMetaAttribute>()
                .FirstOrDefault();

            if (categoryMeta != null)
            {
                operation.Extensions["x-game-category-icon"] = new OpenApiString(categoryMeta.Icon);
                operation.Extensions["x-game-category-color"] = new OpenApiString(categoryMeta.Color);
                operation.Extensions["x-game-category-order"] = new OpenApiInteger(categoryMeta.Order);
            }
        }
    }
}
