namespace ApiCombatGame.Filters.Attributes;

/// <summary>
/// Adds a game strategy tip to an endpoint's documentation.
/// Tips appear as callout boxes in the custom API docs.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ApiGameTipAttribute : Attribute
{
    public string Tip { get; }
    public ApiGameTipAttribute(string tip) => Tip = tip;
}

/// <summary>
/// Adds a rich request/response example to an endpoint's documentation.
/// Multiple examples can be added per endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ApiExampleAttribute : Attribute
{
    public string Name { get; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public ApiExampleAttribute(string name) => Name = name;
}

/// <summary>
/// Describes what must be done before calling this endpoint.
/// Rendered as a prerequisite chain in the docs.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ApiPrerequisiteAttribute : Attribute
{
    public string[] Steps { get; }
    public ApiPrerequisiteAttribute(params string[] steps) => Steps = steps;
}

/// <summary>
/// Indicates the complexity level of an endpoint.
/// Rendered as a colored indicator in the docs.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ApiDifficultyAttribute : Attribute
{
    public string Level { get; }
    public ApiDifficultyAttribute(string level) => Level = level;
}

/// <summary>
/// Adds icon, color, and ordering metadata to a controller's tag group.
/// Applied at the controller class level.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ApiCategoryMetaAttribute : Attribute
{
    public string Icon { get; }
    public string Color { get; }
    public int Order { get; set; }

    public ApiCategoryMetaAttribute(string icon, string color)
    {
        Icon = icon;
        Color = color;
    }
}
