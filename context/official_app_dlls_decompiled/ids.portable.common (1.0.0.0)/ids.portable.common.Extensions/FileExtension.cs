using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.common.Exceptions;

namespace ids.portable.common.Extensions
{
	public static class FileExtension
	{
		public enum FileIoLocation
		{
			DocumentFolder
		}

		public enum FileType
		{
			Document,
			Video,
			File
		}

		public static Environment.SpecialFolder GetFolderLocation(this FileIoLocation location)
		{
			return Environment.SpecialFolder.Personal;
		}

		public static string GetFullFilePath(string baseFilename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			return Path.Combine(Environment.GetFolderPath(location.GetFolderLocation()), baseFilename);
		}

		public static void Delete(this string filename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			File.Delete(GetFullFilePath(filename, location));
		}

		public static bool TryDelete(this string filename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			try
			{
				filename.Delete(location);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static void SaveText(this string filename, string text, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			try
			{
				File.WriteAllText(GetFullFilePath(filename, location), text);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error saving text to ");
				defaultInterpolatedStringHandler.AppendFormatted(filename);
				defaultInterpolatedStringHandler.AppendLiteral(" in ");
				defaultInterpolatedStringHandler.AppendFormatted(location);
				defaultInterpolatedStringHandler.AppendLiteral(" location: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				throw new FileExtensionException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
		}

		public static Task SaveTextAsync(this string filename, string text, FileIoLocation location = FileIoLocation.DocumentFolder, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.Run(delegate
			{
				filename.SaveText(text, location);
			}, cancellationToken);
		}

		public static string LoadText(this string filename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			return File.ReadAllText(GetFullFilePath(filename, location));
		}

		public static Task<string> LoadTextAsync(this string filename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			return Task.Run(() => filename.LoadText(location));
		}

		public static Task MoveAsync(this string fromFilename, string toFilename, FileIoLocation location = FileIoLocation.DocumentFolder)
		{
			if (string.IsNullOrEmpty(fromFilename) || string.IsNullOrEmpty(toFilename))
			{
				throw new ArgumentException("Invalid To/From Filename - null or empty string");
			}
			string fromFilenameFull = GetFullFilePath(fromFilename, location);
			string toFilenameFull = GetFullFilePath(toFilename, location);
			return Task.Run(delegate
			{
				File.Move(fromFilenameFull, toFilenameFull);
			});
		}

		public static string LoadTextFromAssemblyResource(this Assembly assembly, string location)
		{
			string result = string.Empty;
			using Stream stream = assembly.GetManifestResourceStream(location);
			if (stream == null)
			{
				return result;
			}
			using (StreamReader streamReader = new StreamReader(stream))
			{
				result = streamReader.ReadToEnd();
				streamReader.Close();
			}
			stream.Close();
			return result;
		}

		public static FileType GetFileType(this FileInfo fileInfo)
		{
			return fileInfo.Extension.GetFileTypeFromFileName();
		}

		public static FileType GetFileTypeFromFileName(this string fileName)
		{
			string extension = Path.GetExtension(fileName);
			switch (extension.TrimStart('.'))
			{
			case "mp4":
			case "avi":
				return FileType.Video;
			case "pdf":
			case "doc":
			case "txt":
				return FileType.Document;
			default:
				return FileType.File;
			}
		}
	}
}
