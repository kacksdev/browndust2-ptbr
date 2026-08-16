using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace BD2PTBR;

internal sealed class TranslationCatalog
{
	private readonly Dictionary<string, TranslationRecord> _translations;

	private readonly Dictionary<string, string> _translationsByTableAndSource;

	private readonly Dictionary<string, string> _translationsBySource;

	private readonly HashSet<string> _translatedOutputs;

	private readonly HashSet<string> _protectedOriginalSources;

	private readonly ConcurrentDictionary<string, string> _missing = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

	private readonly ConcurrentDictionary<string, byte> _stale = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

	private readonly ManualLogSource _logger;

	public int Count => _translations.Count;

	public int SourceIndexCount => _translationsBySource.Count;

	public int ProtectedOriginalSourceCount => _protectedOriginalSources.Count;

	public bool ContainsKey(string key)
	{
		return !string.IsNullOrEmpty(key) && _translations.ContainsKey(key);
	}

	public bool HasCurrentSource(string key, string source)
	{
		if (string.IsNullOrEmpty(key) || !_translations.TryGetValue(key, out TranslationRecord record))
		{
			return false;
		}
		if (string.Equals(NormalizeGameText(record.Source), NormalizeGameText(source), StringComparison.Ordinal))
		{
			return true;
		}
		return TryApplyRuntimePlaceholder(record.Source, record.Translation, source, "{MASTER_NAME}", out _);
	}

	private TranslationCatalog(Dictionary<string, TranslationRecord> translations, Dictionary<string, string> translationsByTableAndSource, Dictionary<string, string> translationsBySource, HashSet<string> translatedOutputs, HashSet<string> protectedOriginalSources, ManualLogSource logger)
	{
		_translations = translations;
		_translationsByTableAndSource = translationsByTableAndSource;
		_translationsBySource = translationsBySource;
		_translatedOutputs = translatedOutputs;
		_protectedOriginalSources = protectedOriginalSources;
		_logger = logger;
	}

	public static TranslationCatalog Load(string directory, ManualLogSource logger)
	{
		Dictionary<string, TranslationRecord> dictionary = new Dictionary<string, TranslationRecord>(StringComparer.Ordinal);
		if (!Directory.Exists(directory))
		{
			logger.LogWarning("Pasta de traduções não encontrada: " + directory);
			return new TranslationCatalog(dictionary, new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal), logger);
		}
		foreach (string item in Directory.GetFiles(directory, "*.jsonl", SearchOption.AllDirectories).OrderBy((string path) => path, StringComparer.OrdinalIgnoreCase))
		{
			int num = 0;
			foreach (string item2 in File.ReadLines(item, Encoding.UTF8))
			{
				num++;
				if (string.IsNullOrWhiteSpace(item2))
				{
					continue;
				}
				try
				{
					TranslationRecord translationRecord = JsonConvert.DeserializeObject<TranslationRecord>(item2);
					if (translationRecord != null && !string.IsNullOrWhiteSpace(translationRecord.Key) && IsUsableTranslation(translationRecord))
					{
						dictionary[translationRecord.Key] = translationRecord;
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning("Linha inválida em " + item + ":" + num + " - " + ex.Message);
				}
			}
		}
		foreach (KeyValuePair<string, TranslationRecord> item in dictionary.ToArray())
		{
			TranslationRecord record = item.Value;
			if (!PlaceholderValidator.HasSameTokens(item.Key, record.Source, record.Translation, out var reason))
			{
				logger.LogWarning("Tradução ignorada por alterar marcadores em " + item.Key + ": " + reason);
				dictionary.Remove(item.Key);
			}
		}
		Dictionary<string, string> translationsByTableAndSource = BuildTableAndSourceIndex(dictionary.Values);
		Dictionary<string, string> translationsBySource = BuildSourceIndex(dictionary.Values);
		HashSet<string> translatedOutputs = BuildTranslatedOutputIndex(dictionary.Values);
		HashSet<string> protectedOriginalSources = BuildProtectedOriginalSourceIndex(dictionary.Values);
		return new TranslationCatalog(dictionary, translationsByTableAndSource, translationsBySource, translatedOutputs, protectedOriginalSources, logger);
	}

	private static bool IsUsableTranslation(TranslationRecord record)
	{
		return record.Translation != null &&
			(!string.IsNullOrWhiteSpace(record.Translation) || string.IsNullOrWhiteSpace(record.Source));
	}

	public bool TryTranslate(string key, string originalResult, out string translation)
	{
		if (!_translations.TryGetValue(key, out var value))
		{
			translation = null;
			return false;
		}
		string a = NormalizeGameText(value.Source);
		string b = NormalizeGameText(originalResult);
		string text = NormalizeGameText(value.Translation);
		if (string.Equals(text, b, StringComparison.Ordinal))
		{
			translation = text;
			return true;
		}
		if (TryApplyRuntimePlaceholder(value.Source, value.Translation, originalResult, "{MASTER_NAME}", out translation))
		{
			return true;
		}
		if (!string.Equals(a, b, StringComparison.Ordinal))
		{
			if (_stale.TryAdd(key, 0))
			{
				_logger.LogWarning("Tradução desatualizada ignorada: " + key);
			}
			translation = null;
			return false;
		}
		translation = text;
		return true;
	}

	public bool TryTranslateByTableAndSource(string table, string originalResult, out string translation)
	{
		string text = NormalizeGameText(originalResult);
		if (_protectedOriginalSources.Contains(text))
		{
			translation = null;
			return false;
		}
		if (_translatedOutputs.Contains(text))
		{
			translation = null;
			return false;
		}
		return _translationsByTableAndSource.TryGetValue(BuildTableAndSourceKey(table, text), out translation);
	}

	public bool TryTranslateBySource(string originalResult, out string translation)
	{
		string text = NormalizeGameText(originalResult);
		if (_protectedOriginalSources.Contains(text))
		{
			translation = null;
			return false;
		}
		if (_translatedOutputs.Contains(text))
		{
			translation = null;
			return false;
		}
		if (!_translationsBySource.TryGetValue(text, out translation) || string.Equals(text, translation, StringComparison.Ordinal))
		{
			translation = null;
			return false;
		}
		return true;
	}

	public bool IsTranslatedOutput(string value)
	{
		return !string.IsNullOrEmpty(value) && _translatedOutputs.Contains(NormalizeGameText(value));
	}

	public void RecordMissing(string key, string source)
	{
		if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(source))
		{
			_missing.TryAdd(key, source);
		}
	}

	public void FlushMissing(string outputPath)
	{
		if (_missing.IsEmpty)
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		using StreamWriter streamWriter = new StreamWriter(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		foreach (KeyValuePair<string, string> item in _missing.OrderBy((KeyValuePair<string, string> item) => item.Key, StringComparer.Ordinal))
		{
			streamWriter.Write("{\"key\":\"");
			streamWriter.Write(EscapeJson(item.Key));
			streamWriter.Write("\",\"source\":\"");
			streamWriter.Write(EscapeJson(item.Value));
			streamWriter.WriteLine("\"}");
		}
	}

	private static string NormalizeGameText(string value)
	{
		return (value ?? string.Empty).Replace("\\n", "\n").Replace("\\\"", "\"").Normalize(NormalizationForm.FormC);
	}

	private static Dictionary<string, string> BuildTableAndSourceIndex(IEnumerable<TranslationRecord> records)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (TranslationRecord record in records)
		{
			string text = record.Table;
			if (string.IsNullOrEmpty(text))
			{
				string[] array = (record.Key ?? string.Empty).Split('|');
				if (array.Length >= 2)
				{
					text = array[1];
				}
			}
			if (string.IsNullOrEmpty(text) || (!string.Equals(text, "PayAreaNameTextTable", StringComparison.Ordinal) && !string.Equals(text, "HardcodedText", StringComparison.Ordinal)) || string.IsNullOrEmpty(record.Source) || string.IsNullOrEmpty(record.Translation))
			{
				continue;
			}
			string text2 = BuildTableAndSourceKey(text, NormalizeGameText(record.Source));
			string text3 = NormalizeGameText(record.Translation);
			if (!string.Equals(NormalizeGameText(record.Source), text3, StringComparison.Ordinal))
			{
				if (dictionary.TryGetValue(text2, out var value) && !string.Equals(value, text3, StringComparison.Ordinal))
				{
					hashSet.Add(text2);
				}
				else
				{
					dictionary[text2] = text3;
				}
			}
		}
		foreach (string item in hashSet)
		{
			dictionary.Remove(item);
		}
		return dictionary;
	}

	private static Dictionary<string, string> BuildSourceIndex(IEnumerable<TranslationRecord> records)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (TranslationRecord record in records)
		{
			if (string.IsNullOrEmpty(record.Source) || string.IsNullOrEmpty(record.Translation))
			{
				continue;
			}
			string text = NormalizeGameText(record.Source);
			string text2 = NormalizeGameText(record.Translation);
			if (!string.Equals(text, text2, StringComparison.Ordinal))
			{
				if (dictionary.TryGetValue(text, out var value) && !string.Equals(value, text2, StringComparison.Ordinal))
				{
					hashSet.Add(text);
				}
				else
				{
					dictionary[text] = text2;
				}
			}
		}
		foreach (string item in hashSet)
		{
			dictionary.Remove(item);
		}
		return dictionary;
	}

	private static HashSet<string> BuildTranslatedOutputIndex(IEnumerable<TranslationRecord> records)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (TranslationRecord record in records)
		{
			if (!string.IsNullOrEmpty(record.Source) && !string.IsNullOrEmpty(record.Translation))
			{
				string a = NormalizeGameText(record.Source);
				string text = NormalizeGameText(record.Translation);
				if (!string.Equals(a, text, StringComparison.Ordinal))
				{
					hashSet.Add(text);
				}
			}
		}
		return hashSet;
	}

	private static HashSet<string> BuildProtectedOriginalSourceIndex(IEnumerable<TranslationRecord> records)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (TranslationRecord record in records)
		{
			if (string.Equals(record.Scope, "common", StringComparison.Ordinal) &&
				string.Equals(record.Table, "NameTextTable", StringComparison.Ordinal) &&
				record.Id >= 4310000 && record.Id <= 4339999 &&
				!string.IsNullOrEmpty(record.Source))
			{
				hashSet.Add(NormalizeGameText(record.Source));
			}
		}
		return hashSet;
	}

	private static string BuildTableAndSourceKey(string table, string source)
	{
		return (table ?? string.Empty) + "\u001f" + (source ?? string.Empty);
	}

	private static bool TryApplyRuntimePlaceholder(string sourceTemplate, string translationTemplate, string runtimeSource, string token, out string translation)
	{
		translation = null;
		if (string.IsNullOrEmpty(sourceTemplate) || string.IsNullOrEmpty(translationTemplate) || string.IsNullOrEmpty(runtimeSource))
		{
			return false;
		}
		string source = NormalizeGameText(sourceTemplate);
		string translated = NormalizeGameText(translationTemplate);
		string runtime = NormalizeGameText(runtimeSource);
		int tokenIndex = source.IndexOf(token, StringComparison.Ordinal);
		if (tokenIndex < 0 || translated.IndexOf(token, StringComparison.Ordinal) < 0)
		{
			return false;
		}
		string prefix = source.Substring(0, tokenIndex);
		string suffix = source.Substring(tokenIndex + token.Length);
		if (!runtime.StartsWith(prefix, StringComparison.Ordinal) || !runtime.EndsWith(suffix, StringComparison.Ordinal) ||
			runtime.Length < prefix.Length + suffix.Length)
		{
			return false;
		}
		string runtimeValue = runtime.Substring(prefix.Length, runtime.Length - prefix.Length - suffix.Length);
		translation = translated.Replace(token, runtimeValue);
		return true;
	}

	private static string EscapeJson(string value)
	{
		return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
	}
}
