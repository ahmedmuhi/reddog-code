using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedDog.AccountingModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    LoyaltyId = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.LoyaltyId);
                });

            migrationBuilder.CreateTable(
                name: "StoreLocation",
                columns: table => new
                {
                    StoreId = table.Column<string>(type: "nvarchar(54)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    StateProvince = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(54)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(12,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(12,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLocation", x => x.StoreId);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    PlacedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerLoyaltyId = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Order_Customer_CustomerLoyaltyId",
                        column: x => x.CustomerLoyaltyId,
                        principalTable: "Customer",
                        principalColumn: "LoyaltyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_CustomerLoyaltyId",
                table: "Order",
                column: "CustomerLoyaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "StoreLocation");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Customer");
        }
    }
}
