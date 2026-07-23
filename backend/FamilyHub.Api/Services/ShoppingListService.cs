using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Shopping;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class ShoppingListService : IShoppingListService
{
    private const string ShoppingUrl = "/shopping";

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ShoppingListService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<ShoppingListResponse>> CreateListAsync(
        string userId, Guid familyId, CreateShoppingListRequest request)
    {
        var access = await AuthorizeFamilyAsync(userId, familyId);
        if (access is not null)
        {
            return Result<ShoppingListResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = request.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ShoppingLists.Add(list);
        await _db.SaveChangesAsync();

        return Result<ShoppingListResponse>.Success(ToListResponse(list));
    }

    public async Task<Result<IReadOnlyList<ShoppingListResponse>>> GetListsAsync(string userId, Guid familyId)
    {
        var access = await AuthorizeFamilyAsync(userId, familyId);
        if (access is not null)
        {
            return Result<IReadOnlyList<ShoppingListResponse>>.Failure(access.Value.Error, access.Value.Message);
        }

        var lists = await _db.ShoppingLists
            .Include(l => l.Items)
            .Where(l => l.FamilyId == familyId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        var response = lists.Select(ToListResponse).ToList();
        return Result<IReadOnlyList<ShoppingListResponse>>.Success(response);
    }

    public async Task<Result<ShoppingItemResponse>> AddItemAsync(
        string userId, Guid listId, CreateShoppingItemRequest request)
    {
        var access = await AuthorizeListAsync(userId, listId);
        if (access.Error is not null)
        {
            return Result<ShoppingItemResponse>.Failure(access.Error.Value, access.Message!);
        }

        var item = new ShoppingItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = listId,
            Name = request.Name.Trim(),
            Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim(),
            Category = request.Category,
            IsPurchased = false,
            IsImportant = request.IsImportant,
            AddedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ShoppingItems.Add(item);
        await _db.SaveChangesAsync();

        // Shopping rule: only important items raise a notification, to the whole family.
        if (item.IsImportant)
        {
            await NotifyImportantItemAsync(userId, listId, "Important shopping item",
                $"\"{item.Name}\" was added as an important shopping item.");
        }

        return Result<ShoppingItemResponse>.Success(ToItemResponse(item));
    }

    public async Task<Result<ShoppingItemResponse>> UpdateItemAsync(
        string userId, Guid listId, Guid itemId, UpdateShoppingItemRequest request)
    {
        var access = await AuthorizeListAsync(userId, listId);
        if (access.Error is not null)
        {
            return Result<ShoppingItemResponse>.Failure(access.Error.Value, access.Message!);
        }

        var item = await GetItemAsync(listId, itemId);
        if (item is null)
        {
            return Result<ShoppingItemResponse>.Failure(ErrorType.NotFound, "Item not found in this list.");
        }

        var becameImportant = request.IsImportant && !item.IsImportant;

        item.Name = request.Name.Trim();
        item.Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim();
        item.Category = request.Category;
        item.IsImportant = request.IsImportant;

        await _db.SaveChangesAsync();

        // Notify only on the transition into "important", not on every edit of an important item.
        if (becameImportant)
        {
            await NotifyImportantItemAsync(userId, listId, "Important shopping item",
                $"\"{item.Name}\" was flagged as an important shopping item.");
        }

        return Result<ShoppingItemResponse>.Success(ToItemResponse(item));
    }

    public async Task<Result<ShoppingItemResponse>> ToggleItemAsync(string userId, Guid listId, Guid itemId)
    {
        var access = await AuthorizeListAsync(userId, listId);
        if (access.Error is not null)
        {
            return Result<ShoppingItemResponse>.Failure(access.Error.Value, access.Message!);
        }

        var item = await GetItemAsync(listId, itemId);
        if (item is null)
        {
            return Result<ShoppingItemResponse>.Failure(ErrorType.NotFound, "Item not found in this list.");
        }

        if (item.IsPurchased)
        {
            item.IsPurchased = false;
            item.PurchasedByUserId = null;
            item.PurchasedAt = null;
        }
        else
        {
            item.IsPurchased = true;
            item.PurchasedByUserId = userId;
            item.PurchasedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Result<ShoppingItemResponse>.Success(ToItemResponse(item));
    }

    public async Task<Result> DeleteItemAsync(string userId, Guid listId, Guid itemId)
    {
        var access = await AuthorizeListAsync(userId, listId);
        if (access.Error is not null)
        {
            return Result.Failure(access.Error.Value, access.Message!);
        }

        var item = await GetItemAsync(listId, itemId);
        if (item is null)
        {
            return Result.Failure(ErrorType.NotFound, "Item not found in this list.");
        }

        _db.ShoppingItems.Remove(item);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ClearPurchasedItemsAsync(string userId, Guid listId)
    {
        var access = await AuthorizeListAsync(userId, listId);
        if (access.Error is not null)
        {
            return Result.Failure(access.Error.Value, access.Message!);
        }

        await _db.ShoppingItems
            .Where(i => i.ShoppingListId == listId && i.IsPurchased)
            .ExecuteDeleteAsync();

        return Result.Success();
    }

    /// <summary>Family-scoped access check (create/list operations).</summary>
    private async Task<(ErrorType Error, string Message)?> AuthorizeFamilyAsync(string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (ErrorType.NotFound, "Family not found.");
        }

        var isMember = await _db.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (!isMember)
        {
            return (ErrorType.Forbidden, "You do not have access to this family.");
        }

        return null;
    }

    /// <summary>List-scoped access check — resolves the owning family and verifies membership.</summary>
    private async Task<(ErrorType? Error, string? Message)> AuthorizeListAsync(string userId, Guid listId)
    {
        var list = await _db.ShoppingLists.FirstOrDefaultAsync(l => l.Id == listId);
        if (list is null)
        {
            return (ErrorType.NotFound, "Shopping list not found.");
        }

        var isMember = await _db.FamilyMembers.AnyAsync(m => m.FamilyId == list.FamilyId && m.UserId == userId);
        if (!isMember)
        {
            return (ErrorType.Forbidden, "You do not have access to this shopping list.");
        }

        return (null, null);
    }

    private Task<ShoppingItem?> GetItemAsync(Guid listId, Guid itemId) =>
        _db.ShoppingItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ShoppingListId == listId);

    /// <summary>Notifies the whole owning family (except the actor) about an important item.</summary>
    private async Task NotifyImportantItemAsync(string actorUserId, Guid listId, string title, string message)
    {
        var familyId = await _db.ShoppingLists
            .Where(l => l.Id == listId)
            .Select(l => l.FamilyId)
            .FirstOrDefaultAsync();

        if (familyId == Guid.Empty)
        {
            return;
        }

        await _notifications.CreateForFamilyAsync(
            familyId, NotificationType.Shopping, title, message, ShoppingUrl, actorUserId);
    }

    private static ShoppingListResponse ToListResponse(ShoppingList list) =>
        new(list.Id, list.FamilyId, list.Name, list.CreatedAt,
            list.Items
                .OrderBy(i => i.IsPurchased)
                .ThenBy(i => i.CreatedAt)
                .Select(ToItemResponse)
                .ToList());

    private static ShoppingItemResponse ToItemResponse(ShoppingItem i) =>
        new(i.Id, i.ShoppingListId, i.Name, i.Quantity, i.Category, i.IsPurchased, i.IsImportant,
            i.AddedByUserId, i.PurchasedByUserId, i.CreatedAt, i.PurchasedAt);
}
