namespace Catalog.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Plate> Plates { get; set; }

        // new "tables" based on my ERD
        public DbSet<AuditLogEvent> AuditLogEvents => Set<AuditLogEvent>(); 
        public DbSet<AuditLogEventChange> AuditLogEventChanges => Set<AuditLogEventChange>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Plate>(entity =>
            {
                entity.HasKey(e => e.Id);

                // decimal precision for prices, also, never use money in SQL server, always use decimal
                entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
                entity.Property(e => e.SalePrice).HasPrecision(18, 2);
                entity.Property(e => e.SoldPrice).HasPrecision(18, 2);

                // enum explicit to integer
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.ReservedDate).IsRequired(false); // nullable
                entity.Property(e => e.SoldDate).IsRequired(false); // nullable
                entity.Property(e => e.Status).HasDefaultValue(PlateStatus.ForSale); // default to forsale as per reqs

                // indexes
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Letters);
                entity.HasIndex(e => e.Numbers);
                entity.HasIndex(e => e.Registration);
                entity.HasIndex(e => e.SalePrice);
                entity.HasIndex(e => new { e.Status, e.SalePrice }); // composite index
            });

            // DDL & indexes for new entities
            modelBuilder.Entity<AuditLogEvent>(b =>
            {
                b.ToTable("AUDIT_LOG_EVENT");
                b.HasKey(x => x.AuditLogEventId);

                b.Property(x => x.Timestamp).IsRequired();

                b.Property(x => x.Status).HasConversion<int>().IsRequired();

                b.HasIndex(x => x.PlateId);
                b.HasIndex(x => x.Timestamp);
                b.HasIndex(x => new { x.PlateId, x.Timestamp });

                b.Property(x => x.Status).HasConversion<int>(); // enum out, was string to begin with
            });

            modelBuilder.Entity<AuditLogEventChange>(b =>
            {
                b.ToTable("AUDIT_LOG_EVENT_CHANGE");
                b.HasKey(x => x.AuditLogEventChangeId);

                b.Property(x => x.FieldName).HasMaxLength(128).IsRequired();
                b.Property(x => x.OldValue).HasMaxLength(1024).IsRequired(false); // nullable
                b.Property(x => x.NewValue).HasMaxLength(1024).IsRequired(false);

                b.HasIndex(x => x.AuditLogEventId);
            });
        }
    }
}
