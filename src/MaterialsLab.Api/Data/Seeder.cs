using MaterialsLab.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MaterialsLab.Api.Data;

public static class Seeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        await context.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new AppRole { Name = Roles.Admin, Description = "Manages projects, mix designs and technician assignments" });

        if (!await roleManager.RoleExistsAsync(Roles.Reviewer))
            await roleManager.CreateAsync(new AppRole { Name = Roles.Reviewer, Description = "Reviews, approves or rejects samples" });

        if (!await roleManager.RoleExistsAsync(Roles.Technician))
            await roleManager.CreateAsync(new AppRole { Name = Roles.Technician, Description = "Registers samples and enters test results" });

        if (await userManager.FindByNameAsync("admin") == null)
        {
            var adminUser = new AppUser
            {
                UserName = "admin",
                Email = "admin@materialslab.test",
                FirstName = "Ernesto",
                LastName = "Torres",
                EmailConfirmed = true
            };  
            var result = await userManager.CreateAsync(adminUser, "Matrix0001!");
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }

        if (await userManager.FindByNameAsync("reviewerTech") == null)
        {
            var reviewerTechUser = new AppUser
            {
                UserName = "reviewerTech",
                Email = "reviewer.tech@materialslab.test",
                FirstName = "Reviewer",
                LastName = "Tech",
                EmailConfirmed = true
            };  
            var result = await userManager.CreateAsync(reviewerTechUser, "Matrix0001!");
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(reviewerTechUser, Roles.Reviewer);
        }

        if (await userManager.FindByNameAsync("tech1User") == null)
        {
            var tech1User = new AppUser
            {
                UserName = "tech1User",
                Email = "tech1@materialslab.test",
                FirstName = "Tech",
                LastName = "One",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(tech1User, "Matrix0001!");
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(tech1User, Roles.Technician);
        }

        if (await userManager.FindByNameAsync("tech2User") == null)
        {
            var tech2User = new AppUser
            {
                UserName = "tech2User",
                Email = "tech2@materialslab.test",
                FirstName = "Tech",
                LastName = "Two",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(tech2User, "Matrix0001!");
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(tech2User, Roles.Technician);
        }

        if (!await context.MixDesigns.AnyAsync(m => m.Code == "C-4000-A"))
        {
            context.MixDesigns.Add(new MixDesign
            {
                Code = "C-4000-A",
                Description = "Concrete Mix Design for 4000 psi",
                RequiredStrengthPsi = 4000
            });
        }

        var tech1 = await userManager.FindByNameAsync("tech1User");

        if (!await context.Projects.AnyAsync(p => p.Code == "PRJ-2026-014"))
        {
            var project = new Project { Code = "PRJ-2026-014", Name = "Highway 59 Bridge Replacement" };
            project.ProjectTechnicians.Add(new ProjectTechnician { TechnicianId = tech1!.Id });
            context.Projects.Add(project);
        }

        await context.SaveChangesAsync();
        
    }
}