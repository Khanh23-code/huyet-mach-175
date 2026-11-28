using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeNotificationAndDeliverySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_departments_target_department_id",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_roles_target_role_id",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_users_recipient_user_id",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "idx_notifications_recipient_unread",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "idx_notifications_target_role",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "IX_notification_target_department_id",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "message",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "reference_id",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "reference_type",
                table: "notification");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "notification",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "target_role_id",
                table: "notification",
                newName: "source_event_id");

            migrationBuilder.RenameColumn(
                name: "target_department_id",
                table: "notification",
                newName: "resource_id");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "notification",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<Guid>(
                name: "recipient_user_id",
                table: "notification",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "action_path",
                table: "notification",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deduplication_key",
                table: "notification",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "notification",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_type",
                table: "notification",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "notification",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                table: "notification",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_module",
                table: "notification",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "notification_delivery",
                columns: table => new
                {
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Email"),
                    destination = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error_message = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery", x => x.delivery_id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "donor_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notification_delivery_notification_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notification",
                        principalColumn: "notification_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_notification_recipient_inbox",
                table: "notification",
                columns: new[] { "recipient_user_id", "created_at", "notification_id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "idx_notification_recipient_unread",
                table: "notification",
                columns: new[] { "recipient_user_id", "created_at" },
                descending: new[] { false, true },
                filter: "is_read = FALSE");

            migrationBuilder.CreateIndex(
                name: "uq_notification_recipient_dedup",
                table: "notification",
                columns: new[] { "recipient_user_id", "deduplication_key" },
                unique: true,
                filter: "deduplication_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notification_delivery_donor",
                table: "notification_delivery",
                column: "donor_id",
                filter: "donor_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notification_delivery_queue",
                table: "notification_delivery",
                columns: new[] { "next_attempt_at", "delivery_id" },
                filter: "status IN ('Pending', 'RetryScheduled', 'Processing')");

            migrationBuilder.CreateIndex(
                name: "uq_notification_delivery_dedup",
                table: "notification_delivery",
                columns: new[] { "channel", "destination", "deduplication_key" },
                unique: true,
                filter: "deduplication_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_delivery_notif_dest",
                table: "notification_delivery",
                columns: new[] { "notification_id", "channel", "destination" },
                unique: true,
                filter: "notification_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_users_recipient_user_id",
                table: "notification",
                column: "recipient_user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_users_recipient_user_id",
                table: "notification");

            migrationBuilder.DropTable(
                name: "notification_delivery");

            migrationBuilder.DropIndex(
                name: "idx_notification_recipient_inbox",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "idx_notification_recipient_unread",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "uq_notification_recipient_dedup",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "action_path",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "deduplication_key",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "notification_type",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "resource_type",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "source_module",
                table: "notification");

            migrationBuilder.RenameColumn(
                name: "source_event_id",
                table: "notification",
                newName: "target_role_id");

            migrationBuilder.RenameColumn(
                name: "resource_id",
                table: "notification",
                newName: "target_department_id");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "notification",
                newName: "type");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "notification",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "recipient_user_id",
                table: "notification",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "notification",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "reference_id",
                table: "notification",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_type",
                table: "notification",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_recipient_unread",
                table: "notification",
                columns: new[] { "recipient_user_id", "is_read" },
                filter: "is_read = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_target_role",
                table: "notification",
                column: "target_role_id",
                filter: "target_role_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notification_target_department_id",
                table: "notification",
                column: "target_department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_departments_target_department_id",
                table: "notification",
                column: "target_department_id",
                principalTable: "departments",
                principalColumn: "department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_roles_target_role_id",
                table: "notification",
                column: "target_role_id",
                principalTable: "roles",
                principalColumn: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_users_recipient_user_id",
                table: "notification",
                column: "recipient_user_id",
                principalTable: "users",
                principalColumn: "user_id");
        }
    }
}
