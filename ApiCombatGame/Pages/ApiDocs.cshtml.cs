using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace ApiCombatGame.Pages;

public class ApiDocsModel : PageModel
{
    private readonly ISwaggerProvider _swaggerProvider;

    public ApiDocsModel(ISwaggerProvider swaggerProvider)
    {
        _swaggerProvider = swaggerProvider;
    }

    public List<TagGroup> TagGroups { get; private set; } = new();
    public Dictionary<string, SchemaInfo> Schemas { get; private set; } = new();

    private OpenApiDocument _spec = null!;

    public void OnGet()
    {
        _spec = _swaggerProvider.GetSwagger("v1");
        TagGroups = BuildTagGroups(_spec);
        Schemas = BuildSchemas(_spec);
    }

    private List<TagGroup> BuildTagGroups(OpenApiDocument spec)
    {
        var tagMeta = spec.Tags?.ToDictionary(t => t.Name, t => t) ?? new();
        var groups = new Dictionary<string, TagGroup>();

        foreach (var (path, pathItem) in spec.Paths)
        {
            foreach (var (method, operation) in pathItem.Operations)
            {
                var tagName = operation.Tags?.FirstOrDefault()?.Name ?? "Other";

                if (!groups.TryGetValue(tagName, out var group))
                {
                    tagMeta.TryGetValue(tagName, out var tag);
                    group = new TagGroup
                    {
                        Name = tagName,
                        Description = tag?.Description ?? "",
                        Icon = GetExtString(tag?.Extensions, "x-icon", "code"),
                        Color = GetExtString(tag?.Extensions, "x-color", "#6b7280"),
                        Order = GetExtInt(tag?.Extensions, "x-order", 99),
                        Endpoints = new List<EndpointInfo>()
                    };
                    groups[tagName] = group;
                }

                group.Endpoints.Add(BuildEndpoint(method, path, operation));
            }
        }

        return groups.Values.OrderBy(g => g.Order).ToList();
    }

    private EndpointInfo BuildEndpoint(OperationType method, string path, OpenApiOperation operation)
    {
        var endpoint = new EndpointInfo
        {
            Method = method.ToString().ToUpper(),
            Path = path,
            Summary = operation.Summary ?? "",
            Description = operation.Description ?? "",
            OperationId = operation.OperationId ?? "",
            Difficulty = GetExtString(operation.Extensions, "x-game-difficulty", ""),
            GameTips = GetExtStringArray(operation.Extensions, "x-game-tips"),
            Prerequisites = GetExtStringArray(operation.Extensions, "x-game-prerequisites"),
            Examples = GetExtExamples(operation.Extensions),
            RequiresAuth = operation.Security?.Any() == true ||
                           !operation.Tags?.Any(t => t.Name == "Modifiers") == true,
            Parameters = new List<ParameterInfo>(),
            Responses = new Dictionary<string, ResponseInfo>()
        };

        // Check if operation actually has security requirements
        // If there's no explicit security override, check the global requirement
        // Operations with [AllowAnonymous] typically have empty security
        endpoint.RequiresAuth = operation.Security == null || operation.Security.Count > 0;

        // Parameters
        if (operation.Parameters != null)
        {
            foreach (var param in operation.Parameters)
            {
                endpoint.Parameters.Add(new ParameterInfo
                {
                    Name = param.Name,
                    In = param.In?.ToString() ?? "query",
                    Required = param.Required,
                    Description = param.Description ?? "",
                    Type = param.Schema?.Type ?? "string",
                    Default = param.Schema?.Default is OpenApiString s ? s.Value :
                              param.Schema?.Default is OpenApiInteger i ? i.Value.ToString() : null
                });
            }
        }

        // Request body
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) == true)
        {
            endpoint.RequestBody = BuildSchemaInfo(mediaType.Schema);
        }

        // Responses
        foreach (var (code, response) in operation.Responses)
        {
            var responseInfo = new ResponseInfo
            {
                Description = response.Description ?? "",
                Schema = null
            };

            if (response.Content?.TryGetValue("application/json", out var responseMedia) == true)
            {
                responseInfo.Schema = BuildSchemaInfo(responseMedia.Schema);
            }

            endpoint.Responses[code] = responseInfo;
        }

        return endpoint;
    }

    private SchemaInfo BuildSchemaInfo(OpenApiSchema? schema, int depth = 0)
    {
        if (schema == null) return new SchemaInfo { Type = "unknown" };

        // Resolve $ref schemas from components
        if (schema.Reference != null && (schema.Properties == null || !schema.Properties.Any()))
        {
            if (_spec.Components?.Schemas?.TryGetValue(schema.Reference.Id, out var resolved) == true)
                schema = resolved;
        }

        if (depth > 3) return new SchemaInfo { Type = schema.Type ?? "object", Description = "(nested)" };

        var info = new SchemaInfo
        {
            Type = schema.Type ?? "object",
            Description = schema.Description ?? "",
            Required = schema.Required?.ToList() ?? new(),
            Properties = new List<PropertyInfo>(),
            RefName = schema.Reference?.Id
        };

        // Handle array types
        if (schema.Type == "array" && schema.Items != null)
        {
            info.Type = "array";
            info.ItemSchema = BuildSchemaInfo(schema.Items, depth + 1);
        }

        // Handle enum values
        if (schema.Enum?.Any() == true)
        {
            info.EnumValues = schema.Enum
                .OfType<OpenApiString>()
                .Select(e => e.Value)
                .ToList();
        }

        // Properties
        if (schema.Properties != null)
        {
            foreach (var (name, propSchema) in schema.Properties)
            {
                var prop = new PropertyInfo
                {
                    Name = name,
                    Type = propSchema.Type ?? "object",
                    Format = propSchema.Format,
                    Description = propSchema.Description ?? "",
                    Required = schema.Required?.Contains(name) == true,
                    MinLength = propSchema.MinLength,
                    MaxLength = propSchema.MaxLength,
                    Minimum = propSchema.Minimum,
                    Maximum = propSchema.Maximum
                };

                if (propSchema.Type == "array" && propSchema.Items != null)
                {
                    prop.Type = $"array<{propSchema.Items.Type ?? propSchema.Items.Reference?.Id ?? "object"}>";
                }
                else if (propSchema.Reference != null)
                {
                    prop.Type = propSchema.Reference.Id;
                }

                if (propSchema.Enum?.Any() == true)
                {
                    prop.EnumValues = propSchema.Enum
                        .OfType<OpenApiString>()
                        .Select(e => e.Value)
                        .ToList();
                }

                info.Properties.Add(prop);
            }
        }

        return info;
    }

    private Dictionary<string, SchemaInfo> BuildSchemas(OpenApiDocument spec)
    {
        var schemas = new Dictionary<string, SchemaInfo>();
        if (spec.Components?.Schemas == null) return schemas;

        foreach (var (name, schema) in spec.Components.Schemas)
        {
            // Skip internal/complex schemas, focus on DTOs
            if (schema.Properties?.Any() != true && schema.Enum?.Any() != true)
                continue;

            schemas[name] = BuildSchemaInfo(schema);
            schemas[name].RefName = name;
        }

        return schemas;
    }

    // Extension helpers
    private static string GetExtString(IDictionary<string, IOpenApiExtension>? ext, string key, string fallback)
    {
        if (ext?.TryGetValue(key, out var val) == true && val is OpenApiString s)
            return s.Value;
        return fallback;
    }

    private static int GetExtInt(IDictionary<string, IOpenApiExtension>? ext, string key, int fallback)
    {
        if (ext?.TryGetValue(key, out var val) == true && val is OpenApiInteger i)
            return i.Value;
        return fallback;
    }

    private static List<string> GetExtStringArray(IDictionary<string, IOpenApiExtension>? ext, string key)
    {
        if (ext?.TryGetValue(key, out var val) == true && val is OpenApiArray arr)
            return arr.OfType<OpenApiString>().Select(s => s.Value).ToList();
        return new();
    }

    private static List<GameExample> GetExtExamples(IDictionary<string, IOpenApiExtension>? ext)
    {
        if (ext?.TryGetValue("x-game-examples", out var val) != true || val is not OpenApiArray arr)
            return new();

        return arr.OfType<OpenApiObject>().Select(obj => new GameExample
        {
            Name = obj.TryGetValue("name", out var n) && n is OpenApiString ns ? ns.Value : "",
            Request = obj.TryGetValue("request", out var r) && r is OpenApiString rs ? rs.Value : null,
            Response = obj.TryGetValue("response", out var resp) && resp is OpenApiString resps ? resps.Value : null
        }).ToList();
    }
}

// View models
public class TagGroup
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "code";
    public string Color { get; set; } = "#6b7280";
    public int Order { get; set; }
    public List<EndpointInfo> Endpoints { get; set; } = new();
}

public class EndpointInfo
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string OperationId { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public List<string> GameTips { get; set; } = new();
    public List<string> Prerequisites { get; set; } = new();
    public List<GameExample> Examples { get; set; } = new();
    public List<ParameterInfo> Parameters { get; set; } = new();
    public SchemaInfo? RequestBody { get; set; }
    public Dictionary<string, ResponseInfo> Responses { get; set; } = new();
    public bool RequiresAuth { get; set; }
}

public class GameExample
{
    public string Name { get; set; } = "";
    public string? Request { get; set; }
    public string? Response { get; set; }
}

public class ParameterInfo
{
    public string Name { get; set; } = "";
    public string In { get; set; } = "query";
    public bool Required { get; set; }
    public string Description { get; set; } = "";
    public string Type { get; set; } = "string";
    public string? Default { get; set; }
}

public class SchemaInfo
{
    public string Type { get; set; } = "object";
    public string Description { get; set; } = "";
    public string? RefName { get; set; }
    public List<string> Required { get; set; } = new();
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<string>? EnumValues { get; set; }
    public SchemaInfo? ItemSchema { get; set; }
}

public class PropertyInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "string";
    public string? Format { get; set; }
    public string Description { get; set; } = "";
    public bool Required { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public List<string>? EnumValues { get; set; }
}

public class ResponseInfo
{
    public string Description { get; set; } = "";
    public SchemaInfo? Schema { get; set; }
}

public class CodeBlockModel
{
    public string Label { get; set; } = "";
    public string Code { get; set; } = "";
    public string Language { get; set; } = "json";
}
