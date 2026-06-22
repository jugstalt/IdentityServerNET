using System.Collections.Generic;

namespace IdentityServerNET.Abstractions.EmailSender;

public interface IMailTemplateService
{
    string Render(string templateName, Dictionary<string, string> variables);
}
