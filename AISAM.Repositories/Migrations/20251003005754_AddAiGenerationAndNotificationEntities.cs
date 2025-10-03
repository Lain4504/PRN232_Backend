using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISAM.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAiGenerationAndNotificationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assets_users_UploadedBy",
                table: "assets");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "assets",
                newName: "width");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "assets",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "assets",
                newName: "metadata");

            migrationBuilder.RenameColumn(
                name: "Height",
                table: "assets",
                newName: "height");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "assets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UploadedBy",
                table: "assets",
                newName: "uploaded_by");

            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "assets",
                newName: "storage_path");

            migrationBuilder.RenameColumn(
                name: "SizeBytes",
                table: "assets",
                newName: "size_bytes");

            migrationBuilder.RenameColumn(
                name: "MimeType",
                table: "assets",
                newName: "mime_type");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "assets",
                newName: "duration_seconds");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "assets",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_assets_UploadedBy",
                table: "assets",
                newName: "IX_assets_uploaded_by");

            migrationBuilder.Sql("ALTER TABLE contents ALTER COLUMN image_url TYPE jsonb USING image_url::jsonb;");

            migrationBuilder.Sql("ALTER TABLE assets ALTER COLUMN type TYPE integer USING CASE WHEN type = 'Image' THEN 0 WHEN type = 'Video' THEN 1 WHEN type = 'Audio' THEN 2 ELSE 0 END;");

            migrationBuilder.AlterColumn<string>(
                name: "metadata",
                table: "assets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "duration_seconds",
                table: "assets",
                type: "numeric(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ai_generations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_prompt = table.Column<string>(type: "text", nullable: false),
                    generated_text = table.Column<string>(type: "text", nullable: true),
                    generated_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    generated_video_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_generations", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_generations_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_generations_content_id",
                table: "ai_generations",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_generations_status",
                table: "ai_generations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_at",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_read",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_type",
                table: "notifications",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_assets_users_uploaded_by",
                table: "assets",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assets_users_uploaded_by",
                table: "assets");

            migrationBuilder.DropTable(
                name: "ai_generations");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.RenameColumn(
                name: "width",
                table: "assets",
                newName: "Width");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "assets",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "metadata",
                table: "assets",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "height",
                table: "assets",
                newName: "Height");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "assets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "uploaded_by",
                table: "assets",
                newName: "UploadedBy");

            migrationBuilder.RenameColumn(
                name: "storage_path",
                table: "assets",
                newName: "StoragePath");

            migrationBuilder.RenameColumn(
                name: "size_bytes",
                table: "assets",
                newName: "SizeBytes");

            migrationBuilder.RenameColumn(
                name: "mime_type",
                table: "assets",
                newName: "MimeType");

            migrationBuilder.RenameColumn(
                name: "duration_seconds",
                table: "assets",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "assets",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_assets_uploaded_by",
                table: "assets",
                newName: "IX_assets_UploadedBy");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "contents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "assets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DurationSeconds",
                table: "assets",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_assets_users_UploadedBy",
                table: "assets",
                column: "UploadedBy",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
