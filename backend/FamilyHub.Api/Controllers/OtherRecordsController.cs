using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/vault/other")]
public class OtherRecordsController : ApiControllerBase
{
    private readonly IOtherRecordService _service;
    private readonly IValidator<CreateOtherRecordRequest> _createValidator;
    private readonly IValidator<UpdateOtherRecordRequest> _updateValidator;

    public OtherRecordsController(
        IOtherRecordService service,
        IValidator<CreateOtherRecordRequest> createValidator,
        IValidator<UpdateOtherRecordRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OtherRecordResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid familyId, [FromForm] CreateOtherRecordRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        var result = await _service.CreateAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OtherRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid familyId) =>
        HandleResult(await _service.GetAllAsync(CurrentUserId, familyId));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OtherRecordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid familyId, Guid id) =>
        HandleResult(await _service.GetAsync(CurrentUserId, familyId, id));

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OtherRecordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid familyId, Guid id, [FromForm] UpdateOtherRecordRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        return HandleResult(await _service.UpdateAsync(CurrentUserId, familyId, id, request));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid familyId, Guid id)
    {
        var result = await _service.DeleteAsync(CurrentUserId, familyId, id);
        return result.Succeeded ? NoContent() : Error(result);
    }
}
