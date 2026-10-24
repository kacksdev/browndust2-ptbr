using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace BD2PTBR.Installer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args.Any(arg => string.Equals(arg, "--headless", StringComparison.OrdinalIgnoreCase)))
            {
                RunHeadless(e.Args);
                return;
            }

            var rootIndex = Array.FindIndex(e.Args, value => value == "--game-root");
            var window = new MainWindow(rootIndex >= 0 && rootIndex + 1 < e.Args.Length ? e.Args[rootIndex + 1] : null);
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }

        private void RunHeadless(string[] args)
        {
            var options = CommandLineOptions.Parse(args);
            OperationReport report;
            try
            {
                var engine = new InstallerEngine();
                var progress = new InlineProgress<InstallerProgress>(item =>
                {
                    if (!string.IsNullOrWhiteSpace(options.LogPath))
                    {
                        File.AppendAllText(options.LogPath, item.Message + Environment.NewLine);
                    }
                });

                if (options.Action == InstallerAction.Restore)
                {
                    report = engine.Restore(options.GameRoot, progress, options.ToEngineOptions());
                }
                else if (options.Action == InstallerAction.Uninstall)
                {
                    report = engine.Uninstall(options.GameRoot, progress, options.ToEngineOptions());
                }
                else if (options.Action == InstallerAction.Verify)
                {
                    report = engine.Verify(options.GameRoot, progress, options.ToEngineOptions());
                }
                else
                {
                    report = engine.Install(options.GameRoot, progress, options.ToEngineOptions());
                }
            }
            catch (InstallerOperationException ex)
            {
                report = ex.Report;
            }
            catch (Exception ex)
            {
                report = OperationReport.Failure(options.Action.ToString(), options.GameRoot, ex);
            }

            if (!string.IsNullOrWhiteSpace(options.ReportPath))
            {
                JsonStorage.WriteAtomic(options.ReportPath, report);
            }

            Environment.ExitCode = report.Success ? 0 : 1;
            Shutdown(Environment.ExitCode);
        }
    }

    internal sealed class CommandLineOptions
    {
        public InstallerAction Action { get; private set; } = InstallerAction.Install;
        public string GameRoot { get; private set; }
        public string ReportPath { get; private set; }
        public string LogPath { get; private set; }
        public int FaultAfter { get; private set; }
        public bool SkipProcessCheck { get; private set; }
        public string StateRoot { get; private set; }
        public string Payload { get; private set; }
        public string PayloadHash { get; private set; }
        public string ModVersion { get; private set; }

        public static CommandLineOptions Parse(IEnumerable<string> args)
        {
            var result = new CommandLineOptions();
            var values = args.ToArray();
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (string.Equals(value, "--action", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    InstallerAction action;
                    if (!Enum.TryParse(values[++i], true, out action))
                    {
                        throw new ArgumentException("Ação headless inválida.");
                    }
                    result.Action = action;
                }
                else if (string.Equals(value, "--game-root", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    result.GameRoot = values[++i];
                }
                else if (string.Equals(value, "--report", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    result.ReportPath = Path.GetFullPath(values[++i]);
                }
                else if (string.Equals(value, "--log", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    result.LogPath = Path.GetFullPath(values[++i]);
                }
                else if (string.Equals(value, "--fault-after", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    result.FaultAfter = int.Parse(values[++i]);
                }
                else if (string.Equals(value, "--skip-process-check", StringComparison.OrdinalIgnoreCase))
                {
                    result.SkipProcessCheck = true;
                }
                else if (string.Equals(value, "--state-root", StringComparison.OrdinalIgnoreCase) && i + 1 < values.Length)
                {
                    result.StateRoot = Path.GetFullPath(values[++i]);
                }
                else if (value == "--payload" && i + 1 < values.Length) result.Payload = Path.GetFullPath(values[++i]);
                else if (value == "--payload-sha256" && i + 1 < values.Length) result.PayloadHash = values[++i];
                else if (value == "--mod-version" && i + 1 < values.Length) result.ModVersion = values[++i];
            }

            if (string.IsNullOrWhiteSpace(result.GameRoot))
            {
                result.GameRoot = GameDetector.Detect();
            }
            return result;
        }

        public InstallerEngineOptions ToEngineOptions()
        {
            return new InstallerEngineOptions
            {
                FaultAfterFileOperations = FaultAfter,
                SkipProcessCheck = SkipProcessCheck,
                StateRootOverride = StateRoot,
                PayloadPath = Payload,
                ExpectedPayloadSha256 = PayloadHash,
                ExpectedModVersion = ModVersion
            };
        }
    }

    internal sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        public InlineProgress(Action<T> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Report(T value)
        {
            _callback(value);
        }
    }
}
