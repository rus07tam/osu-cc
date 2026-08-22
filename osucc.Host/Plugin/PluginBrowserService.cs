using osucc.Common.GitHub;
using System.Text.Json;
using System.Xml.Linq;

namespace osucc.Plugin;

public sealed class PluginBrowserService : IDisposable
{
    public static PluginBrowserService? Instance { get; private set; }

    private readonly HttpClient http;
    private readonly Dictionary<string, List<RemotePluginInfo>> repoCache = new(StringComparer.OrdinalIgnoreCase);

    public PluginBrowserService()
    {
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("osucc");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        Instance = this;
    }

    public async Task<List<RemotePluginInfo>> GetPluginsAsync(int page = 1, int perPage = 20, CancellationToken ct = default)
    {
        string searchUrl = $"https://api.github.com/search/repositories?q=topic:osucc-plugin&per_page={perPage}&page={page}";
        using var searchResp = await http.GetAsync(searchUrl, ct).ConfigureAwait(false);
        searchResp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await searchResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var items = doc.RootElement.GetProperty("items");

        var result = new List<RemotePluginInfo>();

        foreach (var item in items.EnumerateArray())
        {
            string repoFullName = item.GetProperty("full_name").GetString() ?? string.Empty;
            int stars = item.TryGetProperty("stargazers_count", out var s) ? s.GetInt32() : 0;

            var plugins = await GetPluginsFromRepoAsync(repoFullName, stars, ct).ConfigureAwait(false);
            result.AddRange(plugins);
        }

        return result;
    }

    public async Task<List<RemotePluginInfo>> GetPluginsFromRepoAsync(string repoFullName, int stars = 0, CancellationToken ct = default)
    {
        if (repoCache.TryGetValue(repoFullName, out var cached))
            return cached;

        var plugins = new List<RemotePluginInfo>();

        string treeUrl = $"https://api.github.com/repos/{repoFullName}/git/trees/HEAD?recursive=1";
        using var treeResp = await http.GetAsync(treeUrl, ct).ConfigureAwait(false);
        if (treeResp.StatusCode == System.Net.HttpStatusCode.Forbidden) treeResp.EnsureSuccessStatusCode();
        if (!treeResp.IsSuccessStatusCode) return plugins;

        using var treeDoc = JsonDocument.Parse(await treeResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var tree = treeDoc.RootElement.GetProperty("tree");

        var csprojPaths = tree.EnumerateArray()
            .Where(node => node.GetProperty("type").GetString() == "blob"
                        && node.GetProperty("path").GetString()?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true)
            .Select(node => node.GetProperty("path").GetString()!)
            .ToList();

        foreach (string csprojPath in csprojPaths)
        {
            var info = await parsePluginFromCsprojAsync(repoFullName, csprojPath, stars, ct).ConfigureAwait(false);
            if (info != null)
                plugins.Add(info);
        }

        repoCache[repoFullName] = plugins;
        return plugins;
    }

    private static readonly char[] authorSeparators = { ',', ';' };
    private static readonly char[] tagSeparators = { ',', ';', ' ' };

    private async Task<RemotePluginInfo?> parsePluginFromCsprojAsync(string repoFullName, string csprojPath, int stars, CancellationToken ct)
    {
        string contentUrl = $"https://api.github.com/repos/{repoFullName}/contents/{csprojPath}";
        using var resp = await http.GetAsync(contentUrl, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        using var jsonDoc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        string? base64 = jsonDoc.RootElement.TryGetProperty("content", out var c) ? c.GetString() : null;
        if (base64 == null) return null;

        string xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64.Replace("\n", string.Empty)));

        try
        {
            var root = XElement.Parse(xml);

            string? isPlugin = root.Descendants("IsPlugin").FirstOrDefault()?.Value;
            if (!string.Equals(isPlugin, "true", StringComparison.OrdinalIgnoreCase))
                return null;

            string? id = root.Descendants("PackageId").FirstOrDefault()?.Value;
            string? name = root.Descendants("Title").FirstOrDefault()?.Value
                        ?? root.Descendants("Product").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                return null;

            string? description = root.Descendants("Description").FirstOrDefault()?.Value;
            string? version = root.Descendants("Version").FirstOrDefault()?.Value ?? "1.0.0";
            string? repositoryUrl = root.Descendants("RepositoryUrl").FirstOrDefault()?.Value
                                 ?? $"https://github.com/{repoFullName}";
            string? iconGlyph = root.Descendants("IconGlyph").FirstOrDefault()?.Value;

            var authors = root.Descendants("Author")
                .Select(a => new PluginAuthor(a.Value))
                .ToList();

            if (authors.Count == 0)
            {
                string? authorsStr = root.Descendants("Authors").FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(authorsStr))
                    authors = authorsStr.Split(authorSeparators, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => new PluginAuthor(s.Trim()))
                        .ToList();
            }

            var tags = root.Descendants("Tag")
                .Select(t => t.Value)
                .Concat(root.Descendants("PackageTags")
                    .SelectMany(t => t.Value.Split(tagSeparators, StringSplitOptions.RemoveEmptyEntries)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var documents = new List<PluginDocument>();
            foreach (var docItem in root.Descendants("PluginDocument").Concat(root.Descendants("Document")))
            {
                string path = docItem.Attribute("Include")?.Value ?? docItem.Value;
                if (!string.IsNullOrEmpty(path))
                {
                    string title = docItem.Attribute("Title")?.Value ?? Path.GetFileNameWithoutExtension(path);
                    string? icon = docItem.Attribute("IconGlyph")?.Value ?? docItem.Attribute("Icon")?.Value;
                    documents.Add(new PluginDocument { Path = path, Title = title, IconGlyph = icon });
                }
            }

            return new RemotePluginInfo
            {
                Id = id,
                Name = name,
                Description = description,
                Version = version,
                Icon = iconGlyph,
                Repository = repositoryUrl,
                RepoFullName = repoFullName,
                Stars = stars,
                Authors = authors,
                Tags = tags,
                Documents = documents,
            };
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        http.Dispose();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }
}
