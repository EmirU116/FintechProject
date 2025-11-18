using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                    .IsRequired();
                
                entity.Property(e => e.ProcessedAt)
                    .HasColumnName("processed_at")
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
        }
    }
}
