using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging; // Add this using statement

namespace LogisticsScheduler.API.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger; // Add a logger field

        // Modify the constructor to accept ILogger
        public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
        {
            _db = multiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetData<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                // Log when we successfully find an item in the cache
                _logger.LogInformation("CACHE HIT for key: {CacheKey}", key);
                return JsonSerializer.Deserialize<T>(value!);
            }
            // Log when we don't find an item
            _logger.LogWarning("CACHE MISS for key: {CacheKey}", key);
            return default;
        }

        public async Task<bool> SetData<T>(string key, T value, TimeSpan expiration)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            var result = await _db.StringSetAsync(key, jsonValue, expiration);

            if (result)
            {
                _logger.LogInformation("CACHE SET for key: {CacheKey}", key);
            }
            else
            {
                _logger.LogError("CACHE FAILED TO SET for key: {CacheKey}", key);
            }
            return result;
        }

        public async Task<bool> RemoveData(string key)
        {
            bool _isKeyExist = await _db.KeyExistsAsync(key);
            if (_isKeyExist)
            {
                var result = await _db.KeyDeleteAsync(key);
                if (result)
                {
                    // Log when an item is successfully removed
                    _logger.LogWarning("CACHE REMOVED for key: {CacheKey}", key);
                }
                return result;
            }
            // Log if we attempt to remove a key that doesn't exist
            _logger.LogInformation("CACHE KEY TO REMOVE WAS NOT FOUND: {CacheKey}", key);
            return false;
        }
    }
}