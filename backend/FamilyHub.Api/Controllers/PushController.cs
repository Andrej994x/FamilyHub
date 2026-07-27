using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Push;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push")]
public class PushController : ApiControllerBase
{
    private readonly IPushSubscriptionService _subscriptions;
    private readonly PushSettings _pushSettings;
    private readonly IValidator<CreatePushSubscriptionRequest> _createValidator;
    private readonly IValidator<UpdatePushSubscriptionRequest> _updateValidator;

    public PushController(
        IPushSubscriptionService subscriptions,
        IOptions<PushSettings> pushSettings,
        IValidator<CreatePushSubscriptionRequest> createValidator,
        IValidator<UpdatePushSubscriptionRequest> updateValidator)
    {
        _subscriptions = subscriptions;
        _pushSettings = pushSettings.Value;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>The VAPID public key the browser needs to create a push subscription.</summary>
    [HttpGet("vapid-public-key")]
    [ProducesResponseType(typeof(VapidPublicKeyResponse), StatusCodes.Status200OK)]
    public IActionResult GetVapidPublicKey() =>
        Ok(new VapidPublicKeyResponse(_pushSettings.PublicKey));

    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(IReadOnlyList<PushSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions() =>
        Ok(await _subscriptions.GetForUserAsync(CurrentUserId));

    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(PushSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreatePushSubscriptionRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ToValidationProblem(validation);
        }

        var result = await _subscriptions.RegisterAsync(CurrentUserId, request);
        return HandleResult(result);
    }

    [HttpPut("subscriptions/{id:guid}")]
    [ProducesResponseType(typeof(PushSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePushSubscriptionRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ToValidationProblem(validation);
        }

        var result = await _subscriptions.UpdateAsync(CurrentUserId, id, request);
        return HandleResult(result);
    }

    [HttpDelete("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid id)
    {
        var result = await _subscriptions.RemoveAsync(CurrentUserId, id);
        return result.Succeeded ? NoContent() : Error(result);
    }
}
