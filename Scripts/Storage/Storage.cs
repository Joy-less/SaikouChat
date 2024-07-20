using System.Linq;
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
        File.WriteAllText(SavePath, JsonConvert.SerializeObject(SaveData, Formatting.Indented));
    }
    public void Migrate() {
        // null -> 1.0
        SaveData.Version ??= "1.0";
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
    public SettingsRecord GetSettings() {
        return SaveData.Settings;
    }
    public void ResetSettings() {
        SaveData.Settings = new SettingsRecord();
        Save();
    }
    public CharacterRecord CreateCharacter(string Name, string Bio, Image Icon = null) {
        // Create character
        CharacterRecord Character = new() {
            Name = Name,
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
    public CharacterRecord GetCharacter(Guid CharacterId) {
        return SaveData.Characters[CharacterId];
    }
    public IEnumerable<CharacterRecord> GetCharacters() {
        return SaveData.Characters.Values
            .OrderByDescending(Character => Character.CreatedTime);
    }
    public CharacterRecord GetCharacterFromChatId(Guid ChatId) {
        return SaveData.Characters[SaveData.Chats[ChatId].CharacterId];
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
    public ChatRecord GetChat(Guid ChatId) {
        return SaveData.Chats[ChatId];
    }
    public IEnumerable<ChatRecord> GetChatsFromCharacterId(Guid CharacterId) {
        return SaveData.Chats.Values
            .Where(Chat => Chat.CharacterId == CharacterId)
            .OrderByDescending(Chat => Chat.CreatedTime);
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
    public ChatMessageRecord GetChatMessage(Guid ChatId, Guid ChatMessageId) {
        return SaveData.Chats[ChatId].ChatMessages[ChatMessageId];
    }
    public IEnumerable<ChatMessageRecord> GetChatMessages(Guid ChatId) {
        return SaveData.Chats[ChatId].ChatMessages.Values
            .OrderBy(ChatMessage => ChatMessage.CreatedTime);
    }
    public IEnumerable<ChatMessageRecord> GetPinnedChatMessages(Guid ChatId) {
        return GetChatMessages(ChatId).Where(ChatMessage => ChatMessage.Pinned);
    }
}

public record SaveRecord {
    public string Version;
    public SettingsRecord Settings = new();
    public Dictionary<Guid, CharacterRecord> Characters = [];
    public Dictionary<Guid, ChatRecord> Chats = [];
    public Dictionary<Guid, byte[]> Images = [];
}
public abstract record Record {
    public Guid Id = Guid.NewGuid();
}
public record CharacterRecord : Record {
    public string Name;
    public string Bio;
    public Guid? Icon;
    public DateTime CreatedTime = DateTime.UtcNow;
}
public record ChatRecord : Record {
    public Guid CharacterId;
    public string SceneDescription = "";
    public Dictionary<Guid, ChatMessageRecord> ChatMessages = [];
    public DateTime CreatedTime = DateTime.UtcNow;
}
public record ChatMessageRecord : Record {
    public string Message;
    public Guid? Author;
    public bool Pinned;
    public DateTime CreatedTime = DateTime.UtcNow;
}
public record SettingsRecord : Record {
    public string ModelPath = "";
    public int ChatHistoryLength = 100;
    public int MaxMessageLength = 500;
    public string Instructions = """
        You are the character in a conversation with the user.
        Add a message to the conversation in character.
        Your message should fit the context of the conversation.
        Do not break character.
        """;
}