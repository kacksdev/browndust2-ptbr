using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace BD2PTBR;

internal static class TextRenderPatcher
{
	private static TranslationCatalog _catalog;

	private static ManualLogSource _logger;

	private static int _matchedCount;

	private static int _loggedMatches;

	private static int _renderHookEntered;

	private const int MatchLogLimit = 20;

	public static int Install(Harmony harmony, TranslationCatalog catalog, ManualLogSource logger)
	{
		_catalog = catalog;
		_logger = logger;
		HarmonyMethod prefix = new HarmonyMethod(typeof(TextRenderPatcher).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic));
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
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly item) => string.Equals(item.GetName().Name, "Unity.TextMeshPro", StringComparison.Ordinal));
		if (assembly == null)
		{
			yield break;
		}
		var array = new[]
		{
			new
			{
				Type = "TMPro.TMP_Text",
				Method = "ParseInputText"
			},
			new
			{
				Type = "TMPro.TextMeshProUGUI",
				Method = "OnPreRenderCanvas"
			},
			new
			{
				Type = "TMPro.TextMeshPro",
				Method = "OnPreRenderObject"
			}
		};
		foreach (var anon in array)
		{
			Type type = assembly.GetType(anon.Type, throwOnError: false);
			MethodInfo methodInfo = ((type == null) ? null : type.GetMethod(anon.Method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
			if (methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	private static void Prefix(object __instance)
	{
		if (Interlocked.Exchange(ref _renderHookEntered, 1) == 0 && _logger != null)
		{
			_logger.LogInfo("[PTBR-RUNTIME] interceptação de pré-renderização ativa.");
		}
		TMP_Text tMP_Text = __instance as TMP_Text;
		if (_catalog == null || tMP_Text == null || TranslationExecutionGuard.IsVisualTranslationSuppressed)
		{
			return;
		}
		string text = tMP_Text.text;
		if (_catalog.IsTranslatedOutput(text))
		{
			PortugueseFontFallback.EnsureFor(tMP_Text);
			return;
		}
		if (!string.IsNullOrEmpty(text) && _catalog.TryTranslateBySource(text, out var translation) && !string.Equals(text, translation, StringComparison.Ordinal))
		{
			tMP_Text.text = translation;
			PortugueseFontFallback.EnsureFor(tMP_Text);
			_matchedCount++;
			if (_logger != null && _loggedMatches < 20)
			{
				_loggedMatches++;
				_logger.LogInfo("Substituição antes da renderização confirmada [" + _matchedCount + "]: " + Abbreviate(text) + " => " + Abbreviate(translation));
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
