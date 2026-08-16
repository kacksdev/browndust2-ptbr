using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BD2PTBR;

internal static class PlaceholderValidator
{
	private const string FormattingTokenException = "common|LocalTextTable|1081";

	private const string StoryTooltipGodsLabel = "common|StoryTooltipTextTable|2";

	private const string WorldBookKeyPrefix = "common|WorldbookTextTable|";

	private static readonly Regex TokenPattern = new Regex(@"<\s*/?[A-Za-z][^>]*>|<#[0-9A-Fa-f]{6,8}>|<\d+<|>\d+>|\{\d+\{|\}\d+\}|\[\d+\[|\]\d+\]|&\d+&|\{[^{}\r\n]+\}|%[A-Za-z][A-Za-z0-9_]*%|%\d*\$?[A-Za-z]|\\[nrt]|\[[0-9][0-9.,%+\- ]*\]|#[0-9A-Fa-f]{6,8}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static bool HasSameTokens(string key, string source, string translation, out string reason)
	{
		List<string> list = Extract(source);
		List<string> list2 = Extract(translation);
		if (string.Equals(key, FormattingTokenException, StringComparison.Ordinal))
		{
			list.RemoveAll(IsLayoutToken);
			list2.RemoveAll(IsLayoutToken);
		}
		if (string.Equals(key, StoryTooltipGodsLabel, StringComparison.Ordinal))
		{
			list.RemoveAll(token => string.Equals(token, "%Gods%", StringComparison.Ordinal));
		}
		if (!string.IsNullOrEmpty(key) && key.StartsWith(WorldBookKeyPrefix, StringComparison.Ordinal))
		{
			list.RemoveAll(token => string.Equals(token, "%Gods%", StringComparison.Ordinal));
		}
		if (list.SequenceEqual(list2, StringComparer.Ordinal))
		{
			reason = null;
			return true;
		}
		reason = "tokens do original: [" + string.Join(", ", list) + "]; tokens da tradução: [" + string.Join(", ", list2) + "]";
		return false;
	}

	private static bool IsLayoutToken(string token)
	{
		return token == "\\n" || token == "\\r" || token == "\\t";
	}

	private static List<string> Extract(string value)
	{
		return (from Match match in TokenPattern.Matches(value ?? string.Empty)
			select match.Value).ToList();
	}
}
