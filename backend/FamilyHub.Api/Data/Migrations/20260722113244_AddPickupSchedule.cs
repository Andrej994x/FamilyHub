using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickupSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickupSchedules_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PickupSchedules_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PickupSchedules_FamilyMembers_AssignedMemberId",
                        column: x => x.AssignedMemberId,
                        principalTable: "FamilyMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedules_AssignedMemberId",
                table: "PickupSchedules",
                column: "AssignedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedules_ChildProfileId",
                table: "PickupSchedules",
                column: "ChildProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedules_FamilyId",
                table: "PickupSchedules",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedules_PickupDateTime",
                table: "PickupSchedules",
                column: "PickupDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_PickupSchedules_Status",
                table: "PickupSchedules",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickupSchedules");
        }
    }
}
