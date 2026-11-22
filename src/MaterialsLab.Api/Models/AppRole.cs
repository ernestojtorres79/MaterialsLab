using Microsoft.AspNetCore.Identity;

namespace MaterialsLab.Api.Models;

public class AppRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
}