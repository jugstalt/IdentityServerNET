using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages
{
    public class AdminPageModel : PageModel
    {
        public AdminPageModel()
        {

        }

        [TempData]
        public string StatusMessage { get; set; }

        async protected Task<IActionResult> PostFormHandlerAsync(Func<Task> func, Func<IActionResult> onFinally, string successMessage, Func<Exception, IActionResult> onException = null)
        {
            try
            {
                StatusMessage = successMessage;

                await func();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: { ex.Message }";

                if (onException != null)
                {
                    return onException(ex);
                }
                //return RedirectToPage();
            }

            return onFinally();

        }
    }
}