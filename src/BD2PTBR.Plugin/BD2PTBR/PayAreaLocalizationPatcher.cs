using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BD2PTBR;

internal static class PayAreaLocalizationPatcher
{
	private const string CurrentPayAreaTypeName = "ὦὡὣὯὨὫὧὬὮὣὪ";

	private const string PayAreaTableName = "PayAreaNameTextTable";

	private static TranslationCatalog _catalog;

	private static bool _captureMissing;

	public static int Install(Harmony harmony, TranslationCatalog catalog, bool captureMissing)
	{
		_catalog = catalog;
		_captureMissing = captureMissing;
		HarmonyMethod postfix = new HarmonyMethod(typeof(PayAreaLocalizationPatcher).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
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
		Type type = assembly.GetType("ὦὡὣὯὨὫὧὬὮὣὪ", throwOnError: false);
		if (type == null)
		{
			yield break;
		}
		MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.ReturnType != typeof(string))
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length == 1)
			{
				Type parameterType = parameters[0].ParameterType;
				int num2 = parameterType.GetFields(BindingFlags.Instance | BindingFlags.Public).Count((FieldInfo field) => field.FieldType == typeof(string));
				if (parameterType.IsValueType && num2 == 7)
				{
					yield return methodInfo;
				}
			}
		}
	}

	private static void Postfix(ref string __result)
	{
		if (_catalog != null && !string.IsNullOrEmpty(__result))
		{
			string text = __result;
			if (_catalog.TryTranslateByTableAndSource("PayAreaNameTextTable", text, out var translation))
			{
				__result = translation;
			}
			else if (_captureMissing)
			{
				_catalog.RecordMissing("common|PayAreaNameTextTable|source:" + text, text);
			}
		}
	}
}
