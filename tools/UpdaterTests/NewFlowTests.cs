using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using BD2PTBR.Installer;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class NewFlowTests
{
    public static void Run(string output, Action<string, Action> run)
    {
        run("structural_profile_accepts_new_version_package_and_unrelated_binary_changes", () => {
            var root = Fixture(output, "new-client");
            var identity = GameCompatibility.Capture(root);
            var before = IdentityHashes(root);
            File.WriteAllText(Path.Combine(root, "BrownDust II_Data/globalgamemanagers"), "synthetic 2.99.12\0");
            File.WriteAllText(Path.Combine(root, "20991231000.tied.dat"), "new content");
            var native = File.ReadAllBytes(Path.Combine(root, "BrownDust II.exe")); native[120] = 77; File.WriteAllBytes(Path.Combine(root, "BrownDust II.exe"), native);
            WriteGameAssembly(root, true, "UpdatedUnrelatedSystem");
            Check(before != IdentityHashes(root));
            var release = Release(identity);
            Check(UpdateService.SelectRelease(new UpdateFeed { schemaVersion = 1, releases = new List<UpdateRelease> { release } }, root) == release);
            GameCompatibility.EnsureCompatible(root, new PayloadManifest { schemaVersion = 3, integrationProfile = IntegrationCompatibility.Profile, compatibleClients = new List<ClientIdentity> { identity } });
        });
        run("structural_profile_rejects_missing_mono", () => {
            var root = Fixture(output, "missing-mono"); File.Delete(Path.Combine(root, "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll")); Reject(() => IntegrationCompatibility.Inspect(root));
        });
        run("older_catalog_download_install_verify_on_newer_structurally_compatible_client", () => {
            var root = Fixture(output, "install-older-catalog");
            var identity = GameCompatibility.Capture(root);
            var options = Program.Package(root, "0.2.0", integrationProfile: IntegrationCompatibility.Profile);
            File.WriteAllText(Path.Combine(root, "BrownDust II_Data/globalgamemanagers"), "synthetic 2.99.12\0");
            File.WriteAllText(Path.Combine(root, "20991231000.tied.dat"), "new content");
            WriteGameAssembly(root, true, "LaterUnrelatedChanges");
            var officialBefore = IdentityHashes(root);
            var release = Release(identity); release.bytes = new FileInfo(options.PayloadPath).Length; release.sha256 = Hashing.Sha256(options.PayloadPath);
            var feed = new UpdateFeed { schemaVersion = 1, releases = new List<UpdateRelease> { release } };
            using (var service = new UpdateService(new StubHandler(uri => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = uri.RequestUri.AbsoluteUri == UpdateService.FeedUrl ? (HttpContent)new StringContent(JsonStorage.Serialize(feed), Encoding.UTF8, "application/json") : new ByteArrayContent(File.ReadAllBytes(options.PayloadPath))
            }), Path.Combine(output, "older-catalog-cache")))
            {
                var selected = service.CheckAllAsync(root, CancellationToken.None).GetAwaiter().GetResult().Mod;
                Check(selected?.modVersion == "0.2.0");
                var downloaded = service.DownloadAsync(selected, root, null, CancellationToken.None).GetAwaiter().GetResult();
                downloaded.StateRootOverride = options.StateRootOverride; downloaded.SkipProcessCheck = true;
                var engine = new InstallerEngine();
                Check(engine.Install(root, null, downloaded).Success);
                Check(engine.Verify(root, null, downloaded).Success);
                Check(IdentityHashes(root) == officialBefore);
            }
        });
        run("structural_profile_rejects_incompatible_architecture", () => {
            var root = Fixture(output, "x86"); var bytes = Native(); bytes[68] = 0x4c; bytes[69] = 0x01; File.WriteAllBytes(Path.Combine(root, "BrownDust II.exe"), bytes); Reject(() => IntegrationCompatibility.Inspect(root));
        });
        run("structural_profile_rejects_changed_localization_api", () => {
            var root = Fixture(output, "changed-api"); WriteGameAssembly(root, false, "ChangedApi"); Reject(() => IntegrationCompatibility.Inspect(root));
        });
        run("unknown_profile_requires_application_upgrade", () => {
            var root = Fixture(output, "unknown-profile"); Reject(() => GameCompatibility.EnsureCatalogCompatible(root, null, "unrecognized-future-profile"));
        });
        run("app_update_is_discoverable_without_game", () => {
            var launcher = new LauncherRelease { version = "9.0.0", url = "https://github.com/kacksdev/browndust2-ptbr/releases/download/v9.0.0/BD2PTBR.exe", bytes = 123, sha256 = new string('A', 64) };
            var feed = new UpdateFeed { schemaVersion = 1, releases = new List<UpdateRelease>(), launcher = launcher };
            using (var service = new UpdateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonStorage.Serialize(feed), Encoding.UTF8, "application/json") }), Path.Combine(output, "app-no-game")))
            {
                var available = service.CheckAllAsync(null, CancellationToken.None).GetAwaiter().GetResult();
                Check(available.Mod == null && available.Launcher.version == "9.0.0");
            }
        });
        run("app_metadata_rejects_untrusted_download", () => {
            Reject(() => UpdateService.ValidateLauncher(new LauncherRelease { version = "9.0.0", url = "https://example.com/app.exe", bytes = 1, sha256 = new string('A', 64) }));
        });
        run("app_update_still_available_when_game_metadata_is_unreadable", () => {
            var root = Fixture(output, "unreadable-game"); var mod = Release(GameCompatibility.Capture(root));
            File.WriteAllText(Path.Combine(root, "BrownDust II_Data/Managed/Assembly-CSharp.dll"), "incomplete official update");
            var feed = new UpdateFeed { schemaVersion = 1, releases = new List<UpdateRelease> { mod }, launcher = new LauncherRelease { version = "9.0.0", url = "https://github.com/kacksdev/browndust2-ptbr/releases/download/v9.0.0/BD2PTBR.exe", bytes = 123, sha256 = new string('A', 64) } };
            using (var service = new UpdateService(new StubHandler(request => {
                Check(request.Headers.CacheControl?.NoCache == true && request.Headers.CacheControl.NoStore);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonStorage.Serialize(feed), Encoding.UTF8, "application/json") };
            }), Path.Combine(output, "app-broken-game-cache")))
            {
                var available = service.CheckAllAsync(root, CancellationToken.None).GetAwaiter().GetResult();
                Check(available.Mod == null && available.Launcher?.version == "9.0.0" && !string.IsNullOrEmpty(available.CompatibilityMessage));
            }
        });
        run("self_update_replaces_application_and_preserves_previous", () => {
            var folder = Path.Combine(output, "application with spaces"); Directory.CreateDirectory(folder);
            var source = Path.Combine(folder, "new.exe"); var target = Path.Combine(folder, "app.exe"); File.WriteAllText(source, "new-app"); File.WriteAllText(target, "old-app");
            LauncherSelfUpdate.ReplaceVerified(source, target, Hashing.Sha256(source), Hashing.Sha256(target), () => File.ReadAllText(target) == "new-app");
            Check(File.ReadAllText(target) == "new-app" && File.ReadAllText(target + ".previous") == "old-app");
        });
        run("self_update_failed_start_rolls_back_application", () => {
            var folder = Path.Combine(output, "app-rollback"); Directory.CreateDirectory(folder);
            var source = Path.Combine(folder, "new.exe"); var target = Path.Combine(folder, "app.exe"); File.WriteAllText(source, "broken-new-app"); File.WriteAllText(target, "working-old-app");
            Reject(() => LauncherSelfUpdate.ReplaceVerified(source, target, Hashing.Sha256(source), Hashing.Sha256(target), () => false));
            Check(File.ReadAllText(target) == "working-old-app" && Directory.GetFiles(folder, "*.failed-*").Length == 1);
        });
        run("self_update_rejects_corrupted_download_before_replacing", () => {
            var folder = Path.Combine(output, "app-corrupt"); Directory.CreateDirectory(folder);
            var source = Path.Combine(folder, "new.exe"); var target = Path.Combine(folder, "app.exe"); File.WriteAllText(source, "corrupt"); File.WriteAllText(target, "working-old-app");
            Reject(() => LauncherSelfUpdate.ReplaceVerified(source, target, new string('0', 64), Hashing.Sha256(target), () => true));
            Check(File.ReadAllText(target) == "working-old-app" && !File.Exists(target + ".previous"));
        });
        run("self_update_rejects_receipt_outside_owned_directory_without_writing", () => {
            var folder = Path.Combine(output, "untrusted-receipt"); Directory.CreateDirectory(folder);
            var receipt = Path.Combine(folder, "request.json"); File.WriteAllText(receipt, "{}");
            Reject(() => LauncherSelfUpdate.Apply(receipt));
            Check(!File.Exists(Path.Combine(folder, "failure.txt")));
        });
    }

    private static UpdateRelease Release(ClientIdentity identity) => new UpdateRelease {
        modVersion = "0.2.0", minimumLauncherVersion = "0.2.0", bytes = 123, sha256 = new string('A', 64),
        url = "https://github.com/kacksdev/browndust2-ptbr/releases/download/v0.2.0/BD2PTBR-payload.zip",
        integrationProfile = IntegrationCompatibility.Profile, compatibleClients = new List<ClientIdentity> { identity }
    };
    private static string IdentityHashes(string root) => string.Join("/", GameCompatibility.IdentityPaths.Select(p => Hashing.Sha256(Path.Combine(root, p))));
    private static void Check(bool ok) { if (!ok) throw new Exception("New-flow assertion failed."); }
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } catch (IOException) { return; } throw new Exception("Expected rejection."); }
    private static byte[] Native() { var bytes = new byte[128]; bytes[0] = 0x4d; bytes[1] = 0x5a; bytes[60] = 64; bytes[64] = 0x50; bytes[65] = 0x45; bytes[68] = 0x64; bytes[69] = 0x86; bytes[88] = 0x0b; bytes[89] = 0x02; return bytes; }
    private static string Fixture(string output, string name)
    {
        var root = Path.Combine(output, name); var managed = Path.Combine(root, "BrownDust II_Data/Managed"); Directory.CreateDirectory(managed);
        Directory.CreateDirectory(Path.Combine(root, "MonoBleedingEdge/EmbedRuntime")); File.WriteAllBytes(Path.Combine(root, "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll"), Native());
        File.WriteAllBytes(Path.Combine(root, "BrownDust II.exe"), Native()); File.WriteAllBytes(Path.Combine(root, "UnityPlayer.dll"), Native());
        File.WriteAllText(Path.Combine(root, "BrownDust II_Data/globalgamemanagers"), "synthetic 2.36.7\0"); File.WriteAllText(Path.Combine(root, "20261002000.tied.dat"), "old content");
        WriteGameAssembly(root, true, "OriginalUnrelatedSystem");
        using (var tmp = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Unity.TextMeshPro", new Version(1, 0)), "Unity.TextMeshPro", ModuleKind.Dll))
        {
            var module = tmp.MainModule;
            var text = new TypeDefinition("TMPro", "TMP_Text", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(text);
            var setter = Method(text, "set_text", false); setter.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            var font = new TypeDefinition("TMPro", "TMP_FontAsset", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(font);
            var character = new TypeDefinition("TMPro", "TMP_Character", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(character);
            var add = Method(font, "TryAddCharacterInternal", false); add.Parameters.Add(new ParameterDefinition(module.TypeSystem.UInt32)); add.Parameters.Add(new ParameterDefinition(new ByReferenceType(character)));
            tmp.Write(Path.Combine(managed, "Unity.TextMeshPro.dll"));
        }
        return root;
    }
    private static MethodDefinition Method(TypeDefinition type, string name, bool isStatic)
    {
        var method = new MethodDefinition(name, MethodAttributes.Public | (isStatic ? MethodAttributes.Static : 0), type.Module.TypeSystem.Void);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); type.Methods.Add(method); return method;
    }
    private static void WriteGameAssembly(string root, bool localization, string unrelatedName)
    {
        using (var game = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Assembly-CSharp", new Version(1, 0)), "Assembly-CSharp", ModuleKind.Dll))
        {
            var module = game.MainModule;
            var text = new TypeDefinition("Proto.Design.common", "NameTextTable", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(text);
            text.Properties.Add(new PropertyDefinition("Id", PropertyAttributes.None, module.TypeSystem.Int32)); text.Properties.Add(new PropertyDefinition("TextEn", PropertyAttributes.None, module.TypeSystem.String));
            var central = new TypeDefinition("", "RenamableCentral", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(central);
            if (localization) for (int i = 0; i < 5; i++) { var method = Method(central, "Text" + i, true); method.ReturnType = module.TypeSystem.String; method.Parameters.Add(new ParameterDefinition(text)); }
            var guide = new TypeDefinition("", "GuideBookUI", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(guide);
            var content = new TypeDefinition("", "GuideBookContentState", TypeAttributes.NestedPublic | TypeAttributes.Class, module.TypeSystem.Object); guide.NestedTypes.Add(content);
            var tutorial = new TypeDefinition("Proto.Design.common", "GuideTutorialTable", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object); module.Types.Add(tutorial);
            var list = new GenericInstanceType(module.ImportReference(typeof(List<>))); list.GenericArguments.Add(tutorial);
            Method(content, "DrawGuide", false).Parameters.Add(new ParameterDefinition(list));
            module.Types.Add(new TypeDefinition("", unrelatedName, TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
            game.Write(Path.Combine(root, "BrownDust II_Data/Managed/Assembly-CSharp.dll"));
        }
    }
}
