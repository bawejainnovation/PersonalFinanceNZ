using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialInsights.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContactsAndBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alias = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactAliases_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ContactId",
                table: "Transactions",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactAliases_ContactId_Alias",
                table: "ContactAliases",
                columns: new[] { "ContactId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CanonicalKey",
                table: "Contacts",
                column: "CanonicalKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Contacts_ContactId",
                table: "Transactions",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Contacts_ContactId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "ContactAliases");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ContactId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Accounts");
        }
    }
}
