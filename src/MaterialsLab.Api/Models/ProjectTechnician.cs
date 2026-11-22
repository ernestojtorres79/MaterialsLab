using Microsoft.EntityFrameworkCore;

namespace MaterialsLab.Api.Models;

[PrimaryKey(nameof(ProjectId), nameof(TechnicianId))]
public class ProjectTechnician
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid TechnicianId { get; set; }
    public AppUser Technician { get; set; } = null!;
}