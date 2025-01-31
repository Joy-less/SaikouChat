public partial class SettingsScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] BaseButton ResetSettingsButton;
    [Export] BaseButton BackButton;
    [Export] LineEdit ModelPathValue;
    [Export] SpinBox ChatHistoryLengthValue;
    [Export] SpinBox MaxMessageLengthValue;
    [Export] Slider NotificationVolumeValue;
    [Export] CheckBox AutoRespondValue;
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
        SettingsRecord Settings = Storage.GetSettings();
        ModelPathValue.Text = Settings.ModelPath;
        ChatHistoryLengthValue.Value = Settings.ChatHistoryLength;
        MaxMessageLengthValue.Value = Settings.MaxMessageLength;
        NotificationVolumeValue.Value = Settings.NotificationVolume;
        AutoRespondValue.ButtonPressed = Settings.AutoRespond;
        InstructionsValue.Text = Settings.Instructions;
    }
    public new void Hide() {
        base.Hide();

        // Save settings
        SettingsRecord Settings = Storage.GetSettings();
        Settings.ModelPath = ModelPathValue.Text;
        Settings.ChatHistoryLength = Mathf.RoundToInt(ChatHistoryLengthValue.Value);
        Settings.MaxMessageLength = Mathf.RoundToInt(MaxMessageLengthValue.Value);
        Settings.NotificationVolume = NotificationVolumeValue.Value;
        Settings.AutoRespond = AutoRespondValue.ButtonPressed;
        Settings.Instructions = InstructionsValue.Text;
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