using System;
using System.IO;
using System.Reflection;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace FruitDefense.Editor
{
    public static class RuntimeUiPerformanceSmoke
    {
        private const string ReleaseThemePath =
            "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset";
        private const int ContextIterations = 64;
        private const int LookupPasses = 25000;
        private delegate RuntimeUiArtBinding BindingResolver(
            RuntimeUiDrawContext context, RuntimeUiArtSlot slot);

        public static void Run()
        {
            var theme = AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(ReleaseThemePath);
            Assert(theme != null && theme.ActiveArtSet != null,
                "release theme and art set are available for performance validation");

            ValidateBindingCacheContract(theme);
            ValidateNoDrawingPathLinearScan();

            for (var index = 0; index < 4; index++)
                RuntimeUiDrawContext.Create(theme, 1f);
            Measure("context-create-after", ContextIterations, () =>
                RuntimeUiDrawContext.Create(theme, 1f).Styles.HitTarget.fontSize);

            var slots = RuntimeUiArtSlots.Required;
            var checksum = 0;
            for (var pass = 0; pass < 100; pass++)
            {
                for (var index = 0; index < slots.Count; index++)
                    checksum ^= theme.ActiveArtSet.GetRequiredBinding(slots[index])
                        .Texture.GetInstanceID();
            }

            Measure("art-set-linear-required-binding-control",
                LookupPasses * slots.Count, () =>
                {
                    var value = checksum;
                    for (var pass = 0; pass < LookupPasses; pass++)
                    {
                        for (var index = 0; index < slots.Count; index++)
                        {
                            value ^= theme.ActiveArtSet.GetRequiredBinding(slots[index])
                                .Texture.GetInstanceID();
                        }
                    }
                    return value;
                });

            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var resolver = CreateBindingResolver();
            Measure("draw-context-cached-required-binding-after",
                LookupPasses * slots.Count, () =>
                {
                    var value = checksum;
                    for (var pass = 0; pass < LookupPasses; pass++)
                    {
                        for (var index = 0; index < slots.Count; index++)
                            value ^= resolver(context, slots[index]).Texture.GetInstanceID();
                    }
                    return value;
                });

            Debug.Log("RUNTIME_UI_PERFORMANCE_SMOKE_OK slots=" + slots.Count
                + " lookup-passes=" + LookupPasses);
        }

        private static void ValidateBindingCacheContract(RuntimeUiTheme releaseTheme)
        {
            var resolver = CreateBindingResolver();
            var cacheField = typeof(RuntimeUiDrawContext).GetField("bindingCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(cacheField != null, "draw context owns one private binding cache");

            var context = RuntimeUiDrawContext.Create(releaseTheme, 1f);
            var cache = cacheField.GetValue(context) as RuntimeUiArtBinding[];
            Assert(cache != null && cache.Length == RuntimeUiArtSlots.RequiredCount
                && cache.Length == 53,
                "draw context cache contains exactly the finite 53-slot contract");
            for (var index = 0; index < RuntimeUiArtSlots.Required.Count; index++)
            {
                var slot = RuntimeUiArtSlots.Required[index];
                Assert(ReferenceEquals(resolver(context, slot),
                        releaseTheme.ActiveArtSet.GetRequiredBinding(slot)),
                    "cache preserves the one semantic binding for "
                    + RuntimeUiArtSlots.SemanticId(slot));
            }
            Assert(ReferenceEquals(RuntimeUiDrawContext.Require(context, releaseTheme, 1f), context),
                "context reuse key remains stable for identical theme/art-set identity and scale");

            var identitySet = UnityEngine.Object.Instantiate(releaseTheme.ActiveArtSet);
            var identityTheme = CreateThemeClone(releaseTheme, identitySet);
            SetString(identitySet, "setId", "perf-identity");
            var identityContext = RuntimeUiDrawContext.Create(identityTheme, 1f);
            Assert(identityContext.CacheKey != context.CacheKey
                && !context.IsCurrent(identityTheme, 1f),
                "art-set identity participates in context reuse");

            var revisionSet = UnityEngine.Object.Instantiate(releaseTheme.ActiveArtSet);
            var revisionTheme = CreateThemeClone(releaseTheme, revisionSet);
            SetString(revisionSet, "revision", "perf-revision");
            var revisionContext = RuntimeUiDrawContext.Create(revisionTheme, 1f);
            Assert(revisionContext.CacheKey != context.CacheKey
                && !context.IsCurrent(revisionTheme, 1f),
                "art-set revision participates in context reuse");

            var duplicateSet = UnityEngine.Object.Instantiate(releaseTheme.ActiveArtSet);
            var duplicateSerialized = new SerializedObject(duplicateSet);
            duplicateSerialized.FindProperty("bindings").InsertArrayElementAtIndex(0);
            duplicateSerialized.ApplyModifiedPropertiesWithoutUndo();
            var duplicateTheme = CreateThemeClone(releaseTheme, duplicateSet);
            Assert(!duplicateTheme.Validate().IsValid,
                "duplicate slot is rejected before a drawing cache can be constructed");
            ExpectInvalidContext(duplicateTheme, "duplicate slot");

            var missingSet = UnityEngine.Object.Instantiate(releaseTheme.ActiveArtSet);
            var missingSerialized = new SerializedObject(missingSet);
            missingSerialized.FindProperty("bindings").DeleteArrayElementAtIndex(0);
            missingSerialized.ApplyModifiedPropertiesWithoutUndo();
            var missingTheme = CreateThemeClone(releaseTheme, missingSet);
            Assert(!missingTheme.Validate().IsValid,
                "missing slot is rejected before a drawing cache can be constructed");
            ExpectInvalidContext(missingTheme, "missing slot");

            UnityEngine.Object.DestroyImmediate(identityTheme);
            UnityEngine.Object.DestroyImmediate(identitySet);
            UnityEngine.Object.DestroyImmediate(revisionTheme);
            UnityEngine.Object.DestroyImmediate(revisionSet);
            UnityEngine.Object.DestroyImmediate(duplicateTheme);
            UnityEngine.Object.DestroyImmediate(duplicateSet);
            UnityEngine.Object.DestroyImmediate(missingTheme);
            UnityEngine.Object.DestroyImmediate(missingSet);
            Debug.Log("RUNTIME_UI_BINDING_CACHE_CONTRACT_OK slots=" + cache.Length
                + " complete=pass duplicate=rejected missing=rejected identity=pass revision=pass");
        }

        private static BindingResolver CreateBindingResolver()
        {
            var method = typeof(RuntimeUiDrawContext).GetMethod("RequiredBinding",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(method != null, "draw-context required-binding resolver exists");
            return (BindingResolver)method.CreateDelegate(typeof(BindingResolver));
        }

        private static void ValidateNoDrawingPathLinearScan()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                "Assets/Scripts/UI/RuntimeUiGui.cs"));
            var source = File.ReadAllText(sourcePath);
            Assert(!source.Contains(".GetRequiredBinding("),
                "drawing path never calls the ArtSet linear binding resolver");
        }

        private static RuntimeUiTheme CreateThemeClone(RuntimeUiTheme source,
            RuntimeUiArtSet artSet)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            var serialized = new SerializedObject(clone);
            serialized.FindProperty("activeArtSet").objectReferenceValue = artSet;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return clone;
        }

        private static void SetString(UnityEngine.Object target, string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ExpectInvalidContext(RuntimeUiTheme theme, string caseName)
        {
            try
            {
                RuntimeUiDrawContext.Create(theme, 1f);
            }
            catch (InvalidOperationException)
            {
                // Expected: theme validation rejects the invalid art set before cache creation.
                return;
            }
            throw new InvalidOperationException(
                "Runtime UI performance smoke failed: " + caseName
                + " unexpectedly created a drawing context");
        }

        private static void Measure(string name, int operationCount, Func<int> operation)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            var checksum = operation();
            stopwatch.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            var nanosecondsPerOperation = stopwatch.Elapsed.TotalMilliseconds
                * 1000000d / operationCount;
            Debug.Log("RUNTIME_UI_PERF_SAMPLE name=" + name
                + " operations=" + operationCount
                + " elapsed-ms=" + stopwatch.Elapsed.TotalMilliseconds.ToString("F4")
                + " ns-per-operation=" + nanosecondsPerOperation.ToString("F2")
                + " thread-allocated-bytes=" + allocated
                + " checksum=" + checksum);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Runtime UI performance baseline failed: " + message);
        }
    }
}
