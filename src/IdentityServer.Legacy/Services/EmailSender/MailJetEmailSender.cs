using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.EmailSender
{
    public class MailJetEmailSender : ICustomEmailSender
    {
        //"MailJet": {
        //	"ApiKey": "xxxxxxxx",
        //  "ApiSecret": "***********",
        //	"FromEmail": "identity@server.com",
        //	"FromName": "Identity Server"
        //}

        private readonly IConfiguration _configuration;

        public MailJetEmailSender(IConfiguration configuration)
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
                    _configuration.GetSection("MailJet").GetValue<string>("FromEmail"),
                    _configuration.GetSection("MailJet").GetValue<string>("FromName"));
                msg.To.Add(new MailAddress(email));

                msg.Subject = subject;
                msg.Body = htmlMessage;
                msg.IsBodyHtml = true;

                SmtpClient client = new SmtpClient("in.mailjet.com", 587);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(
                     _configuration.GetSection("MailJet").GetValue<string>("ApiKey"),
                     _configuration.GetSection("MailJet").GetValue<string>("ApiSecret"));

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
