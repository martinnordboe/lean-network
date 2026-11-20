using MySqlConnector;

namespace game_match.tools
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
