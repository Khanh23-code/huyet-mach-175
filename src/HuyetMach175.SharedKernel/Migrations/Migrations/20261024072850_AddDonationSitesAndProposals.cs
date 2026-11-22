using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationSitesAndProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "donation_sites",
                columns: table => new
                {
                    site_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    site_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    site_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_sites", x => x.site_id);
                });

            migrationBuilder.CreateTable(
                name: "donation_site_slots",
                columns: table => new
                {
                    slot_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_site_slots", x => x.slot_id);
                    table.ForeignKey(
                        name: "FK_donation_site_slots_donation_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "donation_sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_proposals",
                columns: table => new
                {
                    proposal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proposal_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    proposal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    site_id = table.Column<int>(type: "integer", nullable: true),
                    site_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    site_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    slots_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_proposals", x => x.proposal_id);
                    table.ForeignKey(
                        name: "FK_site_proposals_donation_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "donation_sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_site_proposals_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_site_proposals_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_donation_site_slots_site_id_start_time",
                table: "donation_site_slots",
                columns: new[] { "site_id", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_donation_sites_site_code",
                table: "donation_sites",
                column: "site_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_proposals_created_by",
                table: "site_proposals",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_site_proposals_proposal_code",
                table: "site_proposals",
                column: "proposal_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_proposals_reviewed_by",
                table: "site_proposals",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_site_proposals_site_id_status",
                table: "site_proposals",
                columns: new[] { "site_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_site_proposals_status_created_at",
                table: "site_proposals",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "donation_site_slots");

            migrationBuilder.DropTable(
                name: "site_proposals");

            migrationBuilder.DropTable(
                name: "donation_sites");
        }
    }
}
