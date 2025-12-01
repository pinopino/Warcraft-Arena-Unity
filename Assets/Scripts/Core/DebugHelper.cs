namespace Core
{
    public static class DebugHelper
    {
        public static string PlayerName { set; get; }
        public static string Prefix
        {
            get { return PlayerName == null ? "9." : (PlayerName.Contains("Server") ? "8." : "9."); }
        }
    }
}