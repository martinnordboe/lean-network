using MySqlConnector;

namespace game_match_api.tools
{
    public class DbConnectionFactory(string? connectionString)
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Fejl, måske manglende .env fil");
            }

             return new MySqlConnection(connectionString);
        }
    }
}
