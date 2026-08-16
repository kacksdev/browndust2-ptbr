using System;

namespace BD2PTBR;

internal static class TranslationExecutionGuard
{
	[ThreadStatic]
	private static int _visualTranslationSuppressionDepth;

	public static bool IsVisualTranslationSuppressed => _visualTranslationSuppressionDepth > 0;

	public static void EnterVisualTranslationSuppression()
	{
		_visualTranslationSuppressionDepth++;
	}

	public static void ExitVisualTranslationSuppression()
	{
		if (_visualTranslationSuppressionDepth > 0)
		{
			_visualTranslationSuppressionDepth--;
		}
	}
}
