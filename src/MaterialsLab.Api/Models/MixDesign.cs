namespace MaterialsLab.Api.Models;

public class MixDesign
{
    public Guid Id { get; set; }
    public int RequiredStrengthPsi { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
}