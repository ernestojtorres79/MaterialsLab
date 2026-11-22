using System.ComponentModel.DataAnnotations;

namespace MaterialsLab.Api.Dtos;

public record LoginRequestDto
{
    [Required]
    public string Username { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;
}

public record LoginResponseDto
{
    public string Token { get; init; } = null!;
    public string Username { get; init; } = null!;
    public IList<string> Roles { get; init; } = new List<string>();
}