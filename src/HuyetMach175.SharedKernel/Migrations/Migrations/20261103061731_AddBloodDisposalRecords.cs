using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodDisposalRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE donation_appointments DROP CONSTRAINT IF EXISTS ""FK_donation_appointments_donation_campaigns_campaign_id"";
                ALTER TABLE donation_sessions DROP CONSTRAINT IF EXISTS ""FK_donation_sessions_users_doctor_id"";
                DROP INDEX IF EXISTS ""IX_donation_sessions_doctor_id"";
                DROP INDEX IF EXISTS ""IX_donation_appointments_campaign_id"";

                ALTER TABLE pre_screening_surveys DROP COLUMN IF EXISTS submitted_at;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS blood_pressure;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS deferral_reason;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS doctor_id;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS hemoglobin_level;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS screening_status;
                ALTER TABLE donation_sessions DROP COLUMN IF EXISTS weight_kg;
                ALTER TABLE donation_appointments DROP COLUMN IF EXISTS campaign_id;
                ALTER TABLE donation_appointments DROP COLUMN IF EXISTS time_slot;

                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS body_temp numeric(4,1) NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS clinical_notes character varying(500) NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS deferral_reason character varying(500) NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS diastolic_blood_pressure integer NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS doctor_id integer NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS hemoglobin_level numeric(5,2) NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS pulse integer NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS rapid_hbsag_negative boolean NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screened_at timestamp with time zone NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screening_status text NOT NULL DEFAULT '';
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS survey_submitted_at timestamp with time zone NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS systolic_blood_pressure integer NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS target_volume_ml text NULL;
                ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS weight_kg numeric(5,2) NULL;

                ALTER TABLE donation_sessions ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                ALTER TABLE donation_sessions ALTER COLUMN collection_incident TYPE character varying(500);

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_pre_screening_surveys_users_doctor_id'
                    ) THEN
                        ALTER TABLE pre_screening_surveys
                        ADD CONSTRAINT ""FK_pre_screening_surveys_users_doctor_id""
                        FOREIGN KEY (doctor_id) REFERENCES users(user_id) ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ""IX_pre_screening_surveys_doctor_id"" ON pre_screening_surveys(doctor_id);
            ");

            migrationBuilder.CreateTable(
                name: "blood_disposal_records",
                columns: table => new
                {
                    disposal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    disposal_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bag_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    previous_status = table.Column<string>(type: "text", nullable: false),
                    requested_by = table.Column<int>(type: "integer", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blood_disposal_records", x => x.disposal_id);
                    table.ForeignKey(
                        name: "FK_blood_disposal_records_blood_bags_bag_id",
                        column: x => x.bag_id,
                        principalTable: "blood_bags",
                        principalColumn: "bag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_disposal_records_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_blood_disposal_records_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_records_approved_by",
                table: "blood_disposal_records",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_records_bag_id",
                table: "blood_disposal_records",
                column: "bag_id");

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_records_disposal_code",
                table: "blood_disposal_records",
                column: "disposal_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blood_disposal_records_requested_by",
                table: "blood_disposal_records",
                column: "requested_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pre_screening_surveys_users_doctor_id",
                table: "pre_screening_surveys");

            migrationBuilder.DropTable(
                name: "blood_disposal_records");

            migrationBuilder.DropIndex(
                name: "IX_pre_screening_surveys_doctor_id",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "body_temp",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "clinical_notes",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "deferral_reason",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "diastolic_blood_pressure",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "doctor_id",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "hemoglobin_level",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "pulse",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "rapid_hbsag_negative",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "screened_at",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "screening_status",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "survey_submitted_at",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "systolic_blood_pressure",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "target_volume_ml",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "weight_kg",
                table: "pre_screening_surveys");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "donation_sessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "pre_screening_surveys",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "target_volume_ml",
                table: "donation_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "collection_incident",
                table: "donation_sessions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "blood_pressure",
                table: "donation_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "deferral_reason",
                table: "donation_sessions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "doctor_id",
                table: "donation_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "hemoglobin_level",
                table: "donation_sessions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "screening_status",
                table: "donation_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                table: "donation_sessions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "campaign_id",
                table: "donation_appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_slot",
                table: "donation_appointments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_donation_sessions_doctor_id",
                table: "donation_sessions",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_donation_appointments_campaign_id",
                table: "donation_appointments",
                column: "campaign_id");

            migrationBuilder.AddForeignKey(
                name: "FK_donation_appointments_donation_campaigns_campaign_id",
                table: "donation_appointments",
                column: "campaign_id",
                principalTable: "donation_campaigns",
                principalColumn: "campaign_id");

            migrationBuilder.AddForeignKey(
                name: "FK_donation_sessions_users_doctor_id",
                table: "donation_sessions",
                column: "doctor_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
