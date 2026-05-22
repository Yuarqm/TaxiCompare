using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaxiCompare.Identity.Domain.Entities;

namespace TaxiCompare.Identity.Application.Commands;

// ─── Register ─────────────────────────────────────────────────────────────────

public record RegisterCommand(string Email, string Name, string Password) : IRequest<AuthResult>;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;

public record RevokeTokenCommand(string UserId) : IRequest<Unit>;

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string? Error
);

// ─── Interfaces ───────────────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

// ─── Register Handler ─────────────────────────────────────────────────────────

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(IUserRepository users, IPasswordHasher hasher,
        ITokenService tokens, ILogger<RegisterHandler> logger)
    {
        _users = users; _hasher = hasher; _tokens = tokens; _logger = logger;
    }

    public async Task<AuthResult> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await _users.ExistsByEmailAsync(cmd.Email, ct))
            return new AuthResult(false, null, null, null, "Email already registered");

        var hash = _hasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, cmd.Name, hash);

        await _users.AddAsync(user, ct);

        var (accessToken, refreshToken, expiresAt) = await _tokens.GenerateTokenPairAsync(user, ct);
        user.AddRefreshToken(RefreshToken.Create(user.Id, refreshToken, expiresAt.AddDays(7)));
        user.RecordLogin();

        await _users.SaveChangesAsync(ct);
        _logger.LogInformation("User registered: {Email}", cmd.Email);

        return new AuthResult(true, accessToken, refreshToken, expiresAt, null);
    }
}

// ─── Login Handler ────────────────────────────────────────────────────────────

public class LoginHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users; _hasher = hasher; _tokens = tokens;
    }

    public async Task<AuthResult> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(cmd.Email, ct);
        if (user is null || user.PasswordHash is null || !_hasher.Verify(cmd.Password, user.PasswordHash))
            return new AuthResult(false, null, null, null, "Invalid email or password");

        var (accessToken, refreshToken, expiresAt) = await _tokens.GenerateTokenPairAsync(user, ct);
        user.AddRefreshToken(RefreshToken.Create(user.Id, refreshToken, expiresAt.AddDays(7)));
        user.RecordLogin();

        await _users.SaveChangesAsync(ct);
        return new AuthResult(true, accessToken, refreshToken, expiresAt, null);
    }
}

// ─── Token Service ────────────────────────────────────────────────────────────

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokenPairAsync(
        User user, CancellationToken ct = default);
}

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokenPairAsync(
        User user, CancellationToken ct)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return Task.FromResult((accessToken, refreshToken, expiresAt));
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// ─── Password Hasher ──────────────────────────────────────────────────────────

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
