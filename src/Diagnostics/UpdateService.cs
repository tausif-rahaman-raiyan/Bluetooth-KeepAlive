using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace HocoKeepAlive.Diagnostics;

public sealed record UpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    bool IsUpdateAvailable,
    string ReleaseName,
    string ReleaseNotes,
    string PublishedAt,
    string? SetupDownloadUrl,
    string HtmlUrl
);

public static class UpdateService
{
    public static string CurrentVersion
    {
        get
        {
            try
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                if (ver != null && ver.Major > 0)
                {
                    return $"{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
            catch { }
            return "1.0.5";
        }
    }

    public const string GitHubRepo = "tausif-rahaman-raiyan/Bluetooth-KeepAlive";
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"KeepAlive-Updater/{CurrentVersion}");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
    }

    /// <summary>
    /// Checks GitHub Releases for the latest version and setup installer URL.
    /// </summary>
    public static async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        string current = CurrentVersion;
        string apiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateInfo(
                    current, current, false, "Keep Alive v" + current,
                    "Up to date. No new releases found on GitHub.",
                    DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    null, $"https://github.com/{GitHubRepo}/releases"
                );
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            string releaseName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : tagName;
            string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "No changelog provided.";
            string publishedAt = root.TryGetProperty("published_at", out var pubProp) ? pubProp.GetString() ?? "" : "";
            string htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? "" : $"https://github.com/{GitHubRepo}/releases";

            string cleanLatest = tagName.TrimStart('v', 'V');
            string cleanCurrent = current.TrimStart('v', 'V');

            bool updateAvailable = false;
            if (Version.TryParse(cleanLatest, out var vLatest) && Version.TryParse(cleanCurrent, out var vCurrent))
            {
                updateAvailable = vLatest > vCurrent;
            }
            else if (!string.IsNullOrEmpty(cleanLatest) && !cleanLatest.Equals(cleanCurrent, StringComparison.OrdinalIgnoreCase))
            {
                updateAvailable = true;
            }

            string? setupUrl = null;
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    string assetName = asset.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                    if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        (assetName.Contains("Setup", StringComparison.OrdinalIgnoreCase) || assetName.Contains("Keep-Alive", StringComparison.OrdinalIgnoreCase)))
                    {
                        setupUrl = asset.TryGetProperty("browser_download_url", out var bdu) ? bdu.GetString() : null;
                        if (!string.IsNullOrEmpty(setupUrl)) break;
                    }
                }
            }

            return new UpdateInfo(
                CurrentVersion: current,
                LatestVersion: cleanLatest.Length > 0 ? cleanLatest : current,
                IsUpdateAvailable: updateAvailable,
                ReleaseName: releaseName,
                ReleaseNotes: body,
                PublishedAt: publishedAt,
                SetupDownloadUrl: setupUrl,
                HtmlUrl: htmlUrl
            );
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Warning, "UpdateService", "Check update failed: " + ex.Message);
            return new UpdateInfo(
                current, current, false, "Keep Alive v" + current,
                $"Could not check updates: {ex.Message}",
                DateTime.UtcNow.ToString("yyyy-MM-dd"),
                null, $"https://github.com/{GitHubRepo}/releases"
            );
        }
    }

    /// <summary>
    /// Downloads the latest Setup EXE to temp directory, launches installer, and exits app to replace build.
    /// </summary>
    public static async Task<bool> DownloadAndApplyUpdateAsync(string setupUrl, Action<string>? progressCallback = null)
    {
        try
        {
            progressCallback?.Invoke("Downloading setup installer from GitHub...");
            string tempDir = Path.GetTempPath();
            string tempExe = Path.Combine(tempDir, $"Keep-Alive-Setup-Update-{Guid.NewGuid():N}.exe");

            using (var downloadStream = await _httpClient.GetStreamAsync(setupUrl))
            using (var fileStream = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await downloadStream.CopyToAsync(fileStream);
            }

            if (!File.Exists(tempExe) || new FileInfo(tempExe).Length < 100000)
            {
                throw new InvalidOperationException("Downloaded setup installer file is invalid or incomplete.");
            }

            progressCallback?.Invoke("Launching Setup Installer & closing application...");
            LogService.Log(LogSeverity.Info, "UpdateService", $"Launching setup installer: {tempExe}");

            Process.Start(new ProcessStartInfo
            {
                FileName = tempExe,
                UseShellExecute = true
            });

            // Gracefully close application so setup installer can replace executable
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                Environment.Exit(0);
            });

            return true;
        }
        catch (Exception ex)
        {
            LogService.Log(LogSeverity.Error, "UpdateService", "Failed to download/apply update.", ex.ToString());
            progressCallback?.Invoke($"Update failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Launches the update download or release page in default browser.
    /// </summary>
    public static void LaunchUpdate(string? url)
    {
        string target = !string.IsNullOrWhiteSpace(url) ? url : $"https://github.com/{GitHubRepo}/releases/latest";
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch { }
    }
}
