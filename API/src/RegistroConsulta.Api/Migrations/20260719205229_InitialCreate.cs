using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegistroConsulta.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConvenioActivo = table.Column<bool>(type: "bit", nullable: false),
                    ConvenioVigenciaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConvenioVigenciaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CuotaDiaria = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Registros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identificador = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroRegistro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsConsulta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntidadId = table.Column<int>(type: "int", nullable: true),
                    IdentificadorConsultado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreConsultado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsConsulta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsConsulta_Entidades_EntidadId",
                        column: x => x.EntidadId,
                        principalTable: "Entidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entidades_ApiKeyHash",
                table: "Entidades",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogsConsulta_EntidadId_FechaHora",
                table: "LogsConsulta",
                columns: new[] { "EntidadId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Registros_Identificador_Nombre",
                table: "Registros",
                columns: new[] { "Identificador", "Nombre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsConsulta");

            migrationBuilder.DropTable(
                name: "Registros");

            migrationBuilder.DropTable(
                name: "Entidades");
        }
    }
}
