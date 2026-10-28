using System.Security.Claims;
using FamilyHub.Api.Common;
using FamilyHub.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.UnitTests.Controllers;

public class ApiControllerBaseTests
{
    /// <summary>Test-only controller exposing the protected helpers.</summary>
    private sealed class ProbeController : ApiControllerBase
    {
        public IActionResult MapError(Result result) => Error(result);

        public IActionResult MapResult<T>(Result<T> result) => HandleResult(result);

        public string UserId => CurrentUserId;
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    public void Error_maps_each_error_type_to_its_status_and_error_body(ErrorType type, int expectedStatus)
    {
        var response = Assert.IsType<ObjectResult>(
            new ProbeController().MapError(Result.Failure(type, "Something went wrong.")));

        Assert.Equal(expectedStatus, response.StatusCode);
        var body = Assert.IsType<ErrorResponse>(response.Value);
        Assert.Equal(expectedStatus, body.Status);
        Assert.Equal("Something went wrong.", body.Message);
    }

    [Fact]
    public void HandleResult_returns_200_with_the_value_on_success()
    {
        var response = Assert.IsType<OkObjectResult>(new ProbeController().MapResult(Result<string>.Success("ok")));

        Assert.Equal("ok", response.Value);
    }

    [Fact]
    public void HandleResult_uses_the_error_mapping_on_failure()
    {
        var response = Assert.IsType<ObjectResult>(
            new ProbeController().MapResult(Result<string>.Failure(ErrorType.Forbidden, "No access.")));

        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
    }

    [Fact]
    public void CurrentUserId_reads_the_name_identifier_claim()
    {
        var controller = new ProbeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") }, "Test")),
                },
            },
        };

        Assert.Equal("user-42", controller.UserId);
    }
}
