using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQCScanner.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLoggedInToEmpModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "token",
                table: "LoginTeken",
                newName: "Token");

            migrationBuilder.RenameColumn(
                name: "rele",
                table: "LoginTeken",
                newName: "role");

            migrationBuilder.AlterColumn<string>(
                name: "EmpId",
                table: "empModels",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "empModels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLoggedIn",
                table: "empModels",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "empModels");

            migrationBuilder.DropColumn(
                name: "IsLoggedIn",
                table: "empModels");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "LoginTeken",
                newName: "token");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "LoginTeken",
                newName: "rele");

            migrationBuilder.AlterColumn<int>(
                name: "EmpId",
                table: "empModels",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
