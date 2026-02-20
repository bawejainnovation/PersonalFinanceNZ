using FinancialInsights.Api.DTOs;

namespace FinancialInsights.Api.Services;

public interface INzBankCatalogService
{
    IReadOnlyList<NzBankResponse> GetBanks();

    bool Exists(string? key);
}
