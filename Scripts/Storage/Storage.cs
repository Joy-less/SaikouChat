using Newtonsoft.Json;

public partial class Storage : Node {
    [Export] public Texture2D Placeholder;

    public string SavePath = "SaveData.json";
    public SaveRecord SaveData;

    public override void _Ready() {
        Load();
    }
    public void Load() {
        // Read save data from file
        if (File.Exists(SavePath)) {
            SaveData = JsonConvert.DeserializeObject<SaveRecord>(File.ReadAllText(SavePath));
        }
        // Create new save data
        else {
            SaveData = new SaveRecord() {
                Version = (string)ProjectSettings.GetSetting("application/config/version")
            };
        }
        // Migrate save data
        Migrate();
    }
    public void Save() {
        // Write save data to file
        File.WriteAllText(SavePath, JsonConvert.SerializeObject(SaveData));
    }
    public void Migrate() {
        // 1.0 -> 1.1
        if (SaveData.Version == "1.0") {
            foreach (CharacterRecord Character in SaveData.Characters.Values) {
                if (Character.Icon is Guid IconId && IconId == default) {
                    Character.Icon = null;
                }
            }
            SaveData.Version = "1.1";
        }
        // Save data in new format
        Save();
    }
    public Texture2D GetImage(Guid? ImageId) {
        // Return placeholder if not found
        if (ImageId is null || !SaveData.Images.TryGetValue(ImageId.Value, out byte[] Buffer)) {
            return Placeholder;
        }
        // Create image from buffer
        Image Image = new();
        Image.LoadWebpFromBuffer(Buffer);
        return ImageTexture.CreateFromImage(Image);
    }
    public CharacterRecord CreateCharacter(string Name, string Nickname, string Bio, Image Icon = null) {
        // Create character
        CharacterRecord Character = new() {
            Name = Name,
            Nickname = Nickname,
            Bio = Bio,
        };
        // Store icon
        if (Icon is not null) {
            Character.Icon = Guid.NewGuid();
            SaveData.Images[Character.Icon.Value] = Icon.SaveWebpToBuffer(lossy: true);
        }
        // Add character
        SaveData.Characters[Character.Id] = Character;
        // Save data
        Save();
        return Character;
    }
    public ChatRecord CreateChat(Guid CharacterId) {
        // Create chat
        ChatRecord Chat = new() {
            CharacterId = CharacterId,
        };
        // Add chat
        SaveData.Chats[Chat.Id] = Chat;
        // Save data
        Save();
        return Chat;
    }
    public ChatMessageRecord CreateChatMessage(Guid ChatId, string Message, Guid? AuthorId) {
        // Create chat message
        ChatMessageRecord ChatMessage = new() {
            Message = Message,
            Author = AuthorId,
        };
        // Add chat message
        SaveData.Chats[ChatId].ChatMessages[ChatMessage.Id] = ChatMessage;
        // Save data
        Save();
        return ChatMessage;
    }
    public List<ChatMessageRecord> GetChatMessages(Guid ChatId) {
        List<ChatMessageRecord> ChatMessages = [.. SaveData.Chats[ChatId].ChatMessages.Values];
        ChatMessages.Sort((A, B) => A.CreatedTime.CompareTo(B.CreatedTime));
        return ChatMessages;
    }
}

public record SaveRecord {
    public string Version;
    public Dictionary<Guid, CharacterRecord> Characters = [];
    public Dictionary<Guid, ChatRecord> Chats = [];
    public Dictionary<Guid, byte[]> Images = [];
}
public abstract record Record {
    public Guid Id = Guid.NewGuid();
}
public record CharacterRecord : Record {
    public string Name;
    public string Nickname;
    public string Bio;
    public Guid? Icon;
    public DateTime CreatedTime = DateTime.UtcNow;
}
public record ChatRecord : Record {
    public Guid CharacterId;
    public Dictionary<Guid, ChatMessageRecord> ChatMessages = [];
    public DateTime CreatedTime = DateTime.UtcNow;
}
public record ChatMessageRecord : Record {
    public string Message;
    public Guid? Author;
    public DateTime CreatedTime = DateTime.UtcNow;
}