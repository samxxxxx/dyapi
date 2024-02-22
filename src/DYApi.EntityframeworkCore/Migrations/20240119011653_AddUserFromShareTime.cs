using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DYApi.EntityframeworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFromShareTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FromShareTime",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromShareTime",
                table: "AspNetUsers");
        }
    }
}
