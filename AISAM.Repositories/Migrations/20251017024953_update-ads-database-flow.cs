using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class updateadsdatabaseflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "quota_ad_budget_monthly",
                table: "subscriptions",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "quota_ad_campaigns",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ad_account_id",
                table: "social_integrations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "post_id",
                table: "performance_reports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ad_id",
                table: "performance_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_performance_reports_ad_id",
                table: "performance_reports",
                column: "ad_id");

            migrationBuilder.AddForeignKey(
                name: "FK_performance_reports_ads_ad_id",
                table: "performance_reports",
                column: "ad_id",
                principalTable: "ads",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_performance_reports_ads_ad_id",
                table: "performance_reports");

            migrationBuilder.DropIndex(
                name: "IX_performance_reports_ad_id",
                table: "performance_reports");

            migrationBuilder.DropColumn(
                name: "quota_ad_budget_monthly",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "quota_ad_campaigns",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ad_account_id",
                table: "social_integrations");

            migrationBuilder.DropColumn(
                name: "ad_id",
                table: "performance_reports");

            migrationBuilder.AlterColumn<Guid>(
                name: "post_id",
                table: "performance_reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
