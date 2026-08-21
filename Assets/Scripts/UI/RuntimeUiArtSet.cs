using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.UI
{
    [CreateAssetMenu(fileName = "RuntimeUiArtSet",
        menuName = "Fruit Defense/UI/Runtime UI Art Set")]
    public sealed class RuntimeUiArtSet : ScriptableObject
    {
        private static readonly RuntimeUiArtBinding[] EmptyBindings =
            Array.Empty<RuntimeUiArtBinding>();

        [SerializeField] private string setId = "sunny-orchard";
        [SerializeField] private string revision = "1";
        [SerializeField] private RuntimeUiArtBinding[] bindings = EmptyBindings;

        public string SetId => setId;
        public string Revision => revision;
        public IReadOnlyList<RuntimeUiArtBinding> Bindings =>
            Array.AsReadOnly(bindings ?? EmptyBindings);

        public RuntimeUiValidationResult Validate()
        {
            var result = new RuntimeUiValidationResult();
            if (!RuntimeUiIdentity.IsValid(setId))
            {
                result.Add("art-set.identity", "setId",
                    "Set ID must be a stable lowercase semantic identifier.");
            }

            if (!RuntimeUiIdentity.IsValidRevision(revision))
            {
                result.Add("art-set.revision", "revision",
                    "Revision must be a non-empty stable lowercase token.");
            }

            if (bindings == null)
            {
                result.Add("art-set.bindings.null", "bindings",
                    "The serialized binding array cannot be null.");
                AppendMissingSlots(result, new int[RuntimeUiArtSlots.RequiredCount]);
                return result;
            }

            var occurrences = new int[RuntimeUiArtSlots.RequiredCount];
            var hasIconReference = false;
            var iconPixelsPerLogicalUnit = 0f;
            var iconLogicalWidth = 0f;
            var iconLogicalHeight = 0f;
            var iconSafeLeft = 0f;
            var iconSafeTop = 0f;
            var iconSafeRight = 0f;
            var iconSafeBottom = 0f;
            var hasMicroIconReference = false;
            var microIconPixelsPerLogicalUnit = 0f;
            var microIconLogicalWidth = 0f;
            var microIconLogicalHeight = 0f;
            var microIconSafeLeft = 0f;
            var microIconSafeTop = 0f;
            var microIconSafeRight = 0f;
            var microIconSafeBottom = 0f;
            for (var index = 0; index < bindings.Length; index++)
            {
                var binding = bindings[index];
                var field = "bindings[" + index + "]";
                if (binding == null)
                {
                    result.Add("art-set.binding.null", field,
                        "A serialized art binding cannot be null.");
                    continue;
                }

                var requiredIndex = RuntimeUiArtSlots.RequiredIndex(binding.Slot);
                if (requiredIndex < 0)
                {
                    result.Add("art-set.slot.unknown", field + ".slot",
                        "The slot is outside the finite runtime UI art contract.");
                    continue;
                }

                occurrences[requiredIndex]++;
                if (occurrences[requiredIndex] > 1)
                {
                    result.Add("art-set.slot.duplicate", field + ".slot",
                        "Required slot '" + RuntimeUiArtSlots.SemanticId(binding.Slot)
                        + "' is serialized more than once.");
                }

                binding.AppendValidation(result, field);
                if (binding.Geometry == RuntimeUiArtGeometry.Icon
                    && binding.Sprite != null
                    && RuntimeUiNumbers.IsFinite(binding.PixelsPerLogicalUnit)
                    && binding.PixelsPerLogicalUnit > 0f)
                {
                    if (RuntimeUiArtSlots.IsMicroIcon(binding.Slot))
                    {
                        AppendIconSetConsistency(result, binding, field,
                            ref hasMicroIconReference, ref microIconPixelsPerLogicalUnit,
                            ref microIconLogicalWidth, ref microIconLogicalHeight,
                            ref microIconSafeLeft, ref microIconSafeTop,
                            ref microIconSafeRight, ref microIconSafeBottom);
                    }
                    else
                    {
                        AppendIconSetConsistency(result, binding, field,
                            ref hasIconReference, ref iconPixelsPerLogicalUnit,
                            ref iconLogicalWidth, ref iconLogicalHeight,
                            ref iconSafeLeft, ref iconSafeTop,
                            ref iconSafeRight, ref iconSafeBottom);
                    }
                }
            }

            AppendMissingSlots(result, occurrences);
            AppendActionSurfaceOpticalConsistency(result);
            return result;
        }

        public bool TryValidate(out string reason)
        {
            var validation = Validate();
            reason = validation.FirstIssueOr("ok");
            return validation.IsValid;
        }

        public bool TryGetBinding(RuntimeUiArtSlot slot, out RuntimeUiArtBinding binding)
        {
            binding = null;
            if (!RuntimeUiArtSlots.IsRequired(slot) || bindings == null)
                return false;

            var matchCount = 0;
            for (var index = 0; index < bindings.Length; index++)
            {
                var candidate = bindings[index];
                if (candidate == null || candidate.Slot != slot)
                    continue;

                matchCount++;
                binding = candidate;
            }

            if (matchCount == 1)
                return true;

            binding = null;
            return false;
        }

        public RuntimeUiArtBinding GetRequiredBinding(RuntimeUiArtSlot slot)
        {
            if (!TryGetBinding(slot, out var binding))
            {
                var semanticId = RuntimeUiArtSlots.IsRequired(slot)
                    ? RuntimeUiArtSlots.SemanticId(slot)
                    : slot.ToString();
                throw new InvalidOperationException(
                    "Runtime UI art slot '" + semanticId
                    + "' must be present exactly once in set '" + setId + "'.");
            }

            return binding;
        }

        private static void AppendMissingSlots(RuntimeUiValidationResult result, int[] occurrences)
        {
            var required = RuntimeUiArtSlots.Required;
            for (var index = 0; index < required.Count; index++)
            {
                if (occurrences[index] != 0)
                    continue;

                result.Add("art-set.slot.missing", "bindings",
                    "Required slot '" + RuntimeUiArtSlots.SemanticId(required[index])
                    + "' is missing.");
            }
        }

        private static void AppendIconSetConsistency(RuntimeUiValidationResult result,
            RuntimeUiArtBinding binding, string field, ref bool hasReference,
            ref float referenceScale, ref float referenceWidth, ref float referenceHeight,
            ref float referenceSafeLeft, ref float referenceSafeTop,
            ref float referenceSafeRight, ref float referenceSafeBottom)
        {
            var scale = binding.PixelsPerLogicalUnit;
            var rect = binding.Sprite.rect;
            var width = rect.width / scale;
            var height = rect.height / scale;
            var safeLeft = binding.SafeInset.Left / scale;
            var safeTop = binding.SafeInset.Top / scale;
            var safeRight = binding.SafeInset.Right / scale;
            var safeBottom = binding.SafeInset.Bottom / scale;
            if (!hasReference)
            {
                hasReference = true;
                referenceScale = scale;
                referenceWidth = width;
                referenceHeight = height;
                referenceSafeLeft = safeLeft;
                referenceSafeTop = safeTop;
                referenceSafeRight = safeRight;
                referenceSafeBottom = safeBottom;
                return;
            }

            if (!NearlyEqual(scale, referenceScale))
            {
                result.Add("art-set.icon.scale-consistency",
                    field + ".pixelsPerLogicalUnit",
                    "Every icon slot in one art set must use the same source scale.");
            }

            if (!NearlyEqual(width, referenceWidth) || !NearlyEqual(height, referenceHeight))
            {
                result.Add("art-set.icon.canvas-consistency", field + ".sprite",
                    "Every icon slot in one art set must use the same logical canvas size.");
            }

            if (!NearlyEqual(safeLeft, referenceSafeLeft)
                || !NearlyEqual(safeTop, referenceSafeTop)
                || !NearlyEqual(safeRight, referenceSafeRight)
                || !NearlyEqual(safeBottom, referenceSafeBottom))
            {
                result.Add("art-set.icon.safe-inset-consistency", field + ".safeInset",
                    "Every icon slot in one art set must use the same logical safe inset.");
            }
        }

        private void AppendActionSurfaceOpticalConsistency(RuntimeUiValidationResult result)
        {
            RuntimeUiPixelInsets? reference = null;
            foreach (var slot in new[]
                     {
                         RuntimeUiArtSlot.ActionPrimary,
                         RuntimeUiArtSlot.ActionSecondary,
                         RuntimeUiArtSlot.ActionQuiet,
                         RuntimeUiArtSlot.ActionDanger,
                     })
            {
                if (!TryGetBinding(slot, out var binding))
                    return;
                if (!reference.HasValue)
                {
                    reference = binding.OpticalInset;
                    continue;
                }
                if (SameInsets(reference.Value, binding.OpticalInset))
                    continue;
                result.Add("art-set.action.optical-envelope", "bindings",
                    "Every action surface in one art set must use the same visible optical envelope.");
                return;
            }
        }

        private static bool SameInsets(RuntimeUiPixelInsets left,
            RuntimeUiPixelInsets right)
        {
            return left.Left == right.Left && left.Top == right.Top
                && left.Right == right.Right && left.Bottom == right.Bottom;
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Mathf.Abs(left - right) <= .001f;
        }
    }

    [Serializable]
    public sealed class RuntimeUiArtBinding
    {
        [SerializeField] private RuntimeUiArtSlot slot;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Sprite sprite;
        [SerializeField] private RuntimeUiPixelInsets sliceBorder;
        [SerializeField] private RuntimeUiPixelInsets safeInset;
        [SerializeField] private RuntimeUiPixelInsets opticalInset;
        [SerializeField, Min(.01f)] private float pixelsPerLogicalUnit = 1f;

        public RuntimeUiArtSlot Slot => slot;
        public Texture2D Texture => texture;
        public Sprite Sprite => sprite;
        public RuntimeUiPixelInsets SliceBorder => sliceBorder;
        public RuntimeUiPixelInsets SafeInset => safeInset;
        public RuntimeUiPixelInsets OpticalInset => opticalInset;
        public float PixelsPerLogicalUnit => pixelsPerLogicalUnit;
        public RuntimeUiArtGeometry Geometry => RuntimeUiArtSlots.Geometry(slot);

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            if (texture == null)
                result.Add("art-set.resource.texture-null", field + ".texture",
                    "Every required slot must reference its imported runtime texture.");
            if (sprite == null)
                result.Add("art-set.resource.sprite-null", field + ".sprite",
                    "Every required slot must reference its imported runtime sprite.");
            if (!RuntimeUiNumbers.IsFinite(pixelsPerLogicalUnit) || pixelsPerLogicalUnit <= 0f)
            {
                result.Add("art-set.scale.invalid", field + ".pixelsPerLogicalUnit",
                    "Source scale must be a finite positive value.");
            }

            if (sliceBorder.HasNegativeValue)
                result.Add("art-set.slice.negative", field + ".sliceBorder",
                    "Slice borders cannot contain negative values.");
            if (safeInset.HasNegativeValue)
                result.Add("art-set.safe-inset.negative", field + ".safeInset",
                    "Safe insets cannot contain negative values.");
            if (opticalInset.HasNegativeValue)
                result.Add("art-set.optical-inset.negative", field + ".opticalInset",
                    "Optical insets cannot contain negative values.");

            if (texture != null && (texture.width <= 0 || texture.height <= 0))
            {
                result.Add("art-set.resource.texture-size", field + ".texture",
                    "The referenced texture must have positive dimensions.");
            }

            if (sprite == null)
                return;

            var rect = sprite.rect;
            if (!RuntimeUiNumbers.IsFinite(rect.width) || !RuntimeUiNumbers.IsFinite(rect.height)
                || rect.width <= 0f || rect.height <= 0f)
            {
                result.Add("art-set.resource.sprite-size", field + ".sprite",
                    "The referenced sprite must have positive finite dimensions.");
                return;
            }

            if (texture != null && sprite.texture != texture)
            {
                result.Add("art-set.resource.mismatch", field,
                    "Texture and sprite references must identify the same standalone asset.");
            }

            if (sliceBorder.Horizontal >= rect.width || sliceBorder.Vertical >= rect.height)
            {
                result.Add("art-set.slice.bounds", field + ".sliceBorder",
                    "Slice borders must leave a positive center region inside the sprite rect.");
            }

            if (safeInset.Horizontal >= rect.width || safeInset.Vertical >= rect.height)
            {
                result.Add("art-set.safe-inset.bounds", field + ".safeInset",
                    "Safe insets must leave a positive content region inside the sprite rect.");
            }

            if (opticalInset.Horizontal >= rect.width || opticalInset.Vertical >= rect.height)
            {
                result.Add("art-set.optical-inset.bounds", field + ".opticalInset",
                    "Optical insets must leave a positive visible region inside the sprite rect.");
            }

            var geometry = RuntimeUiArtSlots.Geometry(slot);
            if (geometry == RuntimeUiArtGeometry.NineSlice)
            {
                if (sliceBorder.Left <= 0 || sliceBorder.Top <= 0
                    || sliceBorder.Right <= 0 || sliceBorder.Bottom <= 0)
                {
                    result.Add("art-set.slice.required", field + ".sliceBorder",
                        "Nine-slice slots must declare positive protected borders on every side.");
                }
            }
            else if (!sliceBorder.IsZero)
            {
                result.Add("art-set.slice.unexpected", field + ".sliceBorder",
                    "Stretch and icon slots cannot declare nine-slice borders.");
            }

            if (geometry != RuntimeUiArtGeometry.Icon)
                return;

            if (Mathf.Abs(rect.width - rect.height) > .01f)
            {
                result.Add("art-set.icon.canvas", field + ".sprite",
                    "Icon slots must use a square sprite canvas.");
            }

            if (safeInset.Left <= 0 || safeInset.Top <= 0
                || safeInset.Right <= 0 || safeInset.Bottom <= 0)
            {
                result.Add("art-set.icon.safe-inset", field + ".safeInset",
                    "Icon slots must declare a positive safe inset on every side.");
            }
        }
    }
}
