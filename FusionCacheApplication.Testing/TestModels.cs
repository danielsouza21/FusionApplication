using System.Text.Json.Serialization;

namespace FusionCacheApplication.Testing;

public record UpsertUserDto(Guid? Id, string Username, string Email);

public record User(Guid Id, string Username, string Email, DateTimeOffset UpdatedAt);

public record ChaosSettings(bool Fail, int SlowMs, DateTimeOffset LastUpdated, string UpdatedBy, string InstanceId);

public record ApiResponse<T>(T Data, string Message = "");

public record ErrorResponse(string Error, string Message);
