using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A macroscopic analysis cache: a manager of sorts for analysis caches.
    /// </summary>
    internal sealed class MacroAnalysisCache
    {
        /// <summary>
        /// Creates an empty macro analysis cache.
        /// </summary>
        public MacroAnalysisCache()
        {
            this.distinctCaches = new List<FlowGraphAnalysisCache>();
            this.cacheIndices = ImmutableDictionary.Create<Type, int>();
            this.cacheRefCounts = ImmutableDictionary.Create<int, int>();
        }

        private MacroAnalysisCache(
            List<FlowGraphAnalysisCache> distinctCaches,
            ImmutableDictionary<Type, int> cacheIndices,
            ImmutableDictionary<int, int> cacheRefCounts)
        {
            this.distinctCaches = distinctCaches;
            this.cacheIndices = cacheIndices;
            this.cacheRefCounts = cacheRefCounts;
        }

        /// <summary>
        /// A list of all distinct analysis caches.
        /// </summary>
        private List<FlowGraphAnalysisCache> distinctCaches;

        /// <summary>
        /// A mapping of types to the analysis caches that
        /// perform analyses for those types. The analysis
        /// caches are encoded as indices into the
        /// `distinctCaches` list.
        /// </summary>
        private ImmutableDictionary<Type, int> cacheIndices;

        /// <summary>
        /// A reference count for each flow graph analysis cache
        /// managed by this macro cache. The keys of this dictionary
        /// are indices into the `distinctCaches` list. The values
        /// are reference counts.
        /// </summary>
        private ImmutableDictionary<int, int> cacheRefCounts;

        /// <summary>
        /// Updates this macro analysis cache with a tweak to
        /// the graph. The update is not performed in place: instead,
        /// a derived cache is created.
        /// </summary>
        /// <param name="update">A tweak to the graph.</param>
        /// <returns>
        /// A macro analysis cache that incorporates the update.
        /// </returns>
        /// <remarks>This method is thread-safe.</remarks>
        public MacroAnalysisCache Update(FlowGraphUpdate update)
        {
            var newCaches = new List<FlowGraphAnalysisCache>();
            foreach (var cache in newCaches)
            {
                newCaches.Add(cache.Update(update));
            }
            return new MacroAnalysisCache(newCaches, cacheIndices, cacheRefCounts);
        }

        /// <summary>
        /// Creates a new analysis cache that incorporates a particular
        /// analysis.
        /// </summary>
        /// <param name="analysis">
        /// The analysis to include in the new analysis cache.
        /// </param>
        /// <typeparam name="T">
        /// The result type of the analysis.
        /// </typeparam>
        /// <returns>
        /// A new analysis cache.
        /// </returns>
        public MacroAnalysisCache WithAnalysis<T>(IFlowGraphAnalysis<T> analysis)
        {
            // Figure out which types the analysis is assignable to.
            var resultTypes = GetAssignableTypes(typeof(T));

            // Decrement reference counts for those types and maintain
            // a list of all cache indices with a reference count of zero.
            var cacheRefCountsBuilder = cacheRefCounts.ToBuilder();

            var danglingCaches = new List<int>();
            foreach (var type in resultTypes)
            {
                int cacheIndex;
                if (cacheIndices.TryGetValue(type, out cacheIndex))
                {
                    int refCount = cacheRefCountsBuilder[cacheIndex];
                    refCount--;
                    cacheRefCountsBuilder[cacheIndex] = refCount;
                    if (refCount == 0)
                    {
                        danglingCaches.Add(cacheIndex);
                    }
                }
            }

            // The next thing we want to do is insert the new analysis cache
            // and maybe do some cleanup if we really have to.
            int newCacheIndex;
            var newCaches = new List<FlowGraphAnalysisCache>(distinctCaches);
            var cache = new FlowGraphAnalysisCache<T>(analysis);

            var cacheIndicesBuilder = cacheIndices.ToBuilder();

            int danglingCacheCount = danglingCaches.Count;
            if (danglingCacheCount > 0)
            {
                // If at least one cache has become a dangling cache, then
                // we can replace it with the new analysis' cache.
                newCacheIndex = danglingCaches[danglingCacheCount - 1];
                newCaches[newCacheIndex] = cache;
                danglingCaches.RemoveAt(danglingCacheCount - 1);
                danglingCacheCount--;

                // If we have more than one dangling cache, then we'll have to
                // delete all but one of them (one gets overwritten) and rewrite
                // all indices into the cache list.
                //
                // That's pretty expensive, but it should also be very rare.
                if (danglingCacheCount > 0)
                {
                    // Efficiently delete all dangling caches from the list
                    // of caches and also construct a mapping of old indices
                    // to new indices.
                    var indexRemapping = new int[newCaches.Count];
                    var newCacheList = new List<FlowGraphAnalysisCache>();
                    var danglingCacheSet = new HashSet<int>(danglingCaches);
                    int newIndex = 0;
                    for (int i = 0; i < newCaches.Count; i++)
                    {
                        indexRemapping[i] = newIndex;
                        if (!danglingCacheSet.Contains(i))
                        {
                            newIndex++;
                            newCacheList.Add(newCaches[i]);
                        }
                    }
                    newCaches = newCacheList;
                    newCacheIndex = indexRemapping [newCacheIndex];

                    // Rewrite indices into the cache list.
                    foreach (var pair in cacheIndices)
                    {
                        cacheIndicesBuilder[pair.Key] = indexRemapping[pair.Value];
                    }

                    // Rewrite reference counts.
                    var newRefCountsBuilder = ImmutableDictionary.CreateBuilder<int, int>();
                    foreach (var pair in cacheRefCountsBuilder)
                    {
                        if (!danglingCacheSet.Contains(pair.Key))
                        {
                            newRefCountsBuilder[indexRemapping[pair.Key]] = pair.Value;
                        }
                    }
                    cacheRefCountsBuilder = newRefCountsBuilder;
                }
            }
            else
            {
                // Otherwise, just add the new cache to the list.
                newCacheIndex = newCaches.Count;
                newCaches.Add(cache);
            }

            // Update the type-to-cache-index dictionary and increment
            // the new cache's reference count.
            foreach (var type in resultTypes)
            {
                cacheIndicesBuilder[type] = newCacheIndex;
            }
            cacheRefCountsBuilder[newCacheIndex] = resultTypes.Count;

            return new MacroAnalysisCache(
                newCaches,
                cacheIndicesBuilder.ToImmutable(),
                cacheRefCountsBuilder.ToImmutable());
        }

        /// <summary>
        /// Tries to get an analysis result of a particular type.
        /// </summary>
        /// <param name="graph">
        /// The current flow graph.
        /// </param>
        /// <param name="result">
        /// The analysis result, if one can be fetched or computed.
        /// </param>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if there is an analyzer to compute the result;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetResultAs<T>(FlowGraph graph, out T result)
        {
            var t = typeof(T);
            int cacheIndex;
            if (cacheIndices.TryGetValue(t, out cacheIndex))
            {
                result = distinctCaches[cacheIndex].GetResultAs<T>(graph);
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Gets an analysis result of a particular type. Throws
        /// an exception if there is no analyzer to compute the result.
        /// </summary>
        /// <param name="graph">
        /// The current flow graph.
        /// </param>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>An analysis result.</returns>
        public T GetResultAs<T>(FlowGraph graph)
        {
            T result;
            if (TryGetResultAs(graph, out result))
            {
                return result;
            }
            else
            {
                throw new NotSupportedException(
                    "No analysis was registered to produce results of type '" +
                    typeof(T).FullName + "'.");
            }
        }

        /// <summary>
        /// Tells if this macro analysis cache has an analysis
        /// that produces a particular type of result.
        /// </summary>
        /// <typeparam name="T">
        /// The type of analysis result that is sought.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if a registered analysis produces a result of type <c>T</c>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool HasAnalysisFor<T>()
        {
            return cacheIndices.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Gets the set of all types to which a particular type is assignable.
        /// </summary>
        /// <param name="rootType">The root type to start at.</param>
        /// <returns>A set of types.</returns>
        private static HashSet<Type> GetAssignableTypes(Type rootType)
        {
            // Construct the set of all types inherited from and implemented by
            // the root type using a worklist-driven algorithm.
            var resultTypes = new HashSet<Type>();
            var typeWorklist = new Queue<Type>();
            typeWorklist.Enqueue(rootType);
            while (typeWorklist.Count > 0)
            {
                var type = typeWorklist.Dequeue();
                if (resultTypes.Add(type))
                {
                    if (type.BaseType != null)
                    {
                        typeWorklist.Enqueue(type.BaseType);
                    }
                    foreach (var item in type.GetInterfaces())
                    {
                        typeWorklist.Enqueue(item);
                    }
                }
            }
            return resultTypes;
        }
    }
}
