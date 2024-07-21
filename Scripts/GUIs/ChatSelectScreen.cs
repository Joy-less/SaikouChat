using System.Linq;

public partial class ChatSelectScreen : Panel {
    [Export] Storage Storage;
    [Export] ChatScreen ChatScreen;
    [Export] ChatCreateScreen ChatCreateScreen;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] ItemList ChatList;
    [Export] BaseButton CreateButton;
    [Export] BaseButton BackButton;

    public Guid CharacterId;

    private readonly Dictionary<long, Guid> ChatIndexes = [];

    public override void _Ready() {
        CreateButton.Pressed += Create;
        BackButton.Pressed += Back;
        ChatList.ItemSelected += SelectChat;
    }
    public new void Show() {
        base.Show();

        // Clear displayed chats
        ChatList.Clear();
        // Display chats
        foreach (ChatRecord Chat in Storage.GetChatsFromCharacterId(CharacterId)) {
            string ChatTitle = Chat.CreatedTime.ToLocalTime().ToConciseString()
                + $" ({string.Join(", ", Chat.CharacterIds.Select(Id => Storage.GetCharacter(Id).Name))})";

            long ItemIndex = ChatList.AddItem(ChatTitle);
            ChatIndexes[ItemIndex] = Chat.Id;
        }
    }

    private void Create() {
        Hide();
        ChatCreateScreen.Show();
    }
    private void Back() {
        Hide();
        CharacterSelectScreen.Show();
    }
    private void SelectChat(long ItemIndex) {
        ChatScreen.ChatId = ChatIndexes[ItemIndex];
        Hide();
        ChatScreen.Show();
    }
}