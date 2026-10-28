namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>Renames a category and/or archives or restores it.</summary>
public record UpdateExpenseCategoryRequest(string Name, bool IsArchived);
