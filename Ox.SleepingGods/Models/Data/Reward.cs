namespace Ox.SleepingGods.Models.Data
{
	public record Reward(
		ResourceType Type,
		int Quantity)
	{
		public override int GetHashCode() => (int)this.Type;
	}

	[Flags]
	public enum ResourceType : int
	{
		Coin,
		Grain,
		Meat,
		Vegetables,
		Materials,
		Artefact,
		Totem,
		XP,
		Adventure,
	}
}
