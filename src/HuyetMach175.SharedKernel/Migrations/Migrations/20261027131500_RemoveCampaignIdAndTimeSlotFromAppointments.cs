using HuyetMach175.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027131500_RemoveCampaignIdAndTimeSlotFromAppointments")]
    public partial class RemoveCampaignIdAndTimeSlotFromAppointments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE donation_appointments ADD COLUMN IF NOT EXISTS slot_id integer NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_donation_appointments_donation_campaign_slots_slot_id'
                    ) THEN
                        ALTER TABLE donation_appointments
                        ADD CONSTRAINT ""FK_donation_appointments_donation_campaign_slots_slot_id""
                        FOREIGN KEY (slot_id) REFERENCES donation_campaign_slots(slot_id) ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ""IX_donation_appointments_slot_id"" ON donation_appointments(slot_id);

                DELETE FROM pre_screening_surveys
                WHERE appointment_id IN (SELECT appointment_id FROM donation_appointments WHERE slot_id IS NULL);

                DELETE FROM donation_sessions
                WHERE appointment_id IN (SELECT appointment_id FROM donation_appointments WHERE slot_id IS NULL)
                  AND session_id NOT IN (SELECT session_id FROM blood_bags WHERE session_id IS NOT NULL);

                DELETE FROM donation_appointments
                WHERE slot_id IS NULL
                  AND appointment_id NOT IN (SELECT appointment_id FROM donation_sessions);

                ALTER TABLE donation_appointments DROP CONSTRAINT IF EXISTS ""FK_donation_appointments_donation_campaigns_campaign_id"";
                DROP INDEX IF EXISTS ""IX_donation_appointments_campaign_id"";
                ALTER TABLE donation_appointments DROP COLUMN IF EXISTS campaign_id;
                ALTER TABLE donation_appointments DROP COLUMN IF EXISTS time_slot;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_donation_appointments_campaign_id",
                table: "donation_appointments",
                column: "campaign_id");

            migrationBuilder.AddForeignKey(
                name: "FK_donation_appointments_donation_campaigns_campaign_id",
                table: "donation_appointments",
                column: "campaign_id",
                principalTable: "donation_campaigns",
                principalColumn: "campaign_id");
        }
    }
}
