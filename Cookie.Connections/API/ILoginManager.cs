﻿using static Cookie.Connections.API.User;

namespace Cookie.Connections.API
{
    public interface ILoginManager
    {

        public User? GetUser(string username, string hash);

        public User? GetUser(string token);

        public User? CreateUser(string username, string hash, PermissionLevel level);

        public void Save();

        public void Load();


    }

}
