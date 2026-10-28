using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.DTOs.Children;
using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.IntegrationTests.Authorization;

/// <summary>Owner / Parent / Member / Child permissions, enforced by the services.</summary>
[Collection(ApiCollection.Name)]
public class RolePermissionTests(FamilyHubApiFixture fixture)
{
    private static readonly CreateChildRequest Child = new("Mia", "Tester", new DateOnly(2018, 3, 4), null);

    [Theory]
    [InlineData(FamilyRole.Parent, HttpStatusCode.Created)]
    [InlineData(FamilyRole.Member, HttpStatusCode.Forbidden)]
    [InlineData(FamilyRole.Child, HttpStatusCode.Forbidden)]
    public async Task Only_owners_and_parents_can_manage_child_profiles(FamilyRole role, HttpStatusCode expected)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, role);

        var asMember = await member.Client.PostAsJsonAsync($"/api/families/{family.Id}/children", Child);
        var asOwner = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/children", Child);
        var read = await member.Client.GetFromJsonAsync<List<ChildResponse>>($"/api/families/{family.Id}/children");

        Assert.Equal(expected, asMember.StatusCode);
        await asOwner.EnsureStatusAsync(HttpStatusCode.Created);
        // Every member can read the family's children.
        Assert.Equal(expected == HttpStatusCode.Created ? 2 : 1, read!.Count);
    }

    [Theory]
    [InlineData(FamilyRole.Parent, HttpStatusCode.Created)]
    [InlineData(FamilyRole.Member, HttpStatusCode.Forbidden)]
    public async Task Only_owners_and_parents_can_invite(FamilyRole role, HttpStatusCode expected)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, role);

        var response = await member.Client.PostAsJsonAsync($"/api/families/{family.Id}/invitations",
            new CreateInvitationRequest($"{Guid.NewGuid():N}@test.familyhub", FamilyRole.Member));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_can_rename_the_family()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);

        var asParent = await parent.Client.PutAsJsonAsync($"/api/families/{family.Id}", new UpdateFamilyRequest("Parent's name"));
        var asOwner = await family.Owner.Client.PutAsJsonAsync($"/api/families/{family.Id}", new UpdateFamilyRequest("Owner's name"));

        Assert.Equal(HttpStatusCode.Forbidden, asParent.StatusCode);
        await asOwner.EnsureStatusAsync(HttpStatusCode.OK);
        var current = await parent.Client.GetFromJsonAsync<FamilyResponse>("/api/families/current");
        Assert.Equal("Owner's name", current!.Name);
        Assert.Equal(FamilyRole.Parent, current.CurrentUserRole);
    }

    [Theory]
    [InlineData(FamilyRole.Parent)]
    [InlineData(FamilyRole.Member)]
    [InlineData(FamilyRole.Child)]
    public async Task Every_member_can_work_with_tasks(FamilyRole role)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, role);

        var task = await member.CreateTaskAsync(family.Id, $"Task by {role}");
        var status = await member.Client.PatchAsJsonAsync($"/api/families/{family.Id}/tasks/{task.Id}/status",
            new UpdateTaskStatusRequest(Models.Enums.TaskStatus.InProgress));

        await status.EnsureStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_owner_cannot_be_removed_and_a_parent_cannot_remove_members()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var members = await family.Owner.GetMembersAsync(family.Id);
        var ownerMembership = members.Single(m => m.UserId == family.Owner.UserId);
        var memberMembership = members.Single(m => m.UserId == member.UserId);

        var removeOwner = await family.Owner.Client.DeleteAsync($"/api/families/{family.Id}/members/{ownerMembership.Id}");
        var parentRemoves = await parent.Client.DeleteAsync($"/api/families/{family.Id}/members/{memberMembership.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, removeOwner.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, parentRemoves.StatusCode);
        Assert.Equal(3, (await family.Owner.GetMembersAsync(family.Id)).Count);
    }

    [Fact]
    public async Task Removing_a_member_unassigns_their_tasks_and_revokes_access()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var membership = (await family.Owner.GetMembersAsync(family.Id)).Single(m => m.UserId == member.UserId);
        var task = await family.Owner.CreateTaskAsync(family.Id, "Assigned chore", assignee: membership.Id);

        var remove = await family.Owner.Client.DeleteAsync($"/api/families/{family.Id}/members/{membership.Id}");

        await remove.EnsureStatusAsync(HttpStatusCode.NoContent);
        var reloaded = await family.Owner.Client.GetFromJsonAsync<TaskResponse>($"/api/families/{family.Id}/tasks/{task.Id}");
        Assert.Null(reloaded!.AssignedToMemberId);
        var afterRemoval = await member.Client.GetAsync($"/api/families/{family.Id}/tasks");
        Assert.Equal(HttpStatusCode.Forbidden, afterRemoval.StatusCode);
    }
}
