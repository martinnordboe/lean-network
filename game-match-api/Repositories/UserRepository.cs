using game_match_api.DTO;
using game_match_api.Models;
using game_match_api.tools;
using MySqlConnector;

namespace game_match_api.Repositories
{
    public interface IUserRepository
    {
        Task<UserDto?> GetUserByUsername(string username);
        Task<UserDto?> GetUserById(int id);
        Task CreateUser(string username, string password);
        Task UpdateUser(User user);
        Task DeleteUserById(int id);
        Task UpdatePassword(int userId, string passwordHash);
    }

    public class UserRepository : IUserRepository
    {
        private readonly DbConnectionFactory connectionFactory;

        public UserRepository(DbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async Task<UserDto?> GetUserByUsername(string username)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
				SELECT * FROM users WHERE username = @username;
			";

            command.Parameters.AddWithValue("@username", username);

            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                UserDto userDto = new UserDto(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("username")),
                    reader.GetString(reader.GetOrdinal("password_hash")),
                    reader.GetString(reader.GetOrdinal("role"))
                );

                return userDto;
            }

            return null;
        }

        public async Task<UserDto?> GetUserById(int id)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                SELECT * FROM users WHERE id = @id;
            ";

            MySqlParameter idParameter = command.CreateParameter();
            idParameter.ParameterName = "@id";
            idParameter.Value = id;
            idParameter.DbType = System.Data.DbType.Int32;

            command.Parameters.Add(idParameter);

            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                UserDto userDto = new UserDto(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("username")),
                    reader.GetString(reader.GetOrdinal("password_hash")),
                    reader.GetString(reader.GetOrdinal("role"))
                );

                return userDto;
            }
            return null;
        }

        public async Task CreateUser(string username, string password)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
				INSERT INTO users (username, password_hash)
				VALUES (@username, @passwordHash);
			";

            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@passwordHash", password);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateUser(User user)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE users SET username = @username, password_hash = @passwordHash WHERE id = @id;
            ";

            command.Parameters.AddWithValue("@id", user.Id);
            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserById(int id)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                DELETE FROM users WHERE id = @id;
            ";

            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdatePassword(int userId, string passwordHash)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE users
                SET password_hash = @passwordHash
                WHERE id = @userId;
            ";

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);

            await command.ExecuteNonQueryAsync();
        }

        public static UserDto MapModelToDto(User user)
        {
            UserDto userDto = new UserDto(
                user.Id,
                user.Username,
                user.PasswordHash,
                user.Role
            );

            return userDto;
        }
    }
}
