using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.EnrollmentService.Migrations
{
    /// <inheritdoc />
    public partial class FixCourseRefIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop only if exists to avoid errors
            migrationBuilder.Sql("IF OBJECT_ID('CourseRefs', 'U') IS NOT NULL DROP TABLE CourseRefs;");

            // Recreate it without Identity
            migrationBuilder.CreateTable(
                name: "CourseRefs",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRefs", x => x.CourseId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CourseId",
                table: "CourseRefs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
