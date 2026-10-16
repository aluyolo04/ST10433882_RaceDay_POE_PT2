using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.RoleId);
            e.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.RoleName).IsUnique();
            e.HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Organiser" },
                new Role { RoleId = 3, RoleName = "Participant" });
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(150);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Role).WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).IsRequired().HasMaxLength(200);
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(20).HasDefaultValue("Run");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_MaxParticipants", "[MaxParticipants] > 0"));
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(c => c.DistanceKm).HasPrecision(5, 2);
            e.Property(c => c.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.Property(c => c.MaxParticipants).HasDefaultValue(100);
            e.HasIndex(c => new { c.EventId, c.CategoryName }).IsUnique();
            e.HasOne(c => c.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Confirmed");
            e.HasIndex(x => new { x.CategoryId, x.ParticipantId }).IsUnique();
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(r => r.ResultId);
            e.Property(r => r.FinishTime).HasColumnType("time");
            e.Property(r => r.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Finished");
            e.HasIndex(r => r.EnrolmentId).IsUnique();
            e.HasOne(r => r.Enrolment).WithOne(x => x.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId).OnDelete(DeleteBehavior.NoAction);
        });
    }
} 
