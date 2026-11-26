using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BD2PTBR.Installer
{
    [DataContract]
    internal sealed class UpdateFeed
    {
        [DataMember(Name = "schemaVersion")] public int schemaVersion { get; set; }
        [DataMember(Name = "releases")] public List<UpdateRelease> releases { get; set; }
        [DataMember(Name = "launcher", EmitDefaultValue = false)] public LauncherRelease launcher { get; set; }
    }

    [DataContract]
    internal sealed class UpdateRelease
    {
        [DataMember(Name = "modVersion")] public string modVersion { get; set; }
        [DataMember(Name = "minimumLauncherVersion")] public string minimumLauncherVersion { get; set; }
        [DataMember(Name = "url")] public string url { get; set; }
        [DataMember(Name = "bytes")] public long bytes { get; set; }
        [DataMember(Name = "sha256")] public string sha256 { get; set; }
        [DataMember(Name = "compatibleClients")] public List<ClientIdentity> compatibleClients { get; set; }
        [DataMember(Name = "integrationProfile", EmitDefaultValue = false)] public string integrationProfile { get; set; }
        [DataMember(Name = "testedClientVersion", EmitDefaultValue = false)] public string testedClientVersion { get; set; }
    }

    [DataContract]
    internal sealed class LauncherRelease
    {
        [DataMember(Name = "version")] public string version { get; set; }
        [DataMember(Name = "url")] public string url { get; set; }
        [DataMember(Name = "bytes")] public long bytes { get; set; }
        [DataMember(Name = "sha256")] public string sha256 { get; set; }
    }

    internal sealed class UpdateAvailability
    {
        public UpdateRelease Mod { get; set; }
        public LauncherRelease Launcher { get; set; }
        public string CompatibilityMessage { get; set; }
    }

    internal sealed class UpdateService : IDisposable
    {
        public const string FeedUrl = "https://raw.githubusercontent.com/kacksdev/browndust2-ptbr/main/updates/stable.json";
        private const long MaximumPackageBytes = 512L * 1024 * 1024;
        private static readonly HttpClient Client = CreateClient();
        private readonly HttpClient _client;
        private readonly string _cacheRootOverride;
        private readonly bool _ownsClient;
        public UpdateService() { _client = Client; }
        internal UpdateService(HttpMessageHandler handler, string isolatedCache)
        {
            _client = new HttpClient(handler); _cacheRootOverride = isolatedCache; _ownsClient = true;
        }
        public void Dispose() { if (_ownsClient) _client.Dispose(); }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BD2PTBR-Launcher/" + InstallerEngine.InstallerVersion);
            return client;
        }

        public async Task<UpdateRelease> CheckAsync(string gameRoot, CancellationToken cancellation)
        {
            return (await CheckAllAsync(gameRoot, cancellation)).Mod;
        }

        public async Task<UpdateAvailability> CheckAllAsync(string gameRoot, CancellationToken cancellation)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
            deadline.CancelAfter(TimeSpan.FromSeconds(45));
            cancellation = deadline.Token;
            using (var response = await GetAsync(new Uri(FeedUrl), false, cancellation))
            {
                if (response.Content.Headers.ContentLength > 1024 * 1024) throw new InvalidDataException("Canal de atualização excede o limite de tamanho.");
                var bytes = await ReadBoundedAsync(response, 1024 * 1024, cancellation);
                var feed = JsonStorage.ReadText<UpdateFeed>(System.Text.Encoding.UTF8.GetString(bytes));
                ValidateFeed(feed);
                var available = new UpdateAvailability();
                if (feed.launcher != null)
                {
                    ValidateLauncher(feed.launcher);
                    if (Version.Parse(feed.launcher.version) > Version.Parse(InstallerEngine.InstallerVersion)) available.Launcher = feed.launcher;
                }
                if (!string.IsNullOrEmpty(gameRoot))
                {
                    string reason;
                    available.Mod = SelectRelease(feed, gameRoot, out reason);
                    available.CompatibilityMessage = reason;
                }
                return available;
            }
            }
        }

        internal static UpdateRelease SelectRelease(UpdateFeed feed, string gameRoot)
        {
            string reason;
            return SelectRelease(feed, gameRoot, out reason);
        }

        private static void ValidateFeed(UpdateFeed feed)
        {
            if (feed == null || feed.schemaVersion != 1 || feed.releases == null || feed.releases.Count > 200)
                throw new InvalidDataException("Canal de atualização inválido.");
            foreach (var release in feed.releases) ValidateRelease(release);
        }

        private static UpdateRelease SelectRelease(UpdateFeed feed, string gameRoot, out string reason)
        {
            ValidateFeed(feed);
            reason = null;
            foreach (var release in feed.releases.OrderByDescending(r => Version.Parse(r.modVersion)))
            {
                if (Version.Parse(release.minimumLauncherVersion) > Version.Parse(InstallerEngine.InstallerVersion))
                {
                    if (reason == null) reason = "Atualize o aplicativo para instalar este catálogo.";
                    continue;
                }
                try { GameCompatibility.EnsureCatalogCompatible(gameRoot, release.compatibleClients, release.integrationProfile); return release; }
                catch (Exception error) when (error is InvalidDataException || error is IOException || error is BadImageFormatException || error is UnauthorizedAccessException)
                { if (reason == null) reason = error.Message; }
            }
            return null;
        }

        internal static void ValidateLauncher(LauncherRelease release)
        {
            if (release == null) throw new InvalidDataException("Metadados do aplicativo ausentes.");
            ValidateRelease(new UpdateRelease { modVersion = release.version, minimumLauncherVersion = "0.2.0", url = release.url, bytes = release.bytes, sha256 = release.sha256 });
            if (!new Uri(release.url).AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A atualização do aplicativo deve ser um executável oficial.");
        }

        internal static void ValidateRelease(UpdateRelease release)
        {
            Version version;
            Version minimum;
            if (release == null || !Version.TryParse(release.modVersion, out version) || !Version.TryParse(release.minimumLauncherVersion, out minimum) ||
                release.bytes < 1 || release.bytes > MaximumPackageBytes || string.IsNullOrEmpty(release.sha256) ||
                !System.Text.RegularExpressions.Regex.IsMatch(release.sha256, "\\A[0-9a-fA-F]{64}\\z"))
                throw new InvalidDataException("Metadados de atualização inválidos.");
            Uri uri;
            if (!Uri.TryCreate(release.url, UriKind.Absolute, out uri) || uri.Scheme != "https" || !uri.IsDefaultPort ||
                uri.Host != "github.com" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
                !uri.AbsolutePath.StartsWith("/kacksdev/browndust2-ptbr/releases/download/", StringComparison.Ordinal))
                throw new InvalidDataException("O download não pertence às Releases oficiais do projeto.");
        }

        public async Task<InstallerEngineOptions> DownloadAsync(UpdateRelease release, string gameRoot, IProgress<InstallerProgress> progress, CancellationToken cancellation)
        {
            ValidateRelease(release);
            GameCompatibility.EnsureCatalogCompatible(gameRoot, release.compatibleClients, release.integrationProfile);
            var destination = await DownloadFileAsync(release.url, release.bytes, release.sha256, ".zip", "Baixando mod " + release.modVersion, progress, cancellation);
            return new InstallerEngineOptions { PayloadPath = destination, ExpectedPayloadSha256 = release.sha256, ExpectedModVersion = release.modVersion };
        }

        public Task<string> DownloadLauncherAsync(LauncherRelease release, IProgress<InstallerProgress> progress, CancellationToken cancellation)
        {
            ValidateLauncher(release);
            return DownloadFileAsync(release.url, release.bytes, release.sha256, ".exe", "Baixando aplicativo " + release.version, progress, cancellation);
        }

        private async Task<string> DownloadFileAsync(string url, long bytes, string sha256, string extension, string label, IProgress<InstallerProgress> progress, CancellationToken cancellation)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                deadline.CancelAfter(TimeSpan.FromMinutes(5));
                cancellation = deadline.Token;
            var cacheRoot = _cacheRootOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kacks", "BD2PTBR", "downloads");
            Directory.CreateDirectory(cacheRoot);
            var destination = PathSafety.CombineUnder(cacheRoot, sha256.ToUpperInvariant() + extension);
            if (!File.Exists(destination) || new FileInfo(destination).Length != bytes || !Hashing.EqualsSha256(destination, sha256))
            {
                var temporary = destination + ".partial-" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var response = await GetAsync(new Uri(url), true, cancellation))
                    {
                        if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength != bytes)
                            throw new InvalidDataException("Tamanho do download divergente.");
                        using (var input = await response.Content.ReadAsStreamAsync())
                        using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
                        {
                            long total = 0;
                            var buffer = new byte[65536];
                            int count;
                            while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation)) > 0)
                            {
                                total += count;
                                if (total > bytes) throw new InvalidDataException("Download maior que o anunciado.");
                                await output.WriteAsync(buffer, 0, count, cancellation);
                                progress?.Report(new InstallerProgress { Percent = (int)(100 * total / bytes), Phase = "DOWNLOAD", Message = label + ": " + total / 1024 + " KiB" });
                            }
                            if (total != bytes) throw new EndOfStreamException("Download incompleto.");
                            output.Flush(true);
                        }
                    }
                    if (!Hashing.EqualsSha256(temporary, sha256)) throw new InvalidDataException("SHA-256 do download divergente.");
                    if (File.Exists(destination)) File.Replace(temporary, destination, null);
                    else File.Move(temporary, destination);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            return destination;
            }
        }

        private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, int limit, CancellationToken cancellation)
        {
            using (var input = await response.Content.ReadAsStreamAsync())
            using (var output = new MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation)) > 0)
                {
                    if (output.Length + count > limit) throw new InvalidDataException("Resposta HTTP excede o limite.");
                    output.Write(buffer, 0, count);
                }
                return output.ToArray();
            }
        }

        private async Task<HttpResponseMessage> GetAsync(Uri uri, bool asset, CancellationToken cancellation)
        {
            {
                for (int redirects = 0; redirects <= 5; redirects++)
                {
                    HttpResponseMessage response;
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        if (!asset) request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true, NoStore = true };
                        response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
                    }
                    int status = (int)response.StatusCode;
                    if (status >= 300 && status <= 399)
                    {
                        var location = response.Headers.Location;
                        response.Dispose();
                        if (location == null) throw new HttpRequestException("Redirecionamento sem destino.");
                        var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                        bool hostAllowed = asset && (next.Host == "github.com" || next.Host == "release-assets.githubusercontent.com" || next.Host == "objects.githubusercontent.com");
                        if (!hostAllowed || next.Scheme != "https" || !next.IsDefaultPort || !string.IsNullOrEmpty(next.UserInfo))
                            throw new HttpRequestException("Redirecionamento de download não autorizado.");
                        uri = next;
                        continue;
                    }
                    if (!response.IsSuccessStatusCode) { response.Dispose(); throw new HttpRequestException("Canal indisponível (HTTP " + status + "). Nenhum arquivo do jogo foi alterado."); }
                    // Response owns its content stream; caller disposes it after consuming.
                    return response;
                }
                throw new HttpRequestException("Limite de redirecionamentos excedido.");
            }
        }
    }
}
