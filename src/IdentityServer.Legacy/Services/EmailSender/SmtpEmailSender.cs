using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.EmailSender
{
    public class SmtpEmailSender : ICustomEmailSender
    {
        //"Smtp": {
        //	"Username": "xxxxxxxx",   // optional
        //  "Password": "***********",  // optional
        //  "SmtpServer": "smtp.server.com",
        //  "SmtpPort": 234,
        //	"FromEmail": "identity@server.com",
        //	"FromName": "Identity Server"
        //}

        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region ICustomEmailSender

        async public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                MailMessage msg = new MailMessage();

                msg.From = new MailAddress(
                    _configuration.GetSection("Smtp").GetValue<string>("FromEmail"),
                    _configuration.GetSection("Smtp").GetValue<string>("FromName"));
                msg.To.Add(new MailAddress(email));

                msg.Subject = subject;
                msg.Body = htmlMessage;
                msg.IsBodyHtml = true;

                SmtpClient client = new SmtpClient(
                    _configuration.GetSection("Smtp").GetValue<string>("SmtpServer"),
                    int.Parse(_configuration.GetSection("Smtp").GetValue<string>("SmtpPort")));

                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = true;

                string username = _configuration.GetSection("Smtp").GetValue<string>("Username");
                string password = _configuration.GetSection("Smtp").GetValue<string>("Password");

                if (String.IsNullOrWhiteSpace(username))
                {
                    client.UseDefaultCredentials = true;
                }
                else
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(
                         username,
                         password);
                }

                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

        }

        #endregion
    }
}
