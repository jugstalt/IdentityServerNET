namespace IdentityServer.Nova.Models.UserInteraction;

public class AdminAccountEditor : EditorInfoCollection
{
    public bool AllowDelete { get; set; }

    public bool AllowSetPassword { get; set; }
}
