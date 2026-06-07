namespace Ox.SleepingGods.Exceptions
{
	public class DuplicateRecordException : InvalidGameDataException
	{
		public DuplicateRecordException()
		{
		}

		public DuplicateRecordException(string? message) : base(message)
		{
		}

		public DuplicateRecordException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
