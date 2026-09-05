using UnityEngine;

namespace FruitDefense.Editor
{
    /// <summary>
    /// Focused entry point for the shared Hub/UI component contract. The project
    /// aggregate remains the release authority; this suite keeps component,
    /// finite-copy, packaged-glyph, and ArtSet checks runnable as one batch job.
    /// </summary>
    public static class RuntimeUiHubComponentSmoke
    {
        public static void Run()
        {
            RuntimeUiQualitySmoke.Run();
            RuntimeUiGlyphCoverageSmoke.Run();
            RuntimeUiVisualSystemSmoke.Run();
            Debug.Log("RUNTIME_UI_HUB_COMPONENTS_OK");
        }
    }
}
