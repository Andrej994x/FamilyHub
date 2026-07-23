using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Notifications;

public record NotificationResponse(
    Guid Id,
    Guid? FamilyId,
    NotificationType Type,
    string Title,
    string Message,
    string? RelatedUrl,
    bool IsRead,
    DateTimeOffset CreatedAt);
