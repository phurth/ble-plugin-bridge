using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public static class TextConsoleExtension
	{
		public static List<string> ToList(this ITextConsole textConsole, bool padText = true)
		{
			try
			{
				List<string> list = new List<string>();
				if (textConsole == null)
				{
					return list;
				}
				IReadOnlyList<string> lines = textConsole.Lines;
				if (lines == null || lines.Count == 0 || !textConsole.IsDetected)
				{
					return list;
				}
				lock (lines)
				{
					int width = textConsole.Size.Width;
					int height = textConsole.Size.Height;
					if (width == 0 || height == 0)
					{
						return list;
					}
					int i = 0;
					foreach (string line in textConsole.Lines)
					{
						if (i < height)
						{
							string text = line ?? "";
							list.Add(padText ? text.PadRight(width) : text);
							i++;
							continue;
						}
						break;
					}
					if (padText)
					{
						for (; i < height; i++)
						{
							list.Add(string.Empty);
						}
					}
				}
				return list;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("TextConsoleExtension", "Converting Text from TextConsole into list unexpected failure: " + ex.Message);
				return new List<string>();
			}
		}

		public static string Text(this ITextConsole textConsole, bool padText = true)
		{
			return string.Join("\n", textConsole.ToList(padText));
		}
	}
}
