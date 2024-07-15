public partial class CharacterSelectScreen : Panel {
    [Export] Storage Storage;
    [Export] CharacterCreateScreen CharacterCreateScreen;
    [Export] ChatSelectScreen ChatSelectScreen;
    [Export] ItemList CharacterList;
    [Export] Button CreateButton;

    private readonly Dictionary<long, Guid> CharacterIds = [];

    public override void _Ready() {
        Show();
        CreateButton.Pressed += Create;
        CharacterList.ItemSelected += SelectCharacter;
    }
    public new void Show() {
        base.Show();

        // Clear displayed characters
        CharacterList.Clear();
        // Display characters
        foreach (CharacterRecord Character in Storage.SaveData.Characters.Values) {
            long ItemId = CharacterList.AddItem(Character.Name, Storage.GetImage(Character.Icon));
            CharacterIds[ItemId] = Character.Id;
        }
    }

    private void Create() {
        Hide();
        CharacterCreateScreen.Show();
    }
    private void SelectCharacter(long ItemId) {
        ChatSelectScreen.CharacterId = CharacterIds[ItemId];
        Hide();
        ChatSelectScreen.Show();
    }
}