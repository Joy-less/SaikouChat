using System.Linq;

public partial class ChatScreen : Panel {
    [Export] Node LLM;
    [Export] Storage Storage;
    [Export] ChatSelectScreen ChatSelectScreen;
    [Export] SceneCreateScreen SceneCreateScreen;
    [Export] TextureRect CharacterIconRect;
    [Export] BaseButton EditSceneButton;
    [Export] BaseButton PinnedMessagesButton;
    [Export] ScrollContainer MessageList;
    [Export] Control MessageTemplate;
    [Export] TextEdit MessageInput;
    [Export] Label TypingIndicator;
    [Export] BaseButton SendButton;
    [Export] BaseButton GenerateButton;
    [Export] BaseButton BackButton;

    public Guid ChatId;

    private readonly SemaphoreSlim Semaphore = new(1, 1);
    private LLMBinding LLMBinding;
    private CharacterState CharacterState;
    private bool FilterPinnedMessages;

    public override void _Ready() {
        LLMBinding = new LLMBinding(LLM);
        SendButton.Pressed += Send;
        GenerateButton.Pressed += Generate;
        BackButton.Pressed += Back;
        EditSceneButton.Pressed += EditScene;
        PinnedMessagesButton.Pressed += PinnedMessages;
        MessageList.GetVScrollBar().Changed += UpdateScroll;
        MessageInput.TextChanged += UpdateText;
    }
    public override void _Process(double Delta) {
        // Update typing indicator
        if (CharacterState is not CharacterState.Idle && ChatId != default) {
            TypingIndicator.Show();
            TypingIndicator.Text = $"{Storage.GetCharacterFromChatId(ChatId).Name} is {(CharacterState is CharacterState.Thinking ? "thinking" : "typing")}...";
        }
        else {
            TypingIndicator.Hide();
        }
    }
    public new async void Show() {
        base.Show();

        // Display character icon
        CharacterRecord Character = Storage.GetCharacterFromChatId(ChatId);
        CharacterIconRect.Texture = Storage.GetImage(Character.Icon);
        CharacterIconRect.TooltipText = Character.Name;
        // Clear displayed chat messages
        Clear();
        // Get filtered chat messages
        IEnumerable<ChatMessageRecord> ChatMessagesToShow = FilterPinnedMessages
            ? Storage.GetPinnedChatMessages(ChatId)
            : GetChatHistory(ChatId);
        // Display chat messages
        foreach (ChatMessageRecord ChatMessage in ChatMessagesToShow) {
            AddChatMessage(ChatMessage);
        }
        // Scroll to bottom
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        UpdateScroll();
    }
    public new void Hide() {
        base.Hide();

        Clear();
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
        ChatSelectScreen.Show();
    }
    private void EditScene() {
        Hide();
        SceneCreateScreen.Show();
    }
    private void PinnedMessages() {
        FilterPinnedMessages = !FilterPinnedMessages;
        Show();
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
    private bool Pin(Guid ChatId, Guid ChatMessageId) {
        ChatMessageRecord ChatMessage = Storage.GetChatMessage(ChatId, ChatMessageId);

        // Toggle chat message pinned
        ChatMessage.Pinned = !ChatMessage.Pinned;
        Storage.Save();

        // Return new pinned value
        return ChatMessage.Pinned;
    }
    private void Regenerate(Guid ChatId, Guid ChatMessageId) {
        ChatRecord Chat = Storage.GetChat(ChatId);
        ChatMessageRecord ChatMessage = Storage.GetChatMessage(ChatId, ChatMessageId);

        // Delete message and all later messages
        Chat.ChatMessages = Chat.ChatMessages.Where(ChatMessage1 => ChatMessage1.Value.CreatedTime < ChatMessage.CreatedTime).ToDictionary();
        Storage.Save();
        Show();

        // Generate new message
        _ = GenerateChatMessageAsync(ChatId);
    }
    private void Delete(Guid ChatId, Guid ChatMessageId) {
        ChatRecord Chat = Storage.GetChat(ChatId);

        // Delete message
        Chat.ChatMessages.Remove(ChatMessageId);
        Storage.Save();
        Show();
    }
    private void AddChatMessage(ChatMessageRecord ChatMessage, bool GenerateResponse = false) {
        Control Message = (Control)MessageTemplate.Duplicate();

        // Get message sub-nodes
        Label AuthorNameLabel = Message.GetNode<Label>("Background/AuthorName");
        TextureRect AuthorIconRect = Message.GetNode<TextureRect>("Background/AuthorIcon");
        TextEdit MessageLabel = Message.GetNode<TextEdit>("MessageContainer/MessageLabel");
        BaseButton PinButton = Message.GetNode<BaseButton>("Background/PinButton");
        BaseButton RegenerateButton = Message.GetNode<BaseButton>("Background/RegenerateButton");
        BaseButton DeleteButton = Message.GetNode<BaseButton>("Background/DeleteButton");

        // Get chat message author
        CharacterRecord Author = ChatMessage.Author is Guid AuthorId ? Storage.GetCharacter(AuthorId) : null;
        // Display author name
        AuthorNameLabel.Text = Author is not null ? Author.Name : "You";
        // Display author icon
        AuthorIconRect.Texture = Storage.GetImage(Author is not null ? Author.Icon : default);
        // Display message
        MessageLabel.Text = ChatMessage.Message;
        // Display pinned indicator
        PinButton.ButtonPressed = ChatMessage.Pinned;
        // Connect buttons
        PinButton.Pressed += () => PinButton.ButtonPressed = Pin(ChatId, ChatMessage.Id);
        RegenerateButton.Pressed += () => Regenerate(ChatId, ChatMessage.Id);
        DeleteButton.Pressed += () => Delete(ChatId, ChatMessage.Id);
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
            CharacterRecord Character = Storage.GetCharacterFromChatId(ChatId);

            // Build prompt
            string Prompt = new PromptBuilder() {
                Instructions = Storage.GetSettings().Instructions,
                SceneDescription = Storage.GetChat(ChatId).SceneDescription,
                Character = Character,
                Messages = GetChatHistory(ChatId),
                PinnedMessages = Storage.GetPinnedChatMessages(ChatId),
                GetCharacterFromId = Storage.GetCharacter,
            }.Build();
            
            // Print prompt (debug)
            if (OS.IsDebugBuild()) {
                GD.Print(Prompt);
            }

            // Configure LLM model
            if (!File.Exists(Storage.GetSettings().ModelPath)) {
                GD.PushError("Model path not found");
                throw new InvalidOperationException();
            }
            LLMBinding.ModelPath = Storage.GetSettings().ModelPath;

            // Generate response
            CharacterState = CharacterState.Thinking;
            string Response = (await LLMBinding.PromptAsync(Prompt, MaxLength: Storage.GetSettings().MaxMessageLength, OnPartial: Text => {
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
        catch (Exception Ex) {
            GD.PushError(Ex);
            throw;
        }
        finally {
            CharacterState = CharacterState.Idle;
            Semaphore.Release();
        }
    }
    private IEnumerable<ChatMessageRecord> GetChatHistory(Guid ChatId) {
        return Storage.GetChatMessages(ChatId).TakeLast(Storage.GetSettings().ChatHistoryLength);
    }
}

public enum CharacterState {
    Idle,
    Thinking,
    Typing,
}