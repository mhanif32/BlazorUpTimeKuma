using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorKuma.Migrations
{
    /// <inheritdoc />
    public partial class AddLastResponseToMonitor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastResponse",
                table: "Monitors",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastResponse",
                table: "Monitors");
        }
    }
}
