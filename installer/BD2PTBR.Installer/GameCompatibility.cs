using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BD2PTBR.Installer
{
    internal static class GameCompatibility
    {
        internal static readonly string[] IdentityPaths = {
            "BrownDust II.exe", "BrownDust II_Data/Managed/Assembly-CSharp.dll", "BrownDust II_Data/globalgamemanagers"
        };

        public static string ReadVersion(string root)
        {
            var path = Path.Combine(root, "BrownDust II_Data", "globalgamemanagers");
            if (!File.Exists(path)) return "não identificado";
            var match = Regex.Match(Encoding.ASCII.GetString(File.ReadAllBytes(path)), @"(?<![\d.])2\.\d{1,3}\.\d{1,3}(?![\d.])");
            return match.Success ? match.Value : "não identificado";
        }

        public static ClientIdentity Capture(string root)
        {
            return new ClientIdentity {
                package = GameDetector.DetectPackage(root), version = ReadVersion(root),
                files = IdentityPaths.Select(p => {
                    var full = PathSafety.CombineUnder(root, p);
                    return new PayloadFile { path = p, bytes = new FileInfo(full).Length, sha256 = Hashing.Sha256(full) };
                }).ToList()
            };
        }

        public static void EnsureCompatible(string root, PayloadManifest manifest)
        {
            if (manifest.schemaVersion == 2)
            {
                if (GameDetector.DetectPackage(root) != manifest.testedGamePackage)
                    throw new InvalidDataException("Este pacote antigo não foi validado para o cliente instalado. Busque uma atualização compatível.");
                return;
            }
            EnsureAnyIdentity(root, manifest.compatibleClients);
        }

        public static void EnsureAnyIdentity(string root, IList<ClientIdentity> identities)
        {
            if (identities == null || identities.Count == 0) throw new InvalidDataException("Nenhum cliente compatível foi declarado.");
            foreach (var identity in identities)
            {
                try { EnsureIdentity(root, identity); return; }
                catch (InvalidDataException) { }
            }
            throw new InvalidDataException("Atualização bloqueada: pacote, versão ou arquivos críticos do jogo não correspondem a um cliente validado. Nenhum arquivo do jogo foi alterado.");
        }

        public static void EnsureIdentity(string root, ClientIdentity identity)
        {
            if (identity == null || identity.files == null || identity.files.Count != IdentityPaths.Length ||
                identity.package != GameDetector.DetectPackage(root) || identity.version != ReadVersion(root) ||
                IdentityPaths.Except(identity.files.Select(f => f.path), StringComparer.OrdinalIgnoreCase).Any())
                throw new InvalidDataException("Identidade do cliente divergente.");
            foreach (var file in identity.files)
            {
                var full = PathSafety.CombineUnder(root, file.path);
                if (!File.Exists(full) || new FileInfo(full).Length != file.bytes || !Hashing.EqualsSha256(full, file.sha256))
                    throw new InvalidDataException("Arquivo crítico divergente: " + file.path);
            }
        }

        public static void EnsureModPath(string path)
        {
            var p = PathSafety.NormalizeRelative(path);
            if (p == "winhttp.dll" || p == "doorstop_config.ini" || p == ".doorstop_version" || p == "changelog.txt" ||
                p.StartsWith("BepInEx\\core\\", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("BepInEx\\plugins\\BD2PTBR\\", StringComparison.OrdinalIgnoreCase) ||
                p == "BepInEx\\config\\BD2PTBR.cfg") return;
            throw new InvalidDataException("O pacote tenta alterar um arquivo fora do mod: " + path);
        }
    }

    internal sealed class OperationLock : IDisposable
    {
        private readonly FileStream _stream;
        private OperationLock(FileStream stream) { _stream = stream; }
        public static OperationLock Acquire(string instanceRoot)
        {
            Directory.CreateDirectory(instanceRoot);
            try { return new OperationLock(new FileStream(Path.Combine(instanceRoot, "operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)); }
            catch (IOException error) { throw new IOException("Outra operação do instalador está em andamento para este jogo.", error); }
        }
        public void Dispose() { _stream.Dispose(); }
    }
}
