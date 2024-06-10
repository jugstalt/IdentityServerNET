namespace IdentityServer.Nova.UserInteraction
{
    public class AdminAccountEditor : EditorInfoCollection
    {
        public bool AllowDelete { get; set; }

        public bool AllowSetPassword { get; set; }
    }
}
