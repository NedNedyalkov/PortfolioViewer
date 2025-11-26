using CryptoPortfolio.Abstractions;
using CryptoPortfolio.Domain;
using System.Globalization;

namespace CryptoPortfolio.Services;

public class PortfolioParser
{
    public static Result<Portfolio> ParseFromText(string allText)
    {
        var lines = allText.Split(Environment.NewLine);
        return ParseFromLines(lines);
    }

    public static Result<Portfolio> ParseFromLines(IEnumerable<string> lines)
    {
        Portfolio? portfolio;
        try
        {
            var positions = new List<IPosition>();
            Logger.WriteLine($"Parsing portfolio from provided lines.");

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length != 3)
                    return Result<Portfolio>.Fail($"Invalid line format: {line}\r\n Expected: <decimal>|<string>|<decimal>");

                if (!decimal.TryParse(parts[0], CultureInfo.InvariantCulture, out decimal amount))
                    return Result<Portfolio>.Fail($"Failed to parse amount from portfolio line: {line}");

                if (amount < 0)
                    return Result<Portfolio>.Fail($"Negative amount at line: {line}");

                if (string.IsNullOrWhiteSpace(parts[1]))
                    return Result<Portfolio>.Fail($"Invalid symol at line: {line}");

                var ticker = new Ticker { Symbol = parts[1].Trim().ToUpper() };

                if (!decimal.TryParse(parts[2], CultureInfo.InvariantCulture, out decimal price))
                    return Result<Portfolio>.Fail($"Failed to parse price from portfolio line: {line}");

                if (price < 0)
                    return Result<Portfolio>.Fail($"Negative price at line: {line}");

                positions.Add(new Position(amount, ticker, price));
            }
            Logger.WriteLine($"Parsed {positions.Count} positions from provided lines.");

            portfolio = new Portfolio(positions);
            Logger.WriteLine($"Portfolio with {portfolio.Positions.Count} positions generated.");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"Error parsing portfolio: {ex.Message}");
            return Result<Portfolio>.Fail($"Error parsing portfolio: {ex.Message}");
        }

        return Result<Portfolio>.Ok(portfolio);
    }
}

