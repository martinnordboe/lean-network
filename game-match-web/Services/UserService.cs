using game_match.DTO;
using game_match.Models;
using game_match.Repositories;
using Microsoft.AspNetCore.Identity;

namespace game_match.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByUsername(string username);
        Task<User?> GetUserById(int id);
        Task UpdateUser(User user);
        Task DeleteUserById(int id);
        Task CreateUser(string username, string password);
        Task<User?> ValidateCredentials(string username, string password);
        Task<bool> CreateNewUserAsync(CreateUserModel model);

        Task ChangePassword(int userId, string newPassword);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository repo;

        private readonly PasswordHasher<User> hasher = new PasswordHasher<User>();

        public UserService(IUserRepository repo)
        {
            this.repo = repo;
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            UserDto? dto = await repo.GetUserByUsername(username);

            if (dto != null)
            {
                return MapDtoToModel(dto);
            }
            return null;
        }

        public async Task<User?> GetUserById(int id)
        {
            UserDto? dto = await repo.GetUserById(id);
            if (dto != null)
            {
                return MapDtoToModel(dto);
            }
            return null;
        }

        public async Task UpdateUser(User user)
        {
            await repo.UpdateUser(user);
        }

        public async Task DeleteUserById(int id)
        {
            await repo.DeleteUserById(id);
        }

        public static User MapDtoToModel(UserDto dto)
        {
            User user = new User(
                dto.Id,
                dto.Username,
                dto.Passwordhash,
                dto.Role
            );

            return user;
        }

        public async Task CreateUser(string username, string password)
        {
            await repo.CreateUser(username, password);
        }

        public async Task EnsureAdminIsAddedAsync()
        {
            User? user = await GetUserByUsername("admin");

            if (user == null)
            {
                User newUser = new User("admin", string.Empty);
                string hashedPassword = hasher.HashPassword(newUser, "test123456789");
                await CreateUser("admin", hashedPassword);
            }
        }

        public async Task<bool> CreateNewUserAsync(CreateUserModel model)
        {
            string trimmedUsername = model.Username.Trim(); //Fjerner mellemrum før og efter nanv ligesom 'ValidateCredentials' gør.

            //Tjek om brugernavn eksisterer i forvejen
            User? existingUser = await GetUserByUsername(trimmedUsername);
            if (existingUser != null)
            {
                return false;
            }

            User newUser = new User(trimmedUsername, string.Empty);
            string hashedPasswrod = hasher.HashPassword(newUser, model.Password); //Hash password - Samme som i EnsureAdminIsAddedAsync
            await CreateUser(trimmedUsername, hashedPasswrod);

            return true;
        }

        public async Task<User?> ValidateCredentials(string username, string password)
        {
            string trimmedUsername = username.Trim();

            User? user = await GetUserByUsername(trimmedUsername);
            if (user == null)
            {
                return null;
            }

            PasswordVerificationResult result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return user;
        }

        public async Task ChangePassword(int userId, string newPassword)
        {
            User? user = await GetUserById(userId);

            if (user == null)
            {
                throw new Exception("Brugeren blev ikke fundet.");
            }

            string hashedPassword = hasher.HashPassword(user, newPassword);

            await repo.UpdatePassword(userId, hashedPassword);
        }
    }
}
