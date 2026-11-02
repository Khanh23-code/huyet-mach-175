using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class SchemaSeeder
{
    public static async Task EnsureSchemaUpdatedAsync(ApplicationDbContext context)
    {
        const string sql = @"
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

            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS survey_submitted_at timestamp with time zone NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS doctor_id integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS weight_kg numeric(5,2) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS systolic_blood_pressure integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS diastolic_blood_pressure integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS pulse integer NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS hemoglobin_level numeric(5,2) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS body_temp numeric(4,1) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS rapid_hbsag_negative boolean NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS clinical_notes character varying(500) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screening_status text NOT NULL DEFAULT 'PENDING';
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS deferral_reason character varying(500) NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS target_volume_ml text NULL;
            ALTER TABLE pre_screening_surveys ADD COLUMN IF NOT EXISTS screened_at timestamp with time zone NULL;

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

            ALTER TABLE donation_sessions ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE donation_sessions ALTER COLUMN collection_incident TYPE character varying(500);
            ALTER TABLE donation_sessions DROP CONSTRAINT IF EXISTS ""FK_donation_sessions_users_doctor_id"";
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS doctor_id;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS weight_kg;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS blood_pressure;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS hemoglobin_level;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS screening_status;
            ALTER TABLE donation_sessions DROP COLUMN IF EXISTS deferral_reason;
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}