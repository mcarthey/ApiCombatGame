using System.Text.Json;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Validates API responses against OpenAPI spec schemas using raw JsonElement.
/// Returns lists of validation errors rather than throwing, so a single test
/// can report all issues at once.
/// </summary>
public class ResponseValidator
{
    private readonly OpenApiSpecReader _spec;

    public ResponseValidator(OpenApiSpecReader spec)
    {
        _spec = spec;
    }

    /// <summary>
    /// Validate that a JSON response contains expected properties from a named schema.
    /// Checks property existence, type compatibility, and enum compliance.
    /// </summary>
    public List<string> ValidateAgainstSchema(JsonElement response, string schemaName)
    {
        var errors = new List<string>();
        var schema = _spec.GetSchema(schemaName);

        if (schema == null)
        {
            errors.Add($"Schema '{schemaName}' not found in OpenAPI spec");
            return errors;
        }

        var properties = _spec.GetProperties(schema.Value);
        var required = _spec.GetRequiredProperties(schemaName);

        foreach (var (propName, propSchema) in properties)
        {
            // Check if the property exists using camelCase (JSON naming policy)
            if (!response.TryGetProperty(propName, out var propValue))
            {
                if (required.Contains(propName))
                    errors.Add($"Required property '{propName}' missing from response");
                continue;
            }

            // Skip null values for nullable properties
            if (propValue.ValueKind == JsonValueKind.Null)
            {
                if (propSchema.TryGetProperty("nullable", out var nullable) && nullable.GetBoolean())
                    continue;
                // Non-nullable null is still allowed — many APIs return null for optional fields
                continue;
            }

            // Type check
            var typeError = ValidateType(propName, propValue, propSchema);
            if (typeError != null)
                errors.Add(typeError);

            // Enum check
            if (propSchema.TryGetProperty("enum", out var enumValues))
            {
                var actualValue = propValue.GetString();
                var validValues = enumValues.EnumerateArray().Select(e => e.GetString()).ToList();
                if (actualValue != null && !validValues.Contains(actualValue))
                {
                    errors.Add($"Property '{propName}' has value '{actualValue}' which is not in enum: [{string.Join(", ", validValues)}]");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Validate that a response element matches the expected JSON type from the schema.
    /// </summary>
    private static string? ValidateType(string propName, JsonElement value, JsonElement schema)
    {
        if (!schema.TryGetProperty("type", out var typeElement))
            return null; // No type constraint (could be $ref or allOf)

        var expectedType = typeElement.GetString();
        var actualKind = value.ValueKind;

        return expectedType switch
        {
            "string" when actualKind != JsonValueKind.String =>
                $"Property '{propName}' expected string but got {actualKind}",
            "integer" when actualKind != JsonValueKind.Number =>
                $"Property '{propName}' expected integer but got {actualKind}",
            "number" when actualKind != JsonValueKind.Number =>
                $"Property '{propName}' expected number but got {actualKind}",
            "boolean" when actualKind is not (JsonValueKind.True or JsonValueKind.False) =>
                $"Property '{propName}' expected boolean but got {actualKind}",
            "array" when actualKind != JsonValueKind.Array =>
                $"Property '{propName}' expected array but got {actualKind}",
            "object" when actualKind != JsonValueKind.Object =>
                $"Property '{propName}' expected object but got {actualKind}",
            _ => null
        };
    }

    /// <summary>
    /// Validate that a response's _links dictionary is well-formed.
    /// Each link must have a non-empty "href" string and a valid HTTP "method" string.
    /// </summary>
    public List<string> ValidateLinks(JsonElement response)
    {
        var errors = new List<string>();
        var links = ExtractLinks(response);

        if (links == null)
            return errors; // No _links present — not an error, just nothing to validate

        foreach (var (name, link) in links)
        {
            if (string.IsNullOrWhiteSpace(link.Href))
                errors.Add($"Link '{name}' has empty or null href");

            if (string.IsNullOrWhiteSpace(link.Method))
                errors.Add($"Link '{name}' has empty or null method");
            else if (!IsValidHttpMethod(link.Method))
                errors.Add($"Link '{name}' has invalid method '{link.Method}'");

            // Verify href starts with /
            if (!string.IsNullOrEmpty(link.Href) && !link.Href.StartsWith("/") && !link.Href.StartsWith("http"))
                errors.Add($"Link '{name}' href '{link.Href}' should start with / or http");
        }

        return errors;
    }

    /// <summary>
    /// Extract _links from a response into a typed dictionary.
    /// Returns null if no _links present.
    /// </summary>
    public Dictionary<string, LinkInfo>? ExtractLinks(JsonElement response)
    {
        if (!response.TryGetProperty("_links", out var linksElement))
            return null;

        if (linksElement.ValueKind != JsonValueKind.Object)
            return null;

        var links = new Dictionary<string, LinkInfo>();

        foreach (var linkProp in linksElement.EnumerateObject())
        {
            var href = linkProp.Value.TryGetProperty("href", out var h) ? h.GetString() ?? "" : "";
            var method = linkProp.Value.TryGetProperty("method", out var m) ? m.GetString() ?? "" : "";
            var title = linkProp.Value.TryGetProperty("title", out var t) ? t.GetString() : null;
            links[linkProp.Name] = new LinkInfo(href, method, title);
        }

        return links;
    }

    /// <summary>
    /// Validate that specific link names exist in a response's _links.
    /// </summary>
    public List<string> ValidateLinkPresence(JsonElement response, params string[] expectedLinkNames)
    {
        var errors = new List<string>();
        var links = ExtractLinks(response);

        if (links == null)
        {
            errors.Add($"Response has no _links but expected: [{string.Join(", ", expectedLinkNames)}]");
            return errors;
        }

        foreach (var name in expectedLinkNames)
        {
            if (!links.ContainsKey(name))
                errors.Add($"Expected link '{name}' not found in _links. Found: [{string.Join(", ", links.Keys)}]");
        }

        return errors;
    }

    private static bool IsValidHttpMethod(string method) =>
        method is "GET" or "POST" or "PUT" or "DELETE" or "PATCH";
}

public record LinkInfo(string Href, string Method, string? Title);
