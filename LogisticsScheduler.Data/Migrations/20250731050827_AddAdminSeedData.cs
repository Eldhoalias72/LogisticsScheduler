using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admin",
                columns: new[] { "AdminId", "PasswordHash", "Username" },
                values: new object[] { 2, "$2a$12$xaYILAxGiT.fMakP1yDKne1YwY2dzH0wkJUxcIQtRUCcd4sR8kKDm", "admin0" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admin",
                keyColumn: "AdminId",
                keyValue: 2);
        }
    }
}