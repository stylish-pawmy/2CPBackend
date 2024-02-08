namespace Eventi.Server.Services;

public interface IEmailService
{
    public Task SendEmail(string receiver, string subject, string body);
}