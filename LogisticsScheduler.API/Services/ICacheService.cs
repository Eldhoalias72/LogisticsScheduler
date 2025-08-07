namespace LogisticsScheduler.API.Services
{
    public interface ICacheService
{
    Task<T?> GetData<T>(string key);
    Task<bool> SetData<T>(string key, T value, TimeSpan expiration);
    Task<bool> RemoveData(string key);
}
}