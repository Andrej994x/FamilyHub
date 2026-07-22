using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Events;

/// <summary>
/// Optional query-string filters for listing events. All filters are applied server-side.
/// </summary>
public class EventFilter
{
    public DateTimeOffset? DateFrom { get; set; }

    public DateTimeOffset? DateTo { get; set; }

    public Guid? MemberId { get; set; }

    public Guid? ChildId { get; set; }

    public EventType? EventType { get; set; }
}
