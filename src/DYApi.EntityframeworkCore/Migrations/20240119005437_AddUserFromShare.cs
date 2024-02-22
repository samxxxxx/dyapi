using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DYApi.EntityframeworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFromShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromUser",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromUser",
                table: "AspNetUsers");
        }
    }
}
