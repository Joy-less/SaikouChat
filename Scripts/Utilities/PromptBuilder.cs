using System.Text;

public static class PromptBuilder {
    public static string Build(string Instructions, string SceneDescription, CharacterRecord Character, IEnumerable<ChatMessageRecord> ChatMessages, IEnumerable<ChatMessageRecord> PinnedChatMessages) {
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
        foreach (ChatMessageRecord PinnedChatMessage in PinnedChatMessages) {
            PromptBuilder.Append(PinnedChatMessage.Author is not null ? $"\"{Character.Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{PinnedChatMessage.Message}\"");
        }
        PromptBuilder.AppendLine();

        PromptBuilder.AppendLine("Conversation:");
        foreach (ChatMessageRecord ChatMessage in ChatMessages) {
            PromptBuilder.Append(ChatMessage.Author is not null ? $"\"{Character.Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{ChatMessage.Message}\"");
        }
        PromptBuilder.Append($"\"{Character.Name}\": ");

        return PromptBuilder.ToString();
    }
}