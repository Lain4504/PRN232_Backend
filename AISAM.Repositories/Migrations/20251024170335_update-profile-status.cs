using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class updateprofilestatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "profiles");

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "subscriptions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "subscriptions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "profiles");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
