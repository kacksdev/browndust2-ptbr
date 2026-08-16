using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace BD2PTBR;

internal static class PortugueseFontFallback
{
	private const string RequiredCharacters = "ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüçªº";

	private static readonly object Sync = new object();

	private static readonly HashSet<int> PatchedFonts = new HashSet<int>();

	private static readonly HashSet<int> ReportedUnresolvedFonts = new HashSet<int>();

	private static readonly Dictionary<int, DateTime> NextAttemptUtc = new Dictionary<int, DateTime>();

	private static TMP_FontAsset _dynamicFallback;

	private static ManualLogSource _logger;

	private static int _dynamicFontAttempts;

	private static int _resolvedLogCount;

	public static void Initialize(ManualLogSource logger)
	{
		_logger = logger;
	}

	public static int InstallDynamicGlyphGuard(Harmony harmony)
	{
		MethodInfo target = typeof(TMP_FontAsset).GetMethod(
			"TryAddCharacterInternal",
			BindingFlags.Instance | BindingFlags.NonPublic,
			null,
			new Type[2] { typeof(uint), typeof(TMP_Character).MakeByRefType() },
			null);
		if (target == null)
		{
			return 0;
		}
		HarmonyMethod prefix = new HarmonyMethod(typeof(PortugueseFontFallback).GetMethod(
			nameof(DynamicCharacterPrefix),
			BindingFlags.Static | BindingFlags.NonPublic));
		harmony.Patch(target, prefix);
		return 1;
	}

	public static bool ContainsPortugueseCharacters(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (char character in value)
		{
			if (RequiredCharacters.IndexOf(character) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	public static void EnsureFor(TMP_Text text)
	{
		if (text == null || text.font == null)
		{
			return;
		}
		TMP_FontAsset font = text.font;
		int instanceID = font.GetInstanceID();
		lock (Sync)
		{
			if (PatchedFonts.Contains(instanceID) ||
				(NextAttemptUtc.TryGetValue(instanceID, out DateTime nextAttempt) && DateTime.UtcNow < nextAttempt))
			{
				return;
			}
			NextAttemptUtc[instanceID] = DateTime.UtcNow.AddSeconds(30.0);
		}

		try
		{
			List<TMP_FontAsset> selected = new List<TMP_FontAsset>();
			TMP_FontAsset dynamicFallback = GetOrCreateDynamicFallback(font);
			if (dynamicFallback != null)
			{
				RoutePortugueseCharactersToFallback(font);
				selected.Add(dynamicFallback);
			}
			else
			{
				font.HasCharacters(RequiredCharacters, out List<char> missingCharacters);
				if (missingCharacters == null)
				{
					missingCharacters = new List<char>(RequiredCharacters.ToCharArray());
				}
				selected = SelectLoadedFallbacks(font, missingCharacters);
			}
			if (selected.Count > 0)
			{
				AttachFallbacks(font, selected);
			}

			bool complete = font.HasCharacters(RequiredCharacters, out uint[] unresolved, searchFallbacks: true, tryAddCharacter: false);
			if (complete)
			{
				MarkComplete(instanceID);
				if (selected.Count > 0)
				{
					RefreshTextsUsing(font);
				}
				if (_resolvedLogCount < 10)
				{
					_resolvedLogCount++;
					_logger?.LogInfo("Cobertura PT-BR associada à fonte " + font.name + " via " + JoinFontNames(selected) + ".");
				}
				return;
			}

			lock (Sync)
			{
				if (ReportedUnresolvedFonts.Add(instanceID))
				{
					_logger?.LogWarning("A fonte " + font.name + " ainda não cobre estes caracteres PT-BR: " + FormatCodePoints(unresolved) + ". Novas fontes serão verificadas silenciosamente.");
				}
			}
		}
		catch (Exception ex)
		{
			lock (Sync)
			{
				if (ReportedUnresolvedFonts.Add(instanceID))
				{
					_logger?.LogWarning("Falha ao preparar a cobertura PT-BR para " + font.name + ": " + ex.Message);
				}
			}
		}
	}

	private static List<TMP_FontAsset> SelectLoadedFallbacks(TMP_FontAsset target, List<char> missingCharacters)
	{
		List<TMP_FontAsset> selected = new List<TMP_FontAsset>();
		TMP_FontAsset[] candidates = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
		while (missingCharacters.Count > 0)
		{
			TMP_FontAsset best = null;
			int bestCoverage = 0;
			int bestPriority = int.MinValue;
			foreach (TMP_FontAsset candidate in candidates)
			{
				if (candidate == null || candidate == target || selected.Contains(candidate) || CreatesCycle(candidate, target))
				{
					continue;
				}
				int coverage = CountCoverage(candidate, missingCharacters);
				int priority = NamePriority(candidate.name);
				if (coverage > bestCoverage || (coverage == bestCoverage && coverage > 0 && priority > bestPriority))
				{
					best = candidate;
					bestCoverage = coverage;
					bestPriority = priority;
				}
			}
			if (best == null || bestCoverage == 0)
			{
				break;
			}
			selected.Add(best);
			AddCoverage(best, missingCharacters);
		}
		return selected;
	}

	private static int CountCoverage(TMP_FontAsset font, List<char> characters)
	{
		int count = 0;
		foreach (char character in characters)
		{
			try
			{
				if (font.HasCharacters(character.ToString()))
				{
					count++;
				}
			}
			catch
			{
				return 0;
			}
		}
		return count;
	}

	private static bool AddCoverage(TMP_FontAsset font, List<char> missingCharacters)
	{
		bool added = false;
		for (int index = missingCharacters.Count - 1; index >= 0; index--)
		{
			try
			{
				if (font.HasCharacters(missingCharacters[index].ToString()))
				{
					missingCharacters.RemoveAt(index);
					added = true;
				}
			}
			catch
			{
				return added;
			}
		}
		return added;
	}

	private static TMP_FontAsset GetOrCreateDynamicFallback(TMP_FontAsset templateFont)
	{
		lock (Sync)
		{
			if (_dynamicFallback != null || _dynamicFontAttempts >= 3)
			{
				return _dynamicFallback;
			}
			_dynamicFontAttempts++;
			try
			{
				string fontPath = FindSystemFontFile();
				if (string.IsNullOrEmpty(fontPath))
				{
					_logger?.LogWarning("Nenhum arquivo de fonte compatível foi encontrado no Windows para os caracteres PT-BR.");
					return null;
				}
				_dynamicFallback = CreateStaticFontAsset(File.ReadAllBytes(fontPath), templateFont);
				_logger?.LogInfo("Fonte auxiliar PT-BR criada em memória a partir de " + Path.GetFileName(fontPath) + ".");
				return _dynamicFallback;
			}
			catch (Exception ex)
			{
				_logger?.LogWarning("A tentativa " + _dynamicFontAttempts + " de criar a fonte auxiliar PT-BR falhou: " + ex);
				return null;
			}
		}
	}

	private static TMP_FontAsset CreateStaticFontAsset(byte[] fontData, TMP_FontAsset templateFont)
	{
		const int pointSize = 90;
		const int atlasPadding = 9;
		const int atlasWidth = 1024;
		const int atlasHeight = 1024;
		const GlyphRenderMode renderMode = GlyphRenderMode.SDFAA;

		FontEngineError initialization = FontEngine.InitializeFontEngine();
		if (initialization != FontEngineError.Success)
		{
			throw new InvalidOperationException("FontEngine não inicializou: " + initialization);
		}
		FontEngineError faceLoad = FontEngine.LoadFontFace(fontData, pointSize);
		if (faceLoad != FontEngineError.Success)
		{
			throw new InvalidOperationException("FontEngine não carregou o TTF: " + faceLoad);
		}
		try
		{
			Dictionary<uint, uint> unicodeToGlyph = new Dictionary<uint, uint>();
			HashSet<uint> uniqueGlyphs = new HashSet<uint>();
			List<uint> glyphIndexes = new List<uint>();
			foreach (char character in RequiredCharacters)
			{
				uint unicode = character;
				if (!FontEngine.TryGetGlyphIndex(unicode, out uint glyphIndex) || glyphIndex == 0)
				{
					throw new InvalidOperationException("O TTF não contém " + character + " (U+" + unicode.ToString("X4") + ").");
				}
				unicodeToGlyph[unicode] = glyphIndex;
				if (uniqueGlyphs.Add(glyphIndex))
				{
					glyphIndexes.Add(glyphIndex);
				}
			}

			Texture2D atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, mipChain: false)
			{
				name = "BD2 PT-BR Fallback Atlas"
			};
			ResetAtlasTexture(atlasTexture);
			List<GlyphRect> freeGlyphRects = new List<GlyphRect>(8)
			{
				new GlyphRect(0, 0, atlasWidth - 1, atlasHeight - 1)
			};
			List<GlyphRect> usedGlyphRects = new List<GlyphRect>(8);
			bool allGlyphsAdded = TryAddGlyphsToTexture(
				glyphIndexes,
				atlasPadding,
				GlyphPackingMode.BestShortSideFit,
				freeGlyphRects,
				usedGlyphRects,
				renderMode,
				atlasTexture,
				out Glyph[] glyphs);
			if (!allGlyphsAdded || glyphs == null)
			{
				int generatedGlyphs = glyphs?.Length ?? 0;
				UnityEngine.Object.Destroy(atlasTexture);
				throw new InvalidOperationException("Atlas PT-BR incompleto: resultado=" + allGlyphsAdded +
					", solicitados=" + glyphIndexes.Count + ", gerados=" + generatedGlyphs +
					", retângulos usados=" + usedGlyphRects.Count + ", livres=" + freeGlyphRects.Count + ".");
			}
			Dictionary<uint, Glyph> glyphsByIndex = new Dictionary<uint, Glyph>();
			List<Glyph> glyphTable = new List<Glyph>(glyphs.Length);
			foreach (Glyph glyph in glyphs)
			{
				if (glyph == null)
				{
					break;
				}
				glyphsByIndex[glyph.index] = glyph;
				glyphTable.Add(glyph);
			}
			if (glyphsByIndex.Count != glyphIndexes.Count)
			{
				UnityEngine.Object.Destroy(atlasTexture);
				throw new InvalidOperationException("O atlas gerou " + glyphsByIndex.Count + " de " + glyphIndexes.Count + " glifos PT-BR.");
			}

			TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
			fontAsset.name = "BD2 PT-BR Fallback";
			SetMember(fontAsset, "m_Version", "1.1.0");
			SetMember(fontAsset, "faceInfo", FontEngine.GetFaceInfo());
			SetMember(fontAsset, "atlasPopulationMode", AtlasPopulationMode.Static);
			SetMember(fontAsset, "atlasWidth", atlasWidth);
			SetMember(fontAsset, "atlasHeight", atlasHeight);
			SetMember(fontAsset, "atlasPadding", atlasPadding);
			SetMember(fontAsset, "atlasRenderMode", renderMode);
			SetMember(fontAsset, "atlasTextures", new Texture2D[1] { atlasTexture });
			SetMember(fontAsset, "isMultiAtlasTexturesEnabled", false);
			SetMember(fontAsset, "freeGlyphRects", freeGlyphRects);
			SetMember(fontAsset, "usedGlyphRects", usedGlyphRects);
			SetMember(fontAsset, "glyphTable", glyphTable);

			List<TMP_Character> characterTable = new List<TMP_Character>(unicodeToGlyph.Count);
			foreach (KeyValuePair<uint, uint> mapping in unicodeToGlyph)
			{
				if (!glyphsByIndex.TryGetValue(mapping.Value, out Glyph glyph))
				{
					UnityEngine.Object.Destroy(atlasTexture);
					throw new InvalidOperationException("Glifo ausente do atlas para U+" + mapping.Key.ToString("X4") + ".");
				}
				characterTable.Add(new TMP_Character(mapping.Key, fontAsset, glyph));
			}
			SetMember(fontAsset, "characterTable", characterTable);

			Shader shader = GetMobileSdfShader();
			Material material = shader != null
				? new Material(shader)
				: new Material(templateFont.material);
			material.name = "BD2 PT-BR Fallback Material";
			material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);
			material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
			material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
			material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + 1);
			material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
			material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
			SetMember(fontAsset, "material", material);
			fontAsset.ReadFontAssetDefinition();

			UnityEngine.Object.DontDestroyOnLoad(atlasTexture);
			UnityEngine.Object.DontDestroyOnLoad(material);
			UnityEngine.Object.DontDestroyOnLoad(fontAsset);
			return fontAsset;
		}
		finally
		{
			FontEngine.UnloadFontFace();
		}
	}

	private static string FindSystemFontFile()
	{
		string[] preferredNames = new string[5] { "segoeui.ttf", "arial.ttf", "calibri.ttf", "tahoma.ttf", "verdana.ttf" };
		string fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
		foreach (string preferredName in preferredNames)
		{
			string directPath = Path.Combine(fontsDirectory, preferredName);
			if (File.Exists(directPath))
			{
				return directPath;
			}
		}
		string[] installedPaths = Font.GetPathsToOSFonts();
		foreach (string preferredName in preferredNames)
		{
			foreach (string installedPath in installedPaths)
			{
				if (string.Equals(Path.GetFileName(installedPath), preferredName, StringComparison.OrdinalIgnoreCase) && File.Exists(installedPath))
				{
					return installedPath;
				}
			}
		}
		return null;
	}

	private static bool TryAddGlyphsToTexture(
		List<uint> glyphIndexes,
		int padding,
		GlyphPackingMode packingMode,
		List<GlyphRect> freeGlyphRects,
		List<GlyphRect> usedGlyphRects,
		GlyphRenderMode renderMode,
		Texture2D texture,
		out Glyph[] glyphs)
	{
		MethodInfo selected = null;
		foreach (MethodInfo method in typeof(FontEngine).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (method.Name == "TryAddGlyphsToTexture" && parameters.Length == 8 && parameters[0].ParameterType == typeof(List<uint>))
			{
				selected = method;
				break;
			}
		}
		if (selected == null)
		{
			throw new MissingMethodException(typeof(FontEngine).FullName, "TryAddGlyphsToTexture(List<uint>, ...)");
		}
		object[] arguments = new object[8]
		{
			glyphIndexes,
			padding,
			packingMode,
			freeGlyphRects,
			usedGlyphRects,
			renderMode,
			texture,
			null
		};
		bool result = (bool)selected.Invoke(null, arguments);
		glyphs = arguments[7] as Glyph[];
		return result;
	}

	private static void ResetAtlasTexture(Texture2D texture)
	{
		MethodInfo method = typeof(FontEngine).GetMethod("ResetAtlasTexture", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(Texture2D) }, null);
		if (method == null)
		{
			throw new MissingMethodException(typeof(FontEngine).FullName, "ResetAtlasTexture");
		}
		method.Invoke(null, new object[1] { texture });
	}

	private static Shader GetMobileSdfShader()
	{
		PropertyInfo property = typeof(ShaderUtilities).GetProperty("ShaderRef_MobileSDF", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		return property?.GetValue(null, null) as Shader;
	}

	private static void SetMember(object target, string name, object value)
	{
		for (Type type = target.GetType(); type != null; type = type.BaseType)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			if (property != null && property.GetSetMethod(nonPublic: true) != null)
			{
				property.SetValue(target, value, null);
				return;
			}
			FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			if (field != null)
			{
				field.SetValue(target, value);
				return;
			}
		}
		throw new MissingMemberException(target.GetType().FullName, name);
	}

	private static void AttachFallbacks(TMP_FontAsset target, List<TMP_FontAsset> fallbacks)
	{
		List<TMP_FontAsset> table = target.fallbackFontAssetTable;
		if (table == null)
		{
			table = target.fallbackFontAssetTable = new List<TMP_FontAsset>();
		}
		for (int index = fallbacks.Count - 1; index >= 0; index--)
		{
			TMP_FontAsset fallback = fallbacks[index];
			table.Remove(fallback);
			table.Insert(0, fallback);
		}
	}

	private static void RoutePortugueseCharactersToFallback(TMP_FontAsset target)
	{
		List<TMP_Character> characters = target.characterTable;
		if (characters == null || characters.Count == 0)
		{
			return;
		}
		int removed = 0;
		for (int index = characters.Count - 1; index >= 0; index--)
		{
			TMP_Character character = characters[index];
			if (character != null && character.unicode <= char.MaxValue && RequiredCharacters.IndexOf((char)character.unicode) >= 0)
			{
				characters.RemoveAt(index);
				removed++;
			}
		}
		if (removed > 0)
		{
			target.ReadFontAssetDefinition();
		}
	}

	private static bool DynamicCharacterPrefix(TMP_FontAsset __instance, uint unicode, ref TMP_Character character, ref bool __result)
	{
		if (_dynamicFallback == null || __instance == _dynamicFallback || unicode > char.MaxValue ||
			RequiredCharacters.IndexOf((char)unicode) < 0)
		{
			return true;
		}
		character = null;
		__result = false;
		return false;
	}

	private static void RefreshTextsUsing(TMP_FontAsset font)
	{
		int refreshed = 0;
		foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
		{
			if (text == null || text.font != font)
			{
				continue;
			}
			text.havePropertiesChanged = true;
			text.SetAllDirty();
			refreshed++;
		}
		if (refreshed > 0 && _resolvedLogCount < 10)
		{
			_logger?.LogInfo("Reconstrução de texto solicitada para " + refreshed + " componente(s) que usam " + font.name + ".");
		}
	}

	private static bool CreatesCycle(TMP_FontAsset candidate, TMP_FontAsset target)
	{
		try
		{
			return candidate.fallbackFontAssetTable != null && candidate.fallbackFontAssetTable.Contains(target);
		}
		catch
		{
			return true;
		}
	}

	private static int NamePriority(string name)
	{
		string normalized = (name ?? string.Empty).ToLowerInvariant();
		if (normalized.Contains("segoe") || normalized.Contains("arial") || normalized.Contains("noto") || normalized.Contains("roboto") || normalized.Contains("latin"))
		{
			return 2;
		}
		return normalized.Contains("fallback") ? 1 : 0;
	}

	private static void MarkComplete(int instanceID)
	{
		lock (Sync)
		{
			PatchedFonts.Add(instanceID);
			NextAttemptUtc.Remove(instanceID);
		}
	}

	private static string JoinFontNames(List<TMP_FontAsset> fonts)
	{
		if (fonts.Count == 0)
		{
			return "a própria fonte";
		}
		List<string> names = new List<string>();
		foreach (TMP_FontAsset font in fonts)
		{
			names.Add(font == null ? "<indisponível>" : font.name);
		}
		return string.Join(", ", names);
	}

	private static string FormatCodePoints(uint[] codePoints)
	{
		if (codePoints == null || codePoints.Length == 0)
		{
			return "<não identificados>";
		}
		List<string> formatted = new List<string>();
		foreach (uint codePoint in codePoints)
		{
			formatted.Add(char.ConvertFromUtf32((int)codePoint) + " (U+" + codePoint.ToString("X4") + ")");
		}
		return string.Join(", ", formatted);
	}
}
