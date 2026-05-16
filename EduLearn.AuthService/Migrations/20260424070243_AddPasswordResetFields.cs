using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetPasswordExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetPasswordToken",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetPasswordExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetPasswordToken",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "AvatarUrl", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "RefreshToken", "RefreshTokenExpiryTime", "Role" },
                values: new object[] { 1, null, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@edulearn.com", "System Admin", true, null, "AQAAAAIAAYagAAAAEPBP4BswP6fehwO97ykGxJfiB7fFM16H5g4kJ6ZTB5YYKDPZMeadJy1gUBWEHpR+VQ==", null, null, "ADMIN" });
        }
    }
}
