using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Notifications;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTimeOffset CreatedAt);
