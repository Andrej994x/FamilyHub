namespace FamilyHub.Api.Models.Enums;

// Note: intentionally named TaskStatus per the domain spec. It shadows
// System.Threading.Tasks.TaskStatus, so consumers alias it explicitly.
public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}
