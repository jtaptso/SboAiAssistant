namespace SapAiAssistant.Infrastructure.Configuration;

public sealed class SapOptions
{
    public const string Section = "Sap";

    public string ServiceLayerBaseUrl { get; set; } = string.Empty;
    public string CompanyDb { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
