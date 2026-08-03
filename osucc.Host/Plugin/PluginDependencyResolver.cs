namespace osucc.Plugin
{
    /// <summary>
    /// Turns the discovered plugin candidates into a load order that satisfies declared
    /// dependencies (see <see cref="OsuCcPluginAttribute.DependsOn"/>). A dependency always
    /// loads before its dependents; when no dependency forces an order, the existing priority
    /// order (persisted override wins over the attribute) is preserved exactly, so the priority
    /// system keeps working. Unmet dependencies are soft — the dependent still loads (its
    /// <c>GetApi&lt;T&gt;</c> returns <c>null</c>) and only a warning is produced.
    /// </summary>
    internal static class PluginDependencyResolver
    {
        /// <summary>
        /// Resolves the load order for <paramref name="candidates"/>. Loadable candidates come
        /// first, topologically sorted by their dependencies (priority as the tie-breaker, so a
        /// dependency is only moved earlier when a declaration forces it); the rest (disabled or
        /// built against an unsupported API version) follow in priority order.
        /// </summary>
        public static DependencyResolution Resolve(IReadOnlyList<PluginCandidate> candidates)
        {
            var byId = new Dictionary<string, PluginCandidate>(StringComparer.Ordinal);
            var loadable = new List<PluginCandidate>();
            var others = new List<PluginCandidate>();

            foreach (var candidate in candidates)
            {
                byId.TryAdd(candidate.Attribute.Id, candidate);

                if (candidate.IsLoadable)
                    loadable.Add(candidate);
                else
                    others.Add(candidate);
            }

            var warnings = new List<string>();
            var dependents = new Dictionary<string, List<PluginCandidate>>(StringComparer.Ordinal);
            var unresolved = new Dictionary<PluginCandidate, int>();

            foreach (var candidate in loadable)
            {
                int count = 0;

                foreach (string depId in candidate.Dependencies.Distinct(StringComparer.Ordinal))
                {
                    if (depId == candidate.Attribute.Id)
                    {
                        warnings.Add($"PluginDependencyResolver: '{candidate.Attribute.Name}' depends on itself; dependency ignored");
                        continue;
                    }

                    if (!byId.TryGetValue(depId, out PluginCandidate? dependency))
                    {
                        warnings.Add($"PluginDependencyResolver: '{candidate.Attribute.Name}' depends on missing plugin '{depId}'; it will load without it");
                        continue;
                    }

                    if (!dependency.IsLoadable)
                    {
                        warnings.Add($"PluginDependencyResolver: '{candidate.Attribute.Name}' depends on '{depId}', which is disabled or unavailable; it will load without it");
                        continue;
                    }

                    count++;

                    if (!dependents.TryGetValue(depId, out var list))
                        dependents[depId] = list = new List<PluginCandidate>();

                    list.Add(candidate);
                }

                unresolved[candidate] = count;
            }

            // Kahn's algorithm over the loadable candidates. The ready set stays sorted by
            // priority, so the produced order is the priority order unless a dependency forces
            // a dependent to wait for its dependency.
            var ready = loadable.Where(c => unresolved[c] == 0).OrderBy(c => c, PriorityComparer.Instance).ToList();
            var order = new List<PluginCandidate>(loadable.Count);
            var placed = new HashSet<PluginCandidate>();

            while (ready.Count > 0)
            {
                var next = ready[0];
                ready.RemoveAt(0);

                order.Add(next);
                placed.Add(next);

                if (!dependents.TryGetValue(next.Attribute.Id, out var dependentsOfNext))
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
                warnings.Add($"PluginDependencyResolver: dependency cycle detected among: {string.Join(", ", cycle.Select(c => $"'{c.Attribute.Name}'"))}; the affected plugins load in priority order");
                order.AddRange(cycle);
            }

            order.AddRange(others.OrderBy(c => c, PriorityComparer.Instance));

            return new DependencyResolution(order, warnings);
        }

        /// <summary>Inserts a candidate into a list kept sorted by <see cref="PriorityComparer"/>.</summary>
        private static void insertSorted(List<PluginCandidate> list, PluginCandidate candidate)
        {
            int index = list.BinarySearch(candidate, PriorityComparer.Instance);

            if (index < 0)
                index = ~index;

            list.Insert(index, candidate);
        }

        /// <summary>Orders by load/attach priority (persisted override wins), name as the tie-breaker — matches the manager's own sort.</summary>
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
                return byPriority != 0 ? byPriority : string.CompareOrdinal(x.Attribute.Name, y.Attribute.Name);
            }
        }
    }

    /// <summary>Result of <see cref="PluginDependencyResolver.Resolve"/>: a total load order plus soft warnings.</summary>
    internal sealed class DependencyResolution
    {
        /// <summary>All discovered candidates in load order (dependencies first; the rest follow by priority).</summary>
        public IReadOnlyList<PluginCandidate> Order { get; }

        /// <summary>Soft warnings about self/missing/disabled dependencies and cycles.</summary>
        public IReadOnlyList<string> Warnings { get; }

        public DependencyResolution(IReadOnlyList<PluginCandidate> order, IReadOnlyList<string> warnings)
        {
            Order = order;
            Warnings = warnings;
        }
    }
}
