using CryptoPortfolio.Abstractions;

namespace CryptoPortfolio.Domain;

public class AggregatePosition : IPosition
{
    private readonly List<IPosition> _innerPositions = [];
    private decimal? _amount;
    private decimal? _costBasis;

    public decimal Amount => _amount ??= _innerPositions.Sum(p => p.Amount);
    public decimal CostBasis => _costBasis ??= _innerPositions.Sum(p => p.CostBasis * p.Amount) / (Amount != 0 ? Amount : 1);
    public Ticker Ticker { get; init; }
    public IEnumerable<IPosition> InnerPositions => _innerPositions;

    public AggregatePosition(List<IPosition> innerPositions)
    {
        ArgumentNullException.ThrowIfNull(innerPositions);
        ArgumentOutOfRangeException.ThrowIfLessThan(innerPositions.Count, 1, nameof(innerPositions));

        if (innerPositions.GroupBy(p => p.Ticker).Count() > 1)
            throw new ArgumentException("All inner positions must have the same Ticker", nameof(innerPositions));

        _innerPositions = innerPositions;
        Ticker = innerPositions[0].Ticker;
    }

    public void UpdateCurrentPrice(decimal price)
    {
        Ticker.CurrentPrice = price;
        _innerPositions.ForEach(p => p.UpdateCurrentPrice(price));
    }
}
