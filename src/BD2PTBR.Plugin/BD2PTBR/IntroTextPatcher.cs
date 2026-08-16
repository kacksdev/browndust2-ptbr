using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BD2PTBR;

internal static class IntroTextPatcher
{
	private const string CurrentLocalizationTypeName = "ὮὧὨὯὧὭὮὯὫὥὤ";

	private const string CurrentIntroMethodName = "ὨὤὨὫὧὪὡὢὧὡὥ";

	public const int ExpectedHookCount = 1;

	private static TranslationCatalog _catalog;

	private static bool _captureMissing;

	public static int Install(Harmony harmony, TranslationCatalog catalog, bool captureMissing)
	{
		_catalog = catalog;
		_captureMissing = captureMissing;
		HarmonyMethod postfix = new HarmonyMethod(typeof(IntroTextPatcher).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
		int num = 0;
		foreach (MethodInfo item in FindTargets())
		{
			harmony.Patch(item, null, postfix);
			num++;
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
		Type type = assembly.GetType("ὮὧὨὯὧὭὮὯὫὥὤ", throwOnError: false);
		if (!(type == null))
		{
			MethodInfo method = type.GetMethod("ὨὤὨὫὧὪὡὢὧὡὥ", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(int) }, null);
			if (method != null && method.ReturnType == typeof(string))
			{
				yield return method;
			}
		}
	}

	private static void Postfix(object[] __args, ref string __result)
	{
		if (_catalog != null && __args != null && __args.Length == 1 && !string.IsNullOrEmpty(__result))
		{
			string key = "intro|IntroTextTable|" + Convert.ToInt32(__args[0]);
			string text = __result;
			if (_catalog.TryTranslate(key, text, out var translation))
			{
				__result = translation;
			}
			else if (_captureMissing)
			{
				_catalog.RecordMissing(key, text);
			}
		}
	}
}
