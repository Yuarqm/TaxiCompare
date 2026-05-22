using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiCompare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderedFieldsToRideRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderedProviderName",
                table: "RideRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderedProviderSlug",
                table: "RideRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderedVehicleClass",
                table: "RideRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderedPrice",
                table: "RideRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OrderedProviderName", table: "RideRequests");
            migrationBuilder.DropColumn(name: "OrderedProviderSlug", table: "RideRequests");
            migrationBuilder.DropColumn(name: "OrderedVehicleClass", table: "RideRequests");
            migrationBuilder.DropColumn(name: "OrderedPrice", table: "RideRequests");
            migrationBuilder.DropColumn(name: "OrderedAt", table: "RideRequests");
        }
    }
}
