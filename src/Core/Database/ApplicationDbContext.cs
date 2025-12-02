using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Source.Core;
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
        public DbSet<AuditEvent> AuditEvents { get; set; } = null!;
        public DbSet<FraudAlert> FraudAlerts { get; set; } = null!;
        public DbSet<TransactionMetric> TransactionMetrics { get; set; } = null!;

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

            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.ToTable("audit_events");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.EventId)
                    .HasColumnName("event_id")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.EventType)
                    .HasColumnName("event_type")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.EventSource)
                    .HasColumnName("event_source")
                    .HasMaxLength(500)
                    .IsRequired();
                
                entity.Property(e => e.EventSubject)
                    .HasColumnName("event_subject")
                    .HasMaxLength(500);
                
                entity.Property(e => e.EventData)
                    .HasColumnName("event_data")
                    .HasColumnType("text");
                
                entity.Property(e => e.EventTime)
                    .HasColumnName("event_time")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.RecordedAt)
                    .HasColumnName("recorded_at")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();

                entity.HasIndex(e => e.EventType)
                    .HasDatabaseName("idx_audit_event_type");
                
                entity.HasIndex(e => e.RecordedAt)
                    .HasDatabaseName("idx_audit_recorded_at");
            });

            modelBuilder.Entity<FraudAlert>(entity =>
            {
                entity.ToTable("fraud_alerts");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.TransactionId)
                    .HasColumnName("transaction_id")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.RiskScore)
                    .HasColumnName("risk_score")
                    .IsRequired();
                
                entity.Property(e => e.Alerts)
                    .HasColumnName("alerts")
                    .HasColumnType("text")
                    .IsRequired();
                
                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
                
                entity.Property(e => e.DetectedAt)
                    .HasColumnName("detected_at")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("idx_fraud_transaction_id");
                
                entity.HasIndex(e => e.RiskScore)
                    .HasDatabaseName("idx_fraud_risk_score");
                
                entity.HasIndex(e => e.DetectedAt)
                    .HasDatabaseName("idx_fraud_detected_at");
            });

            modelBuilder.Entity<TransactionMetric>(entity =>
            {
                entity.ToTable("transaction_metrics");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.MetricDate)
                    .HasColumnName("metric_date")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();
                
                entity.Property(e => e.Hour)
                    .HasColumnName("hour")
                    .IsRequired();
                
                entity.Property(e => e.DayOfWeek)
                    .HasColumnName("day_of_week")
                    .HasMaxLength(20)
                    .IsRequired();
                
                entity.Property(e => e.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
                
                entity.Property(e => e.TransactionCount)
                    .HasColumnName("transaction_count")
                    .IsRequired();
                
                entity.Property(e => e.TotalVolume)
                    .HasColumnName("total_volume")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.AverageAmount)
                    .HasColumnName("average_amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(e => e.SuccessCount)
                    .HasColumnName("success_count")
                    .IsRequired();
                
                entity.Property(e => e.FailureCount)
                    .HasColumnName("failure_count")
                    .IsRequired();
                
                entity.Property(e => e.LastUpdated)
                    .HasColumnName("last_updated")
                    .HasConversion(dateTimeConverter)
                    .IsRequired();

                entity.HasIndex(e => new { e.MetricDate, e.Hour, e.Currency })
                    .IsUnique()
                    .HasDatabaseName("idx_metric_date_hour_currency");
            });
        }
    }
}
