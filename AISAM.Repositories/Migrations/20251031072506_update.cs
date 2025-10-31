using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "approver_profile_id",
                table: "approvals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "approver_user_id",
                table: "approvals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_approvals_approver_user_id",
                table: "approvals",
                column: "approver_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_approvals_users_approver_user_id",
                table: "approvals",
                column: "approver_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_approvals_users_approver_user_id",
                table: "approvals");

            migrationBuilder.DropIndex(
                name: "IX_approvals_approver_user_id",
                table: "approvals");

            migrationBuilder.DropColumn(
                name: "approver_user_id",
                table: "approvals");

            migrationBuilder.AlterColumn<Guid>(
                name: "approver_profile_id",
                table: "approvals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
