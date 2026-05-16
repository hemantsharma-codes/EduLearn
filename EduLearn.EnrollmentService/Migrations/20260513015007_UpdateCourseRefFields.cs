using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.EnrollmentService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourseRefFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "CourseRefs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                column: "CourseId");

            // Clean up orphan enrollments that would conflict with the FK
            migrationBuilder.Sql("DELETE FROM Enrollments WHERE CourseId NOT IN (SELECT CourseId FROM CourseRefs)");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_CourseRefs_CourseId",
                table: "Enrollments",
                column: "CourseId",
                principalTable: "CourseRefs",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_CourseRefs_CourseId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "CourseRefs");
        }
    }
}
