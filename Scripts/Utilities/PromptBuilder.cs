using System.Linq;
using System.Text;

public class PromptBuilder {
    public required string Instructions;
    public required string SceneDescription;
    public required IEnumerable<CharacterRecord> Characters;
    public required IEnumerable<ChatMessageRecord> Messages;
    public required IEnumerable<ChatMessageRecord> PinnedMessages;
    public required Func<Guid, CharacterRecord> GetCharacterFromId;

    public string Build() {
        StringBuilder PromptBuilder = new();

        // Instructions for the response
        PromptBuilder.AppendLine("Instructions:");
        PromptBuilder.AppendLine(Instructions);
        PromptBuilder.AppendLine();

        // Extra information to aid the response
        PromptBuilder.AppendLine("Information:");
        PromptBuilder.AppendLine($"""
            Setting description: "{SceneDescription.Replace("\"", "\\\"")}"
            User's time: {DateTimeOffset.Now.ToConciseString()}
            """);
        PromptBuilder.AppendLine();

        // Descriptions of characters present in the conversation
        PromptBuilder.AppendLine("Characters:");
        foreach (CharacterRecord Character in Characters) {
            PromptBuilder.AppendLine($$"""
                • "{{Character.Name}}": "{{Character.Bio.Replace("\"", "\\\"")}}"
                """);
        }
        PromptBuilder.AppendLine();

        // Messages pinned by the user
        PromptBuilder.AppendLine("Pinned Messages:");
        foreach (ChatMessageRecord PinnedMessage in PinnedMessages) {
            PromptBuilder.Append(PinnedMessage.Author is Guid Author ? $"\"{GetCharacterFromId(Author).Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{PinnedMessage.Message}\"");
        }
        PromptBuilder.AppendLine();

        // Recent messages
        PromptBuilder.AppendLine("Conversation:");
        foreach (ChatMessageRecord Message in Messages) {
            PromptBuilder.Append(Message.Author is Guid Author ? $"\"{GetCharacterFromId(Author).Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{Message.Message}\"");
        }

        // Template for the response
        if (Characters.Count() > 1) {
            PromptBuilder.Append($"[{string.Join("/", Characters.Select(Character => $"\"{Character.Name}\""))}]: ");
        }
        else {
            PromptBuilder.Append($"\"{Characters.First().Name}\": ");
        }

        return PromptBuilder.ToString();
    }
}