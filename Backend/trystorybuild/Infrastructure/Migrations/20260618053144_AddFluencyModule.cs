using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFluencyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioRecordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AudioFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationSeconds = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioRecordings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FluencyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WCPM = table.Column<double>(type: "float", nullable: false),
                    AccuracyScore = table.Column<double>(type: "float", nullable: false),
                    ExpectedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MispronouncedWordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FluencyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FluencyReports_AudioRecordings_RecordingId",
                        column: x => x.RecordingId,
                        principalTable: "AudioRecordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FluencyReports_RecordingId",
                table: "FluencyReports",
                column: "RecordingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FluencyReports");

            migrationBuilder.DropTable(
                name: "AudioRecordings");
        }
    }
}
