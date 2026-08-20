using UnityEngine;

namespace FruitDefense.UI
{
    [CreateAssetMenu(fileName = "RuntimeUiTheme",
        menuName = "Fruit Defense/UI/Runtime UI Theme")]
    public sealed class RuntimeUiTheme : ScriptableObject
    {
        [SerializeField] private string themeId = "ui.sunny-orchard";
        [SerializeField] private string revision = "1";
        [SerializeField] private RuntimeUiSemanticColors colors =
            RuntimeUiSemanticColors.SunnyOrchardDefault();
        [SerializeField] private Font packagedChineseFont;
        [SerializeField] private RuntimeUiTypographyTokens typography =
            RuntimeUiTypographyTokens.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiMetrics metrics =
            RuntimeUiMetrics.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiFeedbackTokens feedback =
            RuntimeUiFeedbackTokens.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiArtSet activeArtSet;

        public string ThemeId => themeId;
        public string Revision => revision;
        public RuntimeUiSemanticColors Colors => colors;
        public Font PackagedChineseFont => packagedChineseFont;
        public RuntimeUiTypographyTokens Typography => typography;
        public RuntimeUiMetrics Metrics => metrics;
        public RuntimeUiFeedbackTokens Feedback => feedback;
        public RuntimeUiArtSet ActiveArtSet => activeArtSet;

        public RuntimeUiValidationResult Validate()
        {
            var result = new RuntimeUiValidationResult();
            if (!RuntimeUiIdentity.IsValid(themeId))
            {
                result.Add("theme.identity", "themeId",
                    "Theme ID must be a stable lowercase semantic identifier.");
            }

            if (!RuntimeUiIdentity.IsValidRevision(revision))
            {
                result.Add("theme.revision", "revision",
                    "Revision must be a non-empty stable lowercase token.");
            }

            colors.AppendValidation(result, "colors");
            typography.AppendValidation(result, "typography");
            metrics.AppendValidation(result, "metrics");
            feedback.AppendValidation(result, "feedback");

            if (packagedChineseFont == null)
            {
                result.Add("theme.font.null", "packagedChineseFont",
                    "The release theme must reference its packaged Chinese font.");
            }

            if (activeArtSet == null)
            {
                result.Add("theme.art-set.null", "activeArtSet",
                    "The release theme must reference exactly one active runtime UI art set.");
            }
            else
            {
                result.Append(activeArtSet.Validate(), "activeArtSet");
            }

            return result;
        }

        public bool TryValidate(out string reason)
        {
            var validation = Validate();
            reason = validation.FirstIssueOr("ok");
            return validation.IsValid;
        }
    }
}
