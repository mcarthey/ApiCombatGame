using System.Text.Json;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Parses the OpenAPI spec from /openapi/v1.json into a navigable structure
/// using raw System.Text.Json. Deliberately avoids Swashbuckle model imports
/// to simulate what an external player would see.
/// </summary>
public class OpenApiSpecReader
{
    private JsonDocument? _spec;
    private JsonElement _root;

    public async Task LoadAsync(HttpClient client)
    {
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        _spec = JsonDocument.Parse(json);
        _root = _spec.RootElement;
    }

    public JsonElement Root => _root;

    /// <summary>Returns the info object (title, version, description).</summary>
    public JsonElement GetInfo() => _root.GetProperty("info");

    /// <summary>Returns the paths object containing all API endpoints.</summary>
    public JsonElement GetPaths() => _root.GetProperty("paths");

    /// <summary>Returns the components.schemas object containing all schema definitions.</summary>
    public JsonElement GetSchemas() =>
        _root.GetProperty("components").GetProperty("schemas");

    /// <summary>Look up a specific schema by name (e.g. "TeamResponse").</summary>
    public JsonElement? GetSchema(string name)
    {
        var schemas = GetSchemas();
        return schemas.TryGetProperty(name, out var schema) ? schema : null;
    }

    /// <summary>Extract enum string values from a schema (e.g. Formation → ["aggressive","defensive","balanced"]).</summary>
    public List<string> GetEnumValues(string schemaName)
    {
        var schema = GetSchema(schemaName);
        if (schema == null) return new List<string>();

        if (schema.Value.TryGetProperty("enum", out var enumArray))
        {
            return enumArray.EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
        }

        return new List<string>();
    }

    /// <summary>
    /// Resolve a $ref string like "#/components/schemas/TeamResponse" to the actual schema element.
    /// </summary>
    public JsonElement? ResolveRef(string refPath)
    {
        if (!refPath.StartsWith("#/")) return null;

        var parts = refPath[2..].Split('/');
        JsonElement current = _root;

        foreach (var part in parts)
        {
            if (!current.TryGetProperty(part, out var next))
                return null;
            current = next;
        }

        return current;
    }

    /// <summary>Get the operation object for a specific path and HTTP method.</summary>
    public JsonElement? GetPathOperation(string path, string method)
    {
        var paths = GetPaths();
        if (!paths.TryGetProperty(path, out var pathItem))
            return null;

        if (!pathItem.TryGetProperty(method.ToLowerInvariant(), out var operation))
            return null;

        return operation;
    }

    /// <summary>Get the response schema for a specific endpoint, method, and status code.</summary>
    public JsonElement? GetResponseSchema(string path, string method, int statusCode)
    {
        var operation = GetPathOperation(path, method);
        if (operation == null) return null;

        if (!operation.Value.TryGetProperty("responses", out var responses))
            return null;

        if (!responses.TryGetProperty(statusCode.ToString(), out var response))
            return null;

        if (!response.TryGetProperty("content", out var content))
            return null;

        if (!content.TryGetProperty("application/json", out var jsonContent))
            return null;

        if (!jsonContent.TryGetProperty("schema", out var schema))
            return null;

        // Handle $ref
        if (schema.TryGetProperty("$ref", out var refProp))
            return ResolveRef(refProp.GetString()!);

        return schema;
    }

    /// <summary>Get the list of required properties from a schema.</summary>
    public List<string> GetRequiredProperties(string schemaName)
    {
        var schema = GetSchema(schemaName);
        if (schema == null) return new List<string>();

        if (schema.Value.TryGetProperty("required", out var required))
        {
            return required.EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();
        }

        return new List<string>();
    }

    /// <summary>Get properties from a schema element, resolving $ref if needed.</summary>
    public Dictionary<string, JsonElement> GetProperties(JsonElement schema)
    {
        var result = new Dictionary<string, JsonElement>();

        // Handle $ref at the top level
        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var resolved = ResolveRef(refProp.GetString()!);
            if (resolved != null)
                schema = resolved.Value;
        }

        if (schema.TryGetProperty("properties", out var props))
        {
            foreach (var prop in props.EnumerateObject())
            {
                result[prop.Name] = prop.Value;
            }
        }

        return result;
    }

    /// <summary>Get an x-game-* extension value from an operation.</summary>
    public string? GetExtension(string path, string method, string extensionName)
    {
        var operation = GetPathOperation(path, method);
        if (operation == null) return null;

        if (operation.Value.TryGetProperty(extensionName, out var ext))
            return ext.GetString();

        return null;
    }

    /// <summary>Discover all endpoints with their paths and HTTP methods.</summary>
    public List<OpenApiEndpointInfo> DiscoverEndpoints()
    {
        var endpoints = new List<OpenApiEndpointInfo>();
        var httpMethods = new[] { "get", "post", "put", "delete", "patch" };

        foreach (var path in GetPaths().EnumerateObject())
        {
            foreach (var method in httpMethods)
            {
                if (path.Value.TryGetProperty(method, out var operation))
                {
                    var operationId = operation.TryGetProperty("operationId", out var opId)
                        ? opId.GetString()
                        : null;
                    var summary = operation.TryGetProperty("summary", out var sum)
                        ? sum.GetString()
                        : null;

                    endpoints.Add(new OpenApiEndpointInfo(path.Name, method.ToUpperInvariant(), operationId, summary));
                }
            }
        }

        return endpoints;
    }
}

public record OpenApiEndpointInfo(string Path, string Method, string? OperationId, string? Summary);
