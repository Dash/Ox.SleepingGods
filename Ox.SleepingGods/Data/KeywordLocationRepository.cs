using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// Many-to-many bridge of keywords and locations
	/// </summary>
	/// <remarks>
	/// A keyword can be found in many different locations around the map
	/// A keyword can be required in many different locations around the map
	/// 
	/// This store provides the bridge to find either:
	/// 1) all the keywords at a location
	/// 2) all the locations for a keyword
	/// 
	/// And both can be filtered by the keyword being found, or required for that location
	/// </remarks>
	/// <inheritdoc/>
	public class KeywordLocationRepository(
		IndexedDBManager dBManager,
		Management dbinfo,
		SyncChannel sync) : Repository<Models.Data.KeywordLocation, string>(dBManager, dbinfo, sync)
	{
		public override async Task Add(KeywordLocation item, bool import = false)
		{
			// Check keyword is valid
			if (!KeywordRepository.AllKeywords.Contains(item.Keyword))
				throw new InvalidKeywordException(item.Keyword);

			// Check location is valid
			if (!LocationRepository.AllLocations.Contains(item.Location))
				throw new InvalidLocationException(item.Location);

			// Silently avoid duplicates
			if ((await this.GetById(item.Id!)) == null)
				await base.Add(item, import);
		}

		/// <summary>
		/// Gets mapped items for a given location
		/// </summary>
		/// <param name="location">Location number</param>
		/// <param name="action">Found or Used keywords</param>
		/// <returns>List of recorded keywords-location maps</returns>
		public async Task<List<KeywordLocation>> GetByLocation(string location, KeywordAction action)
		{
			var r = await this.manager.GetAllRecordsByIndex<string, KeywordLocation>(new StoreIndexQuery<string>()
			{
				AllMatching = true,
				QueryValue = location,
				Storename = this.storeName,
				IndexName = $"{this.storeName}_Location"
			});

			return r.Where(l => l.Action == action).ToList();
		}

		/// <summary>
		/// Gets mapped items for a given keyword
		/// </summary>
		/// <param name="keyword">Keyword id</param>
		/// <param name="action">Found or Used keywords</param>
		/// <returns>List of recorded keywords-locations maps</returns>
		public async Task<List<KeywordLocation>> GetByKeyword(string keyword, KeywordAction action)
		{

			var r = await this.manager.GetAllRecordsByIndex<string, KeywordLocation>(new StoreIndexQuery<string>()
			{
				AllMatching = true,
				QueryValue = keyword,
				Storename = this.storeName,
				IndexName = $"{this.storeName}_Keyword"
			});

			return r.Where(k => k.Action == action).ToList();
		}
	}
}
