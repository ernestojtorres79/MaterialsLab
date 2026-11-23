using System.Security.Claims;
using MaterialsLab.Api.Data;
using MaterialsLab.Api.Dtos;
using MaterialsLab.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaterialsLab.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController(ApplicationDbContext context) : ControllerBase
{
    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsOnlyTechnician() => User.IsInRole(Roles.Technician) && !User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Reviewer);

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        var query = context.Projects.AsQueryable();
        if (IsOnlyTechnician())
        {
            var userId = CurrentUserId();
            query = query.Where(p => p.ProjectTechnicians.Any(pt => pt.TechnicianId == userId));
        }
        
        return await query.OrderBy(p => p.Code).Select(p => new ProjectDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            IsActive = p.IsActive,
            TechnicianIds = p.ProjectTechnicians.Select(pt => pt.TechnicianId).ToList()
        }).ToListAsync();
    }
}