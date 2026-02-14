namespace ApiCombatGame.Models.DTOs.Sdk;

/// <summary>Quick-start guide for new API consumers.</summary>
public class QuickStartResponse
{
    public string GameTitle { get; set; } = "API Combat Game";
    public string BaseUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public string OpenApiSpecUrl { get; set; } = string.Empty;
    public List<QuickStartStep> Steps { get; set; } = new();
    public AuthGuide Auth { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public List<SdkSnippet> ExampleSnippets { get; set; } = new();
}

public class QuickStartStep
{
    public int Order { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExampleBody { get; set; }
}

public class AuthGuide
{
    public string TokenType { get; set; } = "Bearer";
    public string HeaderName { get; set; } = "Authorization";
    public string HeaderFormat { get; set; } = "Bearer {token}";
    public int TokenExpiryMinutes { get; set; } = 60;
    public string LoginEndpoint { get; set; } = string.Empty;
    public string RefreshEndpoint { get; set; } = string.Empty;
}

public class SdkSnippet
{
    public string Language { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>Full API endpoint catalog.</summary>
public class EndpointCatalogResponse
{
    public int TotalEndpoints { get; set; }
    public List<EndpointCategoryDto> Categories { get; set; } = new();
}

public class EndpointCategoryDto
{
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EndpointDto> Endpoints { get; set; } = new();
}

public class EndpointDto
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool RequiresAuth { get; set; }
}

/// <summary>Game status/health for external monitoring.</summary>
public class GameStatusResponse
{
    public string Status { get; set; } = "online";
    public string Version { get; set; } = "v1";
    public DateTime ServerTime { get; set; }
    public GameMetrics Metrics { get; set; } = new();
}

public class GameMetrics
{
    public int TotalPlayers { get; set; }
    public int ActiveSeason { get; set; }
    public int ActiveTournaments { get; set; }
    public int TotalBattlesCompleted { get; set; }
}
