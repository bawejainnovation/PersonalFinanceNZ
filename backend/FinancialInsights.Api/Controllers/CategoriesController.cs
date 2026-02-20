using FinancialInsights.Api.Data;
using FinancialInsights.Api.Domain.Entities;
using FinancialInsights.Api.Domain.Enums;
using FinancialInsights.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAsync(
        [FromQuery] CategoryType? categoryType,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Categories.AsNoTracking();
        if (categoryType.HasValue)
        {
            query = query.Where(category => category.CategoryType == categoryType.Value);
        }

        var categories = await query
            .OrderBy(category => category.CategoryType)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                CategoryType = category.CategoryType,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Category name is required." });
        }

        var exists = await dbContext.Categories
            .AnyAsync(category => category.CategoryType == request.CategoryType && category.Name == name, cancellationToken);

        if (exists)
        {
            return Conflict(new { error = "Category already exists." });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            CategoryType = request.CategoryType,
            Name = name,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/categories/{category.Id}", new CategoryResponse
        {
            Id = category.Id,
            CategoryType = category.CategoryType,
            Name = category.Name
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Category name is required." });
        }

        var exists = await dbContext.Categories
            .AnyAsync(entity => entity.Id != id && entity.CategoryType == category.CategoryType && entity.Name == name,
                cancellationToken);

        if (exists)
        {
            return Conflict(new { error = "Category already exists." });
        }

        category.Name = name;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CategoryResponse
        {
            Id = category.Id,
            CategoryType = category.CategoryType,
            Name = category.Name
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
