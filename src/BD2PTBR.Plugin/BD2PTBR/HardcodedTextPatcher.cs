using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace BD2PTBR;

internal static class HardcodedTextPatcher
{
	private static TranslationCatalog _catalog;

	private static ManualLogSource _logger;

	private static int _matchedCount;

	private static int _loggedMatches;

	private static int _hookEntered;

	private const int MatchLogLimit = 20;

	public static int Install(Harmony harmony, TranslationCatalog catalog, bool captureMissing, ManualLogSource logger)
	{
		_catalog = catalog;
		_logger = logger;
		_hookEntered = 0;
		HarmonyMethod prefix = new HarmonyMethod(typeof(HardcodedTextPatcher).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic));
		int num = 0;
		foreach (MethodInfo item in FindTargets())
		{
			harmony.Patch(item, prefix);
			num++;
		}
		return num;
	}

	private static IEnumerable<MethodInfo> FindTargets()
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly3) => string.Equals(assembly3.GetName().Name, "Unity.TextMeshPro", StringComparison.Ordinal));
		if (assembly == null)
		{
			yield break;
		}
		Type type = assembly.GetType("TMPro.TMP_Text", throwOnError: false);
		if (type == null)
		{
			yield break;
		}
		HashSet<MethodInfo> seen = new HashSet<MethodInfo>();
		MethodInfo[] methods;
		foreach (Type typeHierarchyTarget in GetTypeHierarchyTargets(type))
		{
			methods = typeHierarchyTarget.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if ((methodInfo.Name == "set_text" || methodInfo.Name == "SetText") && !methodInfo.IsAbstract && !methodInfo.IsGenericMethodDefinition)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (parameters.Length != 0 && !(parameters[0].ParameterType != typeof(string)) && seen.Add(methodInfo))
					{
						yield return methodInfo;
					}
				}
			}
		}
		Assembly assembly2 = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly3) => string.Equals(assembly3.GetName().Name, "UnityEngine.UI", StringComparison.Ordinal));
		Type type2 = ((assembly2 == null) ? null : assembly2.GetType("UnityEngine.UI.Text", throwOnError: false));
		if (!(type2 != null))
		{
			yield break;
		}
		methods = type2.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (MethodInfo methodInfo2 in methods)
		{
			if (!(methodInfo2.Name != "set_text") && !methodInfo2.IsAbstract && !methodInfo2.IsGenericMethodDefinition)
			{
				ParameterInfo[] parameters2 = methodInfo2.GetParameters();
				if (parameters2.Length == 1 && parameters2[0].ParameterType == typeof(string) && seen.Add(methodInfo2))
				{
					yield return methodInfo2;
				}
			}
		}
	}

	private static IEnumerable<Type> GetTypeHierarchyTargets(Type textType)
	{
		yield return textType;
		Type[] array;
		try
		{
			array = textType.Assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			array = ex.Types.Where((Type type2) => type2 != null).ToArray();
		}
		Type[] array2 = array;
		foreach (Type type in array2)
		{
			if (type != textType && !type.IsAbstract && textType.IsAssignableFrom(type))
			{
				yield return type;
			}
		}
	}

	private static void Prefix(object __instance, ref string __0)
	{
		if (Interlocked.Exchange(ref _hookEntered, 1) == 0 && _logger != null)
		{
			_logger.LogInfo("[PTBR-RUNTIME] setter visual de texto atingido.");
		}
		if (_catalog == null || string.IsNullOrEmpty(__0) || TranslationExecutionGuard.IsVisualTranslationSuppressed)
		{
			return;
		}
		string text = __0;
		TMP_Text textComponent = __instance as TMP_Text;
		if (PortugueseFontFallback.ContainsPortugueseCharacters(text))
		{
			PortugueseFontFallback.EnsureFor(textComponent);
		}
		if (_catalog.IsTranslatedOutput(text))
		{
			PortugueseFontFallback.EnsureFor(textComponent);
			return;
		}
		if (_catalog.TryTranslateBySource(text, out var translation))
		{
			__0 = translation;
			PortugueseFontFallback.EnsureFor(textComponent);
			_matchedCount++;
			if (_logger != null && _loggedMatches < 20)
			{
				_loggedMatches++;
				_logger.LogInfo("Substituição visual confirmada [" + _matchedCount + "]: " + Abbreviate(text) + " => " + Abbreviate(translation));
			}
		}
	}

	private static string Abbreviate(string value)
	{
		string text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
		if (text.Length > 120)
		{
			return text.Substring(0, 117) + "...";
		}
		return text;
	}

}
