using System.Text.Json;
using FinancialInsights.Api.DTOs;

namespace FinancialInsights.Api.Services;

public sealed class NzBankCatalogService : INzBankCatalogService
{
    private readonly IReadOnlyList<NzBankResponse> _banks;

    public NzBankCatalogService(IWebHostEnvironment environment)
    {
        var filePath = Path.Combine(environment.ContentRootPath, "Resources", "nz-banks.json");
        var json = File.ReadAllText(filePath);
        _banks = JsonSerializer.Deserialize<List<NzBankResponse>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    public IReadOnlyList<NzBankResponse> GetBanks() => _banks;

    public bool Exists(string? key) => !string.IsNullOrWhiteSpace(key)
        && _banks.Any(bank => string.Equals(bank.Key, key, StringComparison.OrdinalIgnoreCase));
}
