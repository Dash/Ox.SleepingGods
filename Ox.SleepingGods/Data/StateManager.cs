namespace Ox.SleepingGods.Data
{
	public class StateManager
	{
	}

	/// <summary>
	/// State that needs tracking for <see cref="Pages.Home"/>
	/// </summary>
	public sealed class HomeStateManager : StateManager
	{
		public int PageFilter { get; set; } = 0;
		public Models.Data.ResourceType? ResourceFilter { get; set; }
	}

}
