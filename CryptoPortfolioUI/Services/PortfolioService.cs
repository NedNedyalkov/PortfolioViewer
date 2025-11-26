using CryptoPortfolio.Abstractions;
using CryptoPortfolio.Domain;
using CryptoPortfolio.Services;
using CryptoPortfolioUI.Settings;
using Microsoft.Extensions.Options;

namespace CryptoPortfolioUI.Services;

public class PortfolioService
{
    private CoinLoreApiClient CoinLoreApiClient { get; }
    private readonly PortfolioSettings _settings;
    public PortfolioService(IOptions<PortfolioSettings> options)
    {
        _settings = options.Value;
        CoinLoreApiClient = new CoinLoreApiClient(_settings.PriceCacheSeconds);
        Task.Run(() => CoinLoreApiClient.BuildCache());
    }

    public Result<Portfolio> ProcessUploadedFile(string allText) => PortfolioParser.ParseFromText(allText);

    public async Task<Result> RefreshPrices(Portfolio portfolio)
    {
        foreach (var position in portfolio.Positions)
        {
            var priceResult = await CoinLoreApiClient.GetPriceAsync(position.Ticker.Symbol);

            if (!priceResult.Success)
                return Result.Fail($"Failed to get price for {position.Ticker.Symbol}: {priceResult.Error}");

            position.UpdateCurrentPrice(priceResult.Value);
        }

        return Result.Ok();
    }

    public decimal InitialValue(IPosition p) => p.Amount * p.CostBasis;
    public decimal InitialValue(IEnumerable<IPosition> p) => p.Sum(InitialValue);

    public decimal CurrentValue(IPosition p) => p.Amount * p.Ticker.CurrentPrice!.Value;
    public decimal CurrentValue(IEnumerable<IPosition> p) => p.Sum(CurrentValue);
    public decimal ChangePercent(IPosition p)
    {
        var initial = InitialValue(p);
        return initial != 0 ? (CurrentValue(p) - initial) / initial : 0;
    }
}
