using System;
using System.IO;
using System.Reflection;

namespace BD2PTBR.Installer
{
    internal static class IntegrationCompatibility
    {
        public const string Profile = "bd2-mono-x64-source-bound-v1";

        // Inspect capabilities, never execute a game assembly or require an exact
        // catalog/game version. Cecil reads metadata without loading game code;
        // all input streams are disposed after the probe.
        public static string Inspect(string root)
        {
            root = GameDetector.ResolveAndValidate(root);
            EnsureNativeX64(PathSafety.CombineUnder(root, "BrownDust II.exe"));
            EnsureNativeX64(PathSafety.CombineUnder(root, "UnityPlayer.dll"));
            if (!File.Exists(PathSafety.CombineUnder(root, "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll")))
                throw new InvalidDataException("Este cliente não usa a integração Mono suportada pelo mod.");
            EnsureMetadataDependency();
            return IntegrationMetadataProbe.Inspect(root);
        }

        private static readonly object MetadataLock = new object();
        private static bool _metadataRegistered;
        private static void EnsureMetadataDependency()
        {
            lock (MetadataLock)
            {
                if (_metadataRegistered) return;
                AppDomain.CurrentDomain.AssemblyResolve += (_, args) => {
                    if (new AssemblyName(args.Name).Name != "Mono.Cecil") return null;
                    using (var source = typeof(IntegrationCompatibility).Assembly.GetManifestResourceStream("BD2PTBR.Metadata.Mono.Cecil.dll"))
                    using (var bytes = new MemoryStream())
                    {
                        if (source == null) throw new InvalidDataException("Biblioteca de inspeção ausente no aplicativo.");
                        source.CopyTo(bytes);
                        return Assembly.Load(bytes.ToArray());
                    }
                };
                _metadataRegistered = true;
            }
        }

        internal static void EnsureNativeX64(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (reader.BaseStream.Length < 128 || reader.ReadUInt16() != 0x5a4d) throw new InvalidDataException("Executável inválido: " + Path.GetFileName(path));
                reader.BaseStream.Position = 0x3c;
                var offset = reader.ReadInt32();
                if (offset < 64 || offset > reader.BaseStream.Length - 26) throw new InvalidDataException("Cabeçalho PE inválido.");
                reader.BaseStream.Position = offset;
                if (reader.ReadUInt32() != 0x4550 || reader.ReadUInt16() != 0x8664) throw new InvalidDataException("O mod requer o cliente Windows x64.");
                reader.BaseStream.Position = offset + 24;
                if (reader.ReadUInt16() != 0x20b) throw new InvalidDataException("Formato do cliente x64 não reconhecido.");
            }
        }
    }

}
