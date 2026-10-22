using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.SharedKernel.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelledByAndCancelledAtToDonationCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "donation_campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cancelled_by",
                table: "donation_campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_donation_campaigns_cancelled_by",
                table: "donation_campaigns",
                column: "cancelled_by");

            migrationBuilder.AddForeignKey(
                name: "FK_donation_campaigns_users_cancelled_by",
                table: "donation_campaigns",
                column: "cancelled_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_donation_campaigns_users_cancelled_by",
                table: "donation_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_donation_campaigns_cancelled_by",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "donation_campaigns");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                table: "donation_campaigns");
        }
    }
}
