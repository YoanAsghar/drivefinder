using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace DriveFinder.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "drive_changes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    ChangeToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drive_changes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "drive_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FileId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    WebViewLink = table.Column<string>(type: "text", nullable: true),
                    ModifiedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ContentText = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drive_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageToken = table.Column<string>(type: "text", nullable: true),
                    LastFullSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastIncrementalSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "sync_state",
                columns: new[] { "Id", "LastFullSyncAt", "LastIncrementalSyncAt", "PageToken" },
                values: new object[] { 1, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_drive_changes_DetectedAt",
                table: "drive_changes",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_drive_changes_FileId",
                table: "drive_changes",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_drive_files_Embedding",
                table: "drive_files",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_drive_files_FileId",
                table: "drive_files",
                column: "FileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drive_changes");

            migrationBuilder.DropTable(
                name: "drive_files");

            migrationBuilder.DropTable(
                name: "sync_state");
        }
    }
}
