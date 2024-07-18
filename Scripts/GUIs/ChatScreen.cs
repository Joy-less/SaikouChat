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
    private double TypingIndicatorTimeLeft;
    
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
        TypingIndicatorTimeLeft = Mathf.Max(0, TypingIndicatorTimeLeft - Delta);
        TypingIndicator.Visible = TypingIndicatorTimeLeft > 0;
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
    private void AddChatMessage(ChatMessageRecord ChatMessage, bool GenerateResponse = false) {
        Control Message = (Control)MessageTemplate.Duplicate();
        // Get chat message author
        CharacterRecord Author = ChatMessage.Author is Guid AuthorId ? Storage.SaveData.Characters[AuthorId] : null;
        // Display author name
        Message.GetNode<Label>("Background/AuthorName").Text = Author is not null ? Author.Name : "You";
        // Get author icon
        Texture2D AuthorIcon = Storage.GetImage(Author is not null ? Author.Icon : default);
        // Display author icon
        Message.GetNode<TextureRect>("Background/AuthorIcon").Texture = AuthorIcon;
        // Display message
        Message.GetNode<TextEdit>("MessageContainer/MessageLabel").Text = ChatMessage.Message;
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
            PromptBuilder.AppendLine("""
                You are the character in a conversation with the user.
                Add a message to the conversation as the character.
                Do not break character.
                Add a different message instead of repeating yourself.
                """);
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
            
            // Configure LLM
            LLMBinding.ModelPath = Storage.SaveData.ModelPath;
            // Generate response
            string Response = (await LLMBinding.PromptAsync(PromptBuilder.ToString(), OnPartial: Text => {
                if (ChatId == this.ChatId) {
                    TypingIndicatorTimeLeft = 3;
                }
            })).Trim();
            // Send chat message
            ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, Response, Character.Id);
            TypingIndicatorTimeLeft = 0;
            // Add chat message to list
            if (ChatId == this.ChatId) {
                AddChatMessage(ChatMessage);
            }
        }
        finally {
            Semaphore.Release();
        }
    }
    private IEnumerable<ChatMessageRecord> GetChatHistory(Guid ChatId) {
        return Storage.GetChatMessages(ChatId).TakeLast(Storage.SaveData.ChatHistoryLength);
    }
}