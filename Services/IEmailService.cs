namespace _2cpbackend.Services;

public interface IEmailService
{
    public Task SendEmail(string receiver, string subject, string body);
}