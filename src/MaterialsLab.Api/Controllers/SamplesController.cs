using MaterialsLab.Api.Data;
using MaterialsLab.Api.Models;
using MaterialsLab.Api.Dtos;
using MaterialsLab.Api.Services;
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
    private bool IsOnlyTechnician() => User.IsInRole(Roles.Technician) && !User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Reviewer);
    private Task<bool> IsAssignedAsync(Guid projectId, Guid userId) => context.ProjectTechnicians.AnyAsync(pt => pt.ProjectId == projectId && pt.TechnicianId == userId);
    private Task<Sample?> LoadSampleAsync(Guid id) => context.Samples.Include(s => s.Specimens).FirstOrDefaultAsync(s => s.Id == id);

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
        {
            return NotFound();
        }

        if (IsOnlyTechnician() && !await IsAssignedAsync(sample.ProjectId, CurrentUserId()))
        {
            return NotFound();
        }

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

    [HttpPut("{sampleId:guid}/specimens/{specimenId:guid}/result")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<SampleDto>> RecordResult(Guid sampleId, Guid specimenId, RecordSpecimenResultDto dto)
    {
        var sample = await context.Samples.Include(s => s.Specimens).FirstOrDefaultAsync(s => s.Id == sampleId);

        if (sample == null)
        {
            return NotFound(new { message = "Sample not found" });
        }

        if (!await IsAssignedAsync(sample.ProjectId, CurrentUserId()))
        {
            return NotFound(new { message = "Sample not found" });
        }
            
        if (sample.Status != SampleStatus.Draft && sample.Status != SampleStatus.Rejected)
        {
            return Conflict(new { message = $"Results cannot be entered while the sample is {sample.Status}." });
        }
            
        var specimen = sample.Specimens.FirstOrDefault(sp => sp.Id == specimenId);
        if (specimen == null)
        {
            return NotFound(new { message = "Specimen not found in this sample" });
        }
            
        if (dto.MaxLoadLbf * 10 % 1 != 0)
        {
            return BadRequest(new { message = "Max load allows up to one decimal place." });
        }

        if (dto.TestedAt > DateTimeOffset.UtcNow)
        {
            return BadRequest(new { message = "TestedAt cannot be in the future." });
        }
            
        if (!StrengthCalculator.IsWithinTolerance(sample.SampledAt, specimen.Age, dto.TestedAt))
        {
            return BadRequest(new { message = $"Test is outside the tolerance window for a {(int)specimen.Age}-day specimen." });
        }
            
        specimen.MaxLoadLbf = dto.MaxLoadLbf;
        specimen.TestedAt = dto.TestedAt;
        specimen.StrengthPsi = StrengthCalculator.CalculateStrengthPsi(specimen.DiameterIn, dto.MaxLoadLbf);

        sample.Result = StrengthCalculator.CalculateResult(sample);

        await context.SaveChangesAsync();
        return ToDto(sample);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<SampleDto>> Submit(Guid id)
    {
        var sample = await LoadSampleAsync(id);
        if (sample == null || !await IsAssignedAsync(sample.ProjectId, CurrentUserId()))
        {
            return NotFound(new { message = "Sample not found" });
        }
            
        if (sample.Status != SampleStatus.Draft && sample.Status != SampleStatus.Rejected)
        {
            return Conflict(new { message = $"Only Draft or Rejected samples can be submitted (current: {sample.Status})." });
        }
            
        if (sample.Specimens.Any(sp => sp.StrengthPsi == null))
        {
            return Conflict(new { message = "All specimens (7-day and 28-day) must have results before submitting." });
        }
            
        sample.Status = SampleStatus.Submitted;
        await context.SaveChangesAsync();
        return ToDto(sample);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Reviewer)]
    public Task<ActionResult<SampleDto>> Approve(Guid id) => Review(id, SampleStatus.Approved);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Reviewer)]
    public Task<ActionResult<SampleDto>> Reject(Guid id) => Review(id, SampleStatus.Rejected);

    private async Task<ActionResult<SampleDto>> Review(Guid id, SampleStatus newStatus)
    {
        var sample = await LoadSampleAsync(id);
        if (sample == null)
            return NotFound(new { message = "Sample not found" });

        if (sample.Status != SampleStatus.Submitted)
            return Conflict(new { message = $"Only Submitted samples can be reviewed (current: {sample.Status})." });

        sample.Status = newStatus;
        await context.SaveChangesAsync();
        return ToDto(sample);
    }

    // Technician Samples from projects
    [HttpGet]
    public async Task<ActionResult<List<SampleDto>>> GetAll(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? mixDesignId,
        [FromQuery] SampleStatus? status,
        [FromQuery] DateTimeOffset? sampledFrom,
        [FromQuery] DateTimeOffset? sampledTo
    )
    {
        var query = context.Samples.Include(s => s.Specimens).AsQueryable();
        if (IsOnlyTechnician())
        {
            var userId = CurrentUserId();
            query = query.Where(s => s.Project.ProjectTechnicians.Any(pt => pt.TechnicianId == userId));
        }

        if (projectId.HasValue)   query = query.Where(s => s.ProjectId == projectId);
        if (mixDesignId.HasValue) query = query.Where(s => s.MixDesignId == mixDesignId);
        if (status.HasValue)      query = query.Where(s => s.Status == status);
        if (sampledFrom.HasValue) query = query.Where(s => s.SampledAt >= sampledFrom);
        if (sampledTo.HasValue)   query = query.Where(s => s.SampledAt <= sampledTo);

        var samples = await query.OrderByDescending(s => s.SampledAt).ToListAsync();
        return samples.Select(ToDto).ToList();
    }
}