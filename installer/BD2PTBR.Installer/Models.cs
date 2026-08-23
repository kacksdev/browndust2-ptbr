using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BD2PTBR.Installer
{
    public enum InstallerAction
    {
        Install,
        Repair,
        Verify,
        Uninstall
    }

    public enum InstallationHealth
    {
        NotInstalled,
        LegacyInstallation,
        InstalledAndVerified,
        RepairRequired
    }

    public sealed class InstallerProgress
    {
        public int Percent { get; set; }
        public string Phase { get; set; }
        public string Message { get; set; }
    }

    public sealed class InstallerEngineOptions
    {
        public int FaultAfterFileOperations { get; set; }
        public bool SkipProcessCheck { get; set; }
        public string StateRootOverride { get; set; }
    }

    public sealed class InstallationInspection
    {
        public string GameRoot { get; set; }
        public string DetectedPackage { get; set; }
        public string TestedClient { get; set; }
        public string ModVersion { get; set; }
        public InstallationHealth Health { get; set; }
        public string Summary { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
    }

    public sealed class OperationReport
    {
        public bool Success { get; set; }
        public string Action { get; set; }
        public string GameRoot { get; set; }
        public string ModVersion { get; set; }
        public string Message { get; set; }
        public int FilesProcessed { get; set; }
        public bool RolledBack { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public string ErrorType { get; set; }
        public string ErrorDetails { get; set; }

        public static OperationReport Failure(string action, string gameRoot, Exception error)
        {
            return new OperationReport
            {
                Success = false,
                Action = action,
                GameRoot = gameRoot,
                Message = error.Message,
                ErrorType = error.GetType().FullName,
                ErrorDetails = error.ToString()
            };
        }
    }

    [DataContract]
    internal sealed class PayloadManifest
    {
        [DataMember(Name = "schemaVersion")]
        public int schemaVersion { get; set; }
        [DataMember(Name = "modVersion")]
        public string modVersion { get; set; }
        [DataMember(Name = "testedClientVersion")]
        public string testedClientVersion { get; set; }
        [DataMember(Name = "testedGamePackage")]
        public string testedGamePackage { get; set; }
        [DataMember(Name = "files")]
        public List<PayloadFile> files { get; set; }
    }

    [DataContract]
    internal sealed class PayloadFile
    {
        [DataMember(Name = "path")]
        public string path { get; set; }
        [DataMember(Name = "bytes")]
        public long bytes { get; set; }
        [DataMember(Name = "sha256")]
        public string sha256 { get; set; }
    }

    [DataContract]
    internal sealed class InstallState
    {
        [DataMember(Name = "schemaVersion")]
        public int schemaVersion { get; set; }
        [DataMember(Name = "installationId")]
        public string installationId { get; set; }
        [DataMember(Name = "modVersion")]
        public string modVersion { get; set; }
        [DataMember(Name = "installedAtUtc")]
        public string installedAtUtc { get; set; }
        [DataMember(Name = "gameRoot")]
        public string gameRoot { get; set; }
        [DataMember(Name = "loaderPreExisting")]
        public bool loaderPreExisting { get; set; }
        [DataMember(Name = "files")]
        public List<InstalledFileState> files { get; set; } = new List<InstalledFileState>();
    }

    [DataContract]
    internal sealed class InstalledFileState
    {
        [DataMember(Name = "path")]
        public string path { get; set; }
        [DataMember(Name = "installedSha256")]
        public string installedSha256 { get; set; }
        [DataMember(Name = "originalExisted")]
        public bool originalExisted { get; set; }
        [DataMember(Name = "originalBackupRelativePath")]
        public string originalBackupRelativePath { get; set; }
    }

    [DataContract]
    internal sealed class TransactionJournal
    {
        [DataMember(Name = "schemaVersion")]
        public int schemaVersion { get; set; }
        [DataMember(Name = "status")]
        public string status { get; set; }
        [DataMember(Name = "action")]
        public string action { get; set; }
        [DataMember(Name = "gameRoot")]
        public string gameRoot { get; set; }
        [DataMember(Name = "backupRoot")]
        public string backupRoot { get; set; }
        [DataMember(Name = "statePath")]
        public string statePath { get; set; }
        [DataMember(Name = "stateExisted")]
        public bool stateExisted { get; set; }
        [DataMember(Name = "stateBackupPath")]
        public string stateBackupPath { get; set; }
        [DataMember(Name = "files")]
        public List<TransactionFile> files { get; set; } = new List<TransactionFile>();
    }

    [DataContract]
    internal sealed class TransactionFile
    {
        [DataMember(Name = "path")]
        public string path { get; set; }
        [DataMember(Name = "existed")]
        public bool existed { get; set; }
        [DataMember(Name = "backupRelativePath")]
        public string backupRelativePath { get; set; }
    }

    internal static class JsonStorage
    {
        public static T Read<T>(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
        }

        public static T ReadText<T>(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
        }

        public static string Serialize(object value)
        {
            if (value == null)
            {
                return "null";
            }
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(value.GetType()).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static void WriteAtomic(string path, object value)
        {
            var fullPath = Path.GetFullPath(path);
            var parent = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(parent))
            {
                throw new InvalidOperationException("Destino JSON inválido.");
            }
            Directory.CreateDirectory(parent);
            var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, Serialize(value), new UTF8Encoding(false));
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Replace(temporary, fullPath, null, true);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(temporary, fullPath, true);
                    File.Delete(temporary);
                }
            }
            else
            {
                File.Move(temporary, fullPath);
            }
        }
    }
}
