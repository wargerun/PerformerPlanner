namespace Planner.Common
{
    public static class Utils
    {
        public static bool IsNullOrEmpty(this string text) => string.IsNullOrWhiteSpace(text);

        public static bool IsNotNullOrEmpty(this string text) => !text.IsNullOrEmpty();
    }
}
