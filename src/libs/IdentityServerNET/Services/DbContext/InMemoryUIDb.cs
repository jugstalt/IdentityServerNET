using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.UI;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class InMemoryUIDb : IUIDbContext
{
    private UICustomizationSettings _settings = new();

    public Task<UICustomizationSettings?> GetSettingsAsync()
        => Task.FromResult<UICustomizationSettings?>(_settings);

    public Task SaveSettingsAsync(UICustomizationSettings settings)
    {
        _settings = settings;
        return Task.CompletedTask;
    }
}
