using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BD2PTBR;

[BepInPlugin("br.com.bd2ptbr.translation", "Brown Dust 2 PT-BR", "0.1.1")]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "br.com.bd2ptbr.translation";

	public const string PluginName = "Brown Dust 2 PT-BR";

	public const string PluginVersion = "0.1.1";

	private const int ExpectedCompleteCatalogEntries = 223743;

	private Harmony _harmony;

	private TranslationCatalog _catalog;

	private void Awake()
	{
		base.Logger.LogInfo("[PTBR-CICLO] Awake iniciado.");
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		PortugueseFontFallback.Initialize(base.Logger);
		ConfigEntry<bool> configEntry = base.Config.Bind("Geral", "AtivarTraducao", defaultValue: true, "Ativa a substituição de textos por PT-BR.");
		ConfigEntry<bool> configEntry2 = base.Config.Bind("Diagnostico", "CapturarTextosAusentes", defaultValue: false, "Ativa coleta de textos ausentes para desenvolvimento. Mantenha desativado durante o uso normal.");
		ConfigEntry<string> configEntry3 = base.Config.Bind("Geral", "PastaDeTraducoes", Path.Combine(Paths.PluginPath, "BD2PTBR", "translations"), "Pasta contendo arquivos .jsonl de tradução.");
		if (!configEntry.Value)
		{
			base.Logger.LogInfo("Tradução desativada pela configuração.");
			return;
		}
		_catalog = TranslationCatalog.Load(configEntry3.Value, base.Logger);
		if (_catalog.Count < ExpectedCompleteCatalogEntries)
		{
			base.Logger.LogError("Catálogo parcial detectado: " + _catalog.Count + " traduções; esta versão exige ao menos " + ExpectedCompleteCatalogEntries + ". A tradução permanecerá desativada para preservar o cliente original.");
			return;
		}
		string missingOutputPath = Path.Combine(Paths.ConfigPath, "BD2PTBR", "untranslated-runtime.jsonl");
		_harmony = new Harmony(PluginGuid);
		try
		{
			int fontGuardHooks = PortugueseFontFallback.InstallDynamicGlyphGuard(_harmony);
			int num = LocalizationPatcher.Install(_harmony, _catalog, configEntry2.Value, base.Logger);
			int num2 = PayAreaLocalizationPatcher.Install(_harmony, _catalog, configEntry2.Value);
			int num3 = SkillTextPatcher.Install(_harmony, _catalog, configEntry2.Value);
			int num4 = IntroTextPatcher.Install(_harmony, _catalog, configEntry2.Value);
			int guideSafetyHooks = GuideBookSafetyPatcher.Install(_harmony, base.Logger);
			int num5 = HardcodedTextPatcher.Install(_harmony, _catalog, configEntry2.Value, base.Logger);
			base.Logger.LogInfo("Traduções carregadas: " + _catalog.Count + ". Fontes únicas disponíveis para a interface: " + _catalog.SourceIndexCount + ". Títulos musicais protegidos: " + _catalog.ProtectedOriginalSourceCount + ". Métodos centrais interceptados: " + num + ". Método de regiões interceptado: " + num2 + ". Método de habilidades interceptado: " + num3 + ". Método de mensagens iniciais interceptado: " + num4 + ". Métodos visuais seguros interceptados: " + num5 + ". Proteções de paginação do guia: " + guideSafetyHooks + ". Proteções de glifos dinâmicos: " + fontGuardHooks + ".");
			bool essentialCoverageReady = fontGuardHooks == 1 && num > 0 && num5 > 0 && guideSafetyHooks == 1;
			if (!essentialCoverageReady)
			{
				throw new InvalidOperationException("A cobertura essencial de tradução ficou incompleta após uma alteração do cliente.");
			}
			else
			{
				base.Logger.LogInfo("[PTBR-RUNTIME] cobertura de tradução operacional.");
				if (num2 == 0 || num3 < 1 || num4 == 0)
				{
					base.Logger.LogInfo("Alguns interceptores especializados legados estão indisponíveis; a central de tradução atual permanece ativa.");
				}
			}
			RuntimeCoordinator.Initialize(_catalog, configEntry2.Value, base.Logger, missingOutputPath);
			RuntimeHost.EnsureCreated();
			base.Logger.LogInfo("Coordenador otimizado registrado; varreduras globais ficaram limitadas à inicialização e às mudanças de cena.");
			base.Logger.LogInfo("[PTBR-CICLO] Awake concluído.");
		}
		catch (Exception ex)
		{
			DisableTranslationSafely(ex);
		}
	}

	private void DisableTranslationSafely(Exception failure)
	{
		try
		{
			_harmony?.UnpatchSelf();
		}
		catch (Exception unpatchFailure)
		{
			base.Logger.LogWarning("Falha ao remover interceptores incompletos: " + unpatchFailure.Message);
		}

		RuntimeCoordinator.Disable();
		RuntimeHost.DestroyIfCreated();
		base.Logger.LogError("Falha ao instalar a tradução; todos os interceptores do mod foram desativados e o cliente seguirá com os textos originais. Motivo: " + failure);
	}

	private void OnEnable()
	{
		base.Logger.LogInfo("[PTBR-CICLO] Plugin habilitado.");
	}

	private void Start()
	{
		base.Logger.LogInfo("[PTBR-CICLO] Start executado.");
	}

	private void Update()
	{
		RuntimeCoordinator.Tick();
	}

	private void OnApplicationQuit()
	{
		RuntimeCoordinator.FlushMissing();
	}

	private void OnDestroy()
	{
		base.Logger.LogInfo("[PTBR-CICLO] Componente principal encerrado; patches e coordenador de renderização permanecem ativos.");
		RuntimeCoordinator.FlushMissing();
	}
}
