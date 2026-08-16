using System;
using System.Diagnostics;
using System.Threading;
using BepInEx.Logging;

namespace BD2PTBR;

internal static class RuntimeCoordinator
{
	private static TranslationCatalog _catalog;

	private static RuntimeTextSweeper _sweeper;

	private static ManualLogSource _logger;

	private static string _missingOutputPath;

	private static DateTime _nextMissingFlushUtc;

	private static DateTime _nextTextSweepUtc;

	private static bool _captureMissing;

	private static int _pendingSweeps;

	private static int _tickActive;

	private static int _heartbeatLogged;

	private static int _completedSweeps;

	private static DateTime _nextCacheDumpUtc;

	private static int _remainingCacheDumps;

	public static void Initialize(TranslationCatalog catalog, bool captureMissing, ManualLogSource logger, string missingOutputPath)
	{
		_catalog = catalog;
		_logger = logger;
		_missingOutputPath = missingOutputPath;
		_captureMissing = captureMissing;
		_sweeper = new RuntimeTextSweeper(catalog, captureMissing, logger);
		_pendingSweeps = captureMissing ? 3 : 2;
		_completedSweeps = 0;
		_nextTextSweepUtc = DateTime.UtcNow.AddMilliseconds(500.0);
		_nextMissingFlushUtc = DateTime.UtcNow.AddSeconds(15.0);
		_nextCacheDumpUtc = DateTime.UtcNow.AddSeconds(8.0);
		_remainingCacheDumps = captureMissing ? 24 : 0;
	}

	public static void Disable()
	{
		_catalog = null;
		_sweeper = null;
		_missingOutputPath = null;
		_captureMissing = false;
		Interlocked.Exchange(ref _pendingSweeps, 0);
		Interlocked.Exchange(ref _remainingCacheDumps, 0);
		Interlocked.Exchange(ref _heartbeatLogged, 0);
		_logger = null;
	}

	public static void NotifySceneLoaded()
	{
		if (_catalog == null)
		{
			return;
		}
		Interlocked.Exchange(ref _pendingSweeps, _captureMissing ? 3 : 2);
		_nextTextSweepUtc = DateTime.UtcNow.AddMilliseconds(400.0);
		if (_captureMissing && Volatile.Read(ref _remainingCacheDumps) < 2)
		{
			Interlocked.Exchange(ref _remainingCacheDumps, 2);
			_nextCacheDumpUtc = DateTime.UtcNow.AddSeconds(3.0);
		}
	}

	public static void Tick()
	{
		if (_catalog == null || Interlocked.CompareExchange(ref _tickActive, 1, 0) != 0)
		{
			return;
		}
		try
		{
			if (Interlocked.Exchange(ref _heartbeatLogged, 1) == 0)
			{
				_logger?.LogInfo("[PTBR-RUNTIME] coordenador independente do ciclo do plugin ativo.");
			}
			DateTime utcNow = DateTime.UtcNow;
			if (_captureMissing && Volatile.Read(ref _remainingCacheDumps) > 0 && utcNow >= _nextCacheDumpUtc)
			{
				_nextCacheDumpUtc = utcNow.AddSeconds(3.0);
				if (LocalizationPatcher.CommonDatabaseObserved)
				{
					LocalizationCacheDumper.CaptureMissing(_catalog, _logger);
					Interlocked.Decrement(ref _remainingCacheDumps);
					FlushMissing();
				}
			}
			if (_sweeper != null && Volatile.Read(ref _pendingSweeps) > 0 && utcNow >= _nextTextSweepUtc)
			{
				_nextTextSweepUtc = utcNow.AddMilliseconds(1200.0);
				try
				{
					Stopwatch stopwatch = Stopwatch.StartNew();
					_sweeper.Sweep();
					stopwatch.Stop();
					int sweepNumber = Interlocked.Increment(ref _completedSweeps);
					Interlocked.Decrement(ref _pendingSweeps);
					_logger?.LogInfo("Varredura otimizada #" + sweepNumber + " concluída em " + stopwatch.ElapsedMilliseconds + " ms.");
				}
				catch (Exception ex)
				{
					_logger?.LogWarning("Falha ao verificar os textos visíveis: " + ex.Message);
				}
			}
			if (_captureMissing && utcNow >= _nextMissingFlushUtc)
			{
				_nextMissingFlushUtc = utcNow.AddSeconds(15.0);
				FlushMissing();
			}
		}
		finally
		{
			Volatile.Write(ref _tickActive, 0);
		}
	}

	public static void FlushMissing()
	{
		if (_catalog == null || string.IsNullOrEmpty(_missingOutputPath))
		{
			return;
		}
		try
		{
			_catalog.FlushMissing(_missingOutputPath);
		}
		catch (Exception ex)
		{
			_logger?.LogWarning("Falha ao salvar textos ausentes: " + ex.Message);
		}
	}
}
