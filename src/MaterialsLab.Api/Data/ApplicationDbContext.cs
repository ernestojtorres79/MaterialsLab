using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MaterialsLab.Api.Models;

namespace MaterialsLab.Api.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
            e.Property(u => u.MiddleName).HasMaxLength(100);
        });
        builder.Entity<AppRole>(e => e.Property(r => r.Description).HasMaxLength(250));
        builder.Entity<Project>(e =>
        {
            e.Property(p => p.Code).HasMaxLength(15);
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Name).HasMaxLength(150);
        });
        builder.Entity<MixDesign>(e =>
        {
            e.Property(m => m.Code).HasMaxLength(15);
            e.HasIndex(m => m.Code).IsUnique();
            e.Property(m => m.Description).HasMaxLength(250);
        });
        builder.Entity<Sample>(e =>
        {
            e.Property(s => s.Code).HasMaxLength(20);
            e.Property(s => s.Location).HasMaxLength(200);
            e.HasIndex(s => s.Code).IsUnique();
            e.HasIndex(s => new { s.ProjectId, s.Sequence }).IsUnique();
            e.HasOne(s => s.MixDesign)
                .WithMany(m => m.Samples)
                .HasForeignKey(s => s.MixDesignId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Project)
                .WithMany(p => p.Samples)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Specimen>(e =>
        {
            e.Property(sp => sp.DiameterIn).HasPrecision(9, 3);
            e.Property(sp => sp.MaxLoadLbf).HasPrecision(12, 1);
            e.HasOne(sp => sp.Sample)
                .WithMany(sp => sp.Specimens)
                .HasForeignKey(sp => sp.SampleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public DbSet<Sample> Samples { get; set; }
    public DbSet<Specimen> Specimens { get; set; }
    public DbSet<MixDesign> MixDesigns { get; set; }
    public DbSet<ProjectTechnician> ProjectTechnicians { get; set; }
    public DbSet<Project> Projects { get; set; }
}