using System.Net;
using System.Security.Cryptography;
using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyHub.Api.Services;

public class InvitationService : IInvitationService
{
    private readonly AppDbContext _db;
    private readonly InvitationSettings _settings;
    private readonly ILogger<InvitationService> _logger;
    private readonly INotificationService _notifications;
    private readonly IEmailSender _emailSender;

    public InvitationService(
        AppDbContext db,
        IOptions<InvitationSettings> settings,
        ILogger<InvitationService> logger,
        INotificationService notifications,
        IEmailSender emailSender)
    {
        _db = db;
        _settings = settings.Value;
        _logger = logger;
        _notifications = notifications;
        _emailSender = emailSender;
    }

    public async Task<Result<CreatedInvitationResponse>> CreateInvitationAsync(
        string userId, Guid familyId, CreateInvitationRequest request)
    {
        var familyName = await GetFamilyNameAsync(familyId);
        if (familyName is null)
        {
            return Result<CreatedInvitationResponse>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        // Only Owner and Parent can invite members.
        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can invite members.");
        }

        var email = request.Email.Trim();

        // Email must be unique among this family's pending invitations.
        var duplicatePending = await _db.FamilyInvitations.AnyAsync(i =>
            i.FamilyId == familyId &&
            i.Status == InvitationStatus.Pending &&
            i.Email.ToLower() == email.ToLower());
        if (duplicatePending)
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Conflict, "A pending invitation already exists for this email.");
        }

        var now = DateTimeOffset.UtcNow;
        var invitation = new FamilyInvitation
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Email = email,
            Role = request.Role,
            Token = GenerateSecureToken(),
            Status = InvitationStatus.Pending,
            ExpiresAt = now.AddDays(_settings.ExpiryDays),
            CreatedAt = now,
            LastSentAt = now,
        };

        _db.FamilyInvitations.Add(invitation);
        await _db.SaveChangesAsync();

        var acceptUrl = BuildAcceptUrl(invitation.Token);
        await SendInvitationEmailAsync(invitation, familyName, acceptUrl);

        return Result<CreatedInvitationResponse>.Success(
            new CreatedInvitationResponse(ToResponse(invitation), invitation.Token, acceptUrl));
    }

    public async Task<Result<CreatedInvitationResponse>> ResendInvitationAsync(
        string userId, Guid familyId, Guid invitationId)
    {
        var familyName = await GetFamilyNameAsync(familyId);
        if (familyName is null)
        {
            return Result<CreatedInvitationResponse>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can resend invitations.");
        }

        var invitation = await _db.FamilyInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.FamilyId == familyId);
        if (invitation is null)
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.NotFound, "Invitation not found in this family.");
        }

        // Only an open invitation can be resent; an expired one is revived with a fresh window.
        if (invitation.Status is not (InvitationStatus.Pending or InvitationStatus.Expired))
        {
            return Result<CreatedInvitationResponse>.Failure(
                ErrorType.Validation, "Only pending or expired invitations can be resent.");
        }

        var now = DateTimeOffset.UtcNow;
        invitation.Status = InvitationStatus.Pending;
        invitation.ExpiresAt = now.AddDays(_settings.ExpiryDays);
        invitation.LastSentAt = now;
        await _db.SaveChangesAsync();

        var acceptUrl = BuildAcceptUrl(invitation.Token);
        await SendInvitationEmailAsync(invitation, familyName, acceptUrl);

        return Result<CreatedInvitationResponse>.Success(
            new CreatedInvitationResponse(ToResponse(invitation), invitation.Token, acceptUrl));
    }

    public async Task<Result<IReadOnlyList<InvitationResponse>>> GetInvitationsAsync(string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return Result<IReadOnlyList<InvitationResponse>>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<IReadOnlyList<InvitationResponse>>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<IReadOnlyList<InvitationResponse>>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can view invitations.");
        }

        var invitations = await _db.FamilyInvitations
            .Where(i => i.FamilyId == familyId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationResponse(
                i.Id, i.FamilyId, i.Email, i.Role, i.Status, i.ExpiresAt, i.CreatedAt, i.LastSentAt))
            .ToListAsync();

        return Result<IReadOnlyList<InvitationResponse>>.Success(invitations);
    }

    public async Task<Result<InvitationResponse>> AcceptInvitationAsync(string userId, string token)
    {
        var invitation = await _db.FamilyInvitations.FirstOrDefaultAsync(i => i.Token == token);
        if (invitation is null)
        {
            return Result<InvitationResponse>.Failure(ErrorType.NotFound, "Invitation not found.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result<InvitationResponse>.Failure(
                ErrorType.Validation, "This invitation is no longer valid.");
        }

        // Expire on access if the window has passed.
        if (invitation.ExpiresAt < DateTimeOffset.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _db.SaveChangesAsync();
            return Result<InvitationResponse>.Failure(ErrorType.Validation, "This invitation has expired.");
        }

        // MVP rule: a user cannot join multiple families.
        var alreadyInFamily = await _db.FamilyMembers.AnyAsync(m => m.UserId == userId);
        if (alreadyInFamily)
        {
            return Result<InvitationResponse>.Failure(
                ErrorType.Conflict, "You already belong to a family.");
        }

        var now = DateTimeOffset.UtcNow;
        _db.FamilyMembers.Add(new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = invitation.FamilyId,
            UserId = userId,
            Role = invitation.Role,
            JoinedAt = now
        });

        invitation.Status = InvitationStatus.Accepted;
        await _db.SaveChangesAsync();

        // Notify the family owner that the invitation was accepted.
        var ownerUserId = await _db.Families
            .Where(f => f.Id == invitation.FamilyId)
            .Select(f => f.CreatedByUserId)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(ownerUserId) && ownerUserId != userId)
        {
            await _notifications.CreateAsync(
                ownerUserId,
                "Invitation accepted",
                $"{invitation.Email} has joined your family.",
                NotificationType.InvitationAccepted,
                invitation.FamilyId);
        }

        return Result<InvitationResponse>.Success(ToResponse(invitation));
    }

    public async Task<Result> CancelInvitationAsync(string userId, Guid familyId, Guid invitationId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return Result.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result.Failure(ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result.Failure(ErrorType.Forbidden, "Only an Owner or Parent can cancel invitations.");
        }

        var invitation = await _db.FamilyInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.FamilyId == familyId);
        if (invitation is null)
        {
            return Result.Failure(ErrorType.NotFound, "Invitation not found in this family.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Failure(ErrorType.Validation, "Only pending invitations can be cancelled.");
        }

        invitation.Status = InvitationStatus.Cancelled;
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    private Task<string?> GetFamilyNameAsync(Guid familyId) =>
        _db.Families.Where(f => f.Id == familyId).Select(f => f.Name).FirstOrDefaultAsync();

    private Task<FamilyMember?> GetMembershipAsync(string userId, Guid familyId) =>
        _db.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);

    private string BuildAcceptUrl(string token) =>
        $"{_settings.AcceptUrlBase.TrimEnd('/')}?token={token}";

    /// <summary>Sends the invitation email. A delivery failure is logged but never fails the request.</summary>
    private async Task SendInvitationEmailAsync(FamilyInvitation invitation, string familyName, string acceptUrl)
    {
        var safeFamily = WebUtility.HtmlEncode(familyName);
        var safeRole = WebUtility.HtmlEncode(invitation.Role.ToString());
        var expiry = invitation.ExpiresAt.ToString("yyyy-MM-dd");

        var subject = $"You're invited to join {familyName} on FamilyHub";
        var htmlBody =
            $"<p>Hello,</p>" +
            $"<p>You've been invited to join <strong>{safeFamily}</strong> on FamilyHub as a <strong>{safeRole}</strong>.</p>" +
            $"<p><a href=\"{acceptUrl}\">Accept your invitation</a></p>" +
            $"<p>Or paste this link into your browser:<br>{acceptUrl}</p>" +
            $"<p>This invitation expires on {expiry}.</p>";
        var textBody =
            $"You've been invited to join {familyName} on FamilyHub as a {invitation.Role}.\n" +
            $"Accept your invitation: {acceptUrl}\n" +
            $"This invitation expires on {expiry}.";

        try
        {
            await _emailSender.SendEmailAsync(invitation.Email, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send invitation email to {Email}.", invitation.Email);
        }
    }

    private static string GenerateSecureToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static InvitationResponse ToResponse(FamilyInvitation i) =>
        new(i.Id, i.FamilyId, i.Email, i.Role, i.Status, i.ExpiresAt, i.CreatedAt, i.LastSentAt);
}
