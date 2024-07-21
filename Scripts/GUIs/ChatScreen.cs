using System.Linq;
using Newtonsoft.Json;

public partial class ChatScreen : Panel {
    [Export] Node LLM;
    [Export] Storage Storage;
    [Export] AudioStreamPlayer NotificationPlayer;
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
    }
    public override void _Process(double Delta) {
        // Update typing indicator
        if (CharacterState is not CharacterState.Idle && ChatId != default) {
            TypingIndicator.Show();
            TypingIndicator.Text = CharacterState is CharacterState.Thinking ? "Thinking..." : "Typing...";
        }
        else {
            TypingIndicator.Hide();
        }
    }
    public override void _Input(InputEvent Event) {
        // Send message on enter press
        if (Event is InputEventKey KeyEvent && KeyEvent.Keycode is Key.Enter) {
            Send();
            AcceptEvent();
        }
    }
    public new async void Show() {
        base.Show();

        // Display character icon
        CharacterRecord Character = Storage.GetCharacter(ChatSelectScreen.CharacterId);
        CharacterIconRect.Texture = Storage.GetImage(Character.Icon);
        CharacterIconRect.TooltipText = Character.Name;
        // Get filtered chat messages
        IEnumerable<ChatMessageRecord> ChatMessagesToShow = FilterPinnedMessages
            ? Storage.GetPinnedChatMessages(ChatId)
            : GetChatHistory(ChatId);
        PinnedMessagesButton.ButtonPressed = FilterPinnedMessages;
        // Clear displayed chat messages
        Clear();
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
            // Get involved characters
            IEnumerable<CharacterRecord> Characters = Storage.GetCharactersFromChatId(ChatId);

            // Build prompt
            string Prompt = new PromptBuilder() {
                Instructions = Storage.GetSettings().Instructions,
                SceneDescription = Storage.GetChat(ChatId).SceneDescription,
                Characters = Characters,
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
                throw new InvalidOperationException("Model path not found");
            }
            LLMBinding.ModelPath = Storage.GetSettings().ModelPath;

            // Update character state (for typing indicator)
            CharacterState = CharacterState.Thinking;
            void OnPartial(string Text) {
                if (ChatId == this.ChatId) {
                    CharacterState = CharacterState.Typing;
                }
            }

            // Generate response
            string ResponseMessage;
            CharacterRecord ResponseAuthor;
            // Group chat response
            if (Characters.Count() > 1) {
                string ResponseJson = await LLMBinding.GenerateAsync(Prompt, OnPartial: OnPartial, Json: JsonConvert.SerializeObject(new {
                    type = "object",
                    properties = new {
                        CharacterName = new {
                            type = "string",
                        },
                        Message = new {
                            type = "string",
                            minLength = 1,
                            maxLength = Storage.GetSettings().MaxMessageLength,
                        },
                    },
                    required = (string[])["CharacterName", "Message"],
                }));
                // Print response (debug)
                if (OS.IsDebugBuild()) {
                    GD.Print(ResponseJson);
                }
                // Deserialise response
                var Response = JsonConvert.DeserializeAnonymousType(ResponseJson, new {
                    CharacterName = "",
                    Message = "",
                });
                ResponseMessage = Response.Message;
                ResponseAuthor = Characters.First(Character => Character.Name == Response.CharacterName.Trim());
            }
            // Private chat response
            else {
                string ResponseJson = await LLMBinding.GenerateAsync(Prompt, OnPartial: OnPartial, Json: JsonConvert.SerializeObject(new {
                    type = "string",
                    minLength = 1,
                    maxLength = Storage.GetSettings().MaxMessageLength,
                }));
                // Print response (debug)
                if (OS.IsDebugBuild()) {
                    GD.Print(ResponseJson);
                }
                // Deserialise response
                string Response = JsonConvert.DeserializeObject<string>(ResponseJson);
                ResponseMessage = Response;
                ResponseAuthor = Characters.Single();
            }

            // Send chat message
            ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, ResponseMessage, ResponseAuthor.Id);
            // Add chat message to list
            if (ChatId == this.ChatId) {
                AddChatMessage(ChatMessage);
            }
            // Play notification sound
            NotificationPlayer.VolumeDb = (float)Mathf.LinearToDb(Storage.GetSettings().NotificationVolume);
            NotificationPlayer.Play();
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