using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Appdev_Group_8.Migrations
{
    /// <inheritdoc />
    public partial class ClaimMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FinderNotified",
                table: "Claims",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinderNotified",
                table: "Claims");
        }
    }
}
