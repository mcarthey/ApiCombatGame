namespace ApiCombatGame.Services.Interfaces;

public interface IRecaptchaService
{
    Task<RecaptchaValidationResult> ValidateAsync(string token);
}

public class RecaptchaValidationResult
{
    public bool Success { get; set; }
    public float Score { get; set; }
    public string? ErrorMessage { get; set; }
}
