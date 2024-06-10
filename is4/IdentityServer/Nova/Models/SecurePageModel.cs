using IdentityServer.Nova.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Models
{
    public class SecurePageModel : PageModel
    {
        [TempData]
        public String StatusMessage { get; set; }

        async protected Task<IActionResult> SecureHandlerAsync(Func<Task> func, Func<IActionResult> onFinally, string successMessage, Func<Exception, IActionResult> onException = null)
        {
            try
            {
                StatusMessage = successMessage;

                await func();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {(ex is StatusMessageException ? ex.Message : "Internal error")}";

                if (onException != null)
                {
                    return onException(ex);
                }
            }

            return onFinally();

        }
    }
}
