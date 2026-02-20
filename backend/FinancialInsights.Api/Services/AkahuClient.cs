using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using FinancialInsights.Api.Config;
using Microsoft.Extensions.Options;

namespace FinancialInsights.Api.Services;

public sealed class AkahuClient(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AkahuOptions> optionsMonitor,
    ILogger<AkahuClient> logger) : IAkahuClient
{
    public async Task<IReadOnlyList<AkahuAccount>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        using var document = await SendRequestAsync("accounts", cancellationToken);
        var items = ExtractItems(document.RootElement, "accounts");

        var accounts = new List<AkahuAccount>(items.Count);
        foreach (var item in items)
        {
            var accountId = ReadString(item, "_id", "id");
            if (string.IsNullOrWhiteSpace(accountId))
            {
                continue;
            }

            accounts.Add(new AkahuAccount
            {
                Id = accountId,
                Name = ReadString(item, "name") ?? "Unnamed account",
                Currency = ReadString(item, "currency") ?? "NZD",
                AccountNumber = ReadString(item, "formatted_account", "number", "account_number"),
                CurrentBalance = ReadNestedDecimal(item, ["balance", "current"]) ??
                                 ReadNestedDecimal(item, ["balance", "available"]) ??
                                 ReadNestedDecimal(item, ["balance", "value"]),
                InstitutionName = ReadNestedString(item, ["connection", "name"]) ??
                                  ReadNestedString(item, ["connection", "bank_name"]) ??
                                  ReadNestedString(item, ["institution", "name"])
            });
        }

        return accounts;
    }

    public async Task<IReadOnlyList<AkahuTransaction>> GetTransactionsAsync(
        string accountId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var transactions = new List<AkahuTransaction>();
        string? cursor = null;

        do
        {
            var query =
                $"accounts/{Uri.EscapeDataString(accountId)}/transactions?start={fromUtc:yyyy-MM-dd}&end={toUtc:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                query += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            using var document = await SendRequestAsync(query, cancellationToken);
            var items = ExtractItems(document.RootElement, "transactions");
            cursor = ReadString(document.RootElement, "next_cursor")
                     ?? ReadNestedString(document.RootElement, ["cursor", "next"])
                     ?? ReadString(document.RootElement, "cursor");

            foreach (var item in items)
            {
                var transactionId = ReadString(item, "_id", "id");
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    continue;
                }

                var dateValue = ReadString(item, "date", "created_at", "updated_at");
                if (!DateTime.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                        out var dateUtc))
                {
                    continue;
                }

                var transactionAccountId = ReadString(item, "_account", "account") ??
                                           ReadNestedString(item, ["account", "_id"]) ??
                                           accountId;
                var amount = ReadDecimal(item, "amount");

                transactions.Add(new AkahuTransaction
                {
                    Id = transactionId,
                    AccountId = transactionAccountId,
                    Amount = amount,
                    Description = ReadString(item, "description", "particulars") ?? "No description",
                    MerchantName = ReadNestedString(item, ["merchant", "name"]) ?? ReadString(item, "merchant"),
                    Reference = ReadString(item, "reference"),
                    TransactionType = ReadString(item, "type"),
                    DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc)
                });
            }

            if (string.IsNullOrWhiteSpace(cursor))
            {
                break;
            }
        } while (true);

        return transactions;
    }

    private async Task<JsonDocument> SendRequestAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.AppToken))
        {
            throw new InvalidOperationException("Akahu token is not configured. Set Akahu:AppToken.");
        }
        
        if (string.IsNullOrWhiteSpace(options.UserToken))
        {
            throw new InvalidOperationException("Akahu user token is not configured. Set Akahu:UserToken.");
        }

        var client = httpClientFactory.CreateClient("Akahu");
        client.BaseAddress = BuildAkahuBaseUri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);

        using var request = new HttpRequestMessage(HttpMethod.Get, pathAndQuery.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.UserToken);
        request.Headers.Add("X-Akahu-Id", options.AppToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Akahu request failed. Status: {Status}. Url: {Url}. Body: {Body}",
                response.StatusCode,
                pathAndQuery,
                payload);
            throw new HttpRequestException($"Akahu request failed with status code {(int)response.StatusCode}.");
        }

        return JsonDocument.Parse(payload);
    }

    private static List<JsonElement> ExtractItems(JsonElement root, string fallbackProperty)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return [.. root.EnumerateArray()];
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            return [.. items.EnumerateArray()];
        }

        if (root.TryGetProperty(fallbackProperty, out var fallback) && fallback.ValueKind == JsonValueKind.Array)
        {
            return [.. fallback.EnumerateArray()];
        }

        return [];
    }

    private static string? ReadString(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!element.TryGetProperty(key, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static string? ReadNestedString(JsonElement element, string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
            {
                return null;
            }

            current = next;
        }

        if (current.ValueKind == JsonValueKind.String)
        {
            return current.GetString();
        }

        return current.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
            ? current.ToString()
            : null;
    }

    private static decimal ReadDecimal(JsonElement element, params string[] keys)
    {
        var raw = ReadString(element, keys);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static decimal? ReadNestedDecimal(JsonElement element, string[] path)
    {
        var raw = ReadNestedString(element, path);
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    private static Uri BuildAkahuBaseUri(string configuredBaseUrl)
    {
        var normalized = configuredBaseUrl.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "https://api.akahu.io/v1";
        }

        normalized = normalized.TrimEnd('/');
        if (!normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"{normalized}/v1";
        }

        normalized = $"{normalized}/";
        return new Uri(normalized, UriKind.Absolute);
    }
}
