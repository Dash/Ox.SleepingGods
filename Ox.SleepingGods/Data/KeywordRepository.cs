using System.Reflection;
using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// Data repository for <see cref="Keyword"/>
	/// </summary>
	/// <remarks>
	/// All keywords are stored in an assembly resource file.  This provides validation on entries, so arbitary
	/// keywords cannot be added.
	/// </remarks>
	/// <inheritdoc />
	public class KeywordRepository(
		IndexedDBManager dBManager,
		Management dbinfo,
		SyncChannel sync) : Repository<Keyword, string>(dBManager, dbinfo, sync)
	{

		static KeywordRepository()
		{
			var data = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ox.SleepingGods.Data.Static.keywords.txt")
				?? throw new InvalidOperationException("No keyword reference data in embedded resource file.");

			using StreamReader sr = new(data);
			List<string> keywords = new(30);
			string? line = null;

			while ((line = sr.ReadLine()) != null)
			{
				keywords.Add(line);
			}

			keywords.Sort();

			AllKeywords = keywords.ToArray();

		}

		public static string[] AllKeywords
		{
			get;
			private set;
		}

		/// <summary>
		/// Adds a valid keyword to the user database
		/// </summary>
		/// <inheritdoc/>
		/// <exception cref="InvalidKeywordException">Keyword isn't in the game</exception>
		public override Task Add(Keyword item, bool import = false) => !AllKeywords.Contains(item.Id) ? throw new InvalidKeywordException(item.Id) : base.Add(item, import);

		/// <summary>
		/// Marks a keyword as discovered
		/// </summary>
		/// <remarks>
		/// When a keyword is encountered the record is stored in the user database.
		/// Keywords are rarely found with details, so the details are added via the <see cref="Repository{T, TKey}.Update(T)"/> base method.
		/// The location isn't covered by this repository, be sure to log the location into a <see cref="KeywordLocationRepository"/>
		/// </remarks>
		/// <param name="keyword">Keyword id</param>
		/// <param name="quest">Quest or keyword</param>
		public async Task Found(string keyword, bool? quest = null)
		{
			// See if we have a record to update
			var record = await this.GetById(keyword);

			// If we don't, create an entry!
			if (record == null)
			{
				record = new Keyword(keyword)
				{
					KeywordType = quest.ToKeywordType()
				};
				await this.Add(record);
			}
			else if (quest != null && record.KeywordType != quest.ToKeywordType())
			{
				record.KeywordType = quest.ToKeywordType();
				await this.Update(record);
			}
		}
	}
}
