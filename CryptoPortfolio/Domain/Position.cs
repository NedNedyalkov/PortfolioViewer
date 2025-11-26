using CryptoPortfolio.Abstractions;

namespace CryptoPortfolio.Domain;

public record Position(decimal Amount, Ticker Ticker, decimal CostBasis) : IPosition;
