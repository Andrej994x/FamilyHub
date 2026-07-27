using System.Security.Cryptography;
using System.Text;
using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Push;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class PushSubscriptionService : IPushSubscriptionService
{
    private readonly AppDbContext _db;

    public PushSubscriptionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PushSubscriptionResponse>> GetForUserAsync(string userId)
    {
        return await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new PushSubscriptionResponse(
                s.Id, s.Endpoint, s.UserAgent, s.IsActive, s.CreatedAt, s.UpdatedAt, s.LastUsedAt))
            .ToListAsync();
    }

    public async Task<Result<PushSubscriptionResponse>> RegisterAsync(
        string userId, CreatePushSubscriptionRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var endpoint = request.Endpoint.Trim();
        var endpointHash = HashEndpoint(endpoint);

        // Upsert on the endpoint: the same device re-registering (or a different user signing in
        // on the same browser) reuses the row and takes ownership.
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.EndpointHash == endpointHash);
        if (existing is not null)
        {
            existing.UserId = userId;
            existing.Endpoint = endpoint;
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserAgent = Clean(request.UserAgent);
            existing.IsActive = true;
            existing.UpdatedAt = now;

            await _db.SaveChangesAsync();
            return Result<PushSubscriptionResponse>.Success(ToResponse(existing));
        }

        var subscription = new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            EndpointHash = endpointHash,
            P256dh = request.P256dh,
            Auth = request.Auth,
            UserAgent = Clean(request.UserAgent),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.PushSubscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return Result<PushSubscriptionResponse>.Success(ToResponse(subscription));
    }

    public async Task<Result<PushSubscriptionResponse>> UpdateAsync(
        string userId, Guid id, UpdatePushSubscriptionRequest request)
    {
        var subscription = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (subscription is null)
        {
            return Result<PushSubscriptionResponse>.Failure(ErrorType.NotFound, "Subscription not found.");
        }

        subscription.P256dh = request.P256dh;
        subscription.Auth = request.Auth;
        subscription.UserAgent = Clean(request.UserAgent);
        // A client that can refresh its keys is alive again.
        subscription.IsActive = true;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return Result<PushSubscriptionResponse>.Success(ToResponse(subscription));
    }

    public async Task<Result> RemoveAsync(string userId, Guid id)
    {
        var deleted = await _db.PushSubscriptions
            .Where(s => s.Id == id && s.UserId == userId)
            .ExecuteDeleteAsync();

        return deleted == 0
            ? Result.Failure(ErrorType.NotFound, "Subscription not found.")
            : Result.Success();
    }

    private static string HashEndpoint(string endpoint) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint))).ToLowerInvariant();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PushSubscriptionResponse ToResponse(PushSubscription s) =>
        new(s.Id, s.Endpoint, s.UserAgent, s.IsActive, s.CreatedAt, s.UpdatedAt, s.LastUsedAt);
}
