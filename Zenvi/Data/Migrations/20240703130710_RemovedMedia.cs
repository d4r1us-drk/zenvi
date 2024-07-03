using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Zenvi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Media_BannerPictureMediaID",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Media_ProfilePictureMediaID",
                table: "User");

            migrationBuilder.DropTable(
                name: "Media");

            migrationBuilder.DropIndex(
                name: "IX_User_BannerPictureMediaID",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_ProfilePictureMediaID",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BannerPictureMediaID",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ProfilePictureMediaID",
                table: "User");

            migrationBuilder.AddColumn<byte[]>(
                name: "BannerPicture",
                table: "User",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "User",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerPicture",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "User");

            migrationBuilder.AddColumn<int>(
                name: "BannerPictureMediaID",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfilePictureMediaID",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Media",
                columns: table => new
                {
                    MediaID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MediaBlob = table.Column<byte[]>(type: "bytea", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Media", x => x.MediaID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_BannerPictureMediaID",
                table: "User",
                column: "BannerPictureMediaID");

            migrationBuilder.CreateIndex(
                name: "IX_User_ProfilePictureMediaID",
                table: "User",
                column: "ProfilePictureMediaID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Media_BannerPictureMediaID",
                table: "User",
                column: "BannerPictureMediaID",
                principalTable: "Media",
                principalColumn: "MediaID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Media_ProfilePictureMediaID",
                table: "User",
                column: "ProfilePictureMediaID",
                principalTable: "Media",
                principalColumn: "MediaID");
        }
    }
}
