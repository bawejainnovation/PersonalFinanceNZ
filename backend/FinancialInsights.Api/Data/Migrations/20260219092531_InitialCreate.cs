using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialInsights.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AkahuAccountId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InstitutionName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    NzBankKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AkahuTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Reference = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TransactionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBankTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionAnnotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionTypeCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpendTypeCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionAnnotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionAnnotations_Categories_SpendTypeCategoryId",
                        column: x => x.SpendTypeCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionAnnotations_Categories_TransactionTypeCategoryId",
                        column: x => x.TransactionTypeCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionAnnotations_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_AccountId",
                table: "AccountProfiles",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AkahuAccountId",
                table: "Accounts",
                column: "AkahuAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryType_Name",
                table: "Categories",
                columns: new[] { "CategoryType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAnnotations_SpendTypeCategoryId",
                table: "TransactionAnnotations",
                column: "SpendTypeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAnnotations_TransactionId",
                table: "TransactionAnnotations",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAnnotations_TransactionTypeCategoryId",
                table: "TransactionAnnotations",
                column: "TransactionTypeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AkahuTransactionId",
                table: "Transactions",
                column: "AkahuTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionDateUtc",
                table: "Transactions",
                column: "TransactionDateUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountProfiles");

            migrationBuilder.DropTable(
                name: "TransactionAnnotations");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
