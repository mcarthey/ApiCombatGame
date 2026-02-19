using System.Text.Json;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Follows HATEOAS _links from API responses and validates each target endpoint
/// returns a successful response. Catches broken links, 404s, and malformed responses.
/// </summary>
public class LinkWalker
{
    private readonly ResponseValidator _validator;
    private readonly HashSet<string> _visited = new();

    public LinkWalker(ResponseValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Follow all GET _links in a response and validate each returns a non-error status.
    /// POST/PUT/DELETE links are validated for well-formedness but not followed.
    /// </summary>
    public async Task<List<LinkResult>> FollowAllLinks(JsonElement response, HttpClient client)
    {
        var results = new List<LinkResult>();
        var links = _validator.ExtractLinks(response);

        if (links == null)
            return results;

        foreach (var (name, link) in links)
        {
            var result = await FollowLink(name, link.Href, link.Method, client);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Follow a single HATEOAS link. GET links are actually requested;
    /// other methods are validated for well-formedness only.
    /// </summary>
    public async Task<LinkResult> FollowLink(string name, string href, string method, HttpClient client)
    {
        var errors = new List<string>();

        // Validate href format
        if (string.IsNullOrWhiteSpace(href))
        {
            errors.Add("Empty href");
            return new LinkResult(name, href, method, 0, errors, false);
        }

        // Only follow GET links (POST/PUT/DELETE need request bodies or are destructive)
        if (method != "GET")
        {
            // Just validate the URL is well-formed
            if (!href.StartsWith("/") && !href.StartsWith("http"))
                errors.Add($"href '{href}' is not a valid path");

            return new LinkResult(name, href, method, 0, errors, false);
        }

        // Skip already-visited URLs to prevent cycles
        if (!_visited.Add(href))
        {
            return new LinkResult(name, href, method, 0, errors, false);
        }

        // Follow the GET link
        try
        {
            var response = await client.GetAsync(href);
            var statusCode = (int)response.StatusCode;

            if (statusCode >= 400)
            {
                var body = await response.Content.ReadAsStringAsync();
                errors.Add($"Returned {statusCode}: {Truncate(body, 200)}");
            }
            else
            {
                // Validate _links in the followed response (one level deep only)
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    try
                    {
                        var json = JsonDocument.Parse(content).RootElement;
                        // Only validate _links on object responses (arrays don't have _links at root)
                        if (json.ValueKind == JsonValueKind.Object)
                        {
                            var linkErrors = _validator.ValidateLinks(json);
                            errors.AddRange(linkErrors);
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON — that's fine for some endpoints
                    }
                }
            }

            return new LinkResult(name, href, method, statusCode, errors, true);
        }
        catch (Exception ex)
        {
            errors.Add($"Request failed: {ex.Message}");
            return new LinkResult(name, href, method, 0, errors, true);
        }
    }

    /// <summary>Reset the visited URL tracker (useful between test phases).</summary>
    public void ResetVisited() => _visited.Clear();

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}

/// <summary>
/// Result of following a single HATEOAS link.
/// </summary>
/// <param name="Name">Link relation name (e.g. "self", "replay", "queue_battle")</param>
/// <param name="Href">The link's href value</param>
/// <param name="Method">HTTP method (GET, POST, etc.)</param>
/// <param name="StatusCode">HTTP status code (0 if not followed)</param>
/// <param name="Errors">Any validation errors found</param>
/// <param name="WasFollowed">Whether the link was actually HTTP-requested</param>
public record LinkResult(string Name, string Href, string Method, int StatusCode, List<string> Errors, bool WasFollowed);
