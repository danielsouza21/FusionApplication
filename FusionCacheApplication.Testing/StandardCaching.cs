using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FusionCacheApplication.Testing;

/*
Testing approaches:
    1. Run request for instance 1, hitting the cache L1 and also saving it onL2 (redis). 
       Run request for instance 2 and verify that factory was not called (got value from L2). 
       Then remove key at Redis, re-run requests for both instances and verify again that factory was not called (hitted L1 em both cases).

    2. 
*/
public class StandardCaching
{
    private static readonly ILogger _logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Information))
        .CreateLogger<StandardCaching>();

    private static readonly HttpClient _httpClient = new();

    private const string APP_INSTANCE_1_URL = "https://localhost:57098"; //It has to be set based on the individual ports of each instance (not the LB)
    private const string APP_INSTANCE_2_URL = "https://localhost:57096";

    private const string REDDIS_CONNECTION_STRING = "localhost:6379,password=secretPasswordRedis";

    //TODO: bsucar tambem logs no open telemetry e ver quantas vezes foi no banco de dados

    [Fact]
    public async Task StandardCaching_GivenRetriveUser_ShouldCacheL1andL2_MultipleInstancesAsync()
    {
        _logger.LogInformation("🧪 TEST: Starting standard caching test");

        var redisHelper = new RedisHelper(REDDIS_CONNECTION_STRING);

        // Test user data
        var testUserId = Guid.NewGuid();
        var miniGuid = testUserId.ToString("N")[..8];

        var testUser = new UpsertUserDto(
            testUserId, 
            $"testuser_{miniGuid}", 
            $"test_{miniGuid}@example.com");

        var cacheKey = $"user:id:{testUserId}";

        try
        {
            // Step 1: Run POST for a new user - Instance 1
            _logger.LogInformation("📝 STEP 1: Creating user via Instance 1");
            var createResponse = await CreateUserAsync(APP_INSTANCE_1_URL, testUser);
            Assert.True(createResponse.IsSuccessStatusCode, "Failed to create user");

            // Step 2: Run GET for the user - Instance 1 (should populate cache)
            _logger.LogInformation("🔍 STEP 2: Getting user via Instance 1 (should populate cache)");
            var user1 = await GetUserAsync(APP_INSTANCE_1_URL, testUserId);
            Assert.NotNull(user1);
            Assert.Equal(testUserId, user1.Id);

            // Verify cache was populated in Redis
            var cacheExists = await redisHelper.KeyExistsAsync(cacheKey);
            _logger.LogInformation("🔍 CACHE CHECK: Key {Key} exists in Redis: {Exists}", cacheKey, cacheExists);

            // Step 3: Configure chaos to fail (guaranteeing that factory will not be called)
            _logger.LogInformation("⚡ STEP 3: Configuring chaos to fail database");
            await ConfigureChaosAsync(APP_INSTANCE_1_URL, fail: true);

            // Step 4: Run GET for the user - Instance 2 (should get from L2 cache)
            _logger.LogInformation("🔍 STEP 4: Getting user via Instance 2 (should get from L2 cache)");
            var user2 = await GetUserAsync(APP_INSTANCE_2_URL, testUserId);
            Assert.NotNull(user2);
            Assert.Equal(testUserId, user2.Id);

            // Step 5: Remove key from Redis
            _logger.LogInformation("🗑️ STEP 5: Removing key from Redis");
            await redisHelper.RemoveKeyAsync(cacheKey);

            // Verify key was removed
            var keyRemoved = !await redisHelper.KeyExistsAsync(cacheKey);
            Assert.True(keyRemoved, "Cache key should be removed from Redis");

            // Step 6: Run GET the user - Instance 1 (should get from L1 cache)
            _logger.LogInformation("🔍 STEP 6: Getting user via Instance 1 (should get from L1 cache)");
            var user1Again = await GetUserAsync(APP_INSTANCE_1_URL, testUserId);
            Assert.NotNull(user1Again);
            Assert.Equal(testUserId, user1Again.Id);

            // Step 7: Run GET the user - Instance 2 (should get from L1 cache)
            _logger.LogInformation("🔍 STEP 7: Getting user via Instance 2 (should get from L1 cache)");
            var user2Again = await GetUserAsync(APP_INSTANCE_2_URL, testUserId);
            Assert.NotNull(user2Again);
            Assert.Equal(testUserId, user2Again.Id);

            // Step 8: Remove key at Redis (should already be removed, but let's be sure)
            _logger.LogInformation("🗑️ STEP 8: Ensuring Redis key is removed");
            await redisHelper.RemoveKeyAsync(cacheKey);

            // Step 9: Run GET the user - Instance 1 (should still get from L1 cache)
            _logger.LogInformation("🔍 STEP 9: Getting user via Instance 1 (should still get from L1 cache)");
            var user1Final = await GetUserAsync(APP_INSTANCE_1_URL, testUserId);
            Assert.NotNull(user1Final);
            Assert.Equal(testUserId, user1Final.Id);

            // Step 10: Run GET the user - Instance 2 (should still get from L1 cache)
            _logger.LogInformation("🔍 STEP 10: Getting user via Instance 2 (should still get from L1 cache)");
            var user2Final = await GetUserAsync(APP_INSTANCE_2_URL, testUserId);
            Assert.NotNull(user2Final);
            Assert.Equal(testUserId, user2Final.Id);

            // Reset chaos settings
            await ConfigureChaosAsync(APP_INSTANCE_1_URL, fail: false);

            _logger.LogInformation("✅ TEST: Standard caching test completed successfully!");
        }
        finally
        {
            // Cleanup
            await CleanupTestDataAsync(APP_INSTANCE_1_URL, testUserId);
            await redisHelper.RemoveAllUserKeysAsync();
        }
    }

    private static async Task<HttpResponseMessage> CreateUserAsync(string baseUrl, UpsertUserDto user)
    {
        var json = JsonSerializer.Serialize(user);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        _logger.LogInformation("📝 API: Creating user {UserId} at {Url}", user.Id, baseUrl);
        return await _httpClient.PostAsync($"{baseUrl}/users", content);
    }

    private static async Task<User?> GetUserAsync(string baseUrl, Guid userId)
    {
        _logger.LogInformation("🔍 API: Getting user {UserId} from {Url}", userId, baseUrl);
        var response = await _httpClient.GetAsync($"{baseUrl}/users/{userId}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            _logger.LogInformation("✅ API: Successfully retrieved user {UserId} from {Url}", userId, baseUrl);
            return user;
        }
        else
        {
            _logger.LogWarning("❌ API: Failed to get user {UserId} from {Url}. Status: {Status}",
                userId, baseUrl, response.StatusCode);
            return null;
        }
    }

    private static async Task ConfigureChaosAsync(string baseUrl, bool fail)
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        _logger.LogInformation("⚡ API: Configuring chaos fail={Fail} at {Url}", fail, baseUrl);
        var response = await _httpClient.PostAsync($"{baseUrl}/admin/chaos/fail?on={fail}", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("✅ API: Chaos configured successfully: {Response}", responseContent);
        }
        else
        {
            _logger.LogWarning("❌ API: Failed to configure chaos. Status: {Status}", response.StatusCode);
        }
    }

    private static async Task CleanupTestDataAsync(string baseUrl, Guid userId)
    {
        _logger.LogInformation("🧹 API: Cleaning up test user {UserId} at {Url}", userId, baseUrl);
        var response = await _httpClient.DeleteAsync($"{baseUrl}/users/{userId}");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("✅ API: Test user {UserId} cleaned up successfully", userId);
        }
        else
        {
            _logger.LogWarning("❌ API: Failed to cleanup test user {UserId}. Status: {Status}",
                userId, response.StatusCode);
        }
    }
}
