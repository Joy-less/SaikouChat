public partial class SettingsScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] BaseButton ResetSettingsButton;
    [Export] BaseButton BackButton;
    [Export] LineEdit ModelPathValue;
    [Export] SpinBox ChatHistoryLengthValue;
    [Export] SpinBox MaxMessageLengthValue;
    [Export] TextEdit InstructionsValue;

    private readonly FileDialog ModelFileDialog = new() {
        FileMode = FileDialog.FileModeEnum.OpenFile,
        Access = FileDialog.AccessEnum.Filesystem,
        UseNativeDialog = true,
        Filters = ["*.gguf;Models"],
    };

    public override void _Ready() {
        ResetSettingsButton.Pressed += ResetSettings;
        BackButton.Pressed += Back;
        ModelPathValue.FocusEntered += SelectModel;
        ModelFileDialog.FileSelected += SelectModel;
    }
    public new void Show() {
        base.Show();

        // Load current settings
        ModelPathValue.Text = Storage.GetSettings().ModelPath;
        ChatHistoryLengthValue.Value = Storage.GetSettings().ChatHistoryLength;
        MaxMessageLengthValue.Value = Storage.GetSettings().MaxMessageLength;
        InstructionsValue.Text = Storage.GetSettings().Instructions;
    }
    public new void Hide() {
        base.Hide();

        // Save settings
        Storage.GetSettings().ModelPath = ModelPathValue.Text;
        Storage.GetSettings().ChatHistoryLength = Mathf.RoundToInt(ChatHistoryLengthValue.Value);
        Storage.GetSettings().MaxMessageLength = Mathf.RoundToInt(MaxMessageLengthValue.Value);
        Storage.GetSettings().Instructions = InstructionsValue.Text;
        Storage.Save();
    }
    
    private void ResetSettings() {
        // Set settings to default
        Storage.ResetSettings();

        // Show reset settings
        Show();
    }
    private void Back() {
        Hide();
        CharacterSelectScreen.Show();
    }
    private void SelectModel() {
        ModelFileDialog.Popup();
    }
    private void SelectModel(string ModelPath) {
        ModelPathValue.Text = ModelPath;
    }
}