using IdentityServer.Nova.Abstractions.EmailSender;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.EmailSender;

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

    private readonly IConfiguration _configSection;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configSection = configuration.GetSection("IdentityServer:Mail:Smtp");
    }

    #region ICustomEmailSender

    async public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            MailMessage msg = new MailMessage();

            msg.From = new MailAddress(
                _configSection.GetValue<string>("FromEmail"),
                _configSection.GetValue<string>("FromName"));
            msg.To.Add(new MailAddress(email));

            var smtpServer = _configSection.GetValue<string>("SmtpServer");
            var smtpPort = int.Parse(_configSection.GetValue<string>("SmtpPort"));

            msg.Subject = subject;
            msg.Body = htmlMessage;
            msg.IsBodyHtml = true;

            SmtpClient client = new SmtpClient(smtpServer, smtpPort);

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl =
                _configSection.GetValue<bool>("EnableSsl", smtpServer != "localhost");

            string username = _configSection.GetValue<string>("Username");
            string password = _configSection.GetValue<string>("Password");

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
