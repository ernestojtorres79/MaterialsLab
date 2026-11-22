namespace MaterialsLab.Api.Models;

public class Specimen
{
    public Guid Id { get; set; }
    public Guid SampleId { get; set; }
    public Sample Sample { get; set; } = null!;
    public SpecimenAge Age { get; set; }
    public decimal DiameterIn { get; set; }
    public decimal? MaxLoadLbf { get; set; }
    public DateTimeOffset? TestedAt { get; set; }
    public int? StrengthPsi { get; set; }
}