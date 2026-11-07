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

            CREATE TABLE IF NOT EXISTS blood_disposal_records (
                disposal_id SERIAL PRIMARY KEY,
                disposal_code character varying(30) NOT NULL,
                reason text NOT NULL,
                notes character varying(500) NULL,
                status text NOT NULL DEFAULT 'PENDING_APPROVAL',
                total_bags integer NOT NULL DEFAULT 0,
                requested_by integer NOT NULL,
                requested_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                approved_by integer NULL,
                approved_at timestamp with time zone NULL,
                rejection_reason character varying(500) NULL,
                CONSTRAINT ""FK_blood_disposal_records_users_requested_by"" FOREIGN KEY (requested_by) REFERENCES users(user_id) ON DELETE RESTRICT,
                CONSTRAINT ""FK_blood_disposal_records_users_approved_by"" FOREIGN KEY (approved_by) REFERENCES users(user_id) ON DELETE RESTRICT
            );

            ALTER TABLE blood_disposal_records ADD COLUMN IF NOT EXISTS total_bags integer NOT NULL DEFAULT 0;
            ALTER TABLE blood_disposal_records DROP CONSTRAINT IF EXISTS ""FK_blood_disposal_records_blood_bags_bag_id"";
            DROP INDEX IF EXISTS ""IX_blood_disposal_records_bag_id"";
            ALTER TABLE blood_disposal_records DROP COLUMN IF EXISTS bag_id;
            ALTER TABLE blood_disposal_records DROP COLUMN IF EXISTS previous_status;

            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_blood_disposal_records_disposal_code"" ON blood_disposal_records(disposal_code);
            CREATE INDEX IF NOT EXISTS ""IX_blood_disposal_records_status"" ON blood_disposal_records(status);

            CREATE TABLE IF NOT EXISTS blood_disposal_items (
                item_id SERIAL PRIMARY KEY,
                disposal_id integer NOT NULL,
                bag_id integer NOT NULL,
                previous_status text NOT NULL,
                is_verified boolean NOT NULL DEFAULT TRUE,
                verified_at timestamp with time zone NULL,
                CONSTRAINT ""FK_blood_disposal_items_blood_disposal_records_disposal_id"" FOREIGN KEY (disposal_id) REFERENCES blood_disposal_records(disposal_id) ON DELETE CASCADE,
                CONSTRAINT ""FK_blood_disposal_items_blood_bags_bag_id"" FOREIGN KEY (bag_id) REFERENCES blood_bags(bag_id) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS ""IX_blood_disposal_items_disposal_id"" ON blood_disposal_items(disposal_id);
            CREATE INDEX IF NOT EXISTS ""IX_blood_disposal_items_bag_id"" ON blood_disposal_items(bag_id);

            CREATE TABLE IF NOT EXISTS blood_bag_status_logs (
                log_id SERIAL PRIMARY KEY,
                bag_id integer NOT NULL,
                from_status character varying(50) NOT NULL,
                to_status character varying(50) NOT NULL,
                performed_by_id integer NULL,
                audit_log_id bigint NULL,
                notes character varying(255) NULL,
                created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT ""FK_blood_bag_status_logs_blood_bags_bag_id"" FOREIGN KEY (bag_id) REFERENCES blood_bags(bag_id) ON DELETE CASCADE,
                CONSTRAINT ""FK_blood_bag_status_logs_users_performed_by_id"" FOREIGN KEY (performed_by_id) REFERENCES users(user_id) ON DELETE SET NULL,
                CONSTRAINT ""FK_blood_bag_status_logs_audit_logs_audit_log_id"" FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS idx_blood_bag_status_logs_bag_id ON blood_bag_status_logs(bag_id);
            CREATE INDEX IF NOT EXISTS idx_blood_bag_status_logs_created_at ON blood_bag_status_logs(created_at DESC);
        ";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
