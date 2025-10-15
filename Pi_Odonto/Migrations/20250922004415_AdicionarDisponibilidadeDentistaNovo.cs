using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pi_Odonto.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDisponibilidadeDentistaNovo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disponibilidade_dentista",
                columns: table => new
                {
                    id_disponibilidade = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id_dentista = table.Column<int>(type: "int", nullable: false),
                    dia_semana = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hora_inicio = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    hora_fim = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disponibilidade_dentista", x => x.id_disponibilidade);
                    table.ForeignKey(
                        name: "FK_disponibilidade_dentista_dentista_id_dentista",
                        column: x => x.id_dentista,
                        principalTable: "dentista",
                        principalColumn: "id_dentista",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_disponibilidade_dentista_id_dentista",
                table: "disponibilidade_dentista",
                column: "id_dentista");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disponibilidade_dentista");
        }
    }
}
