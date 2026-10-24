using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BD2PTBR.Installer;

internal static class Program
{
    static string Root;
    static int Passed;
    static readonly InstallerEngine Engine = new InstallerEngine();
    static string Plugin = "BepInEx/plugins/BD2PTBR/BD2PTBR.dll";

    static int Main(string[] args)
    {
        try
        {
            Root = Path.GetFullPath(args[0]);
            if (Directory.Exists(Root)) throw new IOException("Test output must be new.");
            Directory.CreateDirectory(Root);
            Run("starter_registered_location_not_hardcoded", () => {
                foreach(var path in new[]{@"C:\ProgramData\Neowiz\BrownDust2Starter\Browndust2Starter.exe",@"D:\Jogos Oficiais\Starter\Browndust2Starter.exe"}) {
                    var info=OfficialStarter.CreateStartInfo("\""+path+"\" \"%1\"");
                    Assert(info.FileName==path && info.Arguments==OfficialStarter.GameUri && info.UseShellExecute);
                    Assert(info.WorkingDirectory==Path.GetDirectoryName(path));
                }
            });
            Run("starter_invalid_registration_rejected", () => {
                foreach(var command in new[]{null,"",@"cmd.exe /c anything","\"C:\\bad\\cmd.exe\" \"%1\"","\"relative\\Browndust2Starter.exe\" \"%1\"","\"\\\\server\\share\\Browndust2Starter.exe\" \"%1\"","\"C:\\Starter\\Browndust2Starter.exe\" \"%1\" --extra"})
                    Assert(Reject(()=>OfficialStarter.CreateStartInfo(command)) is InvalidOperationException);
            });
            Run("starter_missing_uri_rejected", () => {
                Assert(Reject(()=>OfficialStarter.CreateStartInfo("\"C:\\Starter\\Browndust2Starter.exe\"")) is InvalidOperationException);
                Assert(Reject(()=>OfficialStarter.CreateStartInfo("\"C:\\Starter\\Browndust2Starter.exe\" \"%1")) is InvalidOperationException);
            });
            Run("starter_open_game_blocks_update_entry", () => {
                Assert(Reject(()=>OfficialStarter.EnsureGameClosed(new[]{"other","BrownDust II"})) is InvalidOperationException);
                OfficialStarter.EnsureGameClosed(new[]{"other","Browndust2Starter"});
            });
            Run("legacy_v096_layers_migrate_and_restore", () => {
                var g=Game("l96-round"); var options=Package(g,"0.2.0"); SeedLegacyV096(g);
                var foreign=Path.Combine(g,"BepInEx/plugins/Other/mod.dll"); Directory.CreateDirectory(Path.GetDirectoryName(foreign)); File.WriteAllText(foreign,"foreign");
                var unknown=Path.Combine(g,"BepInEx/plugins/BD2PTBR/translations/zz-custom.jsonl"); File.WriteAllText(unknown,"custom-layer");
                Assert(Engine.Inspect(g,options).Health==InstallationHealth.LegacyInstallation);
                Engine.Install(g,null,options); Engine.Verify(g,null,options);
                foreach(var name in LegacyV096Layers.Keys) Assert(!File.Exists(LegacyLayerPath(g,name)));
                Assert(File.ReadAllText(unknown)=="custom-layer" && File.ReadAllText(foreign)=="foreign");
                var point=JsonStorage.Read<TransactionJournal>(Directory.GetFiles(options.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single());
                foreach(var layer in LegacyV096Layers) {
                    var record=point.files.Single(f=>f.path=="BepInEx/plugins/BD2PTBR/translations/"+layer.Key);
                    Assert(record.existed && record.backupSha256.Equals(layer.Value,StringComparison.OrdinalIgnoreCase));
                    Assert(Hashing.EqualsSha256(PathSafety.CombineUnder(point.backupRoot,record.backupRelativePath),layer.Value));
                }
                Engine.Restore(g,null,options); AssertLegacyV096(g);
                Assert(File.ReadAllText(Path.Combine(g,Plugin))=="legacy-plugin");
                Assert(!Directory.GetFiles(options.StateRootOverride,"install-state.json",SearchOption.AllDirectories).Any());
                Assert(Engine.Inspect(g,options).Health==InstallationHealth.LegacyInstallation);
                Assert(File.ReadAllText(unknown)=="custom-layer" && File.ReadAllText(foreign)=="foreign");
                Engine.Restore(g,null,options); Engine.Verify(g,null,options);
                foreach(var name in LegacyV096Layers.Keys) Assert(!File.Exists(LegacyLayerPath(g,name)));
            });
            Run("legacy_v096_changed_layer_preserved", () => {
                var g=Game("l96-changed"); var options=Package(g,"0.2.0"); SeedLegacyV096(g);
                var changed=LegacyV096Layers.Keys.First(); File.WriteAllText(LegacyLayerPath(g,changed),"user-edited-layer");
                var report=Engine.Install(g,null,options);
                Assert(File.ReadAllText(LegacyLayerPath(g,changed))=="user-edited-layer");
                foreach(var name in LegacyV096Layers.Keys.Where(n=>n!=changed)) Assert(!File.Exists(LegacyLayerPath(g,name)));
                Assert(report.Warnings.Any(w=>w.Contains(changed)));
                Engine.Restore(g,null,options);
                Assert(File.ReadAllText(LegacyLayerPath(g,changed))=="user-edited-layer");
                foreach(var layer in LegacyV096Layers.Where(l=>l.Key!=changed)) Assert(Hashing.EqualsSha256(LegacyLayerPath(g,layer.Key),layer.Value));
            });
            Run("legacy_v096_cleanup_failure_rolls_back", () => {
                var g=Game("l96-fault"); var options=Package(g,"0.2.0"); SeedLegacyV096(g); options.FaultAfterFileOperations=1;
                var error=Reject(()=>Engine.Install(g,null,options));
                Assert(error is InstallerOperationException && ((InstallerOperationException)error).Report.RolledBack);
                AssertLegacyV096(g); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="legacy-plugin");
                Assert(!Directory.GetFiles(options.StateRootOverride,"install-state.json",SearchOption.AllDirectories).Any());
            });
            Run("legacy_v096_layer_in_new_payload_retained", () => {
                var g=Game("l96-incoming"); var included=LegacyV096Layers.Keys.First();
                var options=Package(g,"0.2.0","BepInEx/plugins/BD2PTBR/translations/"+included); SeedLegacyV096(g);
                Engine.Install(g,null,options); Engine.Verify(g,null,options);
                Assert(File.ReadAllText(LegacyLayerPath(g,included))=="not-allowed");
                foreach(var name in LegacyV096Layers.Keys.Where(n=>n!=included)) Assert(!File.Exists(LegacyLayerPath(g,name)));
                Engine.Restore(g,null,options); AssertLegacyV096(g);
            });
            Run("install_verify_update_restore", () => {
                var g = Game("roundtrip"); var v1 = Package(g, "0.1.2"); var v2 = Package(g, "0.1.3");
                Engine.Install(g, null, v1); Engine.Verify(g, null, v1);
                Assert(File.ReadAllText(Path.Combine(g, Plugin)) == "plugin-0.1.2");
                Engine.Install(g, null, v2); Assert(File.ReadAllText(Path.Combine(g, Plugin)) == "plugin-0.1.3");
                Engine.Restore(g, null, v1); Assert(File.ReadAllText(Path.Combine(g, Plugin)) == "plugin-0.1.2");
                Engine.Verify(g, null, v1);
                Engine.Restore(g, null, v1); Assert(File.ReadAllText(Path.Combine(g, Plugin)) == "plugin-0.1.3");
                Engine.Uninstall(g, null, v1); Assert(!File.Exists(Path.Combine(g, Plugin)));
            });
            Run("first_install_restore_clean", () => {
                var g = Game("clean"); var o = Package(g, "0.1.2"); Engine.Install(g, null, o); Engine.Restore(g, null, o);
                Assert(!File.Exists(Path.Combine(g, Plugin))); Assert(!File.Exists(Path.Combine(g, "winhttp.dll")));
            });
            Run("compatibility_version", () => RejectChangedGame("version", "BrownDust II_Data/globalgamemanagers", "2.99.0"));
            Run("compatibility_assembly", () => RejectChangedGame("assembly", "BrownDust II_Data/Managed/Assembly-CSharp.dll", "other"));
            Run("compatibility_executable", () => RejectChangedGame("executable", "BrownDust II.exe", "other"));
            Run("compatibility_package", () => { var g = Game("package"); var o = Package(g, "0.1.2"); File.Move(Path.Combine(g, "20261002000.tied.dat"), Path.Combine(g, "20261101000.tied.dat")); Reject(() => Engine.Install(g, null, o)); Assert(!File.Exists(Path.Combine(g, Plugin))); });
            Run("wrong_download_hash", () => { var g = Game("hash"); var o = Package(g, "0.1.2"); o.ExpectedPayloadSha256 = new string('0',64); Reject(() => Engine.Install(g, null, o)); Assert(!File.Exists(Path.Combine(g, Plugin))); });
            Run("wrong_release_version", () => { var g = Game("release-version"); var o = Package(g, "0.1.2"); o.ExpectedModVersion = "0.1.3"; Reject(() => Engine.Install(g, null, o)); });
            Run("rollback_failed_clean_install", () => { var g = Game("fault-clean"); var o = Package(g,"0.1.2"); o.FaultAfterFileOperations = 1; var e = Reject(() => Engine.Install(g,null,o)); Assert(e is InstallerOperationException && ((InstallerOperationException)e).Report.RolledBack); Assert(!File.Exists(Path.Combine(g,Plugin))); });
            Run("rollback_failed_upgrade", () => { var g = Game("fault-upgrade"); var a = Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,a); b.FaultAfterFileOperations=2; Reject(() => Engine.Install(g,null,b)); Engine.Verify(g,null,a); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="plugin-0.1.2"); });
            Run("rollback_failed_restore", () => { var g=Game("fault-restore"); var a=Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,a); Engine.Install(g,null,b); a.FaultAfterFileOperations=1; Reject(()=>Engine.Restore(g,null,a)); Engine.Verify(g,null,b); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="plugin-0.1.3"); });
            Run("repair_preserves_good_restore", () => { var g=Game("repair"); var a=Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,a); Engine.Install(g,null,b); File.WriteAllText(Path.Combine(g,Plugin),"corrupt"); Reject(()=>Engine.Verify(g,null,b)); Engine.Install(g,null,b); Engine.Restore(g,null,a); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="plugin-0.1.2"); });
            Run("restore_rejects_changed_client", () => { var g=Game("restore-client"); var a=Package(g,"0.1.2"); Engine.Install(g,null,a); File.AppendAllText(Path.Combine(g,"BrownDust II_Data/Managed/Assembly-CSharp.dll"),"changed"); Reject(()=>Engine.Restore(g,null,a)); Assert(File.Exists(Path.Combine(g,Plugin))); });
            Run("restore_rejects_corrupt_backup", () => { var g=Game("restore-backup"); var a=Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,a); Engine.Install(g,null,b); var point=Directory.GetFiles(a.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single(); var journal=JsonStorage.Read<TransactionJournal>(point); File.WriteAllText(PathSafety.CombineUnder(journal.backupRoot,journal.files.First(f=>f.existed).backupRelativePath),"broken"); Reject(()=>Engine.Restore(g,null,a)); Engine.Verify(g,null,b); });
            Run("downgrade_rejected", () => { var g=Game("downgrade"); var a=Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,b); Reject(()=>Engine.Install(g,null,a)); Engine.Verify(g,null,b); });
            Run("foreign_mod_preserved", () => { var g=Game("foreign"); var a=Package(g,"0.1.2"); var foreign=Path.Combine(g,"BepInEx/plugins/Other/mod.dll"); Directory.CreateDirectory(Path.GetDirectoryName(foreign)); File.WriteAllText(foreign,"foreign"); Engine.Install(g,null,a); Engine.Uninstall(g,null,a); Assert(File.ReadAllText(foreign)=="foreign"); });
            Run("undeclared_zip_entry_rejected", () => { var g=Game("extra"); var a=Package(g,"0.1.2"); using(var zip=ZipFile.Open(a.PayloadPath,ZipArchiveMode.Update)) { using(var w=new StreamWriter(zip.CreateEntry("extra.txt").Open()))w.Write("bad"); } a.ExpectedPayloadSha256=Hashing.Sha256(a.PayloadPath); Reject(()=>Engine.Install(g,null,a)); Assert(!File.Exists(Path.Combine(g,Plugin))); });
            Run("duplicate_case_zip_entry_rejected", () => { var g=Game("duplicate"); var a=Package(g,"0.1.2"); using(var zip=ZipFile.Open(a.PayloadPath,ZipArchiveMode.Update)) { using(var w=new StreamWriter(zip.CreateEntry("WINHTTP.DLL").Open()))w.Write("bad"); } a.ExpectedPayloadSha256=Hashing.Sha256(a.PayloadPath); Reject(()=>Engine.Install(g,null,a)); });
            Run("zip_traversal_rejected", () => { var g=Game("traversal"); var a=Package(g,"0.1.2","../outside.txt"); Reject(()=>Engine.Install(g,null,a)); Assert(!File.Exists(Path.Combine(Root,"outside.txt"))); });
            Run("official_file_mutation_rejected", () => { var g=Game("official-file"); var original=Hashing.Sha256(Path.Combine(g,"BrownDust II.exe")); var a=Package(g,"0.1.2","BrownDust II.exe"); Reject(()=>Engine.Install(g,null,a)); Assert(Hashing.Sha256(Path.Combine(g,"BrownDust II.exe"))==original); });
            Run("concurrent_operation_rejected", () => { var g=Game("concurrent"); var a=Package(g,"0.1.2"); Engine.Install(g,null,a); var point=Directory.GetFiles(a.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single(); using(OperationLock.Acquire(Path.GetDirectoryName(point))) Reject(()=>Engine.Install(g,null,a)); });
            Run("feed_highest_compatible", () => { var g=Game("feed"); var a=Release(g,"0.1.2"); var b=Release(g,"0.1.3"); Assert(UpdateService.SelectRelease(new UpdateFeed{schemaVersion=1,releases=new List<UpdateRelease>{a,b}},g).modVersion=="0.1.3"); b.compatibleClients[0].package="other"; Assert(UpdateService.SelectRelease(new UpdateFeed{schemaVersion=1,releases=new List<UpdateRelease>{a,b}},g).modVersion=="0.1.2"); });
            Run("feed_empty_not_installable", () => { var g=Game("empty-feed"); Assert(UpdateService.SelectRelease(new UpdateFeed{schemaVersion=1,releases=new List<UpdateRelease>()},g)==null); });
            Run("feed_untrusted_url_rejected", () => { var g=Game("url"); var a=Release(g,"0.1.2"); a.url="https://example.org/file.zip"; Reject(()=>UpdateService.SelectRelease(new UpdateFeed{schemaVersion=1,releases=new List<UpdateRelease>{a}},g)); });
            Run("feed_invalid_digest_rejected", () => { var g=Game("digest"); var a=Release(g,"0.1.2"); a.sha256="xyz"; Reject(()=>UpdateService.ValidateRelease(a)); });
            Run("feed_size_limit", () => { var g=Game("size"); var a=Release(g,"0.1.2"); a.bytes=1024L*1024*1024; Reject(()=>UpdateService.ValidateRelease(a)); });
            Run("download_apply_end_to_end", () => {
                var g=Game("network-good"); var options=Package(g,"0.1.2"); var body=File.ReadAllBytes(options.PayloadPath); var release=Release(g,"0.1.2"); release.bytes=body.Length; release.sha256=Hashing.Sha256(options.PayloadPath);
                using(var service=new UpdateService(new StubHandler(_=>new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(body)}),Path.Combine(Root,"cache-good"))) {
                    var downloaded=service.DownloadAsync(release,g,null,CancellationToken.None).GetAwaiter().GetResult(); downloaded.StateRootOverride=options.StateRootOverride; downloaded.SkipProcessCheck=true; Engine.Install(g,null,downloaded); Engine.Verify(g,null,downloaded);
                }
            });
            Run("download_corrupt_rejected", () => {
                var g=Game("network-corrupt"); var release=Release(g,"0.1.2"); release.bytes=3;
                using(var service=new UpdateService(new StubHandler(_=>new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(new byte[]{1,2,3})}),Path.Combine(Root,"cache-corrupt")))
                    Reject(()=>service.DownloadAsync(release,g,null,CancellationToken.None).GetAwaiter().GetResult());
                Assert(!Directory.GetFiles(Path.Combine(Root,"cache-corrupt")).Any()); Assert(!File.Exists(Path.Combine(g,Plugin)));
            });
            Run("download_interrupted_rejected", () => {
                var g=Game("network-short"); var release=Release(g,"0.1.2"); release.bytes=9;
                using(var service=new UpdateService(new StubHandler(_=>new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(new byte[]{1,2,3})}),Path.Combine(Root,"cache-short")))
                    Reject(()=>service.DownloadAsync(release,g,null,CancellationToken.None).GetAwaiter().GetResult());
                Assert(!Directory.GetFiles(Path.Combine(Root,"cache-short")).Any());
            });
            Run("download_untrusted_redirect", () => {
                var g=Game("network-redirect"); var release=Release(g,"0.1.2");
                using(var service=new UpdateService(new StubHandler(_=>{ var r=new HttpResponseMessage(HttpStatusCode.Redirect);r.Headers.Location=new Uri("https://example.org/payload.zip");return r;}),Path.Combine(Root,"cache-redirect")))
                    Reject(()=>service.DownloadAsync(release,g,null,CancellationToken.None).GetAwaiter().GetResult());
            });
            Run("feed_http_error_preserves_game", () => {
                var g=Game("network-404");
                using(var service=new UpdateService(new StubHandler(_=>new HttpResponseMessage(HttpStatusCode.NotFound)),Path.Combine(Root,"cache-404")))
                    Reject(()=>service.CheckAsync(g,CancellationToken.None).GetAwaiter().GetResult());
                Assert(!File.Exists(Path.Combine(g,Plugin)));
            });
            Run("feed_uses_fixed_official_url", () => {
                var g=Game("network-feed"); var feed=new UpdateFeed{schemaVersion=1,releases=new List<UpdateRelease>{Release(g,"0.1.2")}};
                using(var service=new UpdateService(new StubHandler(request=>{Assert(request.RequestUri.AbsoluteUri==UpdateService.FeedUrl);return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(JsonStorage.Serialize(feed))};}),Path.Combine(Root,"cache-feed")))
                    Assert(service.CheckAsync(g,CancellationToken.None).GetAwaiter().GetResult().modVersion=="0.1.2");
            });
            Run("download_cancellation_cleanup", () => {
                var g=Game("cancel"); var source=new CancellationTokenSource();source.Cancel();
                using(var service=new UpdateService(new StubHandler(_=>new HttpResponseMessage(HttpStatusCode.OK)),Path.Combine(Root,"cache-cancel")))
                    Reject(()=>service.DownloadAsync(Release(g,"0.1.2"),g,null,source.Token).GetAwaiter().GetResult());
                Assert(!File.Exists(Path.Combine(g,Plugin)));
            });
            Run("interrupted_transaction_recovered_on_inspect", () => {
                var g=Game("interrupted"); var a=Package(g,"0.1.2"); Engine.Install(g,null,a);
                var pointPath=Directory.GetFiles(a.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single(); var instance=Path.GetDirectoryName(pointPath);
                var prepare=typeof(InstallerEngine).GetMethod("PrepareTransaction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                prepare.Invoke(Engine,new object[]{instance,g,Path.Combine(instance,"install-state.json"),"install",new[]{Plugin},null});
                File.WriteAllText(Path.Combine(g,Plugin),"partial-write-before-process-exit");
                var inspection=new InstallerEngine().Inspect(g,a); Assert(inspection.Health==InstallationHealth.InstalledAndVerified);
                Assert(File.ReadAllText(Path.Combine(g,Plugin))=="plugin-0.1.2");
            });
            Run("committed_transaction_not_rolled_back", () => {
                var g=Game("committed"); var a=Package(g,"0.1.2"); var b=Package(g,"0.1.3"); Engine.Install(g,null,a); Engine.Install(g,null,b);
                var pointPath=Directory.GetFiles(a.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single();
                File.Copy(pointPath,Path.Combine(Path.GetDirectoryName(pointPath),"pending-transaction.json"));
                Assert(new InstallerEngine().Inspect(g,a).Health==InstallationHealth.InstalledAndVerified); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="plugin-0.1.3");
            });
            Run("windows_path_aliases_rejected", () => {
                foreach(var path in new[]{"BepInEx/core/test.dll.","BepInEx/core/test.dll ","BepInEx/core/CON.dll","BepInEx/core/invalid?.dll"}) Reject(()=>PathSafety.NormalizeRelative(path));
            });
            Run("incomplete_mod_payload_rejected", () => {
                var g=Game("incomplete"); var a=Package(g,"0.1.2",null,true); Reject(()=>Engine.Install(g,null,a)); Assert(!File.Exists(Path.Combine(g,Plugin)));
            });
            Run("legacy_corrupt_plugin_detectable", () => {
                var g=Game("legacy-corrupt"); var options=Package(g,"0.1.2"); Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(g,Plugin))); File.WriteAllText(Path.Combine(g,Plugin),"corrupted-legacy-dll");
                Assert(Engine.Inspect(g,options).Health==InstallationHealth.LegacyInstallation);
            });
            Run("recovery_blocks_changed_client", () => {
                var g=Game("interrupted-client-change"); var a=Package(g,"0.1.2"); Engine.Install(g,null,a);
                var pointPath=Directory.GetFiles(a.StateRootOverride,"restore-point.json",SearchOption.AllDirectories).Single(); var instance=Path.GetDirectoryName(pointPath);
                var prepare=typeof(InstallerEngine).GetMethod("PrepareTransaction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                prepare.Invoke(Engine,new object[]{instance,g,Path.Combine(instance,"install-state.json"),"install",new[]{Plugin},null});
                File.WriteAllText(Path.Combine(g,Plugin),"partial-write"); File.AppendAllText(Path.Combine(g,"BrownDust II_Data/Managed/Assembly-CSharp.dll"),"changed-client");
                Reject(()=>Engine.Inspect(g,a)); Assert(File.ReadAllText(Path.Combine(g,Plugin))=="partial-write"); Assert(File.Exists(Path.Combine(instance,"pending-transaction.json")));
            });
            File.WriteAllText(Path.Combine(Root,"test-summary.json"),JsonStorage.Serialize(new TestSummary { result="PASS", scenarios=Passed, gameModified=false, realClientTested=false }),new UTF8Encoding(false));
            Console.WriteLine("PASS " + Passed + " scenarios. " + Root); return 0;
        }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    static void Run(string name, Action test) { test(); Passed++; Console.WriteLine("PASS " + name); }
    static readonly Dictionary<string,string> LegacyV096Layers=new Dictionary<string,string> {
        {"zz-safety-overrides.jsonl","863f42ad5fd1c1ff6b8d5903c78e7ffbd646ebfeaf195432a0e851ec61d698f1"},
        {"zzz-reviewed-overrides.jsonl","822e3edb0243b9d56241b360a1fdab419f9343e269e6cb2fb8d13a4400de4246"},
        {"zzzz-v090-contextual-overrides.jsonl","c1b8a165d784aa7524c122d6de1a9420e3c870012f9ab6beb6f6372af4ac552f"},
        {"zzzzz-v096-editorial-overrides.jsonl","02624266b4e4e2252f24b7996fe3a571cdf52ea5dd871ae4e0627f488b29dc45"}
    };
    static string LegacyLayerPath(string game,string name)=>Path.Combine(game,"BepInEx/plugins/BD2PTBR/translations",name);
    static void SeedLegacyV096(string game) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(game,Plugin))); File.WriteAllText(Path.Combine(game,Plugin),"legacy-plugin");
        using(var archive=ZipFile.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Fixtures/legacy-v096.zip"))) {
            foreach(var layer in LegacyV096Layers) {
                var entry=archive.GetEntry(layer.Key);
                if(entry==null) throw new InvalidDataException("Pinned legacy fixture missing: "+layer.Key);
                var destination=LegacyLayerPath(game,layer.Key); Directory.CreateDirectory(Path.GetDirectoryName(destination));
                using(var source=entry.Open()) using(var output=File.Create(destination)) source.CopyTo(output);
                Assert(Hashing.EqualsSha256(destination,layer.Value));
            }
        }
    }
    static void AssertLegacyV096(string game) { foreach(var layer in LegacyV096Layers) Assert(Hashing.EqualsSha256(LegacyLayerPath(game,layer.Key),layer.Value)); }
    static void Assert(bool value) { if(!value)throw new Exception("Assertion failed"); }
    static Exception Reject(Action action) { try { action(); } catch(Exception e) { return e; } throw new Exception("Expected rejection"); }
    static string Game(string name) {
        var g=Path.Combine(Root,name,"game"); Directory.CreateDirectory(Path.Combine(g,"BrownDust II_Data/Managed"));
        File.WriteAllText(Path.Combine(g,"BrownDust II.exe"),"fake-client"); File.WriteAllText(Path.Combine(g,"BrownDust II_Data/Managed/Assembly-CSharp.dll"),"fake-assembly");
        File.WriteAllText(Path.Combine(g,"UnityPlayer.dll"),"fake-unity");
        File.WriteAllText(Path.Combine(g,"BrownDust II_Data/globalgamemanagers"),"synthetic 2.36.7\0"); File.WriteAllText(Path.Combine(g,"20261002000.tied.dat"),"fake-package"); return g;
    }
    static InstallerEngineOptions Package(string game, string version, string extra=null, bool incomplete=false) {
        var path=Path.Combine(Path.GetDirectoryName(game),"payload-"+version+"-"+Guid.NewGuid().ToString("N")+".zip");
        var content=new Dictionary<string,string>{{Plugin,"plugin-"+version},{"winhttp.dll","synthetic-loader"},{"BepInEx/plugins/BD2PTBR/translations/complete.jsonl","synthetic-catalog-"+version}};
        if (!incomplete) foreach(var required in new[]{"doorstop_config.ini","BepInEx/core/BepInEx.dll","BepInEx/core/BepInEx.Preloader.dll","BepInEx/core/0Harmony.dll","BepInEx/plugins/BD2PTBR/translations/hardcoded.jsonl"}) content[required]="synthetic-"+required;
        if(extra!=null)content.Add(extra,"not-allowed");
        var manifest=new PayloadManifest{schemaVersion=3,modVersion=version,testedClientVersion="2.36.7",testedGamePackage="20261002000",compatibleClients=new List<ClientIdentity>{GameCompatibility.Capture(game)},files=content.Select(p=>new PayloadFile{path=p.Key,bytes=Encoding.UTF8.GetByteCount(p.Value),sha256=Hashing.Sha256Text(p.Value)}).ToList()};
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)) {
            using(var w=new StreamWriter(zip.CreateEntry("payload-manifest.json").Open(),new UTF8Encoding(false)))w.Write(JsonStorage.Serialize(manifest));
            foreach(var item in content)using(var w=new StreamWriter(zip.CreateEntry(item.Key).Open(),new UTF8Encoding(false)))w.Write(item.Value);
        }
        return new InstallerEngineOptions{PayloadPath=path,ExpectedPayloadSha256=Hashing.Sha256(path),ExpectedModVersion=version,StateRootOverride=Path.Combine(Path.GetDirectoryName(game),"state"),SkipProcessCheck=true};
    }
    static UpdateRelease Release(string g,string version)=>new UpdateRelease{modVersion=version,minimumLauncherVersion="0.2.0",url="https://github.com/kacksdev/browndust2-ptbr/releases/download/v"+version+"/BD2PTBR-payload.zip",bytes=123,sha256=new string('A',64),compatibleClients=new List<ClientIdentity>{GameCompatibility.Capture(g)}};
    static void RejectChangedGame(string name,string file,string data) {var g=Game(name);var a=Package(g,"0.1.2"); File.WriteAllText(Path.Combine(g,file),data); Reject(()=>Engine.Install(g,null,a));Assert(!File.Exists(Path.Combine(g,Plugin)));}
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage,HttpResponseMessage> _response;
    public StubHandler(Func<HttpRequestMessage,HttpResponseMessage> response) { _response=response; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_response(request));
    }
}

public sealed class TestSummary
{
    public string result { get; set; }
    public int scenarios { get; set; }
    public bool gameModified { get; set; }
    public bool realClientTested { get; set; }
}
