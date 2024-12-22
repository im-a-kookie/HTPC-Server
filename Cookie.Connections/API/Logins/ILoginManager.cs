using static Cookie.Connections.API.Logins.User;

namespace Cookie.Connections.API.Logins
{
    public interface ILoginManager<T>
    {

        public Controller<T>? GetController();

        public User? GetUser(string username, string hash);

        public User? GetUser(string token);

        public User? CreateUser(string username, string hash, PermissionLevel level);

        public void Save();

        public void Load();

        public int GetUserCount();


    }

}
