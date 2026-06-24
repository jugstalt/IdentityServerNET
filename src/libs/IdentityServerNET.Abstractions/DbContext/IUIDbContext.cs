using IdentityServerNET.Abstractions.UI;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IUIDbContext
{
    Task<UICustomizationSettings?> GetSettingsAsync();
    Task SaveSettingsAsync(UICustomizationSettings settings);
}
