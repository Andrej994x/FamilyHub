using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/vault/home")]
public class HomeRecordsController : ApiControllerBase
{
    private readonly IHomeRecordService _service;
    private readonly IValidator<CreateHomeRecordRequest> _createValidator;
    private readonly IValidator<UpdateHomeRecordRequest> _updateValidator;

    public HomeRecordsController(
        IHomeRecordService service,
        IValidator<CreateHomeRecordRequest> createValidator,
        IValidator<UpdateHomeRecordRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(HomeRecordResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid familyId, [FromForm] CreateHomeRecordRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        var result = await _service.CreateAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, id = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HomeRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid familyId) =>
        HandleResult(await _service.GetAllAsync(CurrentUserId, familyId));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HomeRecordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid familyId, Guid id) =>
        HandleResult(await _service.GetAsync(CurrentUserId, familyId, id));

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(HomeRecordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid familyId, Guid id, [FromForm] UpdateHomeRecordRequest request)
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
