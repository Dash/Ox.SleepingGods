namespace Ox.SleepingGods.Models.Data
{
	public abstract class Record<TKey>() where TKey : IComparable
	{
		public Record(TKey id) : this()
		{
			this.Id = id;
		}

		public virtual TKey? Id { get; set; }
		public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
	}
}
