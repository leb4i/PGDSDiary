using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_ClassId",
                table: "ScheduleSlots",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_GradedAt",
                table: "Grades",
                column: "GradedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId",
                table: "Attendances",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduleSlots_ClassId",
                table: "ScheduleSlots");

            migrationBuilder.DropIndex(
                name: "IX_Grades_GradedAt",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId",
                table: "Attendances");
        }
    }
}
