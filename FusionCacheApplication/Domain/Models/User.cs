using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FusionCacheApplication.Domain.Models;

[Table("users")]
public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; }
    public string Email { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // JSON constructor for deserialization
    [JsonConstructor]
    public User(Guid id, string username, string email, DateTimeOffset updatedAt)
    {
        Id = id;
        Username = username;
        Email = email;
        UpdatedAt = updatedAt;
    }

    private User() { } // EF

    public User(string username, string email, Guid? guid = null)
    {
        if (guid is not null) Id = guid.Value;
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void Update(string? username = null, string? email = null)
    {
        if (!string.IsNullOrWhiteSpace(username)) Username = username.Trim();
        if (!string.IsNullOrWhiteSpace(email)) Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
