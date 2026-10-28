using System.Globalization;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Validators;

namespace FamilyHub.Api.UnitTests.Validators;

public class ExpenseRequestValidatorTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static decimal Money(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static CreateExpenseRequest Expense(decimal amount = 1500m, DateOnly? date = null, string paidBy = "ana",
        string? description = null, Guid? categoryId = null) =>
        new(amount, categoryId ?? Guid.NewGuid(), date ?? Today, paidBy, description);

    [Theory]
    [InlineData("0.01")]
    [InlineData("1500")]
    [InlineData("1234.56")]
    [InlineData("10000000")]
    public void Valid_amounts_are_accepted(string amount)
    {
        var result = new CreateExpenseRequestValidator().Validate(Expense(Money(amount)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0", "Amount must be greater than zero.")]
    [InlineData("-5", "Amount must be greater than zero.")]
    [InlineData("10000000.01", "Amount is too large.")]
    [InlineData("12.345", "Amount can have at most two decimals.")]
    public void Invalid_amounts_are_rejected(string amount, string message)
    {
        var result = new CreateExpenseRequestValidator().Validate(Expense(Money(amount)));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseRequest.Amount) && e.ErrorMessage == message);
    }

    [Fact]
    public void Expenses_cannot_be_dated_in_the_future()
    {
        var validator = new CreateExpenseRequestValidator();

        // Today (UTC) is always allowed; two days ahead is in the future in every time zone.
        Assert.True(validator.Validate(Expense(date: Today)).IsValid);
        var future = validator.Validate(Expense(date: Today.AddDays(2)));
        Assert.Contains(future.Errors, e =>
            e.PropertyName == nameof(CreateExpenseRequest.Date) && e.ErrorMessage == "Date cannot be in the future.");
    }

    [Fact]
    public void Dates_before_2000_are_rejected()
    {
        var validator = new CreateExpenseRequestValidator();

        Assert.True(validator.Validate(Expense(date: new DateOnly(2000, 1, 1))).IsValid);
        Assert.False(validator.Validate(Expense(date: new DateOnly(1999, 12, 31))).IsValid);
    }

    [Fact]
    public void A_category_and_payer_are_required()
    {
        var result = new CreateExpenseRequestValidator().Validate(Expense(paidBy: "", categoryId: Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseRequest.CategoryId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseRequest.PaidByUserId));
    }

    [Fact]
    public void Description_is_limited_to_500_characters()
    {
        var validator = new CreateExpenseRequestValidator();

        Assert.True(validator.Validate(Expense(description: new string('a', 500))).IsValid);
        Assert.False(validator.Validate(Expense(description: new string('a', 501))).IsValid);
    }

    [Fact]
    public void Updates_follow_the_same_rules()
    {
        var validator = new UpdateExpenseRequestValidator();

        Assert.True(validator.Validate(new UpdateExpenseRequest(10m, Guid.NewGuid(), Today, "ana", null)).IsValid);
        Assert.False(validator.Validate(new UpdateExpenseRequest(0m, Guid.NewGuid(), Today, "ana", null)).IsValid);
        Assert.False(validator.Validate(new UpdateExpenseRequest(10m, Guid.NewGuid(), Today.AddDays(2), "ana", null)).IsValid);
    }

    [Fact]
    public void The_history_filter_requires_a_valid_year_and_month()
    {
        var validator = new ExpenseFilterValidator();

        Assert.True(validator.Validate(new ExpenseFilter { Year = 2026, Month = 10 }).IsValid);
        Assert.False(validator.Validate(new ExpenseFilter()).IsValid);
        Assert.False(validator.Validate(new ExpenseFilter { Year = 2026 }).IsValid);
        Assert.False(validator.Validate(new ExpenseFilter { Year = 2026, Month = 13 }).IsValid);
        Assert.False(validator.Validate(new ExpenseFilter { Year = 1999, Month = 1 }).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Category_names_are_required(string? name)
    {
        var create = new CreateExpenseCategoryRequestValidator().Validate(new CreateExpenseCategoryRequest(name!));
        var update = new UpdateExpenseCategoryRequestValidator().Validate(new UpdateExpenseCategoryRequest(name!, false));

        Assert.Contains(create.Errors, e => e.ErrorMessage == "Name is required.");
        Assert.Contains(update.Errors, e => e.ErrorMessage == "Name is required.");
    }

    [Fact]
    public void Category_names_are_limited_to_50_characters_after_trimming()
    {
        var validator = new CreateExpenseCategoryRequestValidator();

        Assert.True(validator.Validate(new CreateExpenseCategoryRequest("  " + new string('a', 50) + "  ")).IsValid);
        Assert.False(validator.Validate(new CreateExpenseCategoryRequest(new string('a', 51))).IsValid);
    }
}
