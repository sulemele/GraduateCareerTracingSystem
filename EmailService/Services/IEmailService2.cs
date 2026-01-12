namespace EmailManager.Services
{
    public interface IEmailService2
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
