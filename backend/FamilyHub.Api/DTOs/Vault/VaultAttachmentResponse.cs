namespace FamilyHub.Api.DTOs.Vault;

public record VaultAttachmentResponse(
    Guid Id,
    string? FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);
