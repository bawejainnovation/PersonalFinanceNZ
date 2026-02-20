using FinancialInsights.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialInsights.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<TransactionAnnotation> TransactionAnnotations => Set<TransactionAnnotation>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<ContactAlias> ContactAliases => Set<ContactAlias>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AkahuAccountId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.InstitutionName).HasMaxLength(256);
            entity.Property(x => x.AccountNumber).HasMaxLength(128);
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.HasIndex(x => x.AkahuAccountId).IsUnique();

            entity.HasOne(x => x.Profile)
                .WithOne(x => x.Account)
                .HasForeignKey<AccountProfile>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NzBankKey).HasMaxLength(100);
            entity.Property(x => x.CustomDescription).HasMaxLength(512);
            entity.HasIndex(x => x.AccountId).IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AkahuTransactionId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.MerchantName).HasMaxLength(512);
            entity.Property(x => x.Reference).HasMaxLength(1024);
            entity.Property(x => x.TransactionType).HasMaxLength(128);
            entity.Property(x => x.Amount).HasPrecision(18, 2);

            entity.HasIndex(x => x.AkahuTransactionId).IsUnique();
            entity.HasIndex(x => x.TransactionDateUtc);
            entity.HasIndex(x => x.AccountId);
            entity.HasIndex(x => x.ContactId);

            entity.HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Annotation)
                .WithOne(x => x.Transaction)
                .HasForeignKey<TransactionAnnotation>(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Contact)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.CategoryType, x.Name }).IsUnique();
        });

        modelBuilder.Entity<TransactionAnnotation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasIndex(x => x.TransactionId).IsUnique();

            entity.HasOne(x => x.TransactionTypeCategory)
                .WithMany(x => x.TransactionTypeAnnotations)
                .HasForeignKey(x => x.TransactionTypeCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.SpendTypeCategory)
                .WithMany(x => x.SpendTypeAnnotations)
                .HasForeignKey(x => x.SpendTypeCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CanonicalKey).HasMaxLength(400).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Confidence).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.CanonicalKey).IsUnique();
        });

        modelBuilder.Entity<ContactAlias>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Alias).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.ContactId, x.Alias }).IsUnique();

            entity.HasOne(x => x.Contact)
                .WithMany(x => x.Aliases)
                .HasForeignKey(x => x.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
