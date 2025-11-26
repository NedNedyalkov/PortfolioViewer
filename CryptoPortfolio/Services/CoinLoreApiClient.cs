using CryptoPortfolio.Domain;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace CryptoPortfolio.Services;

public class CoinLoreApiClient(float priceValiditySec = 30)
{

#pragma warning disable IDE1006
    private record TickersResponse(List<TickerRaw> data);
    private record TickerRaw(string symbol, int id, decimal price_usd);
    private record TickerPrice(int id, decimal price_usd);
#pragma warning restore IDE1006

    private const string ApiBaseUrl = "https://api.coinlore.net/api/";
    private const string TickersEndpoint = $"{ApiBaseUrl}tickers/?start={{0}}&limit={{1}}";
    private const string TickerEndpoint = $"{ApiBaseUrl}ticker/?id={{0}}";

    private readonly HttpClient _http = new();
    private readonly ConcurrentDictionary<string, Ticker> _stringToTickerCache = [];

    private const int PageSize = 100;
    private const int MaxPages = 10;
    private const int RetryCount = 3;

    private readonly TimeSpan _priceValidity = TimeSpan.FromSeconds(priceValiditySec);

    public async Task<Result> BuildCache(CancellationToken ct = default)
    {
        Logger.WriteLine($"Cache ticker data...");

        for (var page = 0; page < MaxPages; page++)
        {
            var url = string.Format(TickersEndpoint, page * PageSize, PageSize);
            var response = await TryRequest<TickersResponse>(url, ct);

            if (!response.Success)
                return Result.Fail(response.Error!);

            CacheData(response.Value!.data);
        }

        Logger.WriteLine($"Cache ticker data finished.");
        return Result.Ok();
    }

    public async Task<Result<decimal>> GetPriceAsync(string symbol, CancellationToken ct = default)
    {
        Logger.WriteLine($"Trying to get price for {symbol}");

        if (!_stringToTickerCache.TryGetValue(symbol, out var id))
        {
            Logger.WriteLine($"Symbol {symbol} not in cache, searching...");
            var foundResult = await FindTickerPageByPage(symbol, ct);
            if (!foundResult.Success)
                return Result<decimal>.Fail(foundResult.Error!);
        }

        if (_stringToTickerCache.TryGetValue(symbol, out var ticker)
            && ticker.CurrentPrice.HasValue
            && ticker.LastUpdated + _priceValidity > DateTime.UtcNow)
        {
            Logger.WriteLine($"Using cached price for {symbol}: {ticker.CurrentPrice.Value} (LastUpdated: {ticker.LastUpdated})");
            return Result<decimal>.Ok(ticker.CurrentPrice.Value);
        }

        Logger.WriteLine($"Refreshing price for {symbol} (id: {id}) (LastUpdated: {ticker?.LastUpdated}) from API...");
        return await RefreshPrice(symbol, ct);
    }

    private async Task<Result<Ticker>> FindTickerPageByPage(string symbol, CancellationToken ct)
    {
        Logger.WriteLine($"Searching for symbol {symbol} page by page... (MaxPages: {MaxPages})");
        for (var page = 0; page < MaxPages; page++)
        {
            Logger.WriteLine($"Looking for {symbol} at page {page}");
            var url = string.Format(TickersEndpoint, page * PageSize, PageSize);
            var response = await TryRequest<TickersResponse>(url, ct);

            if (!response.Success)
                return Result<Ticker>.Fail(response.Error!);

            CacheData(response.Value!.data);

            if (_stringToTickerCache.TryGetValue(symbol, out var ticker))
            {
                Logger.WriteLine($"Found symbol {symbol} with id {ticker.Id} at page {page}.");
                return Result<Ticker>.Ok(ticker);
            }
        }

        return Result<Ticker>.Fail($"Symbol '{symbol}' not found after searching {MaxPages} pages.");
    }

    private void CacheData(List<TickerRaw> data)
    {
        foreach (var t in data)
        {
            if (_stringToTickerCache.ContainsKey(t.symbol))
            {
                _stringToTickerCache[t.symbol].CurrentPrice = t.price_usd;
                _stringToTickerCache[t.symbol].LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _stringToTickerCache[t.symbol] = new Ticker
                {
                    Id = t.id,
                    Symbol = t.symbol,
                    CurrentPrice = t.price_usd,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }
    }

    private async Task<Result<decimal>> RefreshPrice(string symbol, CancellationToken ct)
    {
        if (!_stringToTickerCache.TryGetValue(symbol, out var ticker))
            return Result<decimal>.Fail($"Could not find symbol {symbol} in cache");

        var response = await TryRequest<List<TickerPrice>>(string.Format(TickerEndpoint, ticker.Id), ct);
        if (!response.Success)
            return Result<decimal>.Fail(response.Error!);

        var latest = response.Value!.First();
        _stringToTickerCache[symbol].CurrentPrice = latest.price_usd;
        _stringToTickerCache[symbol].LastUpdated = DateTime.UtcNow;

        Logger.WriteLine($"Updated price for id {symbol}: {latest.price_usd}");
        return Result<decimal>.Ok(latest.price_usd);
    }

    private async Task<Result<T>> TryRequest<T>(string url, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= RetryCount; attempt++)
        {
            Logger.WriteLine($"Attempt {attempt} for {url}");
            try
            {
                var response = await _http.GetFromJsonAsync<T>(url, ct);
                if (response is not null)
                    return Result<T>.Ok(response);

                return Result<T>.Fail("Null response body.");
            }
            catch (Exception) when (attempt < RetryCount)
            {
                Logger.WriteLine($"Attempt {attempt} failed for {url}, retrying...");
                await Task.Delay(200 * attempt, ct);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail($"Error calling {url}: {ex.Message}");
            }
        }

        return Result<T>.Fail($"API request failed after {RetryCount} attempts.");
    }
}
