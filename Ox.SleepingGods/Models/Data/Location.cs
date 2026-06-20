namespace Ox.SleepingGods.Models.Data
{
	/// <summary>
	/// Locations in Sleeping Gods
	/// </summary>
	/// <param name="Id">Location number</param>
	/// <param name="Page"></param>
	/// <param name="Danger"></param>
	/// <param name="CombatLevel"></param>
	/// <param name="KeywordsFound"></param>
	public sealed class Location : Record<string>, IComparable<Location>
	{
		public Location() : this("0", 0) { }

		public Location(int id, int page)
		{
			this.Id = id.ToString();
			this.Page = page;
		}
		public Location(string id, int page)
		{
			this.Id = id;
			this.Page = page;
		}

		public Location(KeyValuePair<string, int> initial)
		{
			this.Id = initial.Key;
			this.Page = initial.Value;
		}
		public int Page { get; init; }

		public HashSet<ResourceType> Resources { get; init; } = [];
		public HashSet<Skill> Skills { get; init; } = [];
		public DangerState Danger { get; set; } = DangerState.Unknown;
		public int? CombatLevel { get; set; } = null;
		public LocationState State { get; set; } = LocationState.Unvisited;
		public LocationReturn Return { get; set; } = LocationReturn.Undecided;
		public string? Note { get; set; } = null;

		public int CompareTo(Location? other)
		{
			if (other == null) return 1;

			static int ParseId(string id)
			{
				if (id.ToUpper().StartsWith('R'))
					return int.Parse(id.AsSpan(1)) * 1000;

				return int.Parse(id);
			}

			var id = ParseId(this.Id!);
			var otherId = ParseId(other.Id!);

			return id.CompareTo(otherId);


		}
	}

	public enum LocationState
	{
		Unvisited = 0,
		Visited = 1,
		Completed = 2,
	}

	public enum LocationReturn
	{
		Undecided = 0,
		Return = 1,
		Avoid = 2,
	}


	public enum DangerState
	{
		Unknown = 0,
		Dangerous = 1,
		Safe = 2,
	}
}
