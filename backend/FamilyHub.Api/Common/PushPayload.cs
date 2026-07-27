namespace FamilyHub.Api.Common;

/// <summary>
/// The JSON payload delivered to a device. The service worker reads these fields to render the
/// notification and to navigate to <see cref="Url"/> when the user taps it.
/// </summary>
public record PushPayload(
    string Title,
    string Body,
    string? Url,
    string? Tag);
