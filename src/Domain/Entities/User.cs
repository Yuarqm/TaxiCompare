namespace TaxiCompare.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }
    public UserPreferences Preferences { get; private set; } = new();
    public ICollection<RideRequest> RideRequests { get; private set; } = new List<RideRequest>();
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    private User() { }

    public static User Create(string email, string passwordHash, string firstName, string lastName, string? phone = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Preferences = UserPreferences.Default()
        };
    }

    public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
    public void UpdatePreferences(UserPreferences prefs) => Preferences = prefs;
}
