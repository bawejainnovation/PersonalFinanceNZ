using FinancialInsights.Api.Domain.Enums;

namespace FinancialInsights.Api.DTOs;

public sealed class CategoryResponse
{
    public Guid Id { get; init; }

    public CategoryType CategoryType { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class CreateCategoryRequest
{
    public CategoryType CategoryType { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
}
