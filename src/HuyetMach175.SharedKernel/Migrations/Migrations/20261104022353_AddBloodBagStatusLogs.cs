using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodBagStatusLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blood_bag_status_logs",
                columns: table => new
                {
                    log_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bag_id = table.Column<int>(type: "integer", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_by_id = table.Column<int>(type: "integer", nullable: true),
                    audit_log_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_bag_status_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_blood_bag_status_logs_audit_logs_audit_log_id",
                        column: x => x.audit_log_id,
                        principalTable: "audit_logs",
                        principalColumn: "log_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_blood_bag_status_logs_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blood_bag_status_logs_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_blood_bag_status_logs_bag_id",
                table: "blood_bag_status_logs",
                column: "bag_id");

            migrationBuilder.CreateIndex(
                name: "idx_blood_bag_status_logs_created_at",
                table: "blood_bag_status_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_blood_bag_status_logs_audit_log_id",
                table: "blood_bag_status_logs",
                column: "audit_log_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_bag_status_logs_performed_by_id",
                table: "blood_bag_status_logs",
                column: "performed_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blood_bag_status_logs");
        }
    }
}
