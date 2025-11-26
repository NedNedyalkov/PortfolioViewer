namespace CryptoPortfolio.Domain;

public record Ticker
{
    public required string Symbol { get; init; }
    public int? Id { get; set; }
    public decimal? CurrentPrice { get; set; }
    public DateTime? LastUpdated { get; set; }
}
