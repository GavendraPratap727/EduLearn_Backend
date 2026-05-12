using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserEntityForRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Users",
                newName: "AvatarUrl");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "Users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }
    }
}
