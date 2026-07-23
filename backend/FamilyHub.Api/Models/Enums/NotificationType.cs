namespace FamilyHub.Api.Models.Enums;

/// <summary>
/// The family activity area a notification belongs to. Used for grouping,
/// filtering and choosing an icon in the client. The concrete event is
/// described by the notification's Title/Message.
/// </summary>
public enum NotificationType
{
    Family,
    Task,
    Calendar,
    Shopping,
    FamilyVault,
    System
}
