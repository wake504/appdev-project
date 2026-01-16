    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Appdev_Group_8.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedClaimModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'Claims') 
                    AND name = 'OwnerLostItemId'
                )
                BEGIN
                    ALTER TABLE Claims ADD OwnerLostItemId int NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerLostItemId",
                table: "Claims");
        }
    }
}
