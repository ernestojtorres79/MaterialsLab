using System.ComponentModel.DataAnnotations;

namespace MaterialsLab.Api.Dtos;

public record CreateMixDesignDto
{
    [Required, MaxLength(15)]
    public string Code { get; init; } = null!;

    [MaxLength(250)]
    public string? Description { get; init; }
    
    [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0.")]
    public int RequiredStrengthPsi { get; init; }
}

public record MixDesignDto
{
    public Guid Id{ get; init;}
    public string Code { get; init; } = null!;
    public string? Description { get; init; }
    public int RequiredStrengthPsi { get; init; }
    public bool IsActive { get; init; }
}

