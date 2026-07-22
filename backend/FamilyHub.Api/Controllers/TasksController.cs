using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/tasks")]
public class TasksController : ApiControllerBase
{
    private readonly IFamilyTaskService _taskService;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<UpdateTaskStatusRequest> _statusValidator;

    public TasksController(
        IFamilyTaskService taskService,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<UpdateTaskStatusRequest> statusValidator)
    {
        _taskService = taskService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreateTaskRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _taskService.CreateTaskAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, taskId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId, [FromQuery] TaskFilter filter) =>
        HandleResult(await _taskService.GetTasksAsync(CurrentUserId, familyId, filter));

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid taskId) =>
        HandleResult(await _taskService.GetTaskAsync(CurrentUserId, familyId, taskId));

    [HttpPut("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid familyId, Guid taskId, [FromBody] UpdateTaskRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _taskService.UpdateTaskAsync(CurrentUserId, familyId, taskId, request));
    }

    [HttpPatch("{taskId:guid}/status")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid familyId, Guid taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var validation = await _statusValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _taskService.UpdateTaskStatusAsync(CurrentUserId, familyId, taskId, request));
    }

    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid taskId)
    {
        var result = await _taskService.DeleteTaskAsync(CurrentUserId, familyId, taskId);
        return result.Succeeded ? NoContent() : Error(result);
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
