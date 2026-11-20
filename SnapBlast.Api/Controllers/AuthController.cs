using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SnapBlast.Api.Models;
using SnapBlast.Api.Services;

namespace SnapBlast.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IUserRepository users, JwtTokenService tokens, IPasswordHasher<UserDocument> passwordHasher) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || request.Password.Length < 8)
        {
            return BadRequest("A valid email and password of at least 8 characters are required.");
        }

        if (await users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return Conflict("An account with that email already exists.");
        }

        var user = new UserDocument
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await users.CreateAsync(user, cancellationToken);
        return Ok(tokens.CreateToken(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(tokens.CreateToken(user));
    }
}