using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixProductSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_search",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_search",
                table: "products",
                columns: new[] { "name", "description" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_search",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_search",
                table: "products",
                columns: new[] { "name", "description" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }
    }
}
