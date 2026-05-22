namespace TaxiCompare.Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? AvatarUrl { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? GoogleId { get; private set; }
    public string? AppleId { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public IList<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public static User Create(string email, string name, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Name = name,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static User CreateFromOAuth(string email, string name, string? avatarUrl,
        string? googleId = null, string? appleId = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Name = name,
            AvatarUrl = avatarUrl,
            GoogleId = googleId,
            AppleId = appleId,
            IsEmailVerified = true,  // OAuth emails are pre-verified
            CreatedAt = DateTime.UtcNow
        };
    }

    public void VerifyEmail() => IsEmailVerified = true;

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void AddRefreshToken(RefreshToken token) => RefreshTokens.Add(token);

    public void RevokeAllTokens()
    {
        foreach (var t in RefreshTokens.Where(t => !t.IsRevoked))
            t.Revoke();
    }
}

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedBy = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedBy;
    }
}
