using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Ox.SleepingGods.Models.Data
{
	public sealed class KeywordLocation : Record<string>
	{
		/// <summary>
		/// Computes an unique deterministic key for this record to allow for index searching
		/// </summary>
		/// <returns>Unique GUID</returns>
		public static string DeterministicId(string keyword, string location, KeywordAction action)
			=> new Guid(SHA1.HashData(Encoding.ASCII.GetBytes($"{keyword.Trim().ToUpper()}|{location.Trim().ToUpper()}|{action}"))[..16]).ToString();

		[SetsRequiredMembers()]
		public KeywordLocation(string keyword, string location, KeywordAction action)
		{
			this.Id = DeterministicId(keyword, location, action);
			this.Keyword = keyword.Trim().ToUpper();
			this.Location = location.Trim().ToUpper();
			this.Action = action;
		}

		public required string Keyword
		{
			get;
			set => field = value.Trim().ToUpper();
		}
		public required string Location
		{
			get;
			set => field = value.Trim().ToUpper();
		}
		public required KeywordAction Action { get; set; }

	}

	public enum KeywordAction : byte
	{
		Found,
		Used,
	}
}
