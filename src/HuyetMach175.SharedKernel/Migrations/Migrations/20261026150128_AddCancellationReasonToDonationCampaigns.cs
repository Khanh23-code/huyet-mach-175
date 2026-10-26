using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReasonToDonationCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "donation_site_slots");

            migrationBuilder.DropTable(
                name: "site_proposals");

            migrationBuilder.DropTable(
                name: "donation_sites");

            migrationBuilder.AddColumn<string>(
                name: "campaign_type",
                table: "donation_campaigns",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "donation_campaigns",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "donation_campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_capacity",
                table: "donation_campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "operating_days",
                table: "donation_campaigns",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_id",
                table: "donation_appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "donation_campaign_slots",
                columns: table => new
                {
                    slot_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campaign_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donation_campaign_slots", x => x.slot_id);
                    table.ForeignKey(
                        name: "FK_donation_campaign_slots_donation_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "donation_campaigns",
                        principalColumn: "campaign_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_donation_appointments_slot_id",
                table: "donation_appointments",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_donation_campaign_slots_campaign_id",
                table: "donation_campaign_slots",
                column: "campaign_id");

            migrationBuilder.AddForeignKey(
                name: "FK_donation_appointments_donation_campaign_slots_slot_id",
                table: "donation_appointments",
                column: "slot_id",
                principalTable: "donation_campaign_slots",
                principalColumn: "slot_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_donation_appointments_donation_campaign_slots_slot_id",
                table: "donation_appointments");

            migrationBuilder.DropTable(
                name: "donation_campaign_slots");

            migrationBuilder.DropIndex(
                name: "IX_donation_appointments_slot_id",
                table: "donation_appointments");

            migrationBuilder.DropColumn(
                name: "campaign_type",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "description",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "max_capacity",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "operating_days",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "slot_id",
                table: "donation_appointments");

            migrationBuilder.CreateTable(
                name: "donation_sites",
                columns: table => new
                {
                    site_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    site_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    site_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    site_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
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
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    reviewed_by = table.Column<int>(type: "integer", nullable: true),
                    site_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    proposal_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    proposal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    site_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    site_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    slots_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
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
    }
}
