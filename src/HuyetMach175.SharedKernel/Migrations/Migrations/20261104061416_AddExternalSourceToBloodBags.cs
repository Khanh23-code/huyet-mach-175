using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalSourceToBloodBags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_code",
                table: "blood_bags",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_unit_name",
                table: "blood_bags",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_blood_bags_external",
                table: "blood_bags",
                columns: new[] { "external_unit_name", "external_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_blood_bags_external",
                table: "blood_bags");

            migrationBuilder.DropColumn(
                name: "external_code",
                table: "blood_bags");

            migrationBuilder.DropColumn(
                name: "external_unit_name",
                table: "blood_bags");
        }
    }
}
