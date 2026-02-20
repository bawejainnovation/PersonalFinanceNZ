using FinancialInsights.Api.DTOs;
using FinancialInsights.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/banks")]
public sealed class BanksController(INzBankCatalogService bankCatalogService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<NzBankResponse>> GetBanks() => Ok(bankCatalogService.GetBanks());
}
