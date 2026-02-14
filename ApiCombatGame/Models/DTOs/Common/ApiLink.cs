using System.Text.Json.Serialization;

namespace ApiCombatGame.Models.DTOs.Common;

/// <summary>
/// A hypermedia link to a related API resource.
/// Every response includes <c>_links</c> so you can navigate the API without memorizing URLs.
/// </summary>
public class ApiLink
{
    /// <summary>Relative URL of the linked resource.</summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>HTTP method to use: GET, POST, PUT, DELETE.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Optional human-readable hint about what this link does.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
}

/// <summary>
/// Static factory for building <see cref="ApiLink"/> instances.
/// </summary>
public static class Links
{
    public static ApiLink Get(string href, string? title = null) =>
        new() { Href = href, Method = "GET", Title = title };

    public static ApiLink Post(string href, string? title = null) =>
        new() { Href = href, Method = "POST", Title = title };

    public static ApiLink Put(string href, string? title = null) =>
        new() { Href = href, Method = "PUT", Title = title };

    public static ApiLink Delete(string href, string? title = null) =>
        new() { Href = href, Method = "DELETE", Title = title };
}
