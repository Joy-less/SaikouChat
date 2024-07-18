public partial class SettingsScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] BaseButton BackButton;
    [Export] LineEdit ModelPathValue;
    [Export] SpinBox ChatHistoryLengthValue;

    private readonly FileDialog ModelFileDialog = new() {
        FileMode = FileDialog.FileModeEnum.OpenFile,
        Access = FileDialog.AccessEnum.Filesystem,
        UseNativeDialog = true,
        Filters = ["*.gguf;Models"],
    };

    public override void _Ready() {
        BackButton.Pressed += Back;
        ModelPathValue.FocusEntered += SelectModel;
        ModelFileDialog.FileSelected += SelectModel;
    }
    public new void Show() {
        base.Show();

        // Load current settings
        ModelPathValue.Text = Storage.SaveData.ModelPath;
        ChatHistoryLengthValue.Value = Storage.SaveData.ChatHistoryLength;
    }
    public new void Hide() {
        base.Hide();

        // Save settings
        Storage.SaveData.ModelPath = ModelPathValue.Text;
        Storage.SaveData.ChatHistoryLength = Mathf.RoundToInt(ChatHistoryLengthValue.Value);
        Storage.Save();
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