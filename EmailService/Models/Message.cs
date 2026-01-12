

using Microsoft.AspNetCore.Http;
using MimeKit;

namespace EmailManager.Models
{
    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public IFormFile[]? Attachments { get; set; }

        public Message(IEnumerable<string> to, string subject,string content, IFormFile[]? attachments)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => new MailboxAddress("email",x)));
            Subject = subject;
            Content = content;
            Attachments = attachments;
        }
    }
}
