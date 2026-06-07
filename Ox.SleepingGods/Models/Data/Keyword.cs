namespace Ox.SleepingGods.Models.Data
{
	public class Keyword : Record<string>
	{
		public Keyword() : base()
		{ }

		public Keyword(string id) : base(id)
		{
		}

		/// <summary>
		/// Keyword name
		/// </summary>
		public override string? Id
		{
			get;
			set => field = value?.ToUpper().Trim();
		}
		public KeywordType KeywordType { get; set; } = KeywordType.Unknown;
		public List<Reward> Rewards { get; set; } = [];
		public string? Note { get; set; }

		public void ToggleResource(ResourceType res)
		{
			int i;
			if ((i = this.Rewards.FindIndex(x => x.Type == res)) >= 0)
				this.Rewards.RemoveAt(i);
			else
				this.Rewards.Add(new(res, 0));
		}
	}

	public enum KeywordType
	{
		Unknown = 0,
		Standard = 1,
		Quest = 2
	}

	public static class KeywordExtensions
	{
		public static KeywordType ToKeywordType(this bool? isQuest)
		{
			if (!isQuest.HasValue)
				return KeywordType.Unknown;

			return isQuest.Value ? KeywordType.Quest : KeywordType.Standard;
		}
	}
}
