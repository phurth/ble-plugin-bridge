using System.Text;
using IDS.Portable.Common;

namespace OneControl.Devices
{
	public class EmbeddedVersion
	{
		private const string LogTag = "EmbeddedVersion";

		private const int ExpectedStringSize = 7;

		private const int ExpectedVersionNumberSize = 5;

		private const char Deliminator = '-';

		private const int DeliminatorIndex = 0;

		private const int RevisionIndexWithDeliminator = 1;

		private const int RevisionIndexWithMinor = 0;

		private const int MinorRevisionIndex = 1;

		private const int DefaultVersionNumber = -1;

		private const char DefaultRevision = ' ';

		public int VersionNumber { get; set; }

		public char Revision { get; set; }

		public char? MinorRevision { get; set; }

		public bool IsDefault { get; set; }

		public EmbeddedVersion()
		{
			VersionNumber = -1;
			Revision = ' ';
			MinorRevision = null;
			IsDefault = true;
		}

		public EmbeddedVersion(int versionNumber, char revision, char? revisionMinor = null)
		{
			VersionNumber = versionNumber;
			Revision = revision;
			MinorRevision = revisionMinor;
			IsDefault = false;
		}

		public static bool TryParse(byte[] bytes, out EmbeddedVersion embeddedVersion)
		{
			return TryParse(Encoding.ASCII.GetString(bytes), out embeddedVersion);
		}

		public static bool TryParse(string stringToParse, out EmbeddedVersion embeddedVersion)
		{
			embeddedVersion = new EmbeddedVersion();
			try
			{
				stringToParse = stringToParse.Substring(0, 7);
			}
			catch
			{
				TaggedLog.Information("EmbeddedVersion", $"Unable to parse embedded version, there aren't {7} valid characters");
				return false;
			}
			try
			{
				if (!int.TryParse(stringToParse.Substring(0, 5), out var result))
				{
					TaggedLog.Information("EmbeddedVersion", $"Unable to parse embedded version, the first {5} characters aren't a valid int");
					return false;
				}
				embeddedVersion.VersionNumber = result;
				string text = stringToParse.Substring(5, 2);
				if (text[0] == '-')
				{
					embeddedVersion.Revision = text[1];
				}
				else
				{
					embeddedVersion.Revision = text[0];
					embeddedVersion.MinorRevision = text[1];
				}
			}
			catch
			{
				TaggedLog.Information("EmbeddedVersion", "Unable to parse embedded version, an unexpected error occurred.");
				return false;
			}
			embeddedVersion.IsDefault = false;
			return true;
		}

		public override string ToString()
		{
			if (IsDefault)
			{
				return string.Empty;
			}
			if (MinorRevision.HasValue)
			{
				return $"{VersionNumber}{Revision}{MinorRevision}";
			}
			return $"{VersionNumber}{'-'}{Revision}";
		}
	}
}
