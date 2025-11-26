using CryptoPortfolio.Domain;

namespace CryptoPortfolio.Abstractions
{
    public interface IPosition
    {
        decimal Amount { get; }
        decimal CostBasis { get; }
        Ticker Ticker { get; }

        void UpdateCurrentPrice(decimal price) => Ticker.CurrentPrice = price;
    }
}