namespace Ox.SleepingGods.Exceptions
{
	public class InvalidLocationException : InvalidGameDataException
	{
		public string? Location { get; init; }
		public InvalidLocationException()
		{
		}

		public InvalidLocationException(string? locationValue) : base($"Invalid game keyword.")
		{
			this.Location = locationValue;
		}

		public InvalidLocationException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
