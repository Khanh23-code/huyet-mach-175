using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuyetMach175.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffCodeToUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "staff_code",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE users SET staff_code = 'NV' || LPAD(user_id::text, 5, '0') WHERE staff_code = '' OR staff_code IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_users_staff_code",
                table: "users",
                column: "staff_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_staff_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "staff_code",
                table: "users");
        }
    }
}
