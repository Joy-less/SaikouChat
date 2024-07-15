using System.Linq;
using Newtonsoft.Json;

public partial class ChatScreen : Panel {
    [Export] Node LLM;
    [Export] Storage Storage;
    [Export] ChatSelectScreen ChatSelectScreen;
    [Export] ScrollContainer MessageList;
    [Export] Control MessageTemplate;
    [Export] TextEdit MessageInput;
    [Export] Button SendButton;
    [Export] Button BackButton;

    public Guid ChatId;
    
    private LLMBinding LLMBinding;
    private long ResponseCounter;

    public override void _Ready() {
        LLMBinding = new LLMBinding(LLM);
        SendButton.Pressed += Send;
        BackButton.Pressed += Back;
        MessageList.GetVScrollBar().Changed += ScrollChanged;
    }
    public new void Show() {
        base.Show();

        // Clear displayed chat messages
        Clear();
        // Display chat messages
        foreach (ChatMessageRecord ChatMessage in Storage.GetChatMessages(ChatId)) {
            AddChatMessage(ChatMessage);
        }
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
        // Take prompt from input box
        string Prompt = MessageInput.Text;
        MessageInput.Clear();
        // Send chat message
        ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, Prompt, null);
        AddChatMessage(ChatMessage, true);
    }
    private void Back() {
        Hide();
        Clear();
        ChatSelectScreen.Show();
    }
    private void ScrollChanged() {
        // Scroll to bottom when chat message added
        MessageList.GetVScrollBar().Value = MessageList.GetVScrollBar().MaxValue;
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
            _ = GenerateChatMessageAsync();
        }
    }
    private async Task GenerateChatMessageAsync() {
        // Increment response counter
        long ResponseId = Interlocked.Increment(ref ResponseCounter);
        // Stop generating current response
        LLMBinding.StopGenerate();

        // Get target character
        CharacterRecord Character = Storage.SaveData.Characters[Storage.SaveData.Chats[ChatId].CharacterId];
        // Get chat history
        IEnumerable<ChatMessageRecord> ChatMessages = Storage.GetChatMessages(ChatId).TakeLast(100);
        // Format chat history for prompt
        IEnumerable<object> FormattedChatMessages = ChatMessages.Select(ChatMessage => new {
            Author = ChatMessage.Author is not null ? Character.Name : "(User)",
            TimeSent = ChatMessage.CreatedTime.ToConciseString(),
            Message = ChatMessage.Message,
        });

        // Build prompt
        string Prompt = JsonConvert.SerializeObject(new {
            Instructions = """
                You are the character below.
                Respond to the prompt as if you were the character.
                Never break character.
                """,
            Character = new {
                Name = Character.Name,
                Nickname = Character.Nickname,
                Bio = Character.Bio,
            },
            History = FormattedChatMessages,
            Prompt = ChatMessages.Last().Message,
        });
        
        GD.Print(Prompt);
        
        // Generate response
        string Response = await LLMBinding.PromptAsync(Prompt, OnPartial: Text => {
            GD.Print(Text);
        });
        Response = Response.Trim();
        // Ensure newer response is not being generated
        if (ResponseId < ResponseCounter) {
            return;
        }
        // Send chat message
        ChatMessageRecord ChatMessage = Storage.CreateChatMessage(ChatId, Response, Character.Id);
        AddChatMessage(ChatMessage);
    }
}