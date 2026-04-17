using Microsoft.EntityFrameworkCore;
using queue_backend.Models;

namespace queue_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<QueueCounter> QueueCounters => Set<QueueCounter>();

    public DbSet<QueueItem> Queues => Set<QueueItem>();

    public override int SaveChanges()
    {
        PrepareBaseModelTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareBaseModelTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareBaseModelTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareBaseModelTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QueueCounter>(entity =>
        {
            entity.ToTable("queue_counter");

            entity.HasKey(e => e.ID);

            entity.Property(e => e.ID)
                .HasColumnName("id");

            entity.Property(e => e.CurrentPrefix)
                .HasColumnName("current_prefix")
                .HasColumnType("char(1)")
                .HasDefaultValue("A");

            entity.Property(e => e.CurrentNumber)
                .HasColumnName("current_number")
                .HasDefaultValue(0);

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Valid)
                .HasColumnName("valid")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<QueueItem>(entity =>
        {
            entity.ToTable("queues");

            entity.HasKey(e => e.ID);

            entity.Property(e => e.ID)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();

            entity.Property(e => e.QueueNumber)
                .HasColumnName("queue_number")
                .HasMaxLength(3);

            entity.Property(e => e.Prefix)
                .HasColumnName("prefix")
                .HasColumnType("char(1)");

            entity.Property(e => e.RunningNumber)
                .HasColumnName("running_number");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("waiting");

            entity.Property(e => e.UpdatedAt)
                 .HasColumnName("updated_at")
                 .HasColumnType("timestamp")
                 .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Valid)
                .HasColumnName("valid")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private void PrepareBaseModelTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseModel>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                }

                entry.Entity.Valid = true;
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                entry.Entity.CreatedAt = NormalizeTimestamp(entry.Entity.CreatedAt);
            }
        }
    }

    private static DateTime NormalizeTimestamp(DateTime value)
    {
        if (value == default)
        {
            return value;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }
}
