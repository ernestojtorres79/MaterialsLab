namespace MaterialsLab.Api.Dtos;

public record ProjectDto
{
    public Guid Id{ get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public List<Guid> TechnicianIds { get; init; } = new();
}