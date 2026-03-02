namespace School.API.Services.Interfaces
{
    public interface ISmsSender
    {
        Task<bool> SendAsync(string phoneNumber, string message);
    }
}
