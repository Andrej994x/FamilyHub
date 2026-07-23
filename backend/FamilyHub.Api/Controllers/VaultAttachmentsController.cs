using FamilyHub.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/vault/attachments")]
public class VaultAttachmentsController : ApiControllerBase
{
    private readonly IVaultAttachmentService _service;

    public VaultAttachmentsController(IVaultAttachmentService service)
    {
        _service = service;
    }

    [HttpGet("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid familyId, Guid attachmentId)
    {
        var result = await _service.GetForDownloadAsync(CurrentUserId, familyId, attachmentId);
        if (!result.Succeeded)
        {
            return Error(result);
        }

        var file = result.Value!;
        return PhysicalFile(file.AbsolutePath, file.ContentType, file.FileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid attachmentId)
    {
        var result = await _service.DeleteAsync(CurrentUserId, familyId, attachmentId);
        return result.Succeeded ? NoContent() : Error(result);
    }
}
