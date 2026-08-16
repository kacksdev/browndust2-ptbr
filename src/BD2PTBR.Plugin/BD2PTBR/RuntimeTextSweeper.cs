using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BD2PTBR;

internal sealed class RuntimeTextSweeper
{
	private static readonly Regex LikelyEnglishText = new Regex("\\b(?:the|and|you|your|this|that|with|from|have|will|what|where|when|which|but|not|are|was|were|been|into|about|could|would|should|left|skill|settings|shop|confirm|cancel|purchase|close|open|claim|reward|mission|quest|battle|level|inventory|miss|error|failed|success|loading|exit|start|skip|next|back|retry|notice|story|pack|chronicles|timeline|world|master|guest|dream|friend|guild|collection)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private const int MatchLogLimit = 30;

	private readonly TranslationCatalog _catalog;

	private readonly bool _captureMissing;

	private readonly ManualLogSource _logger;

	private int _matchedCount;

	private int _loggedMatches;

	private int _loggedMissingSamples;

	private bool _loggedFirstSweep;

	private const int MissingSampleLogLimit = 20;

	public RuntimeTextSweeper(TranslationCatalog catalog, bool captureMissing, ManualLogSource logger)
	{
		_catalog = catalog;
		_captureMissing = captureMissing;
		_logger = logger;
	}

	public int Sweep()
	{
		if (TranslationExecutionGuard.IsVisualTranslationSuppressed)
		{
			return 0;
		}
		int num = 0;
		TMP_Text[] array = Resources.FindObjectsOfTypeAll<TMP_Text>();
		Text[] array2 = Resources.FindObjectsOfTypeAll<Text>();
		if (!_loggedFirstSweep)
		{
			_loggedFirstSweep = true;
			_logger.LogInfo("Varredura de componentes ativa: " + array.Length + " TMP e " + array2.Length + " UGUI encontrados.");
		}
		TMP_Text[] array3 = array;
		foreach (TMP_Text tMP_Text in array3)
		{
			if (!IsSceneObject(tMP_Text))
			{
				continue;
			}
			string text = tMP_Text.text;
			if (_catalog.IsTranslatedOutput(text))
			{
				PortugueseFontFallback.EnsureFor(tMP_Text);
				continue;
			}
			if (_catalog.TryTranslateBySource(text, out var translation))
			{
				if (!string.Equals(text, translation, StringComparison.Ordinal))
				{
					tMP_Text.text = translation;
					PortugueseFontFallback.EnsureFor(tMP_Text);
					num++;
					LogMatch("TMP", text, translation);
				}
			}
			else
			{
				CaptureMissing("TMP", text);
			}
		}
		Text[] array4 = array2;
		foreach (Text text2 in array4)
		{
			if (!IsSceneObject(text2))
			{
				continue;
			}
			string text3 = text2.text;
			if (_catalog.TryTranslateBySource(text3, out var translation2))
			{
				if (!string.Equals(text3, translation2, StringComparison.Ordinal))
				{
					text2.text = translation2;
					num++;
					LogMatch("UGUI", text3, translation2);
				}
			}
			else
			{
				CaptureMissing("UGUI", text3);
			}
		}
		return num;
	}

	private static bool IsSceneObject(Component component)
	{
		if (component != null && component.gameObject != null)
		{
			return component.gameObject.scene.IsValid();
		}
		return false;
	}

	private void LogMatch(string surface, string source, string translation)
	{
		_matchedCount++;
		if (_logger != null && _loggedMatches < 30)
		{
			_loggedMatches++;
			_logger.LogInfo("Substituição em componente confirmada [" + _matchedCount + "][" + surface + "]: " + Abbreviate(source) + " => " + Abbreviate(translation));
		}
	}

	private void CaptureMissing(string surface, string source)
	{
		if (_captureMissing && ShouldCapture(source))
		{
			_catalog.RecordMissing("runtime|" + surface + "|sha256:" + ComputeSourceHash(source), source);
			if (_logger != null && _loggedMissingSamples < 20)
			{
				_loggedMissingSamples++;
				_logger.LogInfo("Texto visual sem correspondência [" + surface + "]: " + Abbreviate(source));
			}
		}
	}

	private static bool ShouldCapture(string source)
	{
		string text = (source ?? string.Empty).Trim();
		if (text.Length < 2 || text.Length > 4000)
		{
			return false;
		}
		return LikelyEnglishText.IsMatch(text);
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

	private static string ComputeSourceHash(string source)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(source ?? string.Empty));
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
