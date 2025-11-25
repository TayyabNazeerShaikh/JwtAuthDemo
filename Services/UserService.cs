using JwtAuthDemo.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JwtAuthDemo.Services
{
    public class UserService
    {
        private readonly string _userFilePath = "users.json";
        private readonly List<User> _users;
        private static readonly object _fileLock = new object();

        public UserService()
        {
            lock (_fileLock)
            {
                if (File.Exists(_userFilePath))
                {
                    var json = File.ReadAllText(_userFilePath);
                    _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                }
                else
                {
                    _users = new List<User>();
                }
            }
        }

        public User? GetUser(string username)
        {
            lock (_fileLock)
            {
                return _users.SingleOrDefault(u => u.Username == username);
            }
        }

        public void AddUser(User user)
        {
            lock (_fileLock)
            {
                if (_users.Any(u => u.Username == user.Username))
                {
                    return;
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                _users.Add(user);
                SaveChanges();
            }
        }

        public bool VerifyPassword(string username, string password)
        {
            var user = GetUser(username);
            return user != null && BCrypt.Net.BCrypt.Verify(password, user.Password);
        }

        private void SaveChanges()
        {
            var json = JsonSerializer.Serialize(_users);
            File.WriteAllText(_userFilePath, json);
        }
    }
}
