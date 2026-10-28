using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Validators;

namespace FamilyHub.Api.UnitTests.Validators;

public class ExpenseBudgetRequestValidatorTests
{
    private static SetExpenseBudgetsRequest Budgets(decimal? overall = 50_000m, params CategoryBudgetRequest[] categories) =>
        new(2026, 10, overall, categories);

    [Fact]
    public void A_budget_set_with_an_overall_amount_and_category_amounts_is_valid()
    {
        var result = new SetExpenseBudgetsRequestValidator().Validate(
            Budgets(50_000m, new CategoryBudgetRequest(Guid.NewGuid(), 15_000m), new CategoryBudgetRequest(Guid.NewGuid(), null)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Budgets_are_optional()
    {
        var validator = new SetExpenseBudgetsRequestValidator();

        Assert.True(validator.Validate(Budgets(overall: null)).IsValid);
        Assert.True(validator.Validate(new SetExpenseBudgetsRequest(2026, 10, null, null)).IsValid);
    }

    [Theory]
    [InlineData(0, "Budget must be greater than zero.")]
    [InlineData(-1, "Budget must be greater than zero.")]
    [InlineData(10_000_001, "Budget is too large.")]
    [InlineData(100.005, "Budget can have at most two decimals.")]
    public void Invalid_amounts_are_rejected_for_the_overall_and_category_budgets(double amount, string message)
    {
        var validator = new SetExpenseBudgetsRequestValidator();
        var value = (decimal)amount;

        var overall = validator.Validate(Budgets(value));
        var category = validator.Validate(Budgets(null, new CategoryBudgetRequest(Guid.NewGuid(), value)));

        Assert.Contains(overall.Errors, e => e.PropertyName == "OverallAmount" && e.ErrorMessage == message);
        Assert.Contains(category.Errors, e => e.PropertyName == "Categories[0].Amount" && e.ErrorMessage == message);
    }

    [Fact]
    public void A_category_can_appear_only_once_and_must_be_given()
    {
        var id = Guid.NewGuid();
        var validator = new SetExpenseBudgetsRequestValidator();

        var duplicate = validator.Validate(Budgets(null, new CategoryBudgetRequest(id, 1m), new CategoryBudgetRequest(id, 2m)));
        var empty = validator.Validate(Budgets(null, new CategoryBudgetRequest(Guid.Empty, 1m)));
        var nullEntry = validator.Validate(new SetExpenseBudgetsRequest(2026, 10, null, [null!]));

        Assert.Contains(duplicate.Errors, e => e.ErrorMessage == "Each category can appear only once.");
        Assert.Contains(empty.Errors, e => e.ErrorMessage == "A category is required.");
        Assert.Contains(nullEntry.Errors, e => e.ErrorMessage == "Category budgets cannot be null.");
    }

    [Fact]
    public void At_most_100_category_budgets_are_accepted()
    {
        var validator = new SetExpenseBudgetsRequestValidator();
        CategoryBudgetRequest[] Many(int n) => Enumerable.Range(0, n).Select(_ => new CategoryBudgetRequest(Guid.NewGuid(), 1m)).ToArray();

        Assert.True(validator.Validate(Budgets(null, Many(100))).IsValid);
        Assert.False(validator.Validate(Budgets(null, Many(101))).IsValid);
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2101, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public void The_month_must_be_valid(int year, int month)
    {
        var result = new SetExpenseBudgetsRequestValidator().Validate(new SetExpenseBudgetsRequest(year, month, 1m, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Budget_and_summary_queries_require_a_year_and_month()
    {
        var validator = new ExpenseMonthQueryValidator();

        Assert.True(validator.Validate(new ExpenseMonthQuery { Year = 2026, Month = 10 }).IsValid);
        Assert.Contains(validator.Validate(new ExpenseMonthQuery { Year = 2026 }).Errors, e => e.ErrorMessage == "Month is required.");
        Assert.Contains(validator.Validate(new ExpenseMonthQuery { Month = 10 }).Errors, e => e.ErrorMessage == "Year is required.");
        Assert.False(validator.Validate(new ExpenseMonthQuery { Year = 2026, Month = 13 }).IsValid);
    }
}
