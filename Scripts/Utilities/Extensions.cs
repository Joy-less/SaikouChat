public static class Extensions {
    public static string ToConciseString(this DateTime DateTime) {
        return DateTime.ToString("yyyy/MM/dd HH:mm:ss");
    }
}