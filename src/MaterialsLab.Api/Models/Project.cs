namespace MaterialsLab.Api.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int NextSampleSequence { get; set; } = 1;
    public ICollection<ProjectTechnician> ProjectTechnicians { get; set; } = new List<ProjectTechnician>();
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
}