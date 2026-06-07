using Microsoft.AspNetCore.Components;
using Ox.SleepingGods.Models.Data;

namespace Ox.SleepingGods.Components
{
	public partial class KeywordLocationMap
	{
		[Parameter]
		public string LocationId { get; set; } = String.Empty;

		[Parameter]
		public KeywordAction Action { get; set; }

		private string? errorMessage;

		protected List<KeywordLocation> Keywords { get; set; } = [];
		private string newKeywordValue = String.Empty;

		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			this.Keywords = await this.KeywordLocationRepository.GetByLocation(this.LocationId, this.Action);
		}

		protected async Task Remove(string id)
		{
			// Don't delete keywords once they have been discoverd, just unlink them.
			await this.KeywordLocationRepository.Delete(id);
			this.Keywords.RemoveAt(this.Keywords.FindIndex(k => k.Id == id));
		}

		protected async Task AddKeyword()
		{
			this.errorMessage = null;
			try
			{
				// Add keyword if required
				if (!await this.KeywordRepository.ExistsAsync(this.newKeywordValue.ToUpper().Trim()))
					await this.KeywordRepository.Add(new(this.newKeywordValue));

				KeywordLocation kw = new(this.newKeywordValue, this.LocationId, this.Action);
				await this.KeywordLocationRepository.Add(kw);
				this.Keywords.Add(kw);
				this.newKeywordValue = String.Empty;
			}
			catch (Exceptions.InvalidKeywordException)
			{
				this.errorMessage = "Cannot add a keyword not from the game - check spelling.";
			}
		}
	}
}