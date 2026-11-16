namespace game_match.DTO
{
    public class UserDto
    {
        public int Id;
        public string Username = string.Empty;
        public string Passwordhash = string.Empty;
        public string Role = "User";

        public UserDto(int id, string username, string passwordhash, string role)
        {
            Id = id;
            Username = username;
            Passwordhash = passwordhash;
            Role = role;
        }
    }
}
