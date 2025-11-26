using CryptoPortfolio.Abstractions;

namespace CryptoPortfolio.Domain;

public record Portfolio
{
    List<IPosition> _positions = [];
    public List<IPosition> Positions
    {
        get => _positions;
        set => _positions = AggregatePositions(value);
    }

    internal Portfolio(List<IPosition> positions) => Positions = positions;

    private static List<IPosition> AggregatePositions(List<IPosition> positions)
        => positions
            .GroupBy(p => p.Ticker?.Symbol)
            .Select(g => g.Count() > 1 ? new AggregatePosition(g.ToList()) : g.First())
            .Cast<IPosition>()
            .ToList();
}
