using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car_Rental.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Maintenances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Maintenances");
        }
    }
}
