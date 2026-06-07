namespace Ox.SleepingGods.Exceptions
{
	public class InvalidGameDataException : Exception
	{
		public InvalidGameDataException()
		{
		}

		public InvalidGameDataException(string? message) : base(message)
		{
		}

		public InvalidGameDataException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
