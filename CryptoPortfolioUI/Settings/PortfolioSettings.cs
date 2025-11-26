namespace CryptoPortfolioUI.Settings;

public record PortfolioSettings
{
    public required int RefreshRateMinutes { get; init; }
    public required int PriceCacheSeconds { get; init; }
    public required string LogFileName { get; init; }
}

