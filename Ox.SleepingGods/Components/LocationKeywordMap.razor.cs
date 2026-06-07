using Microsoft.AspNetCore.Components;
using Ox.SleepingGods.Models.Data;

namespace Ox.SleepingGods.Components
{
	public partial class LocationKeywordMap
	{
		[Parameter]
		public string KeywordId { get; set; } = String.Empty;

		[Parameter]
		public KeywordAction Action { get; set; }

		protected List<KeywordLocation> Locations { get; set; } = [];
		private string newLocationValue = String.Empty;
		private string? errorMessage = null;

		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			this.Locations = await this.KeywordLocationRepository.GetByKeyword(this.KeywordId, this.Action);
		}

		protected async Task Remove(string id)
		{
			await this.KeywordLocationRepository.Delete(id);
			this.Locations.RemoveAt(this.Locations.FindIndex(k => k.Id == id));
		}

		protected async Task AddKeyword()
		{
			this.errorMessage = null;
			try
			{
				KeywordLocation kw = new(this.KeywordId, this.newLocationValue, this.Action);
				await this.KeywordLocationRepository.Add(kw);
				this.Locations.Add(kw);
				this.newLocationValue = String.Empty;
			}
			catch (Exceptions.InvalidLocationException)
			{
				this.errorMessage = "Invalid location identifier, unable to add.";
			}
		}
	}
}