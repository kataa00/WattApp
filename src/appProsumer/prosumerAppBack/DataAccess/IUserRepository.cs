using prosumerAppBack.Models;

public interface IUserRepository
{
    Task<User> GetUserByIdAsync(int id);
    Task<User> GetUserByEmailAndPasswordAsync(string email, string password);
    Task<User> CreateUser(UserRegisterDto userRegisterDto);
    Task<User> GetUserByEmailAsync(string email);
    Task<List<User>> GetAllUsers();
    Task<string> GetUsernameByIdAsync(string id);
    Task<Boolean> UpdatePassword(int id, string newPassword);
    Task<IEnumerable<User>> GetUsersAsync();
    Task<User> UpdateUser(int id, UserUpdateDto userUpdateDto);
}