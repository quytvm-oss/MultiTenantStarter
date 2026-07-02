using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantStarter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class Update_Group_Name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_Name",
                schema: "identity",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                schema: "identity",
                table: "Groups",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Groups_Name",
                schema: "identity",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                schema: "identity",
                table: "Groups",
                column: "Name",
                unique: true);
        }
    }
}
