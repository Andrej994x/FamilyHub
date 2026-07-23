using FamilyHub.Api.DTOs.Documents;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/documents")]
public class FamilyDocumentsController : ApiControllerBase
{
    private readonly IFamilyDocumentService _documentService;
    private readonly IValidator<CreateFamilyDocumentRequest> _createValidator;
    private readonly IValidator<UpdateFamilyDocumentRequest> _updateValidator;

    public FamilyDocumentsController(
        IFamilyDocumentService documentService,
        IValidator<CreateFamilyDocumentRequest> createValidator,
        IValidator<UpdateFamilyDocumentRequest> updateValidator)
    {
        _documentService = documentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FamilyDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid familyId, [FromForm] CreateFamilyDocumentRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _documentService.CreateDocumentAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, documentId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId, [FromQuery] DocumentFilter filter) =>
        HandleResult(await _documentService.GetDocumentsAsync(CurrentUserId, familyId, filter));

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(FamilyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid documentId) =>
        HandleResult(await _documentService.GetDocumentAsync(CurrentUserId, familyId, documentId));

    [HttpPut("{documentId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FamilyDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid familyId, Guid documentId, [FromForm] UpdateFamilyDocumentRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _documentService.UpdateDocumentAsync(CurrentUserId, familyId, documentId, request));
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid documentId)
    {
        var result = await _documentService.DeleteDocumentAsync(CurrentUserId, familyId, documentId);
        return result.Succeeded ? NoContent() : Error(result);
    }

    [HttpGet("{documentId:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(Guid familyId, Guid documentId)
    {
        var result = await _documentService.GetAttachmentAsync(CurrentUserId, familyId, documentId);
        if (!result.Succeeded)
        {
            return Error(result);
        }

        var file = result.Value!;
        return PhysicalFile(file.AbsolutePath, file.ContentType, file.FileName);
    }

    private ModelStateDictionary ToModelState(FluentValidation.Results.ValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ModelState;
    }
}
