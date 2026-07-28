using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReminderPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderHistory_SourceType_SourceId_DateKind_DueDate_DaysBefore",
                table: "ReminderHistory");

            migrationBuilder.AddColumn<string>(
                name: "RecipientUserId",
                table: "ReminderHistory",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserReminderPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderOffsetsDays = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReminderPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReminderPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderHistory_RecipientUserId_SourceType_SourceId_DateKind_DueDate_DaysBefore",
                table: "ReminderHistory",
                columns: new[] { "RecipientUserId", "SourceType", "SourceId", "DateKind", "DueDate", "DaysBefore" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReminderPreferences_UserId_Category",
                table: "UserReminderPreferences",
                columns: new[] { "UserId", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReminderPreferences");

            migrationBuilder.DropIndex(
                name: "IX_ReminderHistory_RecipientUserId_SourceType_SourceId_DateKind_DueDate_DaysBefore",
                table: "ReminderHistory");

            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "ReminderHistory");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderHistory_SourceType_SourceId_DateKind_DueDate_DaysBefore",
                table: "ReminderHistory",
                columns: new[] { "SourceType", "SourceId", "DateKind", "DueDate", "DaysBefore" },
                unique: true);
        }
    }
}
