using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(string userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse(
                n.Id, n.Title, n.Message, n.Type, n.RelatedEntityId, n.IsRead, n.CreatedAt))
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<Result> MarkAsReadAsync(string userId, Guid notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is null)
        {
            return Result.Failure(ErrorType.NotFound, "Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return Result.Success();
    }

    public Task MarkAllAsReadAsync(string userId) =>
        _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task CreateAsync(
        string userId, string title, string message, NotificationType type, Guid? relatedEntityId = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
