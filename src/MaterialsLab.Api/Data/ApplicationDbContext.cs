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
        builder.Entity<Project>(e =>
        {
            e.Property(p => p.Code).IsRequired().HasMaxLength(15);
            e.HasIndex(p => p.Code).IsUnique();
        });
    }

    public DbSet<Sample> Samples { get; set; }
    public DbSet<Specimen> Specimens { get; set; }
    public DbSet<MixDesign> MixDesigns { get; set; }
    public DbSet<ProjectTechnician> ProjectTechnicians { get; set; }
    public DbSet<Project> Projects { get; set; }
}