using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFieldsToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNumber",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Jobs");
        }
    }
}
