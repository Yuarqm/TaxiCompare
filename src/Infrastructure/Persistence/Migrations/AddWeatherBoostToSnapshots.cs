using Microsoft.EntityFrameworkCore.Migrations;

namespace TaxiCompare.Infrastructure.Persistence.Migrations;

/// <summary>
/// Добавляет колонку WeatherBoost в таблицу PriceSnapshots.
/// Применить: dotnet ef database update --project src/Infrastructure --startup-project src/Gateway
/// </summary>
public partial class AddWeatherBoostToSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "WeatherBoost",
            table: "PriceSnapshots",
            type: "numeric(4,3)",
            nullable: true,
            comment: "Погодный коэффициент (null = нет наценки, 1.25 = +25%)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WeatherBoost",
            table: "PriceSnapshots");
    }
}
