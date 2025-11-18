using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Source.Core.Transaction;

namespace Source.Core.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProcessedTransaction> ProcessedTransactions { get; set; } = null!;
        public DbSet<DummyCreditCard> CreditCards { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UTC DateTime converter for PostgreSQL compatibility
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder.Entity<ProcessedTransaction>(entity =>
            {
                entity.ToTable("processed_transactions");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.TransactionId)
                    .HasColumnName("transaction_id")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.CardNumberMasked)
                    .HasColumnName("card_number_masked")
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
                
                entity.Property(e => e.TransactionTimestamp)
                    .HasColumnName("transaction_timestamp")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.ProcessedAt)
                    .HasColumnName("processed_at")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.AuthorizationStatus)
                    .HasColumnName("authorization_status")
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.ProcessingMessage)
                    .HasColumnName("processing_message")
                    .HasMaxLength(500);

                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("idx_transaction_id");
                
                entity.HasIndex(e => e.ProcessedAt)
                    .HasDatabaseName("idx_processed_at");
            });

            modelBuilder.Entity<DummyCreditCard>(entity =>
            {
                entity.ToTable("credit_cards");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.CardNumber)
                    .HasColumnName("card_number")
                    .HasMaxLength(20)
                    .IsRequired();
                
                entity.Property(e => e.CardHolderName)
                    .HasColumnName("card_holder_name")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.Balance)
                    .HasColumnName("balance")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.CardType)
                    .HasColumnName("card_type")
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.ExpiryDate)
                    .HasColumnName("expiry_date")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.IsActive)
                    .HasColumnName("is_active")
                    .IsRequired();

                entity.HasIndex(e => e.CardNumber)
                    .IsUnique()
                    .HasDatabaseName("idx_card_number");
            });
        }
    }
}
