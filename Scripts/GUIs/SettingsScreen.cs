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
        ModelPathValue.Text = Storage.SaveData.Settings.ModelPath;
        ChatHistoryLengthValue.Value = Storage.SaveData.Settings.ChatHistoryLength;
        MaxMessageLengthValue.Value = Storage.SaveData.Settings.MaxMessageLength;
        InstructionsValue.Text = Storage.SaveData.Settings.Instructions;
    }
    public new void Hide() {
        base.Hide();

        // Save settings
        Storage.SaveData.Settings.ModelPath = ModelPathValue.Text;
        Storage.SaveData.Settings.ChatHistoryLength = Mathf.RoundToInt(ChatHistoryLengthValue.Value);
        Storage.SaveData.Settings.MaxMessageLength = Mathf.RoundToInt(MaxMessageLengthValue.Value);
        Storage.SaveData.Settings.Instructions = InstructionsValue.Text;
        Storage.Save();
    }
    
    private void ResetSettings() {
        // Set settings to default
        Storage.SaveData.Settings = new SettingsRecord();
        Storage.Save();

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