public class LLMBinding(Node Inner) {
    public Node Inner = Inner;
    public string CharacterName {
        get => (string)Inner.Get("character_name");
        set => Inner.Set("character_name", value);
    }
    public string CharacterBio {
        get => (string)Inner.Get("character_bio");
        set => Inner.Set("character_bio", value);
    }

    public async Task<string> GenerateAsync(string Prompt, string Grammar = "", string Json = "", Action<string> OnPartial = null) {
        return await Await<string>(Inner.Call("generate_async", Prompt, Grammar, Json, Callable.From(OnPartial)));
    }
    public async Task<string> PromptAsync(string Prompt, int MinLength = 1, int MaxLength = 400, Action<string> OnPartial = null) {
        return await Await<string>(Inner.Call("prompt_async", Prompt, MinLength, MaxLength, Callable.From(OnPartial)));
    }

    private async Task<Variant[]> Await(Variant Coroutine) {
        return await Inner.ToSignal((GodotObject)Coroutine, "completed");
    }
    private async Task<T> Await<[MustBeVariant] T>(Variant Coroutine) {
        return (await Await(Coroutine))[0].As<T>();
    }
}