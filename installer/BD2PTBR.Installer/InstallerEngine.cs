using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace BD2PTBR.Installer
{
    public sealed class InstallerEngine
    {
        public const string InstallerVersion = "0.2.0";
        private const string StateFileName = "install-state.json";
        private const string JournalFileName = "pending-transaction.json";
        private const string RestorePointFileName = "restore-point.json";
        // Only these exact v096 layers are owned legacy files. A name alone
        // never authorizes deleting a modified or third-party translation.
        private static readonly Dictionary<string, string> LegacyV096CatalogLayers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "zz-safety-overrides.jsonl", "863f42ad5fd1c1ff6b8d5903c78e7ffbd646ebfeaf195432a0e851ec61d698f1" },
            { "zzz-reviewed-overrides.jsonl", "822e3edb0243b9d56241b360a1fdab419f9343e269e6cb2fb8d13a4400de4246" },
            { "zzzz-v090-contextual-overrides.jsonl", "c1b8a165d784aa7524c122d6de1a9420e3c870012f9ab6beb6f6372af4ac552f" },
            { "zzzzz-v096-editorial-overrides.jsonl", "02624266b4e4e2252f24b7996fe3a571cdf52ea5dd871ae4e0627f488b29dc45" }
        };
        private int _fileOperations;

        public InstallationInspection Inspect(string requestedGameRoot, InstallerEngineOptions options = null)
        {
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            {
                var inspection = new InstallationInspection
                {
                    GameRoot = gameRoot,
                    DetectedPackage = GameDetector.DetectPackage(gameRoot),
                    TestedClient = GameCompatibility.ReadVersion(gameRoot),
                    ModVersion = "não instalado"
                };

                var instanceRoot = GetInstanceRoot(gameRoot, options);
                using (OperationLock.Acquire(instanceRoot))
                {
                RecoverPendingTransaction(instanceRoot, gameRoot, null);
                inspection.CanRestore = File.Exists(Path.Combine(instanceRoot, RestorePointFileName));
                var statePath = Path.Combine(instanceRoot, StateFileName);
                var legacyPlugin = Path.Combine(gameRoot, "BepInEx", "plugins", "BD2PTBR", "BD2PTBR.dll");
                if (!File.Exists(statePath))
                {
                    inspection.Health = File.Exists(legacyPlugin)
                        ? InstallationHealth.LegacyInstallation
                        : InstallationHealth.NotInstalled;
                    inspection.Summary = File.Exists(legacyPlugin)
                        ? "Uma instalação anterior foi encontrada e pode ser atualizada com segurança."
                        : "A tradução ainda não está instalada neste cliente.";
                    if (File.Exists(legacyPlugin))
                    {
                        try { inspection.ModVersion = AssemblyName.GetAssemblyName(legacyPlugin).Version.ToString(3); }
                        catch (BadImageFormatException) { inspection.ModVersion = "instalação legada"; }
                    }
                    return inspection;
                }

                var state = LoadState(statePath, gameRoot);
                foreach (var entry in state.files)
                {
                    var destination = PathSafety.CombineUnder(gameRoot, entry.path);
                    if (!File.Exists(destination))
                    {
                        inspection.Issues.Add("Arquivo ausente: " + entry.path);
                        continue;
                    }
                    if (!Hashing.EqualsSha256(destination, entry.installedSha256))
                    {
                        inspection.Issues.Add("Arquivo alterado: " + entry.path);
                    }
                }

                inspection.Health = inspection.Issues.Count == 0
                    ? InstallationHealth.InstalledAndVerified
                    : InstallationHealth.RepairRequired;
                inspection.Summary = inspection.Issues.Count == 0
                    ? "A instalação foi verificada e todos os arquivos correspondem à versão publicada."
                    : "A instalação precisa de reparo. Nenhum arquivo será alterado até sua confirmação.";
                inspection.ModVersion = state.modVersion;
                return inspection;
                }
            }
        }

        public OperationReport Install(
            string requestedGameRoot,
            IProgress<InstallerProgress> progress,
            InstallerEngineOptions options = null)
        {
            options = options ?? new InstallerEngineOptions();
            _fileOperations = 0;
            var warnings = new List<string>();
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            EnsureGameIsClosed(options);
            Report(progress, 2, "PREPARAÇÃO", "Cliente localizado e validado.");

            using (var payload = OpenPayload(options, progress))
            {
                if (!string.IsNullOrWhiteSpace(options.ExpectedModVersion) && options.ExpectedModVersion != payload.Manifest.modVersion)
                    throw new InvalidDataException("A versão interna do pacote não corresponde ao canal de atualização.");
                GameCompatibility.EnsureCompatible(gameRoot, payload.Manifest);
                EnsureWriteAccess(gameRoot);
                var instanceRoot = GetInstanceRoot(gameRoot, options);
                using (OperationLock.Acquire(instanceRoot))
                {
                Directory.CreateDirectory(instanceRoot);
                RecoverPendingTransaction(instanceRoot, gameRoot, progress);
                var statePath = Path.Combine(instanceRoot, StateFileName);
                var previousState = File.Exists(statePath) ? LoadState(statePath, gameRoot) : null;
                Version previousVersion;
                Version newVersion;
                if (previousState != null && Version.TryParse(previousState.modVersion, out previousVersion) &&
                    Version.TryParse(payload.Manifest.modVersion, out newVersion) && newVersion < previousVersion)
                    throw new InvalidDataException("Downgrade bloqueado. Use Restaurar versão anterior para recuperar o backup local.");
                bool repair = previousState != null && previousState.modVersion == payload.Manifest.modVersion;
                if (previousState != null && !repair) VerifyInstalledState(previousState, gameRoot);
                var loaderPreExisting = previousState != null
                    ? previousState.loaderPreExisting
                    : File.Exists(Path.Combine(gameRoot, "BepInEx", "core", "BepInEx.dll"));
                var installationId = previousState != null && !string.IsNullOrWhiteSpace(previousState.installationId)
                    ? previousState.installationId
                    : Guid.NewGuid().ToString("N");

                var previousByPath = previousState == null
                    ? new Dictionary<string, InstalledFileState>(StringComparer.OrdinalIgnoreCase)
                    : previousState.files.ToDictionary(item => PathSafety.NormalizeRelative(item.path), StringComparer.OrdinalIgnoreCase);
                var payloadByPath = payload.Manifest.files.ToDictionary(
                    item => PathSafety.NormalizeRelative(item.path),
                    StringComparer.OrdinalIgnoreCase);
                var legacyObsolete = previousState == null
                    ? FindLegacyV096ObsoleteLayers(gameRoot, payloadByPath, warnings)
                    : new Dictionary<string, InstalledFileState>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in legacyObsolete) previousByPath.Add(entry.Key, entry.Value);
                var changedPaths = payloadByPath.Keys.Concat(previousByPath.Keys.Except(payloadByPath.Keys, StringComparer.OrdinalIgnoreCase)).ToList();
                var journal = PrepareTransaction(instanceRoot, gameRoot, statePath, repair ? "repair" : "install", changedPaths, progress);

                try
                {
                    var newState = new InstallState
                    {
                        schemaVersion = 2,
                        installationId = installationId,
                        modVersion = payload.Manifest.modVersion,
                        installedAtUtc = DateTime.UtcNow.ToString("o"),
                        gameRoot = gameRoot,
                        clientIdentity = GameCompatibility.Capture(gameRoot),
                        loaderPreExisting = loaderPreExisting
                    };

                    var obsolete = previousByPath.Keys.Except(payloadByPath.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                    foreach (var relativePath in obsolete)
                    {
                        RestoreOrRemoveObsolete(gameRoot, instanceRoot, previousByPath[relativePath], warnings);
                        if (legacyObsolete.ContainsKey(relativePath) && !File.Exists(PathSafety.CombineUnder(gameRoot, relativePath)))
                            CountOperation(options);
                    }

                    var total = Math.Max(1, payloadByPath.Count);
                    var index = 0;
                    foreach (var pair in payloadByPath.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        index++;
                        var relativePath = pair.Key;
                        var source = PathSafety.CombineUnder(payload.ExtractedRoot, relativePath);
                        var destination = PathSafety.CombineUnder(gameRoot, relativePath);
                        InstalledFileState previous;
                        previousByPath.TryGetValue(relativePath, out previous);
                        var stateEntry = previous ?? CaptureOriginal(
                            instanceRoot,
                            gameRoot,
                            installationId,
                            relativePath,
                            destination);

                        stateEntry.path = relativePath.Replace('\\', '/');
                        stateEntry.installedSha256 = pair.Value.sha256.ToUpperInvariant();
                        ReplaceFileVerified(source, destination, pair.Value.sha256);
                        newState.files.Add(stateEntry);
                        CountOperation(options);

                        var percent = 18 + (int)Math.Round(index * 62.0 / total);
                        Report(progress, percent, "INSTALAÇÃO", "Aplicado: " + relativePath.Replace('\\', '/'));
                    }

                    Report(progress, 84, "VERIFICAÇÃO", "Conferindo os arquivos instalados por SHA-256.");
                    VerifyPayloadAtDestination(payload.Manifest, gameRoot);
                    JsonStorage.WriteAtomic(statePath, newState);
                    journal.status = "stateCommitted";
                    JsonStorage.WriteAtomic(Path.Combine(instanceRoot, JournalFileName), journal);
                    CommitTransaction(instanceRoot, journal);
                    Report(progress, 100, "CONCLUÍDO", "Brown Dust 2 PT-BR " + payload.Manifest.modVersion + " instalado e verificado.");

                    return new OperationReport
                    {
                        Success = true,
                        Action = previousState == null ? "Install" : "RepairOrUpdate",
                        GameRoot = gameRoot,
                        ModVersion = payload.Manifest.modVersion,
                        Message = "Instalação concluída e verificada. Abra o jogo normalmente.",
                        FilesProcessed = _fileOperations,
                        Warnings = warnings
                    };
                }
                catch (Exception error)
                {
                    var rolledBack = RollbackTransaction(instanceRoot, journal, progress);
                    var report = OperationReport.Failure("Install", gameRoot, error);
                    report.RolledBack = rolledBack;
                    report.FilesProcessed = _fileOperations;
                    report.Warnings = warnings;
                    throw new InstallerOperationException(report, error);
                }
                }
            }
        }

        public OperationReport Verify(
            string requestedGameRoot,
            IProgress<InstallerProgress> progress,
            InstallerEngineOptions options = null)
        {
            options = options ?? new InstallerEngineOptions();
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            EnsureGameIsClosed(options);
            var instanceRoot = GetInstanceRoot(gameRoot, options);
            using (OperationLock.Acquire(instanceRoot))
            {
                RecoverPendingTransaction(instanceRoot, gameRoot, progress);
                var state = LoadState(Path.Combine(instanceRoot, StateFileName), gameRoot);
                VerifyInstalledState(state, gameRoot);
                Report(progress, 100, "VERIFICADO", "Todos os arquivos instalados correspondem ao pacote oficial.");
                return new OperationReport
                {
                    Success = true,
                    Action = "Verify",
                    GameRoot = gameRoot,
                    ModVersion = state.modVersion,
                    Message = "Integridade da instalação confirmada.",
                    FilesProcessed = state.files.Count
                };
            }
        }

        private static EmbeddedPayload OpenPayload(InstallerEngineOptions options, IProgress<InstallerProgress> progress)
        {
            if (string.IsNullOrWhiteSpace(options.PayloadPath)) return EmbeddedPayload.Open(progress);
            if (!Hashing.EqualsSha256(options.PayloadPath, options.ExpectedPayloadSha256))
                throw new InvalidDataException("O pacote baixado não corresponde ao SHA-256 publicado.");
            return EmbeddedPayload.OpenFile(options.PayloadPath, progress, options.ExpectedPayloadSha256);
        }

        private static void VerifyInstalledState(InstallState state, string gameRoot)
        {
            foreach (var item in state.files)
                if (!Hashing.EqualsSha256(PathSafety.CombineUnder(gameRoot, item.path), item.installedSha256))
                    throw new InvalidDataException("Arquivo instalado ausente ou alterado: " + item.path);
        }

        public OperationReport Restore(string requestedGameRoot, IProgress<InstallerProgress> progress, InstallerEngineOptions options = null)
        {
            options = options ?? new InstallerEngineOptions();
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            EnsureGameIsClosed(options);
            var instanceRoot = GetInstanceRoot(gameRoot, options);
            using (OperationLock.Acquire(instanceRoot))
            {
                RecoverPendingTransaction(instanceRoot, gameRoot, progress);
                var point = JsonStorage.Read<TransactionJournal>(Path.Combine(instanceRoot, RestorePointFileName));
                if (!PathSafety.PathsEqual(point.gameRoot, gameRoot)) throw new InvalidDataException("Ponto de restauração de outro cliente.");
                GameCompatibility.EnsureIdentity(gameRoot, point.clientIdentity);
                var statePath = Path.Combine(instanceRoot, StateFileName);
                if (File.Exists(statePath)) VerifyInstalledState(LoadState(statePath, gameRoot), gameRoot);
                ValidateTransactionBackups(instanceRoot, point);
                var journal = PrepareTransaction(instanceRoot, gameRoot, statePath, "restore", point.files.Select(f => f.path), progress);
                try
                {
                    RestoreTransactionFiles(instanceRoot, point, false);
                    CountOperation(options);
                    journal.status = "stateCommitted";
                    JsonStorage.WriteAtomic(Path.Combine(instanceRoot, JournalFileName), journal);
                    CommitTransaction(instanceRoot, journal);
                    return new OperationReport { Success = true, Action = "Restore", GameRoot = gameRoot, Message = "Versão anterior restaurada e verificada.", RolledBack = true };
                }
                catch (Exception error)
                {
                    var report = OperationReport.Failure("Restore", gameRoot, error);
                    report.RolledBack = RollbackTransaction(instanceRoot, journal, progress);
                    throw new InstallerOperationException(report, error);
                }
            }
        }

        public OperationReport Uninstall(
            string requestedGameRoot,
            IProgress<InstallerProgress> progress,
            InstallerEngineOptions options = null)
        {
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            using (OperationLock.Acquire(GetInstanceRoot(gameRoot, options)))
                return UninstallCore(gameRoot, progress, options);
        }

        private OperationReport UninstallCore(string requestedGameRoot, IProgress<InstallerProgress> progress, InstallerEngineOptions options)
        {
            options = options ?? new InstallerEngineOptions();
            _fileOperations = 0;
            var warnings = new List<string>();
            var gameRoot = GameDetector.ResolveAndValidate(requestedGameRoot);
            EnsureGameIsClosed(options);
            EnsureWriteAccess(gameRoot);
            var instanceRoot = GetInstanceRoot(gameRoot, options);
            Directory.CreateDirectory(instanceRoot);
            RecoverPendingTransaction(instanceRoot, gameRoot, progress);
            var statePath = Path.Combine(instanceRoot, StateFileName);

            if (!File.Exists(statePath))
            {
                return RemoveLegacyPluginOnly(gameRoot, instanceRoot, statePath, progress, options);
            }

            var state = LoadState(statePath, gameRoot);
            var managedPaths = new HashSet<string>(
                state.files.Select(item => PathSafety.NormalizeRelative(item.path)),
                StringComparer.OrdinalIgnoreCase);
            var foreignLoaderContent = HasForeignLoaderContent(gameRoot, managedPaths);
            var preserveLoader = !state.loaderPreExisting && foreignLoaderContent;
            if (preserveLoader)
            {
                warnings.Add("O BepInEx foi preservado porque outros mods ou arquivos não gerenciados foram encontrados.");
            }

            var mutationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in state.files)
            {
                var relative = PathSafety.NormalizeRelative(entry.path);
                if (preserveLoader && !PathSafety.IsPluginPath(relative))
                {
                    continue;
                }
                mutationPaths.Add(relative);
            }

            if (!state.loaderPreExisting && !foreignLoaderContent)
            {
                var bepinExRoot = Path.Combine(gameRoot, "BepInEx");
                if (Directory.Exists(bepinExRoot))
                {
                    foreach (var file in Directory.EnumerateFiles(bepinExRoot, "*", SearchOption.AllDirectories))
                    {
                        mutationPaths.Add(PathSafety.RelativeFrom(gameRoot, file));
                    }
                }
            }

            var journal = PrepareTransaction(instanceRoot, gameRoot, statePath, "uninstall", mutationPaths, progress);
            try
            {
                var total = Math.Max(1, mutationPaths.Count);
                var index = 0;
                var stateByPath = state.files.ToDictionary(
                    item => PathSafety.NormalizeRelative(item.path),
                    StringComparer.OrdinalIgnoreCase);
                foreach (var relativePath in mutationPaths.OrderByDescending(item => item.Length))
                {
                    index++;
                    InstalledFileState entry;
                    stateByPath.TryGetValue(relativePath, out entry);
                    var destination = PathSafety.CombineUnder(gameRoot, relativePath);
                    if (entry != null && entry.originalExisted)
                    {
                        var original = PathSafety.CombineUnder(instanceRoot, entry.originalBackupRelativePath);
                        ValidateOriginalBackup(original, entry);
                        if (!File.Exists(original))
                        {
                            throw new InvalidDataException("Backup original ausente: " + relativePath);
                        }
                        ReplaceFileVerified(original, destination, Hashing.Sha256(original));
                    }
                    else if (File.Exists(destination))
                    {
                        if (entry != null && !PathSafety.IsPluginPath(relativePath) &&
                            !Hashing.EqualsSha256(destination, entry.installedSha256))
                        {
                            warnings.Add("Arquivo alterado preservado: " + relativePath.Replace('\\', '/'));
                            continue;
                        }
                        File.Delete(destination);
                    }

                    CountOperation(options);
                    Report(progress, 12 + (int)Math.Round(index * 72.0 / total), "REMOÇÃO", "Processado: " + relativePath.Replace('\\', '/'));
                }

                RemoveEmptyDirectories(Path.Combine(gameRoot, "BepInEx"), gameRoot);
                if (File.Exists(statePath))
                {
                    File.Delete(statePath);
                }
                journal.status = "stateCommitted";
                JsonStorage.WriteAtomic(Path.Combine(instanceRoot, JournalFileName), journal);
                CommitTransaction(instanceRoot, journal);
                CleanupOriginalBackups(instanceRoot, state.installationId, warnings);
                Report(progress, 100, "CONCLUÍDO", "A tradução foi removida com segurança.");

                return new OperationReport
                {
                    Success = true,
                    Action = "Uninstall",
                    GameRoot = gameRoot,
                    ModVersion = state.modVersion,
                    Message = warnings.Count == 0
                        ? "Tradução e carregador instalado pelo pacote foram removidos."
                        : "Tradução removida. Consulte os avisos sobre arquivos preservados.",
                    FilesProcessed = _fileOperations,
                    Warnings = warnings
                };
            }
            catch (Exception error)
            {
                var rolledBack = RollbackTransaction(instanceRoot, journal, progress);
                var report = OperationReport.Failure("Uninstall", gameRoot, error);
                report.RolledBack = rolledBack;
                report.FilesProcessed = _fileOperations;
                report.Warnings = warnings;
                throw new InstallerOperationException(report, error);
            }
        }

        private static OperationReport RemoveLegacyPluginOnly(
            string gameRoot,
            string instanceRoot,
            string statePath,
            IProgress<InstallerProgress> progress,
            InstallerEngineOptions options)
        {
            var pluginRoot = Path.Combine(gameRoot, "BepInEx", "plugins", "BD2PTBR");
            if (!Directory.Exists(pluginRoot))
            {
                return new OperationReport
                {
                    Success = true,
                    Action = "Uninstall",
                    GameRoot = gameRoot,
                    Message = "A tradução não estava instalada. Nenhum arquivo foi alterado."
                };
            }

            var files = Directory.EnumerateFiles(pluginRoot, "*", SearchOption.AllDirectories)
                .Select(path => PathSafety.RelativeFrom(gameRoot, path))
                .ToList();
            var engine = new InstallerEngine();
            var journal = engine.PrepareTransaction(instanceRoot, gameRoot, statePath, "legacy-uninstall", files, progress);
            try
            {
                foreach (var relativePath in files)
                {
                    File.Delete(PathSafety.CombineUnder(gameRoot, relativePath));
                    engine.CountOperation(options);
                }
                RemoveEmptyDirectories(pluginRoot, gameRoot);
                journal.status = "stateCommitted";
                JsonStorage.WriteAtomic(Path.Combine(instanceRoot, JournalFileName), journal);
                engine.CommitTransaction(instanceRoot, journal);
                Report(progress, 100, "CONCLUÍDO", "A instalação antiga da tradução foi removida; o BepInEx foi preservado.");
                return new OperationReport
                {
                    Success = true,
                    Action = "LegacyUninstall",
                    GameRoot = gameRoot,
                    Message = "Tradução antiga removida. O carregador foi preservado por segurança.",
                    FilesProcessed = files.Count
                };
            }
            catch
            {
                engine.RollbackTransaction(instanceRoot, journal, progress);
                throw;
            }
        }

        private static InstallState LoadState(string statePath, string gameRoot)
        {
            var state = JsonStorage.Read<InstallState>(statePath);
            if (state == null || state.schemaVersion < 2 || state.files == null)
            {
                throw new InvalidDataException("O estado da instalação é inválido. Use Reparar para reconstruí-lo.");
            }
            if (!PathSafety.PathsEqual(state.gameRoot, gameRoot))
            {
                throw new InvalidDataException("O estado pertence a outra pasta do jogo.");
            }
            foreach (var entry in state.files) GameCompatibility.EnsureModPath(entry.path);
            if (state.files.Select(f => f.path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.files.Count) throw new InvalidDataException("Estado contém caminhos duplicados.");
            return state;
        }

        private static InstalledFileState CaptureOriginal(
            string instanceRoot,
            string gameRoot,
            string installationId,
            string relativePath,
            string destination)
        {
            var entry = new InstalledFileState
            {
                path = relativePath.Replace('\\', '/'),
                originalExisted = File.Exists(destination) && !PathSafety.IsPluginPath(relativePath)
            };
            if (!entry.originalExisted)
            {
                return entry;
            }

            var backupRelative = Path.Combine("originals", installationId, relativePath);
            var backupPath = PathSafety.CombineUnder(instanceRoot, backupRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
            File.Copy(destination, backupPath, true);
            entry.originalBackupRelativePath = backupRelative.Replace('\\', '/');
            entry.originalBackupSha256 = Hashing.Sha256(destination);
            ValidateOriginalBackup(backupPath, entry);
            return entry;
        }

        private static void ValidateOriginalBackup(string path, InstalledFileState entry)
        {
            if (!File.Exists(path) || (!string.IsNullOrWhiteSpace(entry.originalBackupSha256) && !Hashing.EqualsSha256(path, entry.originalBackupSha256)))
                throw new InvalidDataException("Backup original ausente ou corrompido: " + entry.path);
        }

        private static Dictionary<string, InstalledFileState> FindLegacyV096ObsoleteLayers(
            string gameRoot,
            IDictionary<string, PayloadFile> payloadByPath,
            ICollection<string> warnings)
        {
            var found = new Dictionary<string, InstalledFileState>(StringComparer.OrdinalIgnoreCase);
            foreach (var layer in LegacyV096CatalogLayers)
            {
                var relative = PathSafety.NormalizeRelative("BepInEx/plugins/BD2PTBR/translations/" + layer.Key);
                if (payloadByPath.ContainsKey(relative)) continue;
                var destination = PathSafety.CombineUnder(gameRoot, relative);
                if (!File.Exists(destination)) continue;
                if (!Hashing.EqualsSha256(destination, layer.Value))
                {
                    warnings.Add("Camada legada desconhecida ou alterada foi preservada: " + relative.Replace('\\', '/'));
                    continue;
                }
                // Including the path before PrepareTransaction backs up the
                // old bytes for rollback and the manual legacy restore point.
                found.Add(relative, new InstalledFileState { path = relative.Replace('\\', '/'), installedSha256 = layer.Value });
            }
            return found;
        }

        private static void RestoreOrRemoveObsolete(
            string gameRoot,
            string instanceRoot,
            InstalledFileState entry,
            ICollection<string> warnings)
        {
            var relativePath = PathSafety.NormalizeRelative(entry.path);
            var destination = PathSafety.CombineUnder(gameRoot, relativePath);
            if (!File.Exists(destination))
            {
                return;
            }
            if (!Hashing.EqualsSha256(destination, entry.installedSha256))
            {
                warnings.Add("Arquivo obsoleto alterado foi preservado: " + entry.path);
                return;
            }
            if (entry.originalExisted)
            {
                var original = PathSafety.CombineUnder(instanceRoot, entry.originalBackupRelativePath);
                ValidateOriginalBackup(original, entry);
                ReplaceFileVerified(original, destination, Hashing.Sha256(original));
            }
            else
            {
                File.Delete(destination);
            }
        }

        private TransactionJournal PrepareTransaction(
            string instanceRoot,
            string gameRoot,
            string statePath,
            string action,
            IEnumerable<string> relativePaths,
            IProgress<InstallerProgress> progress)
        {
            var transactionId = Guid.NewGuid().ToString("N");
            var backupRoot = PathSafety.CombineUnder(instanceRoot, Path.Combine("transactions", transactionId));
            Directory.CreateDirectory(backupRoot);
            var journal = new TransactionJournal
            {
                schemaVersion = 1,
                status = "prepared",
                action = action,
                gameRoot = gameRoot,
                backupRoot = backupRoot,
                statePath = statePath,
                stateExisted = File.Exists(statePath),
                clientIdentity = GameCompatibility.Capture(gameRoot)
            };

            if (journal.stateExisted)
            {
                journal.stateBackupPath = Path.Combine(backupRoot, "installer-state.json");
                File.Copy(statePath, journal.stateBackupPath, true);
                journal.stateBackupSha256 = Hashing.Sha256(statePath);
            }

            var distinct = relativePaths
                .Select(PathSafety.NormalizeRelative)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var index = 0;
            foreach (var relativePath in distinct)
            {
                index++;
                var destination = PathSafety.CombineUnder(gameRoot, relativePath);
                var record = new TransactionFile
                {
                    path = relativePath.Replace('\\', '/'),
                    existed = File.Exists(destination)
                };
                if (record.existed)
                {
                    record.backupRelativePath = Path.Combine("files", relativePath).Replace('\\', '/');
                    var backup = PathSafety.CombineUnder(backupRoot, record.backupRelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(destination, backup, true);
                    record.backupSha256 = Hashing.Sha256(destination);
                    if (!Hashing.EqualsSha256(backup, record.backupSha256)) throw new IOException("Falha ao verificar backup: " + relativePath);
                }
                journal.files.Add(record);
                if (index % 25 == 0)
                {
                    Report(progress, Math.Min(16, 3 + index / 25), "BACKUP", "Preparando restauração automática: " + index + " arquivos.");
                }
            }

            JsonStorage.WriteAtomic(Path.Combine(instanceRoot, JournalFileName), journal);
            Report(progress, 17, "BACKUP", "Ponto de restauração transacional criado.");
            return journal;
        }

        private static void RecoverPendingTransaction(
            string instanceRoot,
            string gameRoot,
            IProgress<InstallerProgress> progress)
        {
            var journalPath = Path.Combine(instanceRoot, JournalFileName);
            if (!File.Exists(journalPath))
            {
                return;
            }
            var journal = JsonStorage.Read<TransactionJournal>(journalPath);
            if (journal == null || !PathSafety.PathsEqual(journal.gameRoot, gameRoot))
            {
                throw new InvalidDataException("Foi encontrada uma transação incompleta que não pertence a este cliente.");
            }
            if (string.Equals(journal.status, "stateCommitted", StringComparison.OrdinalIgnoreCase))
            {
                CommitTransactionFiles(instanceRoot, journal);
                return;
            }
            Report(progress, 1, "RECUPERAÇÃO", "Uma operação interrompida foi encontrada. Restaurando o estado anterior.");
            GameCompatibility.EnsureIdentity(gameRoot, journal.clientIdentity);
            RestoreTransactionFiles(instanceRoot, journal);
            Report(progress, 2, "RECUPERAÇÃO", "Estado anterior restaurado com sucesso.");
        }

        private bool RollbackTransaction(
            string instanceRoot,
            TransactionJournal journal,
            IProgress<InstallerProgress> progress)
        {
            try
            {
                Report(progress, 0, "ROLLBACK", "Falha detectada. Restaurando todos os arquivos anteriores.");
                RestoreTransactionFiles(instanceRoot, journal);
                Report(progress, 0, "ROLLBACK", "Restauração concluída. O jogo não ficou parcialmente modificado.");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CommitTransaction(string instanceRoot, TransactionJournal journal)
        {
            CommitTransactionFiles(instanceRoot, journal);
        }

        private static void ValidateTransactionBackups(string instanceRoot, TransactionJournal journal)
        {
            PathSafety.EnsureUnder(Path.Combine(instanceRoot, "transactions"), journal.backupRoot);
            if (!PathSafety.PathsEqual(journal.statePath, Path.Combine(instanceRoot, StateFileName))) throw new InvalidDataException("Estado transacional fora da instância.");
            foreach (var record in journal.files) GameCompatibility.EnsureModPath(record.path);
            foreach (var record in journal.files.Where(f => f.existed))
            {
                GameCompatibility.EnsureModPath(record.path);
                var backup = PathSafety.CombineUnder(journal.backupRoot, record.backupRelativePath);
                if (!File.Exists(backup) || (!string.IsNullOrWhiteSpace(record.backupSha256) && !Hashing.EqualsSha256(backup, record.backupSha256)))
                    throw new InvalidDataException("Backup ausente ou corrompido: " + record.path);
            }
            if (journal.stateExisted)
            {
                PathSafety.EnsureUnder(journal.backupRoot, journal.stateBackupPath);
                if (!File.Exists(journal.stateBackupPath)) throw new InvalidDataException("Estado anterior ausente.");
                if (!string.IsNullOrWhiteSpace(journal.stateBackupSha256) && !Hashing.EqualsSha256(journal.stateBackupPath, journal.stateBackupSha256)) throw new InvalidDataException("Backup do estado corrompido.");
            }
        }

        private static void RestoreTransactionFiles(string instanceRoot, TransactionJournal journal, bool finalize = true)
        {
            ValidateTransactionBackups(instanceRoot, journal);
            var backupRoot = Path.GetFullPath(journal.backupRoot);
            PathSafety.EnsureUnder(instanceRoot, backupRoot);
            foreach (var record in journal.files.AsEnumerable().Reverse())
            {
                var destination = PathSafety.CombineUnder(journal.gameRoot, record.path);
                if (record.existed)
                {
                    var backup = PathSafety.CombineUnder(backupRoot, record.backupRelativePath);
                    if (!File.Exists(backup))
                    {
                        throw new InvalidDataException("Backup transacional ausente: " + record.path);
                    }
                    ReplaceFileVerified(backup, destination, Hashing.Sha256(backup));
                }
                else if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
            }

            if (journal.stateExisted)
            {
                if (string.IsNullOrWhiteSpace(journal.stateBackupPath) || !File.Exists(journal.stateBackupPath))
                {
                    throw new InvalidDataException("Backup do estado do instalador ausente.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(journal.statePath));
                File.Copy(journal.stateBackupPath, journal.statePath, true);
            }
            else if (!string.IsNullOrWhiteSpace(journal.statePath) && File.Exists(journal.statePath))
            {
                File.Delete(journal.statePath);
            }

            RemoveEmptyDirectories(Path.Combine(journal.gameRoot, "BepInEx"), journal.gameRoot);
            if (finalize) CommitTransactionFiles(instanceRoot, journal);
        }

        private static void CommitTransactionFiles(string instanceRoot, TransactionJournal journal)
        {
            if (journal.status == "stateCommitted" && (journal.action == "install" || journal.action == "restore"))
            {
                JsonStorage.WriteAtomic(Path.Combine(instanceRoot, RestorePointFileName), journal);
                var pending = Path.Combine(instanceRoot, JournalFileName);
                if (File.Exists(pending)) File.Delete(pending);
                return;
            }
            if (journal.status == "stateCommitted" && journal.action == "uninstall")
            {
                var pointPath = Path.Combine(instanceRoot, RestorePointFileName);
                if (File.Exists(pointPath)) File.Delete(pointPath);
            }
            var journalPath = Path.Combine(instanceRoot, JournalFileName);
            if (File.Exists(journalPath))
            {
                File.Delete(journalPath);
            }
            if (!string.IsNullOrWhiteSpace(journal.backupRoot) && Directory.Exists(journal.backupRoot))
            {
                PathSafety.EnsureUnder(Path.Combine(instanceRoot, "transactions"), journal.backupRoot);
                Directory.Delete(journal.backupRoot, true);
            }
        }

        private static void VerifyPayloadAtDestination(PayloadManifest manifest, string gameRoot)
        {
            foreach (var entry in manifest.files)
            {
                var destination = PathSafety.CombineUnder(gameRoot, entry.path);
                if (!File.Exists(destination))
                {
                    throw new InvalidDataException("Arquivo instalado ausente: " + entry.path);
                }
                var info = new FileInfo(destination);
                if (info.Length != entry.bytes || !Hashing.EqualsSha256(destination, entry.sha256))
                {
                    throw new InvalidDataException("A verificação final falhou em: " + entry.path);
                }
            }
        }

        private static void ReplaceFileVerified(string source, string destination, string expectedHash)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Arquivo de origem ausente.", source);
            }
            var parent = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(parent);
            var temporary = destination + ".bd2ptbr-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temporary, true);
                if (!Hashing.EqualsSha256(temporary, expectedHash))
                {
                    throw new InvalidDataException("Falha de integridade antes da gravação: " + destination);
                }
                if (File.Exists(destination))
                {
                    try
                    {
                        File.Replace(temporary, destination, null, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temporary, destination, true);
                        File.Delete(temporary);
                    }
                    catch (IOException)
                    {
                        File.Copy(temporary, destination, true);
                        File.Delete(temporary);
                    }
                }
                else
                {
                    File.Move(temporary, destination);
                }
                if (!Hashing.EqualsSha256(destination, expectedHash))
                {
                    throw new InvalidDataException("Falha de integridade após a gravação: " + destination);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static bool HasForeignLoaderContent(string gameRoot, ISet<string> managedPaths)
        {
            var root = Path.Combine(gameRoot, "BepInEx");
            if (!Directory.Exists(root))
            {
                return false;
            }
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var relative = PathSafety.RelativeFrom(gameRoot, file);
                if (managedPaths.Contains(relative) || IsKnownGeneratedFile(relative))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static bool IsKnownGeneratedFile(string relativePath)
        {
            var normalized = PathSafety.NormalizeRelative(relativePath);
            return normalized.Equals("BepInEx\\LogOutput.log", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("BepInEx\\config\\BepInEx.cfg", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("BepInEx\\config\\br.com.bd2ptbr.translation.cfg", StringComparison.OrdinalIgnoreCase) ||
                   normalized.StartsWith("BepInEx\\config\\BD2PTBR\\", StringComparison.OrdinalIgnoreCase) ||
                   normalized.StartsWith("BepInEx\\cache\\", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureGameIsClosed(InstallerEngineOptions options)
        {
            if (options.SkipProcessCheck)
            {
                return;
            }
            var blocked = new[] { "BrownDust II", "BrownDust2", "BrownDust2Starter" };
            foreach (var name in blocked)
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    throw new InvalidOperationException("Feche Brown Dust 2 e o lançador antes de continuar.");
                }
            }
        }

        private static void EnsureWriteAccess(string gameRoot)
        {
            var probe = Path.Combine(gameRoot, ".bd2ptbr-write-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(probe, "test", Encoding.ASCII);
            }
            catch (UnauthorizedAccessException error)
            {
                throw new UnauthorizedAccessException(
                    "O Windows bloqueou a gravação na pasta do jogo. Execute o instalador como administrador e tente novamente.",
                    error);
            }
            finally
            {
                if (File.Exists(probe))
                {
                    File.Delete(probe);
                }
            }
        }

        private void CountOperation(InstallerEngineOptions options)
        {
            _fileOperations++;
            if (options.FaultAfterFileOperations > 0 && _fileOperations >= options.FaultAfterFileOperations)
            {
                throw new IOException("Falha de teste injetada após " + _fileOperations + " operações.");
            }
        }

        private static string GetInstanceRoot(string gameRoot, InstallerEngineOptions options = null)
        {
            var local = options != null && !string.IsNullOrWhiteSpace(options.StateRootOverride)
                ? Path.GetFullPath(options.StateRootOverride)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kacks", "BD2PTBR", "instances");
            var id = Hashing.Sha256Text(Path.GetFullPath(gameRoot).TrimEnd('\\').ToUpperInvariant()).Substring(0, 20);
            return Path.Combine(local, id);
        }

        private static void CleanupOriginalBackups(string instanceRoot, string installationId, ICollection<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(installationId))
            {
                return;
            }
            var path = PathSafety.CombineUnder(instanceRoot, Path.Combine("originals", installationId));
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception error)
            {
                warnings.Add("Não foi possível apagar o backup interno já restaurado: " + error.Message);
            }
        }

        private static void RemoveEmptyDirectories(string start, string authorizedRoot)
        {
            if (!Directory.Exists(start))
            {
                return;
            }
            PathSafety.EnsureUnder(authorizedRoot, start);
            foreach (var directory in Directory.EnumerateDirectories(start, "*", SearchOption.AllDirectories)
                                               .OrderByDescending(path => path.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, false);
                }
            }
            if (!Directory.EnumerateFileSystemEntries(start).Any())
            {
                Directory.Delete(start, false);
            }
        }

        private static void Report(IProgress<InstallerProgress> progress, int percent, string phase, string message)
        {
            progress?.Report(new InstallerProgress
            {
                Percent = Math.Max(0, Math.Min(100, percent)),
                Phase = phase,
                Message = message
            });
        }
    }

    public sealed class InstallerOperationException : Exception
    {
        public OperationReport Report { get; private set; }

        public InstallerOperationException(OperationReport report, Exception inner)
            : base(report.Message, inner)
        {
            Report = report;
        }
    }

    internal sealed class EmbeddedPayload : IDisposable
    {
        private const string ResourceName = "BD2PTBR.Payload.zip";
        public string ExtractedRoot { get; private set; }
        public PayloadManifest Manifest { get; private set; }

        private EmbeddedPayload() { }

        public static EmbeddedPayload Open(IProgress<InstallerProgress> progress = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream(ResourceName))
            {
                if (resource == null)
                {
                    throw new InvalidDataException("O pacote interno do mod não foi incorporado ao instalador.");
                }
                return OpenStream(resource, progress);
            }
        }

        public static EmbeddedPayload OpenFile(string path, IProgress<InstallerProgress> progress = null, string expectedSha256 = null)
        {
            using (var stream = File.OpenRead(path))
            {
                var payload = OpenStream(stream, progress, expectedSha256);
                if (payload.Manifest.schemaVersion != 3)
                {
                    payload.Dispose();
                    throw new InvalidDataException("Atualizações externas exigem manifesto de compatibilidade v3.");
                }
                return payload;
            }
        }

        private static EmbeddedPayload OpenStream(Stream resource, IProgress<InstallerProgress> progress, string expectedSha256 = null)
        {

                var temporaryRoot = Path.Combine(Path.GetTempPath(), "BD2PTBR", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temporaryRoot);
                var zipPath = Path.Combine(temporaryRoot, "payload.zip");
                using (var output = File.Create(zipPath))
                {
                    resource.CopyTo(output);
                }

                var result = new EmbeddedPayload { ExtractedRoot = Path.Combine(temporaryRoot, "files") };
                Directory.CreateDirectory(result.ExtractedRoot);
                try
                {
                    if (expectedSha256 != null && !Hashing.EqualsSha256(zipPath, expectedSha256))
                        throw new InvalidDataException("O pacote mudou durante a preparação. Download recusado.");
                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        var manifestEntry = archive.Entries.SingleOrDefault(entry =>
                            string.Equals(entry.FullName, "payload-manifest.json", StringComparison.Ordinal));
                        if (manifestEntry == null)
                        {
                            throw new InvalidDataException("Manifesto interno ausente.");
                        }
                        if (manifestEntry.Length > 2 * 1024 * 1024) throw new InvalidDataException("Manifesto interno excede o limite.");
                        string manifestJson;
                        using (var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8, true))
                        {
                            manifestJson = reader.ReadToEnd();
                        }
                        result.Manifest = JsonStorage.ReadText<PayloadManifest>(manifestJson);
                        ValidateManifest(result.Manifest);
                        var expected = result.Manifest.files.ToDictionary(
                            item => item.path.Replace('\\', '/'),
                            StringComparer.OrdinalIgnoreCase);
                        var actualFiles = archive.Entries
                            .Where(entry => !string.IsNullOrEmpty(entry.Name) && entry.FullName != "payload-manifest.json")
                            .Select(entry => new
                            {
                                Entry = entry,
                                RelativePath = entry.FullName.Replace('\\', '/')
                            })
                            .ToList();
                        if (actualFiles.Count != expected.Count || actualFiles.Any(item => !expected.ContainsKey(item.RelativePath)) ||
                            actualFiles.Select(item => item.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != actualFiles.Count)
                        {
                            throw new InvalidDataException("O pacote interno contém arquivos não declarados.");
                        }

                        var index = 0;
                        foreach (var item in actualFiles.OrderBy(candidate => candidate.RelativePath, StringComparer.Ordinal))
                        {
                            index++;
                            var entry = item.Entry;
                            var metadata = expected[item.RelativePath];
                            if (entry.Length != metadata.bytes) throw new InvalidDataException("Tamanho ZIP divergente: " + entry.FullName);
                            var relative = PathSafety.NormalizeRelative(item.RelativePath);
                            var destination = PathSafety.CombineUnder(result.ExtractedRoot, relative);
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            using (var input = entry.Open())
                            using (var output = File.Create(destination))
                            {
                                var buffer = new byte[65536];
                                long written = 0;
                                int count;
                                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    written += count;
                                    if (written > metadata.bytes) throw new InvalidDataException("Conteúdo ZIP maior que o declarado.");
                                    output.Write(buffer, 0, count);
                                }
                                if (written != metadata.bytes) throw new InvalidDataException("Conteúdo ZIP incompleto.");
                            }
                            if (new FileInfo(destination).Length != metadata.bytes ||
                                !Hashing.EqualsSha256(destination, metadata.sha256))
                            {
                                throw new InvalidDataException("Falha de integridade no pacote interno: " + entry.FullName);
                            }
                            if (index % 25 == 0)
                            {
                                progress?.Report(new InstallerProgress
                                {
                                    Percent = Math.Min(12, 2 + index / 25),
                                    Phase = "INTEGRIDADE",
                                    Message = "Verificados " + index + " arquivos do pacote interno."
                                });
                            }
                        }
                    }
                    File.Delete(zipPath);
                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
        }

        private static void ValidateManifest(PayloadManifest manifest)
        {
            if (manifest == null || (manifest.schemaVersion != 2 && manifest.schemaVersion != 3) || manifest.files == null || manifest.files.Count == 0 || manifest.files.Count > 5000 || manifest.files.Sum(f => f.bytes) > 1024L * 1024 * 1024)
            {
                throw new InvalidDataException("Manifesto interno inválido.");
            }
            var duplicates = manifest.files
                .GroupBy(item => item.path.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0)
            {
                throw new InvalidDataException("Manifesto interno contém caminhos duplicados.");
            }
            if (manifest.schemaVersion == 3)
            {
                var required = new[] {
                    "winhttp.dll", "doorstop_config.ini", "BepInEx/core/BepInEx.dll", "BepInEx/core/BepInEx.Preloader.dll", "BepInEx/core/0Harmony.dll",
                    "BepInEx/plugins/BD2PTBR/BD2PTBR.dll", "BepInEx/plugins/BD2PTBR/translations/complete.jsonl", "BepInEx/plugins/BD2PTBR/translations/hardcoded.jsonl"
                };
                if (required.Except(manifest.files.Select(f => f.path.Replace('\\', '/')), StringComparer.OrdinalIgnoreCase).Any())
                    throw new InvalidDataException("O pacote não contém todos os componentes essenciais do mod.");
            }
            foreach (var file in manifest.files)
            {
                PathSafety.NormalizeRelative(file.path);
                GameCompatibility.EnsureModPath(file.path);
                if (file.bytes < 0 || string.IsNullOrWhiteSpace(file.sha256) || file.sha256.Length != 64)
                {
                    throw new InvalidDataException("Entrada inválida no manifesto: " + file.path);
                }
            }
        }

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(ExtractedRoot))
            {
                return;
            }
            var temporaryRoot = Directory.GetParent(ExtractedRoot)?.FullName;
            var allowedRoot = Path.Combine(Path.GetTempPath(), "BD2PTBR");
            if (!string.IsNullOrWhiteSpace(temporaryRoot) && Directory.Exists(temporaryRoot))
            {
                PathSafety.EnsureUnder(allowedRoot, temporaryRoot);
                Directory.Delete(temporaryRoot, true);
            }
            ExtractedRoot = null;
        }
    }

    public static class GameDetector
    {
        private const string RegistryPath = @"Software\Neowiz\BrownDust2Starter\10000002";

        public static string Detect()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
            {
                var value = key?.GetValue("path") as string;
                if (IsValid(value))
                {
                    return Path.GetFullPath(value);
                }
            }

            foreach (var drive in DriveInfo.GetDrives().Where(item => item.IsReady && item.DriveType == DriveType.Fixed))
            {
                var candidates = new[]
                {
                    Path.Combine(drive.RootDirectory.FullName, "Neowiz", "Browndust2", "BrownDust2_10000002"),
                    Path.Combine(drive.RootDirectory.FullName, "Games", "BrownDust2_10000002")
                };
                foreach (var candidate in candidates)
                {
                    if (IsValid(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }
            return null;
        }

        public static string ResolveAndValidate(string requested)
        {
            var value = string.IsNullOrWhiteSpace(requested) ? Detect() : requested;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DirectoryNotFoundException("A pasta do jogo não foi localizada automaticamente. Selecione BrownDust2_10000002 manualmente.");
            }
            var fullPath = Path.GetFullPath(value.Trim().Trim('"'));
            if (!IsValid(fullPath))
            {
                throw new DirectoryNotFoundException("A pasta selecionada não contém uma instalação válida de Brown Dust 2.");
            }
            return fullPath.TrimEnd(Path.DirectorySeparatorChar);
        }

        public static bool IsValid(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            try
            {
                var fullPath = Path.GetFullPath(path);
                return File.Exists(Path.Combine(fullPath, "BrownDust II.exe")) &&
                       File.Exists(Path.Combine(fullPath, "UnityPlayer.dll")) &&
                       Directory.Exists(Path.Combine(fullPath, "BrownDust II_Data"));
            }
            catch
            {
                return false;
            }
        }

        public static string DetectPackage(string gameRoot)
        {
            var packages = Directory.GetFiles(gameRoot, "*.tied.dat", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name.EndsWith(".tied", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 5) : name)
                .OrderByDescending(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return packages.FirstOrDefault() ?? "não identificado";
        }
    }

    internal static class PathSafety
    {
        public static string NormalizeRelative(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Caminho relativo vazio.");
            }
            var normalized = value.Replace('/', '\\').TrimStart('\\');
            if (Path.IsPathRooted(value) || normalized.Contains(":") ||
                normalized.Split('\\').Any(part => part == ".." || part == "." || string.IsNullOrWhiteSpace(part)))
            {
                throw new InvalidDataException("Caminho relativo inseguro: " + value);
            }
            foreach (var part in normalized.Split('\\'))
            {
                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal) ||
                    System.Text.RegularExpressions.Regex.IsMatch(part, @"\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|\z)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    throw new InvalidDataException("Nome de arquivo inseguro no Windows: " + part);
            }
            return normalized;
        }

        public static string CombineUnder(string parent, string relative)
        {
            var safeRelative = NormalizeRelative(relative);
            var combined = Path.GetFullPath(Path.Combine(parent, safeRelative));
            EnsureUnder(parent, combined);
            var cursor = combined;
            var boundary = Path.GetFullPath(parent).TrimEnd('\\');
            while (!string.IsNullOrEmpty(cursor) && cursor.Length >= boundary.Length)
            {
                if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Links e junções não são aceitos em caminhos gerenciados: " + cursor);
                if (string.Equals(cursor, boundary, StringComparison.OrdinalIgnoreCase)) break;
                cursor = Path.GetDirectoryName(cursor);
            }
            return combined;
        }

        public static void EnsureUnder(string parent, string child)
        {
            var parentFull = Path.GetFullPath(parent).TrimEnd('\\') + "\\";
            var childFull = Path.GetFullPath(child);
            if (!childFull.StartsWith(parentFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Caminho fora da pasta autorizada: " + childFull);
            }
        }

        public static string RelativeFrom(string parent, string child)
        {
            EnsureUnder(parent, child);
            var prefix = Path.GetFullPath(parent).TrimEnd('\\') + "\\";
            return Path.GetFullPath(child).Substring(prefix.Length);
        }

        public static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }
            return string.Equals(
                Path.GetFullPath(left).TrimEnd('\\'),
                Path.GetFullPath(right).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPluginPath(string relativePath)
        {
            var normalized = NormalizeRelative(relativePath);
            return normalized.StartsWith("BepInEx\\plugins\\BD2PTBR\\", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class Hashing
    {
        public static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        public static string Sha256Text(string value)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
            }
        }

        public static bool EqualsSha256(string path, string expected)
        {
            return string.Equals(Sha256(path), expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
