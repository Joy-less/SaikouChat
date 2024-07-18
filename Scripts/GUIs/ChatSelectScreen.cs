using System.Linq;

public partial class ChatSelectScreen : Panel {
    [Export] Storage Storage;
    [Export] ChatScreen ChatScreen;
    [Export] CharacterSelectScreen CharacterSelectScreen;
    [Export] ItemList ChatList;
    [Export] BaseButton CreateButton;
    [Export] BaseButton BackButton;

    private readonly Dictionary<long, Guid> ChatIds = [];
    public Guid CharacterId;

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
        foreach (ChatRecord Chat in Storage.SaveData.Chats.Values.Where(Chat => Chat.CharacterId == CharacterId)) {
            string ChatTitle = Chat.CreatedTime.ToLocalTime().ToConciseString();
            long ItemId = ChatList.AddItem(ChatTitle);
            ChatIds[ItemId] = Chat.Id;
        }
    }

    private void Create() {
        Storage.CreateChat(CharacterId);
        Show();
    }
    private void Back() {
        Hide();
        CharacterSelectScreen.Show();
    }
    private void SelectChat(long ItemId) {
        ChatScreen.ChatId = ChatIds[ItemId];
        Hide();
        ChatScreen.Show();
    }
}