using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;

namespace BD2PTBR;

internal static class LocalizationPatcher
{
	private sealed class DirectTable
	{
		public string Scope { get; private set; }

		public string Table { get; private set; }

		public bool IsPackScoped { get; private set; }

		public DirectTable(string scope, string table, bool isPackScoped)
		{
			Scope = scope;
			Table = table;
			IsPackScoped = isPackScoped;
		}
	}

	private const string CurrentLocalizationTypeName = "ὣὢὯὥὢὬὠὫὢὡὥ";

	private const string LegacyLocalizationTypeName = "ὮὧὨὯὧὭὮὯὫὥὤ";

	private static readonly Dictionary<string, DirectTable> DirectTables = new Dictionary<string, DirectTable>(StringComparer.Ordinal)
	{
		{
			"ὨὩὡὮὦὢὢὦὫὭὭ",
			new DirectTable(null, "LocalTextTable", isPackScoped: true)
		},
		{
			"ὭὭὤὫὢὭὭὭὧὭὪ",
			new DirectTable(null, "QuestTextTable", isPackScoped: true)
		},
		{
			"ὦὡὡὩὥὩὠὡὩὮὯ",
			new DirectTable(null, "StoryTextTable", isPackScoped: true)
		},
		{
			"ὩὫὫὧὭὪὠὨὣὮὬ",
			new DirectTable("common", "NameTextTable", isPackScoped: false)
		},
		{
			"ὪὮὥὪὡὪὦὯὭὢὫ",
			new DirectTable("common", "StoryTooltipTextTable", isPackScoped: false)
		},
		{
			"ὭὪὡὭὮὡὢὬὮὭὢ",
			new DirectTable("common", "WorldbookTextTable", isPackScoped: false)
		},
		{
			"ὯὭὧὦὯὮὯὧὤὬὨ",
			new DirectTable("common", "ProfileTextTable", isPackScoped: false)
		},
		{
			"ὯὡὩὮὣὣὪὨὮὤὥ",
			new DirectTable("common", "TooltipTextTable", isPackScoped: false)
		},
		{
			"ὬὤὠὨὡὩὡὢὤὧὯ",
			new DirectTable("common", "DatingTextTable", isPackScoped: false)
		},
		{
			"ὢὠὧὨὬὬὤὤὬὫὪ",
			new DirectTable("common", "SkillTextTable.Text", isPackScoped: false)
		},
		{
			"ὦὤὫὣὣὩὬὮὯὢὣ",
			new DirectTable("common", "SkillTextTable.NodeAddText", isPackScoped: false)
		},
		{
			"ὪὨὤὡὠὫὫὧὣὨὩ",
			new DirectTable("common", "RandomBoxTextTable", isPackScoped: false)
		},
		{
			"ὣὯὦὧὩὩὣὤὫὫὡ",
			new DirectTable("intro", "IntroTextTable", isPackScoped: false)
		},
		{
			"ὨὦὧὫὦὧὠὡὬὥὡ",
			new DirectTable("common", "SpineInteractionTextTable", isPackScoped: false)
		},
		{
			"ὪὩὯὦὡὣὣὣὯὢὧ",
			new DirectTable("common", "NameTextTable", isPackScoped: false)
		},
		{
			"ὮὮὭὬὪὭὧὢὩὥὬ",
			new DirectTable("common", "StoryTooltipTextTable", isPackScoped: false)
		},
		{
			"ὭὢὧὫὢὡὫὮὢὦὮ",
			new DirectTable("common", "WorldbookTextTable", isPackScoped: false)
		},
		{
			"ὣὨὯὭὠὯὡὮὩὫὪ",
			new DirectTable("common", "ProfileTextTable", isPackScoped: false)
		},
		{
			"ὡὫὦὫὧὠὯὨὦὢὠ",
			new DirectTable("common", "TooltipTextTable", isPackScoped: false)
		},
		{
			"ὫὥὦὦὪὥὯὧὥὦὢ",
			new DirectTable("common", "DatingTextTable", isPackScoped: false)
		},
		{
			"ὫὢὢὠὨὣὥὯὪὤὨ",
			new DirectTable("common", "RandomBoxTextTable", isPackScoped: false)
		},
		{
			"ὠὯὯὥὩὬὯὠὦὩὣ",
			new DirectTable("common", "SpineInteractionTextTable", isPackScoped: false)
		},
		{
			"ὫὬὯὥὯὥὦὠὨὫὠ",
			new DirectTable("common", "LocalTextTable", isPackScoped: false)
		},
		{
			"ὢὬὡὤὫὡὥὥὪὬὣ",
			new DirectTable("common", "NameTextTable", isPackScoped: false)
		},
		{
			"ὩὯὪὧὣὫὩὫὩὨὧ",
			new DirectTable("common", "SkillTextTable.Text", isPackScoped: false)
		},
		{
			"ὦὬὬὣὦὯὥὨὦὮὩ",
			new DirectTable("common", "SkillTextTable.NodeAddText", isPackScoped: false)
		},
		{
			"ὨὤὨὫὧὪὡὢὧὡὥ",
			new DirectTable("intro", "IntroTextTable", isPackScoped: false)
		},
		{
			"ὨὮὮὦὢὯὣὮὬὤὪ",
			new DirectTable(null, "LocalTextTable", isPackScoped: true)
		},
		{
			"ὧὧὨὠὯὤὥὡὦὡὨ",
			new DirectTable(null, "LocalTextTable", isPackScoped: true)
		},
		{
			"ὭὧὡὤὪὠὤὫὯὬὤ",
			new DirectTable(null, "QuestTextTable", isPackScoped: true)
		},
		{
			"ὧὪὧὧὢὯὪὯὯὡὯ",
			new DirectTable(null, "QuestTextTable", isPackScoped: true)
		},
		{
			"ὬὫὧὠὣὬὬὯὠὢὦ",
			new DirectTable(null, "StoryTextTable", isPackScoped: true)
		}
	};

	private static TranslationCatalog _catalog;

	private static bool _captureMissing;

	private static ManualLogSource _logger;

	private static int _confirmedMatches;

	private static int _hookEntered;

	private static int _commonDatabaseObserved;

	private static string _resolvedLocalizationTypeName;

	internal static Type ResolvedLocalizationType { get; private set; }

	internal static bool CommonDatabaseObserved => Volatile.Read(ref _commonDatabaseObserved) != 0;

	public static int Install(Harmony harmony, TranslationCatalog catalog, bool captureMissing, ManualLogSource logger)
	{
		_catalog = catalog;
		_captureMissing = captureMissing;
		_logger = logger;
		_confirmedMatches = 0;
		_hookEntered = 0;
		_commonDatabaseObserved = 0;
		_resolvedLocalizationTypeName = null;
		ResolvedLocalizationType = null;
		HarmonyMethod postfix = new HarmonyMethod(typeof(LocalizationPatcher).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
		int num = 0;
		foreach (MethodInfo item in FindTargets())
		{
			harmony.Patch(item, null, postfix);
			num++;
		}
		if (num > 0)
		{
			_logger?.LogInfo("Central de localização identificada estruturalmente: " + _resolvedLocalizationTypeName + " (" + num + " métodos).");
		}
		return num;
	}

	private static IEnumerable<MethodInfo> FindTargets()
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly2) => string.Equals(assembly2.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
		if (assembly == null)
		{
			yield break;
		}
		Type type = FindLocalizationType(assembly);
		if (type == null)
		{
			yield break;
		}
		_resolvedLocalizationTypeName = type.FullName;
		ResolvedLocalizationType = type;
		MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.ReturnType != typeof(string) || methodInfo.IsAbstract || methodInfo.IsGenericMethodDefinition || methodInfo.ContainsGenericParameters)
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length < 1 || parameters.Length > 2)
			{
				continue;
			}
			Type parameterType = parameters[0].ParameterType;
			bool num2 = parameterType == typeof(int) && parameters.All((ParameterInfo parameter) => parameter.ParameterType == typeof(int));
			bool flag = string.Equals(parameterType.Namespace, "Proto.Design.common", StringComparison.Ordinal) && parameterType.GetProperty("Id") != null && parameterType.GetProperty("TextEn") != null;
			if (num2 || flag)
			{
				MethodBody methodBody;
				try
				{
					methodBody = methodInfo.GetMethodBody();
				}
				catch
				{
					methodBody = null;
				}
				if (methodBody != null)
				{
					yield return methodInfo;
				}
			}
		}
	}

	private static Type FindLocalizationType(Assembly assembly)
	{
		Type known = assembly.GetType(CurrentLocalizationTypeName, throwOnError: false) ??
			assembly.GetType(LegacyLocalizationTypeName, throwOnError: false);
		if (GetLocalizationTypeScore(known) >= 50)
		{
			return known;
		}
		IEnumerable<Type> types;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = ex.Types.Where(type => type != null);
		}
		return types
			.Select(type => new { Type = type, Score = GetLocalizationTypeScore(type) })
			.Where(candidate => candidate.Score >= 50)
			.OrderByDescending(candidate => candidate.Score)
			.Select(candidate => candidate.Type)
			.FirstOrDefault();
	}

	private static int GetLocalizationTypeScore(Type type)
	{
		if (type == null || !type.IsAbstract || !type.IsSealed)
		{
			return 0;
		}
		MethodInfo[] methods;
		try
		{
			methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch
		{
			return 0;
		}
		int messageAccessors = 0;
		int integerAccessors = 0;
		foreach (MethodInfo method in methods)
		{
			if (method.ReturnType != typeof(string))
			{
				continue;
			}
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length < 1 || parameters.Length > 2)
			{
				continue;
			}
			Type parameterType = parameters[0].ParameterType;
			if (parameterType == typeof(int))
			{
				integerAccessors++;
			}
			else if (string.Equals(parameterType.Namespace, "Proto.Design.common", StringComparison.Ordinal) &&
				parameterType.GetProperty("Id") != null && parameterType.GetProperty("TextEn") != null)
			{
				messageAccessors++;
			}
		}
		return messageAccessors * 10 + integerAccessors;
	}

	private static void Postfix(MethodBase __originalMethod, object[] __args, ref string __result)
	{
		if (Interlocked.Exchange(ref _hookEntered, 1) == 0 && _logger != null)
		{
			_logger.LogInfo("[PTBR-RUNTIME] getter central de localização atingido: " + ((__originalMethod == null) ? "desconhecido" : __originalMethod.Name) + ".");
		}
		if (_catalog == null || __originalMethod == null || __args == null || __args.Length == 0 || string.IsNullOrEmpty(__result))
		{
			return;
		}
		string text = __result;
		string key2;
		bool exactKeyBuilt = TryBuildExactKey(__originalMethod, __args, out var key);
		if (exactKeyBuilt && key.StartsWith("common|", StringComparison.Ordinal))
		{
			Interlocked.Exchange(ref _commonDatabaseObserved, 1);
		}
		if (exactKeyBuilt && _catalog.TryTranslate(key, text, out var translation))
		{
			ApplyTranslation(__originalMethod, key, text, translation, ref __result);
		}
		else if (TryBuildCommonFallbackKey(__originalMethod, __args, out key2) && _catalog.TryTranslate(key2, text, out translation))
		{
			ApplyTranslation(__originalMethod, key2, text, translation, ref __result);
		}
		else if (_catalog.TryTranslateBySource(text, out translation))
		{
			ApplyTranslation(__originalMethod, key ?? "source", text, translation, ref __result);
		}
		else if (_captureMissing && !_catalog.IsTranslatedOutput(text))
		{
			if (string.IsNullOrEmpty(key))
			{
				key = "runtime|" + __originalMethod.Name + "|" + BuildArgumentLabel(__args);
			}
			_catalog.RecordMissing(key, text);
		}
	}

	private static bool TryBuildExactKey(MethodBase method, object[] args, out string key)
	{
		key = null;
		object obj = args[0];
		if (obj == null)
		{
			return false;
		}
		Type type = obj.GetType();
		if (string.Equals(type.Namespace, "Proto.Design.common", StringComparison.Ordinal))
		{
			PropertyInfo property = type.GetProperty("Id");
			if (property == null)
			{
				return false;
			}
			string text = "common";
			if (args.Length == 2 && args[1] is string)
			{
				string text2 = (string)args[1];
				if (!string.IsNullOrWhiteSpace(text2))
				{
					text = text2.ToLowerInvariant();
				}
			}
			key = text + "|" + type.Name + "|" + Convert.ToInt32(property.GetValue(obj, null));
			return true;
		}
		if (!DirectTables.TryGetValue(method.Name, out var value))
		{
			return false;
		}
		if (value.IsPackScoped)
		{
			if (args.Length != 2)
			{
				return false;
			}
			key = "pack" + Convert.ToInt32(args[0]) + "|" + value.Table + "|" + Convert.ToInt32(args[1]);
			return true;
		}
		if (args.Length != 1)
		{
			return false;
		}
		key = value.Scope + "|" + value.Table + "|" + Convert.ToInt32(args[0]);
		return true;
	}

	private static bool TryBuildCommonFallbackKey(MethodBase method, object[] args, out string key)
	{
		key = null;
		if (args == null || args.Length != 2 || !DirectTables.TryGetValue(method.Name, out var value) || !value.IsPackScoped)
		{
			return false;
		}
		key = "common|" + value.Table + "|" + Convert.ToInt32(args[1]);
		return true;
	}

	private static void ApplyTranslation(MethodBase method, string key, string source, string translation, ref string result)
	{
		result = translation;
		if (Interlocked.Increment(ref _confirmedMatches) <= 40 && _logger != null)
		{
			_logger.LogInfo("[PTBR-APLICADO] getter=" + method.Name + " chave=" + key + " origem=\"" + Abbreviate(source) + "\" tradução=\"" + Abbreviate(translation) + "\"");
		}
	}

	private static string BuildArgumentLabel(object[] args)
	{
		return string.Join(",", args.Select((object value) => (value != null) ? Convert.ToString(value) : "null"));
	}

	private static string Abbreviate(string value)
	{
		string text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
		if (text.Length > 100)
		{
			return text.Substring(0, 97) + "...";
		}
		return text;
	}
}
