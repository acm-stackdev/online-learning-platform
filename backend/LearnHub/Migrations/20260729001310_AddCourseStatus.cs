using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Courses",
                type: "text",
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.Sql("UPDATE \"Courses\" SET \"Status\" = 'Published' WHERE \"IsPublished\" = true;");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Courses\" SET \"IsPublished\" = true WHERE \"Status\" = 'Published';");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");
        }
    }
}
