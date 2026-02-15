namespace ApiCombatGame.Models;

public class RecaptchaSettings
{
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public float MinimumScore { get; set; } = 0.5f;
}
