public class LLMBinding(Node Inner) {
    public Node Inner = Inner;
    public string ModelPath {
        get => (string)Inner.Get("model_path");
        set => Inner.Set("model_path", value);
    }
    public int ThreadCount {
        get => (int)Inner.Get("thread_count");
        set => Inner.Set("thread_count", value);
    }

    public async Task<string> GenerateAsync(string Prompt, string Grammar = "", string Json = "", Action<string> OnPartial = null) {
        return await Await<string>(Inner.Call("generate_async", Prompt, Grammar, Json, Callable.From(OnPartial)));
    }

    private async Task<Variant[]> Await(Variant Coroutine) {
        return await Inner.ToSignal((GodotObject)Coroutine, "completed");
    }
    private async Task<T> Await<[MustBeVariant] T>(Variant Coroutine) {
        return (await Await(Coroutine))[0].As<T>();
    }
}