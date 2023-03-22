using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel;
using System.Text;

namespace IdentityServer.Areas.Admin.Pages
{
    public class RandomSecretGeneratorModel : PageModel
    {
        public IActionResult OnGet(int secretLength = 0, int chars = 0)
        {
            StringBuilder sb = new StringBuilder();
            var random = new Random();

            Input = new CreateRandomSecretModel()
            {
                SecretLength = secretLength > 0 ? secretLength : 128,
                SecretCharacters = chars
            };

            var letters = Letters;
            switch (chars)
            {
                case 1:
                    letters = LettersPlus1;
                    break;
                case 2:
                    letters = LettersPlus2;
                    break;
            }

            for (int i = 0; i < secretLength; i++)
            {
                sb.Append(letters[random.Next(letters.Length)]);
            }

            Input.Secret = sb.ToString();

            return Page();
        }

        public IActionResult OnPost()
        {
            return RedirectToPage(new { secretLength = Input.SecretLength, chars = Input.SecretCharacters });
        }

        [BindProperty]
        public CreateRandomSecretModel Input { get; set; }

        private const string Letters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LettersPlus1 = Letters + "-_";
        private const string LettersPlus2 = LettersPlus1 + "#*=)(/&%$!\"@~'^°";


        public class CreateRandomSecretModel
        {
            [DisplayName("Secret length")]
            public int SecretLength { get; set; }

            [DisplayName("Secret Characters")]
            public int SecretCharacters { get; set; }

            [DisplayName("Secret")]
            [ReadOnly(true)]
            public string Secret { get; set; }
        }
    }
}
