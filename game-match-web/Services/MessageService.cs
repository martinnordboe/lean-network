namespace game_match.Services
{
	public class MessageService
	{
		public string? Message { get; private set; }

		public event Action? MessageChanged;

		public void ShowMessage(string message)
		{
			Message = message;
			MessageChanged?.Invoke();
		}
	}
}
