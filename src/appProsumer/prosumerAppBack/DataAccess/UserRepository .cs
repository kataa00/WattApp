using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prosumerAppBack.Helper;
using prosumerAppBack.Models;
using System.Security.Cryptography;

namespace prosumerAppBack.DataAccess;

public class UserRepository : IUserRepository
{
    private readonly DataContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public UserRepository(DataContext dbContext,IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;

    }
    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _dbContext.Users.FindAsync(id);
    }

    public async Task<User> GetUserByUsernameAndPasswordAsync(string username, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return null;
        }
        if (!_passwordHasher.VerifyPassword(password, user.Salt, user.PasswordHash))
        {
            return null;
        }
        return user;
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<User> CreateUser(UserRegisterDto userRegisterDto)
    {
        byte[] salt;
        byte[] hash;
        (salt,hash)= _passwordHasher.HashPassword(userRegisterDto.Password);
        var newUser = new User
        {
            UserName = userRegisterDto.Username,
            PhoneNumber = userRegisterDto.PhoneNumber,
            Email = userRegisterDto.Email,
            Address = userRegisterDto.Address.Split(",")[0],
            City = userRegisterDto.Address.Split(",")[1],
            Country = userRegisterDto.Address.Split(",")[2],
            Salt = salt,
            PasswordHash = hash,
            Role = "RegularUser",
            ID = Guid.NewGuid(),
        };
        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();
        return newUser;
    }

    public Task<List<User>> GetAllUsers()
    {
        return _dbContext.Users.ToListAsync();
    }

    public async Task<User> CreateUserPasswordResetTokenAsync(User user)
    {
        user.PasswordResetToken = CreateRandomToken();
        user.ResetTokenExpires = DateTime.Now.AddDays(1);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private string CreateRandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
    }
    public async Task<User> GetUserByPasswordResetToken(string token)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        if(user.ResetTokenExpires < DateTime.Now)
        {
            user = null;
        }
        if (user == null)
        {
            return null;
        }

        return user;
    }
}