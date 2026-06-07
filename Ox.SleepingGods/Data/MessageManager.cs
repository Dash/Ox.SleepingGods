namespace Ox.SleepingGods.Data
{
	public delegate Task MessageRaised(string message);

	/// <summary>
	/// Holds user feedback messages for common UI.
	/// </summary>
	public class MessageManager
	{
		public List<string> Errors { get; } = [];

		/// <summary>
		/// Listen for new messages from <see cref="AddMessage(string)"/>. Don't let exceptions bubble.
		/// </summary>
		public MessageRaised? OnMesssage;

		/// <summary>
		/// Adds a message to <see cref="Errors"/> and also triggers any subscribers to <see cref="OnMesssage"/>
		/// </summary>
		/// <param name="message">Text to display</param>
		public async Task AddMessage(string message)
		{
			this.Errors.Add(message);
			await (this.OnMesssage?.Invoke(message) ?? Task.CompletedTask);
		}
	}
}
