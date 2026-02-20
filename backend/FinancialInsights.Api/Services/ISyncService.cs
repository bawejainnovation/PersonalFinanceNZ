using FinancialInsights.Api.DTOs;

namespace FinancialInsights.Api.Services;

public interface ISyncService
{
    Task<SyncResponse> SyncAsync(SyncRequest request, CancellationToken cancellationToken);
}
