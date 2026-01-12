
using System.Net.Mail;
using System.Net;
using EmailManager.Models;
using Microsoft.Extensions.Options;

namespace EmailManager.Services
{
    public class EmailService2 : IEmailService2
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService2(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                var mail = new MailMessage()
                {
                    From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName)
                };

                mail.To.Add(new MailAddress(toEmail));
                mail.Subject = subject;
                mail.Body = message;
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
}
