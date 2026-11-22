using MaterialsLab.Api.Dtos;
using MaterialsLab.Api.Models;
using MaterialsLab.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MaterialsLab.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<AppUser> userManager, TokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        var user = await userManager.FindByNameAsync(loginRequest.Username);
        if (user == null || !await userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user, roles);

        return new LoginResponseDto
        {
            Token = token,
            Username = user.UserName!,
            Roles = roles.ToList()
        };
    }
}