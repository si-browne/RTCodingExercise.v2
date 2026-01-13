namespace Catalog.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Plate> Plates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Plate>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configure decimal precision for prices
                entity.Property(e => e.PurchasePrice)
                    .HasPrecision(18, 2);

                entity.Property(e => e.SalePrice)
                    .HasPrecision(18, 2);

                entity.Property(e => e.SoldPrice)
                    .HasPrecision(18, 2);

                // Configure enum as integer
                entity.Property(e => e.Status)
                    .HasConversion<int>();

                // Configure nullable dates
                entity.Property(e => e.ReservedDate)
                    .IsRequired(false);

                entity.Property(e => e.SoldDate)
                    .IsRequired(false);

                // Set default status for new plates
                entity.Property(e => e.Status)
                    .HasDefaultValue(PlateStatus.ForSale);

                // Add indexes for better query performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Letters);
                entity.HasIndex(e => e.Numbers);
                entity.HasIndex(e => e.Registration);
                entity.HasIndex(e => e.SalePrice);
                
                // Composite index for common filter combinations
                entity.HasIndex(e => new { e.Status, e.SalePrice });
            });
        }
    }
}
