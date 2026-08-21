using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using osu.Game;
using osucc.Client;
using osucc.Common;
using osucc.Localisation;

namespace osucc.Plugin
{
    /// <summary>
    /// Validates dependencies (host, inter-plugin, bundled) and resolves a valid load order.
    /// </summary>
    internal static class PluginDependencyResolver
    {
        private static string? cachedOsuCcVersion;
        private static string? cachedOsuGameVersion;

        public static string ResolveOsuCcVersion()
        {
            if (cachedOsuCcVersion != null)
                return cachedOsuCcVersion;

            try
            {
                var asm = typeof(IOsuCcPluginHost).Assembly;
                string? info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info))
                    return cachedOsuCcVersion = normalizeVersion(info);

                var ver = asm.GetName().Version;
                if (ver != null)
                    return cachedOsuCcVersion = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            }
            catch
            {
            }

            return cachedOsuCcVersion = "2.2.0";
        }

        public static string ResolveOsuGameVersion()
        {
            if (cachedOsuGameVersion != null)
                return cachedOsuGameVersion;

            try
            {
                var asm = typeof(OsuGameBase).Assembly;
                string? info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info))
                    return cachedOsuGameVersion = normalizeVersion(info);

                var ver = asm.GetName().Version;
                if (ver != null)
                    return cachedOsuGameVersion = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            }
            catch
            {
            }

            return cachedOsuGameVersion = "2024.1115.0";
        }

        private static string normalizeVersion(string v)
        {
            string s = v.Trim();
            if (s.StartsWith('v') || s.StartsWith('V'))
                s = s[1..].Trim();
            int plusIndex = s.IndexOf('+');
            if (plusIndex >= 0)
                s = s[..plusIndex].Trim();

            return s;
        }

        /// <summary>
        /// Validates dependencies for all candidates and determines the load order.
        /// </summary>
        public static DependencyResolution Resolve(IReadOnlyList<PluginCandidate> candidates)
        {
            string osuccVersion = ResolveOsuCcVersion();
            string osuGameVersion = ResolveOsuGameVersion();

            bool bypassHost = ClientConfig.BypassHostDependencyCheck.Value;
            bool bypassPlugins = ClientConfig.BypassPluginDependencyCheck.Value;

            var byId = new Dictionary<string, PluginCandidate>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();

            foreach (var candidate in candidates)
                byId.TryAdd(candidate.Metadata.Id, candidate);

            // 1. Validate Host, Plugin, and Bundled dependencies for each candidate
            foreach (var candidate in candidates)
            {
                var declarations = candidate.DependencyDeclarations;

                foreach (var decl in declarations)
                {
                    switch (decl.Kind)
                    {
                        case PluginDependencyKind.Host:
                            validateHostDependency(candidate, decl, osuccVersion, osuGameVersion, bypassHost, warnings);
                            break;

                        case PluginDependencyKind.Plugin:
                            validatePluginDependency(candidate, decl, byId, bypassPlugins, warnings);
                            break;

                        case PluginDependencyKind.Bundled:
                            validateBundledDependency(candidate, decl, warnings);
                            break;
                    }
                }
            }

            // 2. Topological sort loadable candidates
            var loadable = candidates.Where(c => c.IsLoadable).ToList();
            var others = candidates.Where(c => !c.IsLoadable).ToList();

            var dependents = new Dictionary<string, List<PluginCandidate>>(StringComparer.OrdinalIgnoreCase);
            var unresolved = new Dictionary<PluginCandidate, int>();

            foreach (var candidate in loadable)
            {
                int count = 0;
                var pluginDeps = candidate.DependencyDeclarations
                    .Where(d => d.Kind == PluginDependencyKind.Plugin)
                    .Select(d => d.Id)
                    .Concat(candidate.Dependencies)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (string depId in pluginDeps)
                {
                    if (string.Equals(depId, candidate.Metadata.Id, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (byId.TryGetValue(depId, out var depCandidate) && depCandidate.IsLoadable)
                    {
                        count++;
                        if (!dependents.TryGetValue(depCandidate.Metadata.Id, out var list))
                            dependents[depCandidate.Metadata.Id] = list = new List<PluginCandidate>();

                        list.Add(candidate);
                    }
                }

                unresolved[candidate] = count;
            }

            var ready = loadable.Where(c => unresolved[c] == 0).OrderBy(c => c, PriorityComparer.Instance).ToList();
            var order = new List<PluginCandidate>(loadable.Count);
            var placed = new HashSet<PluginCandidate>();

            while (ready.Count > 0)
            {
                var next = ready[0];
                ready.RemoveAt(0);

                order.Add(next);
                placed.Add(next);

                if (!dependents.TryGetValue(next.Metadata.Id, out var dependentsOfNext))
                    continue;

                foreach (var dependent in dependentsOfNext)
                {
                    if (--unresolved[dependent] != 0)
                        continue;

                    insertSorted(ready, dependent);
                }
            }

            if (order.Count < loadable.Count)
            {
                var cycle = loadable.Where(c => !placed.Contains(c)).OrderBy(c => c, PriorityComparer.Instance).ToList();
                warnings.Add($"PluginDependencyResolver: dependency cycle detected among: {string.Join(", ", cycle.Select(c => $"'{c.Metadata.Name}'"))}; loading in priority order");
                order.AddRange(cycle);
            }

            order.AddRange(others.OrderBy(c => c, PriorityComparer.Instance));

            return new DependencyResolution(order, warnings);
        }

        private static void validateHostDependency(
            PluginCandidate candidate,
            PluginDependencyDeclaration decl,
            string osuccVersion,
            string osuGameVersion,
            bool bypassHost,
            List<string> warnings)
        {
            bool isOsuCc = decl.Id.StartsWith("osucc", StringComparison.OrdinalIgnoreCase);
            string currentVersion = isOsuCc ? osuccVersion : osuGameVersion;
            string componentName = isOsuCc ? "osu!cc" : "osu!lazer";

            if (!PluginSemanticVersion.IsSatisfied(currentVersion, decl.MinVersion, decl.MaxVersion, out _))
            {
                bool isOutdated = decl.MinVersion != null && PluginSemanticVersion.TryParse(currentVersion, out var curr) && PluginSemanticVersion.TryParse(decl.MinVersion, out var min) && curr! < min!;

                var msg = isOsuCc
                    ? (isOutdated ? PluginsOverlayStrings.DependencyOsuCcOutdated(decl.MinVersion!, currentVersion) : PluginsOverlayStrings.DependencyOsuCcTooNew(decl.MaxVersion!, currentVersion))
                    : (isOutdated ? PluginsOverlayStrings.DependencyOsuGameOutdated(decl.MinVersion!, currentVersion) : PluginsOverlayStrings.DependencyOsuGameTooNew(decl.MaxVersion!, currentVersion));

                if (bypassHost)
                {
                    candidate.Diagnostics.Add(PluginDiagnostic.Warning(msg, details: $"Host compatibility check bypassed via experiments", source: PluginDiagnosticSource.Dependency, target: decl.Id));
                    warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' host requirement mismatch ({msg}), bypassed by experiment");
                }
                else
                {
                    candidate.IsBlocked = true;
                    candidate.Diagnostics.Add(PluginDiagnostic.Error(msg, details: $"Requires compatible {componentName} version", source: PluginDiagnosticSource.Dependency, target: decl.Id));
                    warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' blocked: {msg}");
                }
            }
        }

        private static void validatePluginDependency(
            PluginCandidate candidate,
            PluginDependencyDeclaration decl,
            Dictionary<string, PluginCandidate> byId,
            bool bypassPlugins,
            List<string> warnings)
        {
            if (string.Equals(decl.Id, candidate.Metadata.Id, StringComparison.OrdinalIgnoreCase))
            {
                candidate.Diagnostics.Add(PluginDiagnostic.Warning($"Plugin depends on itself", source: PluginDiagnosticSource.Dependency, target: decl.Id));
                return;
            }

            if (!byId.TryGetValue(decl.Id, out var depCandidate))
            {
                if (!bypassPlugins)
                {
                    string reqVer = decl.MinVersion != null ? $">= {decl.MinVersion}" : (decl.MaxVersion != null ? $"<= {decl.MaxVersion}" : "");
                    candidate.Diagnostics.Add(PluginDiagnostic.Warning(PluginsOverlayStrings.DependencyPluginMissingWithVersion(decl.Id, reqVer), source: PluginDiagnosticSource.Dependency, target: decl.Id));
                    warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' depends on missing plugin '{decl.Id}'");
                }
                return;
            }

            string installedVer = depCandidate.Metadata.Version;

            if (!PluginSemanticVersion.IsSatisfied(installedVer, decl.MinVersion, decl.MaxVersion, out _))
            {
                if (!bypassPlugins)
                {
                    string reqVer = decl.MinVersion != null && decl.MaxVersion != null
                        ? $"{decl.MinVersion} - {decl.MaxVersion}"
                        : (decl.MinVersion != null ? $">= {decl.MinVersion}" : $"<= {decl.MaxVersion}");

                    candidate.Diagnostics.Add(PluginDiagnostic.Warning(PluginsOverlayStrings.DependencyPluginVersionMismatch(depCandidate.Metadata.Name, installedVer, reqVer), source: PluginDiagnosticSource.Dependency, target: decl.Id));
                    warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' depends on '{decl.Id}' version {installedVer} which does not meet {reqVer}");
                }
                return;
            }

            if (!PluginStateStore.IsEnabled(depCandidate.Metadata.Id))
            {
                candidate.Diagnostics.Add(PluginDiagnostic.Notice(PluginsOverlayStrings.DependencyPluginDisabledNotice(depCandidate.Metadata.Name), source: PluginDiagnosticSource.Dependency, target: decl.Id));
            }
        }

        private static void validateBundledDependency(
            PluginCandidate candidate,
            PluginDependencyDeclaration decl,
            List<string> warnings)
        {
            string dllName = decl.Id.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? decl.Id : $"{decl.Id}.dll";
            string fullPath = Path.Combine(candidate.Directory, dllName);

            if (!File.Exists(fullPath))
            {
                string resPath = Path.Combine(candidate.Directory, "res", dllName);
                if (File.Exists(resPath))
                    fullPath = resPath;
            }

            if (!File.Exists(fullPath))
            {
                candidate.Diagnostics.Add(PluginDiagnostic.Error(PluginsOverlayStrings.DependencyBundledMissing(dllName), source: PluginDiagnosticSource.Bundle, target: dllName));
                warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' bundled file '{dllName}' is missing");
                return;
            }

            if (decl.MinVersion != null || decl.MaxVersion != null)
            {
                string? fileVer = OsuCcVersionReader.Read(fullPath);
                if (fileVer != null && !PluginSemanticVersion.IsSatisfied(fileVer, decl.MinVersion, decl.MaxVersion, out _))
                {
                    string reqVer = decl.MinVersion != null ? $">= {decl.MinVersion}" : $"<= {decl.MaxVersion}";
                    candidate.Diagnostics.Add(PluginDiagnostic.Error(PluginsOverlayStrings.DependencyBundledVersionMismatch(dllName, fileVer, reqVer), source: PluginDiagnosticSource.Bundle, target: dllName));
                    warnings.Add($"PluginDependencyResolver: '{candidate.Metadata.Name}' bundled file '{dllName}' version {fileVer} mismatch (expected {reqVer})");
                }
            }
        }

        private static void insertSorted(List<PluginCandidate> list, PluginCandidate candidate)
        {
            int index = list.BinarySearch(candidate, PriorityComparer.Instance);
            if (index < 0)
                index = ~index;

            list.Insert(index, candidate);
        }

        private sealed class PriorityComparer : IComparer<PluginCandidate>
        {
            public static readonly PriorityComparer Instance = new();

            public int Compare(PluginCandidate? x, PluginCandidate? y)
            {
                if (ReferenceEquals(x, y))
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;

                int byPriority = x.EffectivePriority.CompareTo(y.EffectivePriority);
                return byPriority != 0 ? byPriority : string.CompareOrdinal(x.Metadata.Name, y.Metadata.Name);
            }
        }
    }

    internal sealed class DependencyResolution
    {
        public IReadOnlyList<PluginCandidate> Order { get; }
        public IReadOnlyList<string> Warnings { get; }

        public DependencyResolution(IReadOnlyList<PluginCandidate> order, IReadOnlyList<string> warnings)
        {
            Order = order;
            Warnings = warnings;
        }
    }
}
