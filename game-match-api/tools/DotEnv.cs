namespace game_match_api.tools;

public static class DotEnv
{
    public static string? ReadMySqlConnectionString()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), ".env");

        if (!File.Exists(path))
        {
            return null;
        }

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("MYSQL_CONNECTION_STRING="))
            {
                return line.Split('=', 2)[1].Trim();
            }
        }

        return null;
    }
}
