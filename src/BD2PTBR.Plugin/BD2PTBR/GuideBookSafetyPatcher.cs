using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BD2PTBR;

internal static class GuideBookSafetyPatcher
{
	private const string GuideBookTypeName = "GuideBookUI";

	private const string GuideBookContentStateName = "GuideBookContentState";

	private const string GuideTutorialTableName = "Proto.Design.common.GuideTutorialTable";

	private static ManualLogSource _logger;

	public static int Install(Harmony harmony, ManualLogSource logger)
	{
		_logger = logger;
		MethodInfo target = FindTarget();
		if (target == null)
		{
			return 0;
		}
		HarmonyMethod prefix = new HarmonyMethod(typeof(GuideBookSafetyPatcher).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
		HarmonyMethod finalizer = new HarmonyMethod(typeof(GuideBookSafetyPatcher).GetMethod(nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic));
		harmony.Patch(target, prefix, null, null, finalizer, null);
		return 1;
	}

	private static MethodInfo FindTarget()
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(item =>
			string.Equals(item.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
		Type guideBookType = assembly?.GetType(GuideBookTypeName, throwOnError: false);
		Type contentState = guideBookType?.GetNestedType(GuideBookContentStateName, BindingFlags.Public | BindingFlags.NonPublic);
		if (contentState == null)
		{
			return null;
		}
		foreach (MethodInfo method in contentState.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (method.ReturnType != typeof(void) || method.IsAbstract || !method.GetMethodBodySafe())
			{
				continue;
			}
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length != 1 || !parameters[0].ParameterType.IsGenericType ||
				parameters[0].ParameterType.GetGenericTypeDefinition() != typeof(List<>))
			{
				continue;
			}
			Type elementType = parameters[0].ParameterType.GetGenericArguments()[0];
			if (string.Equals(elementType.FullName, GuideTutorialTableName, StringComparison.Ordinal))
			{
				return method;
			}
		}
		return null;
	}

	private static void Prefix()
	{
		TranslationExecutionGuard.EnterVisualTranslationSuppression();
	}

	private static Exception Finalizer(Exception __exception)
	{
		TranslationExecutionGuard.ExitVisualTranslationSuppression();
		if (__exception != null)
		{
			_logger?.LogError("Falha durante a paginação segura do guia: " + __exception);
		}
		return __exception;
	}
}

internal static class ReflectionSafetyExtensions
{
	public static bool GetMethodBodySafe(this MethodInfo method)
	{
		try
		{
			return method.GetMethodBody() != null;
		}
		catch
		{
			return false;
		}
	}
}
