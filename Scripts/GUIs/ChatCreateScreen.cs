using System.Linq;

public partial class ChatCreateScreen : Panel {
    [Export] Storage Storage;
    [Export] ChatSelectScreen ChatSelectScreen;
    [Export] ItemList CharacterList;
    [Export] BaseButton CreateButton;
    [Export] BaseButton BackButton;

    private readonly Dictionary<int, Guid> CharacterIndexes = [];

    public override void _Ready() {
        CreateButton.Pressed += Create;
        BackButton.Pressed += Back;
    }
    public new void Show() {
        base.Show();

        // Clear displayed characters
        CharacterList.Clear();
        CharacterIndexes.Clear();
        // Display characters
        foreach (CharacterRecord Character in Storage.GetCharacters()) {
            int ItemIndex = CharacterList.AddItem(Character.Name, Storage.GetImage(Character.Icon));
            CharacterIndexes[ItemIndex] = Character.Id;
            
            // Select the first character automatically
            if (ChatSelectScreen.CharacterId == Character.Id) {
                CharacterList.Select(ItemIndex, single: false);
            }
        }
    }

    private void Create() {
        // Get selected character IDs
        List<Guid> CharacterIds = CharacterList.GetSelectedItems().Select(Index => CharacterIndexes[Index]).ToList();

        // Create new chat
        Storage.CreateChat(CharacterIds);

        Hide();
        ChatSelectScreen.Show();
    }
    private void Back() {
        Hide();
        ChatSelectScreen.Show();
    }
}