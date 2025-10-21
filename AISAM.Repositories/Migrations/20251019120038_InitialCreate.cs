using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "repeat_type",
                table: "content_calendar",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "integration_ids",
                table: "content_calendar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "content_calendar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_scheduled_date",
                table: "content_calendar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "repeat_interval",
                table: "content_calendar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "repeat_until",
                table: "content_calendar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "content_calendar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "content_calendar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "integration_ids",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "next_scheduled_date",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "repeat_interval",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "repeat_until",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "content_calendar");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "content_calendar");

            migrationBuilder.AlterColumn<string>(
                name: "repeat_type",
                table: "content_calendar",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
