using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InstagramClone.Common;
using InstagramClone.Data;
using InstagramClone.Entities;
using InstagramClone.Exceptions;
using InstagramClone.Repositories.Interfaces;
using InstagramClone.Requests;
using InstagramClone.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InstagramClone.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        AppDbContext context,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _context = context;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new BadRequestException("Email is already registered.");
        }

        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUsername != null)
        {
            throw new BadRequestException("Username is already taken.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var tokens = await GenerateTokens(user);

        return Result<AuthResponse>.Ok(tokens, "User registered successfully");
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string device)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null)
            throw new NotFoundException("User not found");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid password");

        var tokens = await GenerateTokens(user, ipAddress, device);

        return Result<AuthResponse>.Ok(tokens);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (token == null)
            throw new UnauthorizedException("Invalid refresh token");

        if (token.IsRevoked)
            throw new UnauthorizedException("Token has been revoked");

        if (token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Token has expired");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        var user = await _context.Users.FindAsync(token.UserId);

        var newTokens = await GenerateTokens(user);

        await _context.SaveChangesAsync();

        return Result<AuthResponse>.Ok(newTokens, "Token refreshed");
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (token == null)
            throw new NotFoundException("Refresh token not found");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result<bool>.Ok(true, "Logged out successfully");
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var key = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"]);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<AuthResponse> GenerateTokens(User user, string ipAddress = null, string device = null)
    {
        var accessToken = GenerateJwtToken(user);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        var entity = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            IsRevoked = false,
            Device = device,
            IpAddress = ipAddress
        };

        await _context.RefreshTokens.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}