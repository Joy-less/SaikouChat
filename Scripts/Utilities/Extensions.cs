public static class Extensions {
    public static string ToConciseString(this DateTime DateTime) {
        return DateTime.ToString("yyyy/MM/dd HH:mm:ss");
    }
    public static string ToConciseString(this DateTimeOffset DateTimeOffset) {
        return DateTimeOffset.ToString("yyyy/MM/dd HH:mm:ss zzz");
    }
    public static bool ContainsAny(this string String, char[] AnyOf) {
        return String.IndexOfAny(AnyOf) != -1;
    }
}