using System.Text;
public class PromptBuilder {
    public required string Instructions;
    public required string SceneDescription;
    public required CharacterRecord Character;
    public required IEnumerable<ChatMessageRecord> Messages;
    public required IEnumerable<ChatMessageRecord> PinnedMessages;
    public required Func<Guid, CharacterRecord> GetCharacterFromId;

    public string Build() {
        StringBuilder PromptBuilder = new();

        PromptBuilder.AppendLine("Instructions:");
        PromptBuilder.AppendLine(Instructions);
        PromptBuilder.AppendLine();

        PromptBuilder.AppendLine("Information:");
        PromptBuilder.AppendLine($"""
            Setting description: "{SceneDescription.Replace("\"", "\\\"")}"
            User's time: {DateTimeOffset.Now.ToConciseString()}
            """);
        PromptBuilder.AppendLine();

        PromptBuilder.AppendLine("Character:");
        PromptBuilder.AppendLine($"""
            Name: "{Character.Name}"
            Bio: "{Character.Bio.Replace("\"", "\\\"")}"
            """);
        PromptBuilder.AppendLine();

        PromptBuilder.AppendLine("Pinned Messages:");
        foreach (ChatMessageRecord PinnedMessage in PinnedMessages) {
            PromptBuilder.Append(PinnedMessage.Author is Guid Author ? $"\"{GetCharacterFromId(Author).Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{PinnedMessage.Message}\"");
        }
        PromptBuilder.AppendLine();

        PromptBuilder.AppendLine("Conversation:");
        foreach (ChatMessageRecord Message in Messages) {
            PromptBuilder.Append(Message.Author is Guid Author ? $"\"{GetCharacterFromId(Author).Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{Message.Message}\"");
        }
        PromptBuilder.Append($"\"{Character.Name}\": ");

        return PromptBuilder.ToString();
    }
}