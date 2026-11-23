using MaterialsLab.Api.Data;
using MaterialsLab.Api.Models;
using MaterialsLab.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaterialsLab.Api.Controllers;

[ApiController]
[Route("api/mixdesigns")]
[Authorize]
public class MixDesignsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IList<MixDesignDto>>> GetAll()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Reviewer"))
        {
            var mixDesigns = await context.MixDesigns.AsQueryable().Select(md => ToDto(md)).ToListAsync();
            return Ok(mixDesigns);
        }
        if (User.IsInRole("Technician"))
        {
            var mixDesigns = await context.MixDesigns.AsQueryable().Where(md => md.IsActive).Select(md => ToDto(md)).ToListAsync();
            return Ok(mixDesigns);
        }
        else
        {
            return Forbid();
        }
        
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MixDesignDto>> GetById(Guid id)
    {
        var mixDesign = await context.MixDesigns.FindAsync(id);
        if (mixDesign == null)
        {
            return NotFound();
        }
        if (User.IsInRole("Admin") || User.IsInRole("Reviewer"))
        {
            return Ok(ToDto(mixDesign));
        }
        if (User.IsInRole("Technician") && mixDesign.IsActive)
        {
            return Ok(ToDto(mixDesign));
        }
        else
        {
            return NotFound();
        }
        
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<MixDesignDto>> Create(CreateMixDesignDto createMixDesignDto)
    {
        var codeExists = await context.MixDesigns.AnyAsync(md => md.Code == createMixDesignDto.Code);
        if (codeExists)
        {
            return Conflict(new { message = "A Mix Design with the same code already exists." });
        }
        else
        {
            var mixDesign = new MixDesign
            {
                Code = createMixDesignDto.Code,
                Description = createMixDesignDto.Description,
                RequiredStrengthPsi = createMixDesignDto.RequiredStrengthPsi,
                IsActive = true
            };

            context.MixDesigns.Add(mixDesign);
            await context.SaveChangesAsync();

            var mixDesignDto = new MixDesignDto
            {
                Id = mixDesign.Id,
                Code = mixDesign.Code,
                Description = mixDesign.Description,
                RequiredStrengthPsi = mixDesign.RequiredStrengthPsi,
                IsActive = mixDesign.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = mixDesign.Id }, mixDesignDto);
        }
    }

    [HttpPut("{id:guid}/inactivate")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Inactivate(Guid id)
    {
        var mixDesign = await context.MixDesigns.FindAsync(id);
        if (mixDesign == null)
        {
            return NotFound();
        }

        mixDesign.IsActive = false;
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var mixDesign = await context.MixDesigns.FindAsync(id);
        if (mixDesign == null)
        {
            return NotFound();
        }

        if (await context.Samples.AnyAsync(s => s.MixDesignId == id))
        {
            return Conflict(new { message = "Cannot delete Mix Design with associated Samples. Inactivate it instead." });
        }

        context.MixDesigns.Remove(mixDesign);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static MixDesignDto ToDto(MixDesign m)=> new MixDesignDto()
    {
        Id = m.Id,
        Code = m.Code,
        Description = m.Description,
        RequiredStrengthPsi = m.RequiredStrengthPsi,
        IsActive = m.IsActive
    };
}