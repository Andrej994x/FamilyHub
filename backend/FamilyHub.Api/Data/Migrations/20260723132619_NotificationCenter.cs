using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RelatedEntityId",
                table: "Notifications",
                newName: "FamilyId");

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Warranties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedMemberId",
                table: "Warranties",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedMemberId",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "ShoppingItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Pets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedMemberId",
                table: "Pets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "OtherRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedMemberId",
                table: "OtherRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPushSent",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PushSentAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedUrl",
                table: "Notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "HomeRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedMemberId",
                table: "HomeRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsPushSent",
                table: "Notifications",
                column: "IsPushSent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsPushSent",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "RelatedMemberId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RelatedMemberId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "ShoppingItems");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "RelatedMemberId",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "OtherRecords");

            migrationBuilder.DropColumn(
                name: "RelatedMemberId",
                table: "OtherRecords");

            migrationBuilder.DropColumn(
                name: "IsPushSent",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PushSentAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RelatedUrl",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "HomeRecords");

            migrationBuilder.DropColumn(
                name: "RelatedMemberId",
                table: "HomeRecords");

            migrationBuilder.RenameColumn(
                name: "FamilyId",
                table: "Notifications",
                newName: "RelatedEntityId");
        }
    }
}
