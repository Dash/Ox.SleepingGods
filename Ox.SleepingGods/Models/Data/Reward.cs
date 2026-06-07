namespace Ox.SleepingGods.Models.Data
{
	public record Reward(
		ResourceType Type,
		int Quantity)
	{
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
