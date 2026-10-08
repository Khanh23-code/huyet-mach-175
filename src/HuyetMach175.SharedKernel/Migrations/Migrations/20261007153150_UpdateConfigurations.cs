using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blood_request_items_blood_component_types_component_type_id",
                table: "blood_request_items");

            migrationBuilder.AddForeignKey(
                name: "FK_blood_request_items_blood_component_types_component_type_id",
                table: "blood_request_items",
                column: "component_type_id",
                principalTable: "blood_component_types",
                principalColumn: "component_type_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blood_request_items_blood_component_types_component_type_id",
                table: "blood_request_items");

            migrationBuilder.AddForeignKey(
                name: "FK_blood_request_items_blood_component_types_component_type_id",
                table: "blood_request_items",
                column: "component_type_id",
                principalTable: "blood_component_types",
                principalColumn: "component_type_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
