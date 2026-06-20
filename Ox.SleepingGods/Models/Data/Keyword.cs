using System.Text.Json.Serialization;

namespace Ox.SleepingGods.Models.Data
{
	/// <summary>
	/// Describes a game keyword/quest
	/// </summary>
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
			set
			{
				field = value?.ToUpper().Trim();
				this.Index = new([value?[0] ?? Char.MinValue]);
			}
		}

		/// <summary>
		/// Partitioning for database index to stream in blocks
		/// </summary>
		public string Index
		{
			get;
			private set;
		}

		/// <summary>
		/// Whether this keyword is just a keyword or a quest (waves symbol on card)
		/// </summary>
		public KeywordType KeywordType { get; set; } = KeywordType.Unknown;

		/// <summary>
		/// Rewards to be obtained from this quest
		/// </summary>
		public HashSet<Reward> Rewards { get; set; } = [];

		/// <summary>
		/// Whether you've previously completed this quest
		/// </summary>
		[JsonPropertyName("done")]
		public bool Completed { get; set; } = false;

		public string? Note { get; set; }

		/// <summary>
		/// A quest worth revisiting
		/// </summary>
		[JsonPropertyName("fav")]
		public bool Favourite { get; set; } = false;

		/// <summary>
		/// Potential sKill checks encountered on this quest
		/// </summary>
		[JsonPropertyName("sk")]
		public HashSet<Skill> Skills { get; init; } = [];

		/// <summary>
		/// Potential combat level encountered on this quest
		/// </summary>
		[JsonPropertyName("cl")]
		public int? CombatLevel { get; set; } = null;

		public void ToggleResource(ResourceType res)
		{
			var reward = new Reward(res, 0);

			if (this.Rewards.Contains(reward))
				this.Rewards.Remove(reward);
			else
				this.Rewards.Add(reward);
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
