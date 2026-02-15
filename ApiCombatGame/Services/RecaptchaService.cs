using System.Text.Json;
using System.Text.Json.Serialization;
using ApiCombatGame.Models;
using ApiCombatGame.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Services;

public class RecaptchaService : IRecaptchaService
{
    private readonly RecaptchaSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RecaptchaService> _logger;
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    public RecaptchaService(
        IOptions<RecaptchaSettings> settings,
        HttpClient httpClient,
        ILogger<RecaptchaService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RecaptchaValidationResult> ValidateAsync(string token)
    {
        // If no secret key configured, skip validation (dev mode)
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            return new RecaptchaValidationResult { Success = true, Score = 1.0f };

        if (string.IsNullOrWhiteSpace(token))
        {
            return new RecaptchaValidationResult
            {
                Success = false,
                Score = 0f,
                ErrorMessage = "reCAPTCHA token is missing."
            };
        }

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _settings.SecretKey),
                new KeyValuePair<string, string>("response", token)
            });

            var response = await _httpClient.PostAsync(VerifyUrl, content);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);

            if (result == null)
            {
                return new RecaptchaValidationResult
                {
                    Success = false, Score = 0f,
                    ErrorMessage = "Failed to validate reCAPTCHA."
                };
            }

            _logger.LogInformation("reCAPTCHA score: {Score}, success: {Success}", result.Score, result.Success);

            return new RecaptchaValidationResult
            {
                Success = result.Success && result.Score >= _settings.MinimumScore,
                Score = result.Score,
                ErrorMessage = result.Score < _settings.MinimumScore
                    ? "Suspicious activity detected. Please try again."
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "reCAPTCHA validation failed");
            // Fail open in case of network issues to Google
            return new RecaptchaValidationResult { Success = true, Score = 1.0f };
        }
    }

    private class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
