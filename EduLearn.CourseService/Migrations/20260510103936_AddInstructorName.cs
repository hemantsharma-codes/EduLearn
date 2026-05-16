using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.CourseService.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorName",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorName",
                table: "Courses");
        }
    }
}
