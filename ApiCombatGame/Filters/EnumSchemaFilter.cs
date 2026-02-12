using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiCombatGame.Filters;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum) return;

        var enumValues = Enum.GetNames(context.Type);
        schema.Enum.Clear();
        foreach (var name in enumValues)
        {
            schema.Enum.Add(new OpenApiString(name));
        }
        schema.Type = "string";
    }
}
