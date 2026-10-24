using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BD2PTBR;
using Newtonsoft.Json;

namespace BD2PTBR.RuntimeCatalogAudit;

internal sealed class AuditRecord
{
	public string Key { get; set; }

	public string Source { get; set; }

	public string Translation { get; set; }
}

internal static class Program
{
	private const int ExpectedEffectiveRecords = 223731;

	private static int Main(string[] args)
	{
		int expectedEffectiveRecords = ExpectedEffectiveRecords;
		if (args.Length > 1 && (!int.TryParse(args[1], out expectedEffectiveRecords) || expectedEffectiveRecords <= 0))
		{
			Console.Error.WriteLine("Quantidade efetiva esperada inválida: " + args[1]);
			return 2;
		}
		string catalogDirectory = args.Length > 0
			? Path.GetFullPath(args[0])
			: Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "work", "release-catalog-v090"));
		if (!Directory.Exists(catalogDirectory))
		{
			Console.Error.WriteLine("Catálogo não encontrado: " + catalogDirectory);
			return 2;
		}

		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		List<object> failures = new List<object>();
		int invalidCount = 0;
		string[] files = Directory.GetFiles(catalogDirectory, "*.jsonl", SearchOption.AllDirectories);
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		Array.Reverse(files);
		foreach (string file in files)
		{
			int lineNumber = 0;
			foreach (string line in File.ReadLines(file))
			{
				lineNumber++;
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				AuditRecord record = JsonConvert.DeserializeObject<AuditRecord>(line);
				bool usableTranslation = record != null && record.Translation != null &&
					(!string.IsNullOrWhiteSpace(record.Translation) || string.IsNullOrWhiteSpace(record.Source));
				if (record == null || string.IsNullOrWhiteSpace(record.Key) || !usableTranslation || !seen.Add(record.Key))
				{
					continue;
				}
				if (!PlaceholderValidator.HasSameTokens(record.Key, record.Source, record.Translation, out string reason))
				{
					invalidCount++;
					if (failures.Count < 20)
					{
						failures.Add(new
						{
							file = Path.GetFileName(file),
							line = lineNumber,
							key = record.Key,
							reason
						});
					}
				}
			}
		}

		var result = new
		{
			expectedEffectiveRecords,
			effectiveRecords = seen.Count,
			runtimeMarkerFailures = invalidCount,
			ready = seen.Count == expectedEffectiveRecords && invalidCount == 0,
			failures
		};
		Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
		return result.ready ? 0 : 1;
	}
}
