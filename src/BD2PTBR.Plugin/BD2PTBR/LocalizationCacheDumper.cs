using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;

namespace BD2PTBR;

internal static class LocalizationCacheDumper
{
	private sealed class RawOutputDescriptor
	{
		public string KeyTable { get; }

		public string[] SourceColumns { get; }

		public RawOutputDescriptor(string keyTable, params string[] sourceColumns)
		{
			KeyTable = keyTable;
			SourceColumns = sourceColumns;
		}
	}

	private sealed class RawTableDescriptor
	{
		public string Database { get; }

		public string Scope { get; }

		public string Table { get; }

		public bool IsPackScoped { get; }

		public RawOutputDescriptor[] Outputs { get; }

		public RawTableDescriptor(string database, string scope, string table, bool isPackScoped,
			params RawOutputDescriptor[] outputs)
		{
			Database = database;
			Scope = scope;
			Table = table;
			IsPackScoped = isPackScoped;
			Outputs = outputs;
		}
	}

	private sealed class CacheDescriptor
	{
		public string FieldName { get; }

		public string Scope { get; }

		public string Table { get; }

		public bool IsPackScoped { get; }

		public CacheDescriptor(string fieldName, string scope, string table, bool isPackScoped)
		{
			FieldName = fieldName;
			Scope = scope;
			Table = table;
			IsPackScoped = isPackScoped;
		}
	}

	// Campos dos caches de localização do cliente 2.31.8. A coleta só roda
	// quando o modo de diagnóstico é ativado e serve para descobrir entradas
	// adicionadas pelo jogo sem percorrer ou alterar componentes visuais.
	private static readonly CacheDescriptor[] Caches =
	{
		new CacheDescriptor("ὮὡὩὣὮὭὠὥὮὨὠ", null, "LocalTextTable", true),
		new CacheDescriptor("ὡὬὠὦὨὧὣὯὣὣὠ", null, "QuestTextTable", true),
		new CacheDescriptor("ὠὮὠὢὦὢὧὪὪὢὣ", null, "StoryTextTable", true),
		new CacheDescriptor("ὧὨὩὬὪὥὦὥὮὢὤ", "common", "NameTextTable", false),
		new CacheDescriptor("ὧὫὧὭὤὤὢὦὩὪὦ", "common", "StoryTooltipTextTable", false),
		new CacheDescriptor("ὢὪὫὡὧὩὢὦὣὦὢ", "common", "WorldbookTextTable", false),
		new CacheDescriptor("ὪὫὩὣὨὠὠὮὥὮὬ", "common", "ProfileTextTable", false),
		new CacheDescriptor("ὮὩὬὣὮὦὦὬὨὢὫ", "common", "TooltipTextTable", false),
		new CacheDescriptor("ὥὣὢὫὦὡὩὠὡὮὦ", "common", "DatingTextTable", false),
		new CacheDescriptor("ὫὯὦὦὨὩὨὣὦὦὫ", "common", "SkillTextTable.Text", false),
		new CacheDescriptor("ὤὠὡὨὭὨὪὧὫὫὮ", "common", "SkillTextTable.NodeAddText", false),
		new CacheDescriptor("ὬὭὡὪὣὭὪὤὧὬὢ", "common", "RandomBoxTextTable", false),
		new CacheDescriptor("ὡὫὥὫὮὤὩὨὧὨὥ", "intro", "IntroTextTable", false),
		new CacheDescriptor("ὯὦὨὠὣὠὨὮὪὦὡ", "common", "SpineInteractionTextTable", false),
	};

	// O cache LRU contém apenas textos já solicitados pela interface. No modo de
	// desenvolvimento, estas consultas leem uma tabela por ciclo diretamente do
	// gerenciador de dados do próprio cliente, sem modificar o banco do jogo.
	private static readonly RawTableDescriptor[] RawTables =
	{
		new RawTableDescriptor("DB_COMMON", "common", "WorldbookTextTable", false,
			new RawOutputDescriptor("WorldbookTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "StoryTooltipTextTable", false,
			new RawOutputDescriptor("StoryTooltipTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "StoryTextTable", false,
			new RawOutputDescriptor("StoryTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "LocalTextTable", false,
			new RawOutputDescriptor("LocalTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "QuestTextTable", false,
			new RawOutputDescriptor("QuestTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "NameTextTable", false,
			new RawOutputDescriptor("NameTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "ProfileTextTable", false,
			new RawOutputDescriptor("ProfileTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "TooltipTextTable", false,
			new RawOutputDescriptor("TooltipTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "DatingTextTable", false,
			new RawOutputDescriptor("DatingTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "RandomBoxTextTable", false,
			new RawOutputDescriptor("RandomBoxTextTable", "textEn")),
		new RawTableDescriptor("DB_COMMON", "common", "SpineInteractionTextTable", false,
			new RawOutputDescriptor("SpineInteractionTextTable", "textEn")),
		new RawTableDescriptor("DB_INTRO", "intro", "IntroTextTable", false,
			new RawOutputDescriptor("IntroTextTable", "textEn")),
		new RawTableDescriptor("DB_PACK", null, "LocalTextTable", true,
			new RawOutputDescriptor("LocalTextTable", "textEn")),
		new RawTableDescriptor("DB_PACK", null, "QuestTextTable", true,
			new RawOutputDescriptor("QuestTextTable", "textEn")),
		new RawTableDescriptor("DB_PACK", null, "StoryTextTable", true,
			new RawOutputDescriptor("StoryTextTable", "textEn")),
	};

	private static int _nextRawTableIndex;

	public static int CaptureMissing(TranslationCatalog catalog, ManualLogSource logger)
	{
		Type localizationType = LocalizationPatcher.ResolvedLocalizationType;
		if (catalog == null || localizationType == null)
		{
			return 0;
		}
		int captured = 0;
		int availableCaches = 0;
		int enumeratedKeys = 0;
		foreach (CacheDescriptor descriptor in Caches)
		{
			try
			{
				FieldInfo field = localizationType.GetField(descriptor.FieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				object cache = field?.GetValue(null);
				if (cache == null)
				{
					continue;
				}
				availableCaches++;
				Type cacheType = cache.GetType();
				Type[] genericArguments = cacheType.GetGenericArguments();
				if (genericArguments.Length != 2 || genericArguments[1] != typeof(string))
				{
					continue;
				}
				Type keyType = genericArguments[0];
				MethodInfo lookup = cacheType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.FirstOrDefault(method => method.ReturnType == typeof(string) &&
						method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == keyType);
				if (lookup == null)
				{
					continue;
				}
				foreach (object rawKey in EnumerateKeys(cache, keyType))
				{
					enumeratedKeys++;
					if (!TryBuildKey(descriptor, rawKey, out string key))
					{
						continue;
					}
					string source;
					try
					{
						source = lookup.Invoke(cache, new[] { rawKey }) as string;
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrWhiteSpace(source) || catalog.IsTranslatedOutput(source) ||
						catalog.HasCurrentSource(key, source))
					{
						continue;
					}
					catalog.RecordMissing(key, source);
					captured++;
				}
			}
			catch (Exception ex)
			{
				logger?.LogWarning("Falha ao inspecionar o cache " + descriptor.Table + ": " + ex.Message);
			}
		}
		int rawCaptured = CaptureNextRawTable(catalog, logger);
		logger?.LogInfo("[PTBR-DIAGNOSTICO] caches disponíveis=" + availableCaches +
			", chaves examinadas=" + enumeratedKeys + ", novas do cache=" + captured +
			", novas da tabela bruta=" + rawCaptured + ".");
		return captured + rawCaptured;
	}

	private static int CaptureNextRawTable(TranslationCatalog catalog, ManualLogSource logger)
	{
		if (_nextRawTableIndex >= RawTables.Length || !LocalizationPatcher.CommonDatabaseObserved)
		{
			return 0;
		}
		try
		{
			Type rawDataType = Type.GetType("RawDataManager, Assembly-CSharp", throwOnError: false);
			object rawData = GetSingletonInstance(rawDataType);
			MethodInfo queryMethod = rawDataType?.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.FirstOrDefault(method => method.Name == "GetTableDatasFromQuery" &&
					method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(string));
			if (rawData == null || queryMethod == null)
			{
				return 0;
			}
			RawTableDescriptor descriptor = RawTables[_nextRawTableIndex];
			Type databaseType = queryMethod.GetParameters()[0].ParameterType;
			object database = Enum.Parse(databaseType, descriptor.Database);
			string scope = descriptor.IsPackScoped ? ResolveCurrentPackScope() : descriptor.Scope;
			if (string.IsNullOrEmpty(scope))
			{
				return 0;
			}
			object result = queryMethod.Invoke(rawData, new object[]
			{
				database,
				"SELECT * FROM " + descriptor.Table
			});
			if (!(result is IEnumerable rows))
			{
				return 0;
			}
			int rowCount = 0;
			int captured = 0;
			foreach (object rawRow in rows)
			{
				if (!(rawRow is IDictionary row) || !TryGetValue(row, "id", out string id) ||
					string.IsNullOrWhiteSpace(id))
				{
					continue;
				}
				rowCount++;
				foreach (RawOutputDescriptor output in descriptor.Outputs)
				{
					string source = GetFirstValue(row, output.SourceColumns);
					string key = scope + "|" + output.KeyTable + "|" + id;
					if (string.IsNullOrWhiteSpace(source) || catalog.HasCurrentSource(key, source))
					{
						continue;
					}
					catalog.RecordMissing(key, source);
					captured++;
				}
			}
			if (rowCount == 0)
			{
				return 0;
			}
			_nextRawTableIndex++;
			logger?.LogInfo("[PTBR-DIAGNOSTICO] tabela bruta " + scope + "|" + descriptor.Table +
				": linhas=" + rowCount + ", novas=" + captured + ".");
			return captured;
		}
		catch (Exception ex)
		{
			logger?.LogWarning("Falha ao extrair a próxima tabela bruta de localização: " +
				(ex.InnerException?.Message ?? ex.Message));
			return 0;
		}
	}

	private static object GetSingletonInstance(Type targetType)
	{
		if (targetType == null)
		{
			return null;
		}
		for (Type current = targetType; current != null; current = current.BaseType)
		{
			foreach (PropertyInfo property in current.GetProperties(BindingFlags.Static | BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (property.GetIndexParameters().Length == 0 && targetType.IsAssignableFrom(property.PropertyType))
				{
					object value = property.GetValue(null, null);
					if (value != null)
					{
						return value;
					}
				}
			}
			foreach (FieldInfo field in current.GetFields(BindingFlags.Static | BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (targetType.IsAssignableFrom(field.FieldType))
				{
					object value = field.GetValue(null);
					if (value != null)
					{
						return value;
					}
				}
			}
		}
		return null;
	}

	private static string ResolveCurrentPackScope()
	{
		Type packManagerType = Type.GetType("PackManager, Assembly-CSharp", throwOnError: false);
		object packManager = GetSingletonInstance(packManagerType);
		if (packManager == null)
		{
			return null;
		}
		const string memberName = "ὩὬὩὢὮὬὩὪὦὡὪ";
		PropertyInfo property = packManagerType.GetProperty(memberName,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo field = packManagerType.GetField(memberName,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		object value = property?.GetValue(packManager, null) ?? field?.GetValue(packManager);
		int packId;
		try
		{
			packId = Convert.ToInt32(value);
		}
		catch
		{
			return null;
		}
		return packId > 0 ? "pack" + packId : null;
	}

	private static string GetFirstValue(IDictionary row, IEnumerable<string> names)
	{
		foreach (string name in names)
		{
			if (TryGetValue(row, name, out string value) && !string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}
		return null;
	}

	private static bool TryGetValue(IDictionary row, string name, out string value)
	{
		foreach (DictionaryEntry entry in row)
		{
			if (entry.Key is string key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
			{
				value = Convert.ToString(entry.Value);
				return true;
			}
		}
		value = null;
		return false;
	}

	private static IEnumerable<object> EnumerateKeys(object cache, Type keyType)
	{
		HashSet<object> keys = new HashSet<object>();
		foreach (FieldInfo field in EnumerateInstanceFields(cache.GetType()))
		{
			object value;
			try
			{
				value = field.GetValue(cache);
			}
			catch
			{
				continue;
			}
			if (value is IDictionary dictionary)
			{
				foreach (object key in dictionary.Keys)
				{
					if (key != null && keyType.IsInstanceOfType(key))
					{
						keys.Add(key);
					}
				}
			}
			else if (value is IEnumerable enumerable && !(value is string))
			{
				foreach (object item in enumerable)
				{
					if (item != null && keyType.IsInstanceOfType(item))
					{
						keys.Add(item);
					}
				}
			}
		}
		return keys;
	}

	private static IEnumerable<FieldInfo> EnumerateInstanceFields(Type type)
	{
		for (Type current = type; current != null; current = current.BaseType)
		{
			foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				yield return field;
			}
		}
	}

	private static bool TryBuildKey(CacheDescriptor descriptor, object rawKey, out string key)
	{
		key = null;
		if (!descriptor.IsPackScoped)
		{
			try
			{
				key = descriptor.Scope + "|" + descriptor.Table + "|" + Convert.ToInt32(rawKey);
				return true;
			}
			catch
			{
				return false;
			}
		}
		Type tupleType = rawKey?.GetType();
		FieldInfo item1 = tupleType?.GetField("Item1");
		FieldInfo item2 = tupleType?.GetField("Item2");
		if (item1 == null || item2 == null)
		{
			return false;
		}
		int packId = Convert.ToInt32(item1.GetValue(rawKey));
		int textId = Convert.ToInt32(item2.GetValue(rawKey));
		string scope = packId > 0 ? "pack" + packId : "common";
		key = scope + "|" + descriptor.Table + "|" + textId;
		return true;
	}
}
