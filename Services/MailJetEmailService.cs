namespace _2cpbackend.Services;

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class MailJetEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public MailJetEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmail(string receiver, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config.GetValue<string>("MailJet:Sender")));
        email.To.Add(MailboxAddress.Parse(receiver));
        email.Subject = subject;
        email.Body = new TextPart(MimeKit.Text.TextFormat.Html) {Text = body};

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config.GetValue<string>("MailJet:Server"), _config.GetValue<int>("MailJet:Port"), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config.GetValue<string>("MailJet:ApiKey"), _config.GetValue<string>("MailJet:SecretKey"));
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}