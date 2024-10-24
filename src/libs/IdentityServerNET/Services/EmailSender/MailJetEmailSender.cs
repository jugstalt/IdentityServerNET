using IdentityServerNET.Abstractions.EmailSender;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.EmailSender;

public class MailJetEmailSender : ICustomEmailSender
{
    //"MailJet": {
    //	"ApiKey": "xxxxxxxx",
    //  "ApiSecret": "***********",
    //	"FromEmail": "identity@server.com",
    //	"FromName": "Identity Server"
    //}

    private readonly IConfiguration _configSection;

    public MailJetEmailSender(IConfiguration configuration)
    {
        _configSection = configuration.GetSection("IdentityServer:Mail:MailJet");
    }

    #region ICustomEmailSender

    async public Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        try
        {
            Console.WriteLine($"Try send mail to: {to}");

            MailjetClient client = new MailjetClient(
                _configSection.GetValue<string>("ApiKey"),
                _configSection.GetValue<string>("ApiSecret"));

            MailjetRequest request = new MailjetRequest
            {
                Resource = Send.Resource,
            };

            var email = new TransactionalEmailBuilder()
                                        .WithFrom(new SendContact(_configSection.GetValue<string>("FromEmail")))
                                        .WithSubject(subject)
                                        .WithHtmlPart(htmlMessage)
                                        .WithTo(new SendContact(to))
                                        .Build();

            var response = await client.SendTransactionalEmailAsync(email);

            MailMessage msg = new MailMessage();

            if (response.Messages != null)
            {
                foreach (var responseMessage in response.Messages)
                {
                    if (responseMessage?.Errors != null)
                    {
                        Console.WriteLine($"Status: {responseMessage.Status}, Errors: {String.Join(", ", responseMessage.Errors?.Select(e => $"{e.ErrorCode}:{e.ErrorMessage}"))}");
                    }
                }
            }

            Console.WriteLine("succeeded...");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }

    }

    async public Task SendEmailAsync_old(string email, string subject, string htmlMessage)
    {
        try
        {
            Console.WriteLine($"Try send mail to: {email}");
            MailMessage msg = new MailMessage();

            msg.From = new MailAddress(
                _configSection.GetValue<string>("FromEmail"),
                _configSection.GetValue<string>("FromName"));
            msg.To.Add(new MailAddress(email));

            msg.Subject = subject;
            msg.Body = htmlMessage;
            msg.IsBodyHtml = true;

            SmtpClient client = new SmtpClient("in.mailjet.com", 587);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(
                 _configSection.GetValue<string>("ApiKey"),
                 _configSection.GetValue<string>("ApiSecret"));

            await client.SendMailAsync(msg);

            Console.WriteLine("done...");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }

    }

    #endregion
}
