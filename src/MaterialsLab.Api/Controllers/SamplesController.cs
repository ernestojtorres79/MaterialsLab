using MaterialsLab.Api.Data;
using MaterialsLab.Api.Models;
using MaterialsLab.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace MaterialsLab.Api.Controllers;

[ApiController]
[Route("api/samples")]
[Authorize]
public class SamplesController(ApplicationDbContext context) : ControllerBase
{
    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsOnlyTechnician() => User.IsInRole("Technician") && !User.IsInRole("Admin") && !User.IsInRole("Reviewer");
    private Task<bool> IsAssignedAsync(Guid projectId, Guid userId) => context.ProjectTechnicians.AnyAsync(pt => pt.ProjectId == projectId && pt.TechnicianId == userId);

    [HttpPost]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<SampleDto>> CreateSample(CreateSampleDto dto)
    {
        var userId = CurrentUserId();
        var project = await context.Projects.FindAsync(dto.ProjectId);

        if(project == null)
        {
            return NotFound(new{ message = "Project not found" });
        }

        if(!project.IsActive)
        {
            return Conflict(new { message = "Cannot create a sample for an inactive project." });
        }

        if(!await IsAssignedAsync(project.Id, userId))
        {
            return Forbid();
        }

        var mixDesign = await context.MixDesigns.FindAsync(dto.MixDesignId);
        if(mixDesign == null)
        {
            return NotFound(new { message = "Mix design not found" });
        }
        if(!mixDesign.IsActive)
        {
            return Conflict(new { message = "Cannot create a sample for an inactive mix design." });
        }

        if(dto.SampledAt > DateTimeOffset.UtcNow)
        {
            return BadRequest(new { message = "SampledAt cannot be in the future." });
        }

        foreach(var s in dto.Specimens)
        {
            if (!Enum.IsDefined(s.Age))
            {
                return BadRequest(new{ message = "Age of specimen must be 7 or 28 days"});
            }
            if(s.DiameterIn * 1000 % 1 != 0)
            {
                return BadRequest(new {message = "Diameter allows up to three decimal places."});
            }
        }

        if(!dto.Specimens.Any(s => s.Age == SpecimenAge.Days28))
        {
            return BadRequest(new{ message = "At least one 28-day specimen is required"});
        }

        var sequence = project.NextSampleSequence;
        project.NextSampleSequence++;
        
        var sample = new Sample
        {
            ProjectId = project.Id,
            MixDesignId = mixDesign.Id,
            Sequence = sequence,
            Code = $"{project.Code}-{sequence:D4}",
            RequiredStrengthPsi = mixDesign.RequiredStrengthPsi, 
            SampledAt = dto.SampledAt,
            Location = dto.Location,
            CreatedById = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach(var s in dto.Specimens)
        {
            sample.Specimens.Add(new Specimen
            {
                Age = s.Age,
                DiameterIn = s.DiameterIn
            });
        }

        context.Samples.Add(sample);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new {id = sample.Id}, ToDto(sample));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SampleDto>> GetById(Guid id)
    {
        var sample = await context.Samples
            .Include(s => s.Specimens)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sample == null)
            return NotFound();

        if (IsOnlyTechnician() && !await IsAssignedAsync(sample.ProjectId, CurrentUserId()))
            return NotFound();

        return ToDto(sample);
    }

    private static SampleDto ToDto(Sample s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        ProjectId = s.ProjectId,
        MixDesignId = s.MixDesignId,
        RequiredStrengthPsi = s.RequiredStrengthPsi,
        SampledAt = s.SampledAt,
        Location = s.Location,
        CreatedById = s.CreatedById,
        CreatedAt = s.CreatedAt,
        Status = s.Status,
        Result = s.Result,
        Specimens = s.Specimens
            .OrderBy(sp => sp.Age)
            .Select(sp => new SpecimenDto
            {
                Id = sp.Id,
                Age = sp.Age,
                DiameterIn = sp.DiameterIn,
                MaxLoadLbf = sp.MaxLoadLbf,
                TestedAt = sp.TestedAt,
                StrengthPsi = sp.StrengthPsi
            })
            .ToList()
    };
}