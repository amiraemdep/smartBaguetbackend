using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBaguet.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaguetHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeBaguet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodePlant = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateEntree = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateSortie = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaguetHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodePlant = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Client = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ITEM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Baguets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeBaguet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentPlantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baguets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Baguets_Plants_CurrentPlantId",
                        column: x => x.CurrentPlantId,
                        principalTable: "Plants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Baguets_CurrentPlantId",
                table: "Baguets",
                column: "CurrentPlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaguetHistories");

            migrationBuilder.DropTable(
                name: "Baguets");

            migrationBuilder.DropTable(
                name: "Plants");
        }
    }
}
