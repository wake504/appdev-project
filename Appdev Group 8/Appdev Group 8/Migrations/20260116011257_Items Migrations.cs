using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Appdev_Group_8.Migrations
{
    /// <inheritdoc />
    public partial class ItemsMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CollectionDate",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionLocation",
                table: "Claims",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectionDate",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "CollectionLocation",
                table: "Claims");
        }
    }
}
