using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPayOS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "stripe_subscription_id",
                table: "subscriptions",
                newName: "payos_payment_link_id");

            migrationBuilder.RenameColumn(
                name: "stripe_customer_id",
                table: "subscriptions",
                newName: "payos_order_code");

            migrationBuilder.RenameColumn(
                name: "quota_storage_gb",
                table: "subscriptions",
                newName: "quota_platforms");

            migrationBuilder.AlterColumn<decimal>(
                name: "quota_ad_budget_monthly",
                table: "subscriptions",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AddColumn<int>(
                name: "analysis_level",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "quota_accounts",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "quota_ai_content_per_day",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "quota_ai_images_per_day",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analysis_level",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "quota_accounts",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "quota_ai_content_per_day",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "quota_ai_images_per_day",
                table: "subscriptions");

            migrationBuilder.RenameColumn(
                name: "quota_platforms",
                table: "subscriptions",
                newName: "quota_storage_gb");

            migrationBuilder.RenameColumn(
                name: "payos_payment_link_id",
                table: "subscriptions",
                newName: "stripe_subscription_id");

            migrationBuilder.RenameColumn(
                name: "payos_order_code",
                table: "subscriptions",
                newName: "stripe_customer_id");

            migrationBuilder.AlterColumn<decimal>(
                name: "quota_ad_budget_monthly",
                table: "subscriptions",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
