using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBloodDisposalToBatchModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blood_disposal_records_blood_bags_bag_id",
                table: "blood_disposal_records");

            migrationBuilder.DropIndex(
                name: "IX_blood_disposal_records_bag_id",
                table: "blood_disposal_records");

            migrationBuilder.DropColumn(
                name: "previous_status",
                table: "blood_disposal_records");

            migrationBuilder.RenameColumn(
                name: "bag_id",
                table: "blood_disposal_records",
                newName: "total_bags");

            migrationBuilder.CreateTable(
                name: "blood_disposal_items",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    disposal_id = table.Column<int>(type: "integer", nullable: false),
                    bag_id = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<string>(type: "text", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_disposal_items", x => x.item_id);
                    table.ForeignKey(
                        name: "FK_blood_disposal_items_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_disposal_items_blood_disposal_records_disposal_id",
                        column: x => x.disposal_id,
                        principalTable: "blood_disposal_records",
                        principalColumn: "disposal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_items_bag_id",
                table: "blood_disposal_items",
                column: "bag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_items_disposal_id",
                table: "blood_disposal_items",
                column: "disposal_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_disposal_items");

            migrationBuilder.RenameColumn(
                name: "total_bags",
                table: "blood_disposal_records",
                newName: "bag_id");

            migrationBuilder.AddColumn<string>(
                name: "previous_status",
                table: "blood_disposal_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_records_bag_id",
                table: "blood_disposal_records",
                column: "bag_id");

            migrationBuilder.AddForeignKey(
                name: "FK_blood_disposal_records_blood_bags_bag_id",
                table: "blood_disposal_records",
                column: "bag_id",
                principalTable: "blood_bags",
                principalColumn: "bag_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
