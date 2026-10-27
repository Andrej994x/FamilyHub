using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.UnitTests.Services;

public class FamilyServiceTests
{
    [Fact]
    public async Task Creator_becomes_the_owner_and_only_member()
    {
        await using var db = TestDb.Create();

        var result = await new FamilyService(db).CreateFamilyAsync("ana", new CreateFamilyRequest("  The Petrovs  "));

        Assert.True(result.Succeeded);
        Assert.Equal("The Petrovs", result.Value!.Name);
        Assert.Equal(FamilyRole.Owner, result.Value.CurrentUserRole);
        Assert.Equal(1, result.Value.MemberCount);
        var member = await db.FamilyMembers.SingleAsync();
        Assert.Equal(("ana", FamilyRole.Owner), (member.UserId, member.Role));
    }

    [Fact]
    public async Task A_user_cannot_create_a_second_family()
    {
        await using var db = TestDb.Create();
        await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Member));

        var result = await new FamilyService(db).CreateFamilyAsync("ana", new CreateFamilyRequest("Another"));

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(1, await db.Families.CountAsync());
    }

    [Fact]
    public async Task A_missing_family_is_not_found_before_membership_is_checked()
    {
        await using var db = TestDb.Create();

        var result = await new FamilyService(db).GetFamilyByIdAsync("stranger", Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task A_non_member_is_forbidden_from_reading_a_family_or_its_members()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var service = new FamilyService(db);

        Assert.Equal(ErrorType.Forbidden, (await service.GetFamilyByIdAsync("stranger", family.Id)).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await service.GetMembersAsync("stranger", family.Id)).ErrorType);
    }

    [Theory]
    [InlineData(FamilyRole.Parent)]
    [InlineData(FamilyRole.Member)]
    [InlineData(FamilyRole.Child)]
    public async Task Only_the_owner_can_rename_the_family(FamilyRole role)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", role));
        var service = new FamilyService(db);

        var denied = await service.UpdateFamilyAsync("bob", family.Id, new UpdateFamilyRequest("Renamed by bob"));
        var allowed = await service.UpdateFamilyAsync("ana", family.Id, new UpdateFamilyRequest("Renamed by ana"));

        Assert.Equal(ErrorType.Forbidden, denied.ErrorType);
        Assert.True(allowed.Succeeded);
        Assert.Equal("Renamed by ana", (await db.Families.SingleAsync()).Name);
    }

    [Fact]
    public async Task The_owner_cannot_be_removed()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));

        var result = await new FamilyService(db).RemoveMemberAsync("ana", family.Id, members[0].Id);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(1, await db.FamilyMembers.CountAsync());
    }

    [Fact]
    public async Task A_parent_cannot_remove_members()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(
            db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Parent), ("cara", FamilyRole.Member));

        var result = await new FamilyService(db).RemoveMemberAsync("bob", family.Id, members[2].Id);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Removing_a_member_of_another_family_is_not_found()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (_, otherMembers) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner), ("yan", FamilyRole.Member));

        var result = await new FamilyService(db).RemoveMemberAsync("ana", family.Id, otherMembers[1].Id);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
