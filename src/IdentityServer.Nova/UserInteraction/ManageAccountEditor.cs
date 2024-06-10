namespace IdentityServer.Nova.UserInteraction
{
    public class ManageAccountEditor : EditorInfoCollection
    {
        public bool AllowDelete { get; set; }
        public bool ShowChangeEmailPage { get; set; }
        public bool ShowChangePasswordPage { get; set; }
        public bool ShowTfaPage { get; set; }
    }
}
