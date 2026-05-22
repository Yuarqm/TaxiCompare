// ─────────────────────────────────────────────────────────────────────────────
// MIGRATION: 20240101000000_InitialCreate
//
// To generate this migration for real, run:
//   dotnet ef migrations add InitialCreate \
//     --project src/Infrastructure \
//     --startup-project src/Gateway \
//     --output-dir Persistence/Migrations
//
// To apply:
//   dotnet ef database update \
//     --project src/Infrastructure \
//     --startup-project src/Gateway
//
// Or via Docker:
//   docker compose run --rm gateway dotnet ef database update
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiCompare.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Users
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(nullable: false),
                FirstName = table.Column<string>(maxLength: 100, nullable: false),
                LastName = table.Column<string>(maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(maxLength: 20, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                LastLoginAt = table.Column<DateTime>(nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                Preferences_PreferredCurrency = table.Column<string>(maxLength: 3, nullable: false, defaultValue: "EUR"),
                Preferences_PriceAlertEnabled = table.Column<bool>(nullable: false, defaultValue: true),
                Preferences_PriceAlertThreshold = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Preferences_EmailNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                Preferences_PushNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                Preferences_PreferredVehicleClass = table.Column<string>(nullable: false, defaultValue: "Economy")
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);

        // Providers
        migrationBuilder.CreateTable(
            name: "Providers",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Slug = table.Column<string>(maxLength: 50, nullable: false),
                LogoUrl = table.Column<string>(nullable: false),
                Rating = table.Column<double>(nullable: false),
                IsActive = table.Column<bool>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Providers", x => x.Id));

        migrationBuilder.CreateIndex("IX_Providers_Slug", "Providers", "Slug", unique: true);

        // Seed providers
        migrationBuilder.InsertData("Providers", new[] { "Id", "Name", "Slug", "LogoUrl", "Rating", "IsActive" }, new object[,]
        {
            { Guid.Parse("11111111-1111-1111-1111-111111111111"), "Uber",     "uber",    "/logos/uber.svg",    4.7, true },
            { Guid.Parse("22222222-2222-2222-2222-222222222222"), "Yandex Taxi", "yandex", "/logos/yandex.svg", 4.6, true },
            { Guid.Parse("33333333-3333-3333-3333-333333333333"), "Bolt",     "bolt",    "/logos/bolt.svg",    4.5, true },
            { Guid.Parse("44444444-4444-4444-4444-444444444444"), "FREE NOW", "freenow", "/logos/freenow.svg", 4.4, true }
        });

        // RideRequests
        migrationBuilder.CreateTable(
            name: "RideRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                OriginAddress = table.Column<string>(maxLength: 500, nullable: false),
                OriginLat = table.Column<double>(nullable: false),
                OriginLng = table.Column<double>(nullable: false),
                DestinationAddress = table.Column<string>(maxLength: 500, nullable: false),
                DestinationLat = table.Column<double>(nullable: false),
                DestinationLng = table.Column<double>(nullable: false),
                RequestedAt = table.Column<DateTime>(nullable: false),
                Status = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RideRequests", x => x.Id);
                table.ForeignKey("FK_RideRequests_Users", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        // PriceSnapshots
        migrationBuilder.CreateTable(
            name: "PriceSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RideRequestId = table.Column<Guid>(nullable: false),
                ProviderId = table.Column<Guid>(nullable: false),
                Price = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(maxLength: 3, nullable: false),
                EtaMinutes = table.Column<int>(nullable: false),
                VehicleClass = table.Column<string>(maxLength: 50, nullable: false),
                SurgeMultiplier = table.Column<double>(nullable: false),
                RecordedAt = table.Column<DateTime>(nullable: false),
                IsAvailable = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                table.ForeignKey("FK_PriceSnapshots_RideRequests", x => x.RideRequestId, "RideRequests", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PriceSnapshots_Providers", x => x.ProviderId, "Providers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_PriceSnapshots_Route", "PriceSnapshots",
            new[] { "RideRequestId", "ProviderId", "RecordedAt" });

        // Notifications
        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                Title = table.Column<string>(nullable: false),
                Message = table.Column<string>(nullable: false),
                Type = table.Column<int>(nullable: false),
                IsRead = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey("FK_Notifications_Users", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Notifications");
        migrationBuilder.DropTable("PriceSnapshots");
        migrationBuilder.DropTable("RideRequests");
        migrationBuilder.DropTable("Providers");
        migrationBuilder.DropTable("Users");
    }
}
