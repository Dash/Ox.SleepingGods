namespace Ox.SleepingGods.Exceptions
{
	public class InvalidKeywordException : InvalidGameDataException
	{
		public string? Keyword { get; init; }
		public InvalidKeywordException()
		{
		}

		public InvalidKeywordException(string? keywordValue) : base($"Invalid game keyword.")
		{
			this.Keyword = keywordValue;
		}

		public InvalidKeywordException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
