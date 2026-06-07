using System.Reflection;
using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// Data repository for <see cref="Location"/> records
	/// </summary>
	/// <remarks>
	/// Locations are static and known and don't need to be stored in the browser database.  Instead they are stored in
	/// the assembly resource as a list.  The database records provide the user-specified detais to those locations.
	/// </remarks>
	/// <inheritdoc />
	/// <param name="dBManager">IndexDb manager</param>
	/// <param name="dbinfo">Database metadata record</param>
	/// <param name="sync">Synchronisation channel</param>
	public class LocationRepository(IndexedDBManager dBManager, Management dbinfo, SyncChannel sync) : Repository<Location, string>(dBManager, dbinfo, sync)
	{
		/// <summary>
		/// Loads basic static location data from assembly resource
		/// </summary>
		/// <returns>Dictionary of Locations (key) and page number (value)</returns>
		/// <exception cref="InvalidOperationException">Resource data is missing</exception>
		private static Dictionary<string, int> ReadLocationData()
		{
			var data = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ox.SleepingGods.Data.Static.locations.txt")
				?? throw new InvalidOperationException("No location reference data in embedded resource file.");

			using StreamReader sr = new(data);
			Dictionary<string, int> locations = [];
			string? line = null;

			while ((line = sr.ReadLine()) != null)
			{
				var split = line.Split(',');
				locations.Add(split[0], int.Parse(split[1]));
			}

			return locations;
		}

		private readonly static Dictionary<string, int> _locations = ReadLocationData();

		/// <summary>
		/// Flat list of all locations
		/// </summary>
		public static string[] AllLocations => _locations.Keys.ToArray();

		/// <summary>
		/// Augments the static list of locations with user supplied data from the database
		/// </summary>
		/// <param name="locations"></param>
		private static void MergeInitials(ref readonly List<Location> locations)
		{
			foreach (var location in _locations)
			{
				if (!locations.Exists(x => x.Id == location.Key))
				{
					locations.Add(new(location.Key, location.Value));
				}
			}
		}

		/// <summary>
		/// Returns all locations with augmented user details
		/// </summary>
		/// <returns></returns>
		public override async Task<List<Location>> GetAll()
		{
			var saved = await base.GetAll();
			MergeInitials(ref saved);

			saved.Sort();

			return saved;
		}

		/// <summary>
		/// Gets specific location by number (regardless of user-state)
		/// </summary>
		/// <param name="key">Location</param>
		/// <returns>Information on location or null if invalid location</returns>
		public override async Task<Location?> GetById(string key)
		{
			var record = await base.GetById(key);
			return record ?? new Location(_locations.First(x => x.Key == key));
		}
	}
}
