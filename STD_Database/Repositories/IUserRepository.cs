using Microsoft.EntityFrameworkCore;
using STD_Database.Database;
using STD_Database.DTO;
using STD_Database.Model;

namespace STD_Database.Repositories
{
    public interface IUserRepository
    {
        Task<UserDTO?> GetUserAsync(string username,string password);
        Task<UserModel?> GetDbUserByUserName(string username);
        Task SaveAsync();
    }
    public class UserRepository : IUserRepository
    {
        private readonly StudentDB _db;
        public UserRepository(StudentDB db)
        {
            _db = db;
        }
        public async Task<UserDTO?> GetUserAsync(string username, string password)
        {
            var User = await _db.Users.FirstOrDefaultAsync(U => U.UserName == username);
            if (User == null || !BCrypt.Net.BCrypt.Verify(password, User.PassWord))
            {
                return null;
            }
            return new UserDTO
            {
                username = User.UserName,
                role = User.Role,
            };
        }
        public async Task<UserModel?> GetDbUserByUserName(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(U => U.UserName == username);
        }
        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
