using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BD2PTBR.Installer
{
    [DataContract]
    internal sealed class LauncherUpdateRequest
    {
        [DataMember] public int schemaVersion { get; set; }
        [DataMember] public string target { get; set; }
        [DataMember] public string source { get; set; }
        [DataMember] public string previousSha256 { get; set; }
        [DataMember] public LauncherRelease release { get; set; }
        [DataMember] public int parentPid { get; set; }
        [DataMember] public long parentStartedUtcTicks { get; set; }
        [DataMember] public string gameRoot { get; set; }
    }

    internal static class LauncherSelfUpdate
    {
        private static string TransactionsRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kacks", "BD2PTBR", "launcher-updates");
        internal static string CurrentExecutable => Process.GetCurrentProcess().MainModule.FileName;
        internal static string Quote(string value) => "\"" + value.Replace("\"", "") + "\"";

        public static async Task StageAsync(string download, LauncherRelease release, string gameRoot, CancellationToken cancellation)
        {
            UpdateService.ValidateLauncher(release);
            EnsureApplication(download, release.version, release.sha256);
            if (new FileInfo(download).Length != release.bytes) throw new InvalidDataException("Tamanho do aplicativo divergente.");
            if (Version.Parse(release.version) <= Version.Parse(InstallerEngine.InstallerVersion)) throw new InvalidDataException("A atualização deve ser mais recente que este aplicativo.");
            var current = CurrentExecutable;
            var directory = PathSafety.CombineUnder(TransactionsRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var helper = PathSafety.CombineUnder(directory, "BD2PTBR-update.exe");
            File.Copy(download, helper);
            EnsureApplication(helper, release.version, release.sha256);
            var parent = Process.GetCurrentProcess();
            var request = new LauncherUpdateRequest {
                schemaVersion = 1, target = current, source = helper, previousSha256 = Hashing.Sha256(current), release = release,
                parentPid = parent.Id, parentStartedUtcTicks = parent.StartTime.ToUniversalTime().Ticks, gameRoot = gameRoot
            };
            var receipt = Path.Combine(directory, "request.json");
            JsonStorage.WriteAtomic(receipt, request);
            using (var worker = Process.Start(new ProcessStartInfo {
                FileName = helper, Arguments = "--apply-launcher-update " + Quote(receipt), WorkingDirectory = directory,
                UseShellExecute = false, CreateNoWindow = true
            }))
            {
                for (var attempt = 0; attempt < 200; attempt++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (File.Exists(Path.Combine(directory, "ready"))) return;
                    if (worker == null || worker.HasExited) throw new IOException(ReadFailure(directory));
                    await Task.Delay(50, cancellation);
                }
                throw new TimeoutException("O aplicativo atual permaneceu aberto: o auxiliar de atualização não ficou pronto.");
            }
        }

        private static string ReadFailure(string directory)
        {
            var path = Path.Combine(directory, "failure.txt");
            return File.Exists(path) ? File.ReadAllText(path) : "Não foi possível preparar a atualização do aplicativo. A versão atual foi preservada.";
        }

        private static LauncherUpdateRequest ReadRequest(string receipt)
        {
            var full = Path.GetFullPath(receipt);
            var root = Path.GetFullPath(TransactionsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(full) != "request.json")
                throw new InvalidDataException("Solicitação de atualização fora da pasta do aplicativo.");
            PathSafety.CombineUnder(TransactionsRoot, full.Substring(root.Length));
            if (new FileInfo(full).Length > 16384) throw new InvalidDataException("Solicitação de atualização inválida.");
            var request = JsonStorage.Read<LauncherUpdateRequest>(full);
            if (request == null || request.schemaVersion != 1 || request.release == null || request.parentPid < 1 ||
                string.IsNullOrEmpty(request.target) || string.IsNullOrEmpty(request.source) || string.IsNullOrEmpty(request.previousSha256) || !Path.IsPathRooted(request.target) ||
                !string.Equals(Path.GetFullPath(request.source), Path.Combine(Path.GetDirectoryName(full), "BD2PTBR-update.exe"), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Solicitação de atualização incompleta.");
            UpdateService.ValidateLauncher(request.release);
            return request;
        }

        public static void Apply(string receipt)
        {
            string directory = null;
            LauncherUpdateRequest request = null;
            bool parentStopped = false;
            try
            {
                request = ReadRequest(receipt);
                directory = Path.GetDirectoryName(Path.GetFullPath(receipt));
                if (!string.Equals(CurrentExecutable, request.source, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Auxiliar de atualização divergente.");
                EnsureApplication(request.source, request.release.version, request.release.sha256);
                EnsureApplication(request.target, null, request.previousSha256);
                var oldVersion = AssemblyName.GetAssemblyName(request.target).Version;
                if (Version.Parse(request.release.version) <= oldVersion) throw new InvalidDataException("Atualização ou reversão indevida do aplicativo.");
                using (var parent = Process.GetProcessById(request.parentPid))
                {
                    if (parent.StartTime.ToUniversalTime().Ticks != request.parentStartedUtcTicks ||
                        !string.Equals(parent.MainModule.FileName, request.target, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("A instância atual do aplicativo mudou.");
                    File.WriteAllText(Path.Combine(directory, "ready"), "ready");
                    if (!parent.WaitForExit(30000)) throw new TimeoutException("Feche o aplicativo para concluir a atualização. A versão atual foi preservada.");
                }
                parentStopped = true;
                ReplaceVerified(request.source, request.target, request.release.sha256, request.previousSha256, () => {
                    var args = "--launcher-update-receipt " + Quote(receipt);
                    if (!string.IsNullOrEmpty(request.gameRoot)) args += " --game-root " + Quote(request.gameRoot);
                    using (var next = Process.Start(new ProcessStartInfo { FileName = request.target, Arguments = args, WorkingDirectory = Path.GetDirectoryName(request.target), UseShellExecute = false }))
                    {
                        for (int attempt = 0; attempt < 600; attempt++)
                        {
                            if (File.Exists(Path.Combine(directory, "healthy"))) return true;
                            if (next == null || next.HasExited) return false;
                            Thread.Sleep(50);
                        }
                        // Only the unacknowledged instance launched by this helper
                        // is terminated, so its locked binary can be rolled back.
                        if (!next.HasExited) { next.Kill(); next.WaitForExit(5000); }
                        return false;
                    }
                });
                File.WriteAllText(Path.Combine(directory, "completed"), request.release.version);
                NotifyIconChanged(request.target);
            }
            catch (Exception error)
            {
                if (directory != null && Directory.Exists(directory)) File.WriteAllText(Path.Combine(directory, "failure.txt"), error.Message);
                if (parentStopped && request != null && Hashing.EqualsSha256(request.target, request.previousSha256))
                {
                    // The old binary has been preserved or restored. Resume its
                    // normal UI without retrying the failed update automatically.
                    try { Process.Start(new ProcessStartInfo { FileName = request.target, Arguments = string.IsNullOrEmpty(request.gameRoot) ? "" : "--game-root " + Quote(request.gameRoot), WorkingDirectory = Path.GetDirectoryName(request.target), UseShellExecute = false }); }
                    catch { /* The verified previous executable remains available. */ }
                }
                throw;
            }
        }

        internal static void ReplaceVerified(string source, string target, string expectedSha256, string previousSha256, Func<bool> startAndConfirm)
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(target));
            PathSafety.CombineUnder(parent, Path.GetFileName(target));
            if (!Hashing.EqualsSha256(source, expectedSha256) || !Hashing.EqualsSha256(target, previousSha256)) throw new InvalidDataException("O aplicativo mudou antes da substituição.");
            var staged = PathSafety.CombineUnder(parent, ".bd2ptbr-update-" + Guid.NewGuid().ToString("N") + ".exe");
            var backup = target + ".previous";
            PathSafety.CombineUnder(parent, Path.GetFileName(backup));
            bool replaced = false;
            try
            {
                using (var input = File.OpenRead(source))
                using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { input.CopyTo(output); output.Flush(true); }
                if (!Hashing.EqualsSha256(staged, expectedSha256) || !Hashing.EqualsSha256(target, previousSha256)) throw new InvalidDataException("Falha ao preparar o novo aplicativo.");
                File.Replace(staged, target, backup, true);
                replaced = true;
                if (!Hashing.EqualsSha256(target, expectedSha256) || !Hashing.EqualsSha256(backup, previousSha256) || !startAndConfirm())
                    throw new IOException("A nova versão não confirmou a abertura. O aplicativo anterior será restaurado.");
            }
            catch
            {
                if (replaced)
                {
                    if (!Hashing.EqualsSha256(backup, previousSha256)) throw new IOException("O backup do aplicativo requer recuperação manual: " + backup);
                    File.Replace(backup, target, target + ".failed-" + Guid.NewGuid().ToString("N"), true);
                    if (!Hashing.EqualsSha256(target, previousSha256)) throw new IOException("Não foi possível confirmar a restauração do aplicativo: " + target);
                }
                throw;
            }
            finally { if (File.Exists(staged)) File.Delete(staged); }
        }

        public static void ConfirmOpened(string receipt)
        {
            var request = ReadRequest(receipt);
            if (!string.Equals(CurrentExecutable, request.target, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Destino da atualização divergente.");
            EnsureApplication(CurrentExecutable, request.release.version, request.release.sha256);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(receipt)), "healthy"), request.release.version);
        }

        internal static void EnsureApplication(string path, string expectedVersion, string expectedSha256)
        {
            var full = Path.GetFullPath(path);
            PathSafety.CombineUnder(Path.GetDirectoryName(full), Path.GetFileName(full));
            if (!Hashing.EqualsSha256(full, expectedSha256)) throw new InvalidDataException("SHA-256 do aplicativo divergente.");
            var identity = AssemblyName.GetAssemblyName(full);
            if (identity.Name != "BD2PTBR-Installer" || (expectedVersion != null && identity.Version.ToString(3) != expectedVersion))
                throw new InvalidDataException("O executável não corresponde ao aplicativo oficial esperado.");
        }

        [DllImport("shell32.dll")] private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr first, IntPtr second);
        private static void NotifyIconChanged(string path)
        {
            var value = Marshal.StringToHGlobalUni(path);
            try { SHChangeNotify(0x2000, 0x0005, value, IntPtr.Zero); }
            finally { Marshal.FreeHGlobal(value); }
        }
    }
}
