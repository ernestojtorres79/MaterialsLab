namespace MaterialsLab.Api.Models;

public class Sample
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int Sequence { get; set; }
    public string Code { get; set; } = string.Empty;
    public int RequiredStrengthPsi { get; set; }
    public DateTimeOffset SampledAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public SampleStatus Status { get; set; } = SampleStatus.Draft;
    public SampleResult? Result { get; set; }
    public Guid MixDesignId { get; set; }
    public MixDesign MixDesign { get; set; } = null!;
    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();

}