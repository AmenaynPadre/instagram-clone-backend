using InstagramClone.Common;
using InstagramClone.Requests;
using InstagramClone.Responses;

namespace InstagramClone.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress, string device);
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken);

    Task<Result<bool>> LogoutAsync(string refreshToken);
}