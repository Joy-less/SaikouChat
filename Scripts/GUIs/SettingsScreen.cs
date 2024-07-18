public partial class SettingsScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] BaseButton BackButton;
    [Export] SpinBox ChatHistoryLengthValue;

    public override void _Ready() {
        BackButton.Pressed += Back;
    }
    public new void Show() {
        base.Show();

        ChatHistoryLengthValue.Value = Storage.SaveData.ChatHistoryLength;
    }
    public new void Hide() {
        base.Hide();

        // Save settings
        Storage.SaveData.ChatHistoryLength = Mathf.RoundToInt(ChatHistoryLengthValue.Value);
        Storage.Save();
    }
    
    private void Back() {
        Hide();
        CharacterSelectScreen.Show();
    }
}