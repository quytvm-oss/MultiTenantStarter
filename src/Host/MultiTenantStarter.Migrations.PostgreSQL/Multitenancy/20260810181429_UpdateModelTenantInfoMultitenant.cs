using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiTenantStarter.Migrations.PostgreSQL.Multitenancy
{
    /// <inheritdoc />
    public partial class UpdateModelTenantInfoMultitenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                schema: "tenant",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                schema: "tenant",
                table: "Tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotaLimits",
                schema: "tenant",
                table: "Tenants",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "tenant",
                table: "TenantProvisionings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "tenant",
                table: "TenantProvisionings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "QuotaLimits",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                schema: "tenant",
                table: "Tenants",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "tenant",
                table: "TenantProvisionings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "tenant",
                table: "TenantProvisionings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
