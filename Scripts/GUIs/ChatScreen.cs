using System.Linq;
using System.Text;

public partial class ChatScreen : Panel {
    [Export] Node LLM;
    [Export] Storage Storage;
    [Export] ChatSelectScreen ChatSelectScreen;
    [Export] TextureRect CharacterIconRect;
    [Export] Label CharacterNameLabel;
    [Export] ScrollContainer MessageList;
    [Export] Control MessageTemplate;
    [Export] TextEdit MessageInput;
    [Export] Label TypingIndicator;
    [Export] BaseButton SendButton;
    [Export] BaseButton GenerateButton;
    [Export] BaseButton BackButton;

    private readonly SemaphoreSlim Semaphore = new(1, 1);
    public Guid ChatId;
    private CharacterState CharacterState;
    
    private LLMBinding LLMBinding;
    private long ResponseCounter;

    public override void _Ready() {
        LLMBinding = new LLMBinding(LLM);
        SendButton.Pressed += Send;
        GenerateButton.Pressed += Generate;
        BackButton.Pressed += Back;
        MessageList.GetVScrollBar().Changed += UpdateScroll;
        MessageInput.TextChanged += UpdateText;
    }
    public override void _Process(double Delta) {
        // Update typing indicator
        if (CharacterState is not CharacterState.Idle && ChatId != default) {
            TypingIndicator.Show();
            CharacterRecord Character = Storage.SaveData.Characters[Storage.SaveData.Chats[ChatId].CharacterId];
            TypingIndicator.Text = $"{Character.Name} is {(CharacterState is CharacterState.Thinking ? "thinking" : "typing")}...";
        }
        else {
            TypingIndicator.Hide();
        }
    }
    public new async void Show() {
        base.Show();

        // Display character info
        CharacterRecord Character = Storage.SaveData.Characters[Storage.SaveData.Chats[ChatId].CharacterId];
        CharacterIconRect.Texture = Storage.GetImage(Character.Icon);
        CharacterNameLabel.Text = Character.Name;
        // Clear displayed chat messages
        Clear();
        // Display chat messages
        foreach (ChatMessageRecord ChatMessage in GetChatHistory(ChatId)) {
            AddChatMessage(ChatMessage);
        }
        // Scroll to bottom
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        UpdateScroll();
    }
    public void Clear() {
        // Clear displayed chat messages
        foreach (Node Message in MessageTemplate.GetParent().GetChildren().Except([MessageTemplate])) {
            Message.QueueFree();
        }
        // Clear message input box
        MessageInput.Clear();
    }

    private void Send() {
        // Ensure input box is not empty
        if (string.IsNullOrWhiteSpace(MessageInput.Text)) {
            MessageInput.ReleaseFocus();
            return;
        }
        // Take prompt from input box
        string Prompt = MessageInput.Text.Trim();
        MessageInput.Clear();
        // Send chat message
        ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, Prompt, null);
        AddChatMessage(ChatMessage, true);
    }
    public void Generate() {
        // Generate a response
        _ = GenerateChatMessageAsync(ChatId);
    }
    private void Back() {
        Hide();
        Clear();
        ChatSelectScreen.Show();
    }
    private void UpdateScroll() {
        // Scroll to bottom when chat message added
        MessageList.GetVScrollBar().Value = MessageList.GetVScrollBar().MaxValue;
    }
    private void UpdateText() {
        // Send message when enter key pressed
        if (MessageInput.Text.ContainsAny(['\r', '\n'])) {
            Send();
        }
    }
    private void Regenerate(Guid ChatId, Guid ChatMessageId) {
        ChatRecord Chat = Storage.SaveData.Chats[ChatId];
        ChatMessageRecord ChatMessage = Chat.ChatMessages[ChatMessageId];

        // Delete message and all later messages
        Chat.ChatMessages = Chat.ChatMessages.Where(_ChatMessage => _ChatMessage.Value.CreatedTime < ChatMessage.CreatedTime).ToDictionary();
        Storage.Save();
        Show();

        // Generate new message
        _ = GenerateChatMessageAsync(ChatId);
    }
    private void Delete(Guid ChatId, Guid ChatMessageId) {
        ChatRecord Chat = Storage.SaveData.Chats[ChatId];

        // Delete message
        Chat.ChatMessages.Remove(ChatMessageId);
        Storage.Save();
        Show();
    }
    private void AddChatMessage(ChatMessageRecord ChatMessage, bool GenerateResponse = false) {
        Control Message = (Control)MessageTemplate.Duplicate();
        // Get chat message author
        CharacterRecord Author = ChatMessage.Author is Guid AuthorId ? Storage.SaveData.Characters[AuthorId] : null;
        // Display author name
        Message.GetNode<Label>("Background/AuthorName").Text = Author is not null ? Author.Name : "You";
        // Display author icon
        Texture2D AuthorIcon = Storage.GetImage(Author is not null ? Author.Icon : default);
        Message.GetNode<TextureRect>("Background/AuthorIcon").Texture = AuthorIcon;
        // Display message
        Message.GetNode<TextEdit>("MessageContainer/MessageLabel").Text = ChatMessage.Message;
        // Connect regenerate button
        Message.GetNode<BaseButton>("Background/RegenerateButton").Pressed += () => Regenerate(ChatId, ChatMessage.Id);
        // Connect delete button
        Message.GetNode<BaseButton>("Background/DeleteButton").Pressed += () => Delete(ChatId, ChatMessage.Id);
        // Add chat message
        Message.Show();
        MessageTemplate.GetParent().AddChild(Message);
        // Generate response
        if (GenerateResponse) {
            _ = GenerateChatMessageAsync(ChatId);
        }
    }
    private async Task GenerateChatMessageAsync(Guid ChatId) {
        await Semaphore.WaitAsync();
        try {
            // Get target character
            CharacterRecord Character = Storage.SaveData.Characters[Storage.SaveData.Chats[ChatId].CharacterId];
            // Get chat message history
            IEnumerable<ChatMessageRecord> ChatMessages = GetChatHistory(ChatId);

            // Build prompt
            StringBuilder PromptBuilder = new();
            PromptBuilder.AppendLine("Instructions:");
            PromptBuilder.AppendLine(Storage.SaveData.Instructions);
            PromptBuilder.AppendLine();
            PromptBuilder.AppendLine("Information:");
            PromptBuilder.AppendLine($"""
                User's time: {DateTimeOffset.Now.ToConciseString()}
                """);
            PromptBuilder.AppendLine();
            PromptBuilder.AppendLine("Character:");
            PromptBuilder.AppendLine($"""
                Name: "{Character.Name}"
                Bio: "{Character.Bio.Replace("\"", "\\\"")}"
                """);
            PromptBuilder.AppendLine();
            PromptBuilder.AppendLine("Conversation:");
            foreach (ChatMessageRecord CurrentChatMessage in ChatMessages) {
                PromptBuilder.Append(CurrentChatMessage.Author is not null ? $"\"{Character.Name}\"" : "User");
                PromptBuilder.AppendLine($": \"{CurrentChatMessage.Message}\"");
            }
            PromptBuilder.Append($"\"{Character.Name}\": ");
            
            // Print prompt (debug)
            if (OS.IsDebugBuild()) {
                GD.Print(PromptBuilder.ToString());
            }

            // Configure LLM model
            if (!File.Exists(Storage.SaveData.ModelPath)) {
                GD.PushError("Model path not found");
                throw new InvalidOperationException();
            }
            LLMBinding.ModelPath = Storage.SaveData.ModelPath;

            // Generate response
            CharacterState = CharacterState.Thinking;
            string Response = (await LLMBinding.PromptAsync(PromptBuilder.ToString(), OnPartial: Text => {
                if (ChatId == this.ChatId) {
                    CharacterState = CharacterState.Typing;
                }
            })).Trim();
            // Send chat message
            ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, Response, Character.Id);
            // Add chat message to list
            if (ChatId == this.ChatId) {
                AddChatMessage(ChatMessage);
            }
        }
        finally {
            CharacterState = CharacterState.Idle;
            Semaphore.Release();
        }
    }
    private IEnumerable<ChatMessageRecord> GetChatHistory(Guid ChatId) {
        return Storage.GetChatMessages(ChatId).TakeLast(Storage.SaveData.ChatHistoryLength);
    }
}

public enum CharacterState {
    Idle,
    Thinking,
    Typing,
}