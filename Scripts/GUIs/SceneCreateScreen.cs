public partial class SceneCreateScreen : Panel {
    [Export] Storage Storage;
    [Export] ChatScreen ChatScreen;
    [Export] TextEdit DescriptionInput;
    [Export] BaseButton CreateButton;
    [Export] BaseButton BackButton;

    public override void _Ready() {
        CreateButton.Pressed += Create;
        BackButton.Pressed += Back;
    }
    public new void Show() {
        base.Show();

        DescriptionInput.Text = Storage.GetChat(ChatScreen.ChatId).SceneDescription;
    }
    public new void Hide() {
        base.Hide();

        // Clear input boxes
        DescriptionInput.Clear();
    }

    private void Create() {
        // Set chat's scene description
        Storage.GetChat(ChatScreen.ChatId).SceneDescription = DescriptionInput.Text;
        Storage.Save();

        Hide();
        ChatScreen.Show();
    }
    private void Back() {
        Hide();
        ChatScreen.Show();
    }
}