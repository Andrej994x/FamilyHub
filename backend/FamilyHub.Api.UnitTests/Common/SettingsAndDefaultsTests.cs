using FamilyHub.Api.Common;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.UnitTests.Common;

public class SettingsAndDefaultsTests
{
    [Fact]
    public void Every_reminder_category_has_unique_descending_non_negative_offsets()
    {
        foreach (var category in Enum.GetValues<ReminderCategory>())
        {
            var offsets = ReminderCategoryDefaults.For(category);

            Assert.NotEmpty(offsets);
            Assert.All(offsets, o => Assert.True(o >= 0));
            Assert.Equal(offsets.Distinct().Count(), offsets.Length);
            Assert.Equal(offsets.OrderByDescending(o => o), offsets);
        }
    }

    [Fact]
    public void Reminder_defaults_return_a_defensive_copy()
    {
        var first = ReminderCategoryDefaults.For(ReminderCategory.Tasks);
        first[0] = 999;

        Assert.NotEqual(999, ReminderCategoryDefaults.For(ReminderCategory.Tasks)[0]);
    }

    [Fact]
    public void Max_default_offset_bounds_the_scan_horizon()
    {
        var expected = Enum.GetValues<ReminderCategory>().SelectMany(ReminderCategoryDefaults.For).Max();

        Assert.Equal(expected, ReminderCategoryDefaults.MaxDefaultOffset);
    }

    [Theory]
    [InlineData("public", "private", true)]
    [InlineData("public", "", false)]
    [InlineData("", "private", false)]
    [InlineData("  ", "  ", false)]
    public void Push_is_configured_only_with_both_vapid_keys(string publicKey, string privateKey, bool expected)
    {
        var settings = new PushSettings { PublicKey = publicKey, PrivateKey = privateKey };

        Assert.Equal(expected, settings.IsConfigured);
    }
}
