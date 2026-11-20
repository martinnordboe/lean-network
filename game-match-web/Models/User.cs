namespace game_match.Models
{
	public class User
	{
		public int Id { get; set; } = 1;
		public string Username { get; set; } = string.Empty;

		public string PasswordHash { get; set; } = string.Empty;
		public string Role { get; set; } = "User";

		public User()
		{

		}
		public User(string username, string passwordHash)
		{
			Username = username;
			PasswordHash = passwordHash;
		}

		public User(int id, string username, string passwordHash, string role="User")
		{
			Id = id;
			Username = username;
			PasswordHash = passwordHash;
			Role = role;
		}
	}
}
