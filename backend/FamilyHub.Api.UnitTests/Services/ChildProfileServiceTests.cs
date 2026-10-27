using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Children;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.UnitTests.Services;

public class ChildProfileServiceTests
{
    private static readonly CreateChildRequest NewChild = new(" Mia ", " Test ", new DateOnly(2018, 5, 1), "  ");

    [Theory]
    [InlineData(FamilyRole.Owner)]
    [InlineData(FamilyRole.Parent)]
    public async Task Owners_and_parents_can_create_children(FamilyRole role)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", role));

        var result = await new ChildProfileService(db).CreateChildAsync("ana", family.Id, NewChild);

        Assert.True(result.Succeeded);
        Assert.Equal(("Mia", "Test"), (result.Value!.FirstName, result.Value.LastName));
        Assert.Null(result.Value.Notes);
    }

    [Theory]
    [InlineData(FamilyRole.Member)]
    [InlineData(FamilyRole.Child)]
    public async Task Members_and_children_cannot_manage_children_but_can_read_them(FamilyRole role)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", role));
        var service = new ChildProfileService(db);
        var child = (await service.CreateChildAsync("ana", family.Id, NewChild)).Value!;

        var create = await service.CreateChildAsync("bob", family.Id, NewChild);
        var update = await service.UpdateChildAsync("bob", family.Id, child.Id,
            new UpdateChildRequest("Changed", "Name", null, null));
        var list = await service.GetChildrenAsync("bob", family.Id);

        Assert.Equal(ErrorType.Forbidden, create.ErrorType);
        Assert.Equal(ErrorType.Forbidden, update.ErrorType);
        Assert.True(list.Succeeded);
        Assert.Single(list.Value!);
        Assert.Equal("Mia", (await db.ChildProfiles.SingleAsync()).FirstName);
    }

    [Fact]
    public async Task A_missing_family_is_not_found_even_for_a_stranger()
    {
        await using var db = TestDb.Create();

        var result = await new ChildProfileService(db).CreateChildAsync("stranger", Guid.NewGuid(), NewChild);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task A_child_is_looked_up_by_id_and_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var service = new ChildProfileService(db);
        var childOfB = (await service.CreateChildAsync("zed", familyB.Id, NewChild)).Value!;

        // Ana is a member of A but asks for B's child through A's route.
        var result = await service.GetChildAsync("ana", familyA.Id, childOfB.Id);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
