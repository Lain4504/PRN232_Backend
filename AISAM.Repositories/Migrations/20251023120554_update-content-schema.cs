using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class updatecontentschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "contents",
                newName: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_contents_profile_id",
                table: "contents",
                column: "profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contents_profiles_profile_id",
                table: "contents",
                column: "profile_id",
                principalTable: "profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contents_profiles_profile_id",
                table: "contents");

            migrationBuilder.DropIndex(
                name: "IX_contents_profile_id",
                table: "contents");

            migrationBuilder.RenameColumn(
                name: "profile_id",
                table: "contents",
                newName: "user_id");
        }
    }
}
