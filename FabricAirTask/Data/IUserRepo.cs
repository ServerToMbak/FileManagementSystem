﻿using FabricAirTask.Entity;

namespace FabricAirTask.Data
{
    public interface IUserRepo
    {
        public void AddUser(User user);
        public List<User> GetAllUser(User user);
        public User GetUserByName(string name);
    }
}
