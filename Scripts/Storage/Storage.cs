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
    public IOrderedEnumerable<CharacterRecord> GetCharacters() {
        return SaveData.Characters.Values
            .OrderByDescending(Character => Character.CreatedTime);
    }
    public IOrderedEnumerable<ChatRecord> GetChats(Guid CharacterId) {
        return SaveData.Chats.Values
            .Where(Chat => Chat.CharacterId == CharacterId)
            .OrderByDescending(Chat => Chat.CreatedTime);
    }
    public IOrderedEnumerable<ChatMessageRecord> GetChatMessages(Guid ChatId) {
        return SaveData.Chats[ChatId].ChatMessages.Values
            .OrderBy(ChatMessage => ChatMessage.CreatedTime);
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