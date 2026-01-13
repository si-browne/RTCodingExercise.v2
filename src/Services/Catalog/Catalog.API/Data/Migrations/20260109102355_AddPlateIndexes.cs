using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Plates",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Registration",
                table: "Plates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Letters",
                table: "Plates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plates_Letters",
                table: "Plates",
                column: "Letters");

            migrationBuilder.CreateIndex(
                name: "IX_Plates_Numbers",
                table: "Plates",
                column: "Numbers");

            migrationBuilder.CreateIndex(
                name: "IX_Plates_Registration",
                table: "Plates",
                column: "Registration");

            migrationBuilder.CreateIndex(
                name: "IX_Plates_SalePrice",
                table: "Plates",
                column: "SalePrice");

            migrationBuilder.CreateIndex(
                name: "IX_Plates_Status",
                table: "Plates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Plates_Status_SalePrice",
                table: "Plates",
                columns: new[] { "Status", "SalePrice" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Plates_Letters",
                table: "Plates");

            migrationBuilder.DropIndex(
                name: "IX_Plates_Numbers",
                table: "Plates");

            migrationBuilder.DropIndex(
                name: "IX_Plates_Registration",
                table: "Plates");

            migrationBuilder.DropIndex(
                name: "IX_Plates_SalePrice",
                table: "Plates");

            migrationBuilder.DropIndex(
                name: "IX_Plates_Status",
                table: "Plates");

            migrationBuilder.DropIndex(
                name: "IX_Plates_Status_SalePrice",
                table: "Plates");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Plates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Registration",
                table: "Plates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Letters",
                table: "Plates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
