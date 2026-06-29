using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSchoolCodeAddSchoolManagerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchoolCode",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "SchoolCode",
                table: "Classrooms");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolManagerId",
                table: "Teachers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolManagerId",
                table: "Classrooms",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchoolManagerId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "SchoolManagerId",
                table: "Classrooms");

            migrationBuilder.AddColumn<string>(
                name: "SchoolCode",
                table: "Teachers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolCode",
                table: "Classrooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
