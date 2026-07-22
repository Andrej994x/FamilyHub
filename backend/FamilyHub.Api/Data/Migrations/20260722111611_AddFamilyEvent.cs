using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AssignedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyEvents_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FamilyEvents_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyEvents_FamilyMembers_AssignedMemberId",
                        column: x => x.AssignedMemberId,
                        principalTable: "FamilyMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyEvents_AssignedMemberId",
                table: "FamilyEvents",
                column: "AssignedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyEvents_ChildProfileId",
                table: "FamilyEvents",
                column: "ChildProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyEvents_EventType",
                table: "FamilyEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyEvents_FamilyId",
                table: "FamilyEvents",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyEvents_StartDateTime",
                table: "FamilyEvents",
                column: "StartDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyEvents");
        }
    }
}
