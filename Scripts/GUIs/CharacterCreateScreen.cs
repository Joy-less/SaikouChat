public partial class CharacterCreateScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] LineEdit NameInput;
    [Export] LineEdit NicknameInput;
    [Export] TextEdit BioInput;
    [Export] TextureButton IconInput;
    [Export] BaseButton CreateButton;
    [Export] BaseButton BackButton;

    private readonly FileDialog IconFileDialog = new() {
        FileMode = FileDialog.FileModeEnum.OpenFile,
        Access = FileDialog.AccessEnum.Filesystem,
        UseNativeDialog = true,
        Filters = ["*.webp,*.png,*.jpeg,*.jpg,*.bmp,*.svg,*.tga;Images"],
    };
    private Image Icon;

    public override void _Ready() {
        CreateButton.Pressed += Create;
        BackButton.Pressed += Back;
        IconInput.Pressed += SelectIcon;
        IconFileDialog.FileSelected += IconChosen;
    }
    public void Clear() {
        NameInput.Clear();
        NicknameInput.Clear();
        BioInput.Clear();
        IconInput.TextureNormal = Storage.Placeholder;
        Icon = null;
    }

    private void Create() {
        Storage.CreateCharacter(NameInput.Text, NicknameInput.Text, BioInput.Text, Icon);

        Hide();
        Clear();
        CharacterSelectScreen.Show();
    }
    private void Back() {
        Hide();
        Clear();
        CharacterSelectScreen.Show();
    }
    private void SelectIcon() {
        IconFileDialog.Popup();
    }
    private void IconChosen(string IconPath) {
        Icon = Image.LoadFromFile(IconPath);
        IconInput.TextureNormal = ImageTexture.CreateFromImage(Icon);
    }
}