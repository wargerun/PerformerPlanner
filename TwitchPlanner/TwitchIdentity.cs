namespace TwitchPlanner
{
    public struct TwitchIdentity
    {
        public string Login { get; }

        public string Password { get; }

        public TwitchIdentity(string loginUsername, string password) : this()
        {
            Login = loginUsername;
            Password = password;
        }        
    }
}
