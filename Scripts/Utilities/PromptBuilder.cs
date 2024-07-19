using System.Text;

public static class PromptBuilder {
    public static string Build(string Instructions, string SceneDescription, CharacterRecord Character, IEnumerable<ChatMessageRecord> ChatMessages) {
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

        PromptBuilder.AppendLine("Conversation:");
        foreach (ChatMessageRecord CurrentChatMessage in ChatMessages) {
            PromptBuilder.Append(CurrentChatMessage.Author is not null ? $"\"{Character.Name}\"" : "User");
            PromptBuilder.AppendLine($": \"{CurrentChatMessage.Message}\"");
        }
        PromptBuilder.Append($"\"{Character.Name}\": ");

        return PromptBuilder.ToString();
    }
}