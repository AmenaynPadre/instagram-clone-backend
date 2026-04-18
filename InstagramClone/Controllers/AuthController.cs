using InstagramClone.Helpers;
using InstagramClone.Requests;
using InstagramClone.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var session = SessionHelper.GetSessionInfo(HttpContext);
        var result = await _authService.LoginAsync(
            request,
            session.IpAddress,
            session.Device);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}