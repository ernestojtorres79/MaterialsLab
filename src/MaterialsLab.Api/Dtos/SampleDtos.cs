using System.ComponentModel.DataAnnotations;
using MaterialsLab.Api.Models;

namespace MaterialsLab.Api.Dtos;

public record CreateSampleDto
{
    [Required]
    public Guid ProjectId { get; init; }
    
    [Required]
    public Guid MixDesignId { get; init; }
    
    [Required]
    public DateTimeOffset SampledAt { get; init; }

    [Required, MaxLength(200)]
    public string Location { get; init; } = null!;

    [Required, MinLength(1)]
    public List<CreateSpecimenDto> Specimens { get; init; } = new();
}

public record CreateSpecimenDto
{
    [Required]
    public SpecimenAge Age { get; init; }

    [Range(typeof(decimal), "0.001", "1000")]
    public decimal DiameterIn { get; init; }
}

public record SampleDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public Guid ProjectId { get; init; }
    public Guid MixDesignId { get; init; }
    public int RequiredStrengthPsi { get; init; }
    public DateTimeOffset SampledAt { get; init; }
    public string Location { get; init; } = null!;
    public Guid CreatedById { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public SampleStatus Status { get; init; }
    public SampleResult? Result { get; init; }
    public List<SpecimenDto> Specimens { get; init; } = new();
}

public record SpecimenDto
{
    public Guid Id { get; init; }
    public SpecimenAge Age { get; init; }
    public decimal DiameterIn { get; init; }
    public decimal? MaxLoadLbf { get; init; }
    public DateTimeOffset? TestedAt { get; init; }
    public int? StrengthPsi { get; init; }
}

public record RecordSpecimenResultDto
{
    [Range(typeof(decimal), "0.1", "99999999")]
    public decimal MaxLoadLbf { get; init; }

    [Required]
    public DateTimeOffset TestedAt { get; init; }
}