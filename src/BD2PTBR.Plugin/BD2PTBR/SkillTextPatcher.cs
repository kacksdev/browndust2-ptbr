using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BD2PTBR;

internal static class SkillTextPatcher
{
	private const string CurrentSkillAccessorTypeName = "ὭὧὮὡὧὩὧὨὭὢὪ";

	private const string SkillMessageTypeName = "Proto.Design.common.SkillTextTable";

	private const string CurrentLocalizationTypeName = "ὮὧὨὯὧὭὮὯὫὥὤ";

	private const string CurrentTextMethodName = "ὩὯὪὧὣὫὩὫὩὨὧ";

	private const string CurrentNodeAddTextMethodName = "ὦὬὬὣὦὯὥὨὦὮὩ";

	public const int ExpectedHookCount = 3;

	private static TranslationCatalog _catalog;

	private static bool _captureMissing;

	private static readonly Dictionary<MethodBase, string> DirectFields = new Dictionary<MethodBase, string>();

	public static int Install(Harmony harmony, TranslationCatalog catalog, bool captureMissing)
	{
		_catalog = catalog;
		_captureMissing = captureMissing;
		HarmonyMethod postfix = new HarmonyMethod(typeof(SkillTextPatcher).GetMethod("ObjectPostfix", BindingFlags.Static | BindingFlags.NonPublic));
		int num = 0;
		foreach (MethodInfo item in FindObjectTargets())
		{
			harmony.Patch(item, null, postfix);
			num++;
		}
		HarmonyMethod postfix2 = new HarmonyMethod(typeof(SkillTextPatcher).GetMethod("DirectPostfix", BindingFlags.Static | BindingFlags.NonPublic));
		foreach (KeyValuePair<MethodInfo, string> item2 in FindDirectTargets())
		{
			DirectFields[item2.Key] = item2.Value;
			harmony.Patch(item2.Key, null, postfix2);
			num++;
		}
		return num;
	}

	private static IEnumerable<MethodInfo> FindObjectTargets()
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly2) => string.Equals(assembly2.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
		if (assembly == null)
		{
			yield break;
		}
		Type skillMessageType = assembly.GetType("Proto.Design.common.SkillTextTable", throwOnError: false);
		if (skillMessageType == null)
		{
			yield break;
		}
		Type type = assembly.GetType("ὭὧὮὡὧὩὧὨὭὢὪ", throwOnError: false);
		IEnumerable<Type> enumerable;
		if (type != null)
		{
			enumerable = new Type[1] { type };
		}
		else
		{
			try
			{
				enumerable = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				enumerable = ex.Types.Where((Type type2) => type2 != null);
			}
		}
		foreach (Type item in enumerable)
		{
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (methodInfo.ReturnType == skillMessageType && parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
				{
					yield return methodInfo;
				}
			}
		}
	}

	private static IEnumerable<KeyValuePair<MethodInfo, string>> FindDirectTargets()
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly2) => string.Equals(assembly2.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
		if (assembly == null)
		{
			yield break;
		}
		Type type = assembly.GetType("ὮὧὨὯὧὭὮὯὫὥὤ", throwOnError: false);
		if (type == null)
		{
			yield break;
		}
		KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[2]
		{
			new KeyValuePair<string, string>("ὩὯὪὧὣὫὩὫὩὨὧ", "Text"),
			new KeyValuePair<string, string>("ὦὬὬὣὦὯὥὨὦὮὩ", "NodeAddText")
		};
		for (int num = 0; num < array.Length; num++)
		{
			KeyValuePair<string, string> keyValuePair = array[num];
			MethodInfo method = type.GetMethod(keyValuePair.Key, BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(int) }, null);
			if (method != null && method.ReturnType == typeof(string))
			{
				yield return new KeyValuePair<MethodInfo, string>(method, keyValuePair.Value);
			}
		}
	}

	private static void ObjectPostfix(object __result)
	{
		if (_catalog != null && __result != null)
		{
			Type type = __result.GetType();
			PropertyInfo property = type.GetProperty("Id");
			if (!(property == null))
			{
				int id = Convert.ToInt32(property.GetValue(__result, null));
				TranslateProperty(__result, type, id, "Text");
				TranslateProperty(__result, type, id, "NodeAddText");
			}
		}
	}

	private static void DirectPostfix(MethodBase __originalMethod, object[] __args, ref string __result)
	{
		if (_catalog != null && !(__originalMethod == null) && __args != null && __args.Length == 1 && !string.IsNullOrEmpty(__result) && DirectFields.TryGetValue(__originalMethod, out var value))
		{
			int num = Convert.ToInt32(__args[0]);
			string text = "SkillTextTable." + value;
			string key = "common|" + text + "|" + num;
			string text2 = __result;
			if (_catalog.TryTranslate(key, text2, out var translation))
			{
				__result = translation;
			}
			else if (_captureMissing)
			{
				_catalog.RecordMissing(key, text2);
			}
		}
	}

	private static void TranslateProperty(object message, Type messageType, int id, string propertyName)
	{
		PropertyInfo property = messageType.GetProperty(propertyName);
		if (property == null || !property.CanRead || !property.CanWrite || property.PropertyType != typeof(string))
		{
			return;
		}
		string text = Convert.ToString(property.GetValue(message, null));
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = "SkillTextTable." + propertyName;
			string key = "common|" + text2 + "|" + id;
			if (_catalog.TryTranslate(key, text, out var translation))
			{
				property.SetValue(message, translation, null);
			}
			else if (_captureMissing)
			{
				_catalog.RecordMissing(key, text);
			}
		}
	}
}
