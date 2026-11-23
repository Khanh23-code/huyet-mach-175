using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverSessionComponentConfigAndZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "zone",
                table: "storage_locations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "allow_restock",
                table: "blood_component_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "compatibility_rule_type",
                table: "blood_component_types",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "max_outside_minutes_for_restock",
                table: "blood_component_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_restock_count",
                table: "blood_component_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "max_temp",
                table: "blood_component_types",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "min_temp",
                table: "blood_component_types",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "blood_handover_sessions",
                columns: table => new
                {
                    handover_session_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    request_id = table.Column<int>(type: "integer", nullable: false),
                    initiated_by = table.Column<int>(type: "integer", nullable: false),
                    confirmed_by = table.Column<int>(type: "integer", nullable: true),
                    cancelled_by = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_handover_sessions", x => x.handover_session_id);
                    table.ForeignKey(
                        name: "FK_blood_handover_sessions_blood_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "blood_requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_handover_sessions_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_handover_sessions_users_confirmed_by",
                        column: x => x.confirmed_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_handover_sessions_users_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blood_handover_session_items",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    handover_session_id = table.Column<int>(type: "integer", nullable: false),
                    allocation_id = table.Column<int>(type: "integer", nullable: false),
                    bag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_handover_session_items", x => x.item_id);
                    table.ForeignKey(
                        name: "FK_blood_handover_session_items_blood_allocations_allocation_id",
                        column: x => x.allocation_id,
                        principalTable: "blood_allocations",
                        principalColumn: "allocation_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_handover_session_items_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_handover_session_items_blood_handover_sessions_handov~",
                        column: x => x.handover_session_id,
                        principalTable: "blood_handover_sessions",
                        principalColumn: "handover_session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_session_items_allocation_id",
                table: "blood_handover_session_items",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_session_items_bag_id",
                table: "blood_handover_session_items",
                column: "bag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_session_items_handover_session_id_allocation~",
                table: "blood_handover_session_items",
                columns: new[] { "handover_session_id", "allocation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_blood_handover_sessions_status_expires_at",
                table: "blood_handover_sessions",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_sessions_cancelled_by",
                table: "blood_handover_sessions",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_sessions_confirmed_by",
                table: "blood_handover_sessions",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_sessions_initiated_by",
                table: "blood_handover_sessions",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_sessions_request_id",
                table: "blood_handover_sessions",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_handover_sessions_session_code",
                table: "blood_handover_sessions",
                column: "session_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_handover_session_items");

            migrationBuilder.DropTable(
                name: "blood_handover_sessions");

            migrationBuilder.DropColumn(
                name: "zone",
                table: "storage_locations");

            migrationBuilder.DropColumn(
                name: "allow_restock",
                table: "blood_component_types");

            migrationBuilder.DropColumn(
                name: "compatibility_rule_type",
                table: "blood_component_types");

            migrationBuilder.DropColumn(
                name: "max_outside_minutes_for_restock",
                table: "blood_component_types");

            migrationBuilder.DropColumn(
                name: "max_restock_count",
                table: "blood_component_types");

            migrationBuilder.DropColumn(
                name: "max_temp",
                table: "blood_component_types");

            migrationBuilder.DropColumn(
                name: "min_temp",
                table: "blood_component_types");
        }
    }
}
