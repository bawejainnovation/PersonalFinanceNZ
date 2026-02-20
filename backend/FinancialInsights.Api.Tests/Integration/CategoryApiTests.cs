using System.Net;
using System.Net.Http.Json;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using FluentAssertions;

namespace FinancialInsights.Api.Tests.Integration;

public sealed class CategoryApiTests(FinancialInsightsWebApplicationFactory factory) : IClassFixture<FinancialInsightsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CategoryCrud_ShouldWork()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest
        {
            CategoryType = CategoryType.TransactionType,
            Name = $"Salary-{Guid.NewGuid():N}"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        created.Should().NotBeNull();

        var list = await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories?categoryType=TransactionType");
        list.Should().NotBeNull();
        list!.Any(category => category.Id == created!.Id).Should().BeTrue();

        var updateResponse = await _client.PutAsJsonAsync($"/api/categories/{created!.Id}", new UpdateCategoryRequest
        {
            Name = $"Salary-Updated-{Guid.NewGuid():N}"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await _client.DeleteAsync($"/api/categories/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
