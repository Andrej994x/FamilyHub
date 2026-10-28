using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.DTOs.Shopping;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.IntegrationTests.Authorization;

/// <summary>
/// The family authorization contract: family exists (404) → caller is a member (403) → role (403),
/// with child entities looked up by both Id and FamilyId.
/// </summary>
[Collection(ApiCollection.Name)]
public class FamilyAccessTests(FamilyHubApiFixture fixture)
{
    [Fact]
    public async Task An_unknown_family_returns_404()
    {
        var user = await fixture.RegisterAndLoginAsync();

        var response = await user.Client.GetAsync($"/api/families/{Guid.NewGuid()}/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Family not found.", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/members")]
    [InlineData("/tasks")]
    [InlineData("/children")]
    [InlineData("/events")]
    [InlineData("/shopping-lists")]
    [InlineData("/invitations")]
    public async Task A_member_of_another_family_gets_403_for_every_family_resource(string suffix)
    {
        var victim = await fixture.CreateFamilyWithOwnerAsync();
        var attacker = await fixture.CreateFamilyWithOwnerAsync();

        var response = await attacker.Owner.Client.GetAsync($"/api/families/{victim.Id}{suffix}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("You do not have access to this family.",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task Another_familys_task_is_not_reachable_through_your_own_family_route()
    {
        var victim = await fixture.CreateFamilyWithOwnerAsync();
        var attacker = await fixture.CreateFamilyWithOwnerAsync();
        var task = await victim.Owner.CreateTaskAsync(victim.Id, "Private task");

        var read = await attacker.Owner.Client.GetAsync($"/api/families/{attacker.Id}/tasks/{task.Id}");
        var delete = await attacker.Owner.Client.DeleteAsync($"/api/families/{attacker.Id}/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var stillThere = await victim.Owner.Client.GetAsync($"/api/families/{victim.Id}/tasks/{task.Id}");
        await stillThere.EnsureStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Shopping_items_check_membership_of_the_lists_family()
    {
        var victim = await fixture.CreateFamilyWithOwnerAsync();
        var attacker = await fixture.CreateFamilyWithOwnerAsync();
        var created = await victim.Owner.Client.PostAsJsonAsync(
            $"/api/families/{victim.Id}/shopping-lists", new CreateShoppingListRequest("Groceries"));
        await created.EnsureStatusAsync(HttpStatusCode.Created);
        var list = (await created.Content.ReadFromJsonAsync<ShoppingListResponse>())!;
        var item = new CreateShoppingItemRequest("Milk", "1 L", ItemCategory.Grocery);

        // The route has no familyId; the service derives it from the list.
        var denied = await attacker.Owner.Client.PostAsJsonAsync($"/api/shopping-lists/{list.Id}/items", item);
        var missing = await attacker.Owner.Client.PostAsJsonAsync($"/api/shopping-lists/{Guid.NewGuid()}/items", item);
        var allowed = await victim.Owner.Client.PostAsJsonAsync($"/api/shopping-lists/{list.Id}/items", item);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await allowed.EnsureStatusAsync(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_user_can_belong_to_only_one_family()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();

        var second = await family.Owner.Client.PostAsJsonAsync("/api/families", new CreateFamilyRequest("Second"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var current = await family.Owner.Client.GetFromJsonAsync<FamilyResponse>("/api/families/current");
        Assert.Equal(family.Id, current!.Id);
    }

    [Fact]
    public async Task A_user_in_another_family_cannot_accept_an_invitation()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var other = await fixture.CreateFamilyWithOwnerAsync();
        var invite = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/invitations",
            new { email = other.Owner.Email, role = (int)FamilyRole.Member });
        await invite.EnsureStatusAsync(HttpStatusCode.Created);
        var token = (await invite.Content.ReadFromJsonAsync<DTOs.Invitations.CreatedInvitationResponse>())!.Token;

        var accept = await other.Owner.Client.PostAsync($"/api/invitations/{token}/accept", content: null);

        Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode);
        Assert.Single(await family.Owner.GetMembersAsync(family.Id));
    }
}
