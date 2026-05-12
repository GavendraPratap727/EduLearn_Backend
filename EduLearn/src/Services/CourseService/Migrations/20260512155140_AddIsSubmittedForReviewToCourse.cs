using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.CourseService.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSubmittedForReviewToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubmittedForReview",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubmittedForReview",
                table: "Courses");
        }
    }
}
