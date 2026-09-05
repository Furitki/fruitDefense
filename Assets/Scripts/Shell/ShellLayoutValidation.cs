using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellLayoutValidation
    {
        public static void SmokeValidate()
        {
            ValidateReferenceGeometry();
            Debug.Log("FRUIT_DEFENSE_SHELL_LAYOUT_OK");
        }

        public static void ValidateReferenceGeometry()
        {
            var referenceSafeArea = new Rect(0f, 0f,
                PortraitShellLayout.ReferenceWidth, PortraitShellLayout.ReferenceHeight);
            ValidateHubMatrix();
            ValidateSettlement(PortraitShellLayout.ReferenceWidth,
                PortraitShellLayout.ReferenceHeight, referenceSafeArea);
        }

        private static void ValidateHubMatrix()
        {
            var viewports = new[]
            {
                new HubViewportCase(360f, 800f, 32f, 24f),
                new HubViewportCase(375f, 812f, 40f, 21f),
                new HubViewportCase(402f, 874f, 44f, 34f),
                new HubViewportCase(430f, 932f, 50f, 36f),
            };

            for (var index = 0; index < viewports.Length; index++)
            {
                var viewport = viewports[index];
                ValidateHub(viewport.Width, viewport.Height,
                    new Rect(0f, 0f, viewport.Width, viewport.Height), "full");
                ValidateHub(viewport.Width, viewport.Height,
                    new Rect(0f, viewport.BottomInset, viewport.Width,
                        viewport.Height - viewport.TopInset - viewport.BottomInset),
                    "inset");
            }
        }

        private static void ValidateHub(float width, float height, Rect safeArea,
            string safeAreaKind)
        {
            var layout = PortraitHubLayout.Create(width, height, safeArea);
            var repeated = PortraitHubLayout.Create(width, height, safeArea);
            var sharedFrame = PortraitShellLayout.CreateFrame(width, height, safeArea);
            var caseName = width + "x" + height + " " + safeAreaKind;
            var expectedGuiSafeArea = PortraitShellLayout.ToGuiSafeArea(height, safeArea);

            Assert(Equal(layout.Frame.SafeArea, expectedGuiSafeArea),
                caseName + " Hub maps the screen safe area once");
            if (Mathf.Approximately(width, 402f)
                && Mathf.Approximately(height, 874f)
                && Equal(safeArea, new Rect(0f, 0f, 402f, 874f)))
            {
                Assert(Equal(layout.TopBar, new Rect(7f, 15f, 388f, 80f))
                    && Equal(layout.PageSurface, new Rect(11f, 103f, 386f, 690f))
                    && Equal(layout.NavigationTray, new Rect(0f, 794f, 402f, 80f))
                    && Equal(layout.PrimaryNavigation.Home,
                        new Rect(16f, 794f, 118f, 80f))
                    && Equal(layout.PrimaryNavigation.Activity,
                        new Rect(142f, 794f, 118f, 80f))
                    && Equal(layout.PrimaryNavigation.Growth,
                        new Rect(268f, 794f, 118f, 80f))
                    && Equal(layout.HomePage.GrowthPreview,
                        new Rect(24f, 555f, 354f, 221f))
                    && Equal(layout.HomePage.StartButton,
                        new Rect(57f, 700f, 289f, 56f))
                    && Equal(layout.TopBarContent.Title,
                        new Rect(24f, 27f, 194f, 48f))
                    && Equal(layout.TopBarContent.ResourceBalance,
                        new Rect(235f, 29f, 142f, 46f))
                    && Equal(layout.ActivityPage.Card,
                        new Rect(27f, 122f, 351f, 654f))
                    && Equal(layout.ActivityPage.RewardPanel,
                        new Rect(43f, 382f, 319f, 176f))
                    && Equal(layout.ActivityPage.RewardTitle,
                        new Rect(59f, 388f, 287f, 36f))
                    && Equal(layout.ActivityPage.RewardEquipment,
                        new Rect(59f, 428f, 138f, 118f))
                    && Equal(layout.ActivityPage.RewardItem,
                        new Rect(207f, 428f, 139f, 118f))
                    && Equal(layout.ActivityPage.Status,
                        new Rect(62f, 566f, 278f, 52f))
                    && Equal(layout.ActivityPage.Illustration,
                        new Rect(52f, 252f, 302f, 124f))
                    && Equal(layout.ActivityPage.PrimaryAction,
                        new Rect(66f, 641f, 270f, 57f))
                    && Equal(layout.GrowthPage.Navigation.Equipment,
                        new Rect(24f, 106f, 173f, 52f))
                    && Equal(layout.GrowthPage.Navigation.Cultivation,
                        new Rect(205f, 106f, 173f, 52f))
                    && Equal(layout.GrowthPage.EntryCard,
                        new Rect(27f, 174f, 351f, 116f))
                    && Equal(layout.GrowthPage.DetailPanel,
                        new Rect(27f, 302f, 351f, 474f))
                    && Equal(layout.GrowthPage.EquipmentPrimaryAction,
                        new Rect(109f, 707f, 184f, 55f))
                    && Equal(layout.GrowthPage.CultivationPrimaryAction,
                        new Rect(115f, 723f, 171f, 51f)),
                    "402x874 full Hub matches the approved construction grid");
            }
            Assert(Equal(layout.Frame.SafeArea, sharedFrame.SafeArea)
                && Equal(layout.Frame.Content, sharedFrame.Content)
                && Equal(layout.Frame.Header, sharedFrame.Header)
                && Mathf.Approximately(layout.Frame.Scale, sharedFrame.Scale),
                caseName + " Hub consumes the shared Shell frame authority");
            Assert(Equal(layout.TopBar, repeated.TopBar)
                && Equal(layout.PageSurface, repeated.PageSurface)
                && Equal(layout.NavigationTray, repeated.NavigationTray)
                && Equal(layout.PrimaryNavigation.Home,
                    repeated.PrimaryNavigation.Home)
                && Equal(layout.PrimaryNavigation.Activity,
                    repeated.PrimaryNavigation.Activity)
                && Equal(layout.PrimaryNavigation.Growth,
                    repeated.PrimaryNavigation.Growth)
                && Equal(layout.HomePage.Orchard01Card,
                    repeated.HomePage.Orchard01Card)
                && Equal(layout.HomePage.Orchard02Card,
                    repeated.HomePage.Orchard02Card)
                && Equal(layout.HomePage.Orchard03Card,
                    repeated.HomePage.Orchard03Card)
                && Equal(layout.HomePage.StartButton,
                    repeated.HomePage.StartButton)
                && Equal(layout.HomePage.GrowthPreview,
                    repeated.HomePage.GrowthPreview)
                && Equal(layout.TopBarContent.Title,
                    repeated.TopBarContent.Title)
                && Equal(layout.TopBarContent.ResourceBalance,
                    repeated.TopBarContent.ResourceBalance)
                && Equal(layout.ActivityPage.Card,
                    repeated.ActivityPage.Card)
                && Equal(layout.ActivityPage.PrimaryAction,
                    repeated.ActivityPage.PrimaryAction)
                && Equal(layout.GrowthPage.Navigation.Equipment,
                    repeated.GrowthPage.Navigation.Equipment)
                && Equal(layout.GrowthPage.Navigation.Cultivation,
                    repeated.GrowthPage.Navigation.Cultivation)
                && Equal(layout.GrowthPage.EntryCard,
                    repeated.GrowthPage.EntryCard)
                && Equal(layout.GrowthPage.DetailPanel,
                    repeated.GrowthPage.DetailPanel)
                && Equal(layout.GrowthPage.EquipmentPrimaryAction,
                    repeated.GrowthPage.EquipmentPrimaryAction)
                && Equal(layout.GrowthPage.CultivationPrimaryAction,
                    repeated.GrowthPage.CultivationPrimaryAction),
                caseName + " Hub layout is deterministic");

            Assert(Contains(layout.Frame.SafeArea, layout.Frame.Content)
                && Contains(layout.Frame.SafeArea, layout.TopBar)
                && Contains(layout.Frame.SafeArea, layout.PageSurface)
                && Contains(layout.Frame.SafeArea, layout.NavigationTray)
                && Contains(layout.NavigationTray,
                    layout.PrimaryNavigation.Home)
                && Contains(layout.NavigationTray,
                    layout.PrimaryNavigation.Activity)
                && Contains(layout.NavigationTray,
                    layout.PrimaryNavigation.Growth)
                && Contains(layout.PageSurface,
                    layout.HomePage.Orchard01Card)
                && Contains(layout.PageSurface,
                    layout.HomePage.Orchard02Card)
                && Contains(layout.PageSurface,
                    layout.HomePage.Orchard03Card)
                && Contains(layout.PageSurface,
                    layout.HomePage.GrowthPreview)
                && Contains(layout.HomePage.GrowthPreview,
                    layout.HomePage.StartButton)
                && Contains(layout.TopBar, layout.TopBarContent.Title)
                && Contains(layout.TopBar,
                    layout.TopBarContent.ResourceBalance)
                && Contains(layout.PageSurface,
                    layout.ActivityPage.Card)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.RewardPanel)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.Title)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.Description)
                && Contains(layout.ActivityPage.RewardPanel,
                    layout.ActivityPage.RewardTitle)
                && Contains(layout.ActivityPage.RewardPanel,
                    layout.ActivityPage.RewardEquipment)
                && Contains(layout.ActivityPage.RewardPanel,
                    layout.ActivityPage.RewardItem)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.Status)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.Illustration)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.StateIndicator)
                && Contains(layout.ActivityPage.Card,
                    layout.ActivityPage.PrimaryAction)
                && Contains(layout.PageSurface,
                    layout.GrowthPage.Navigation.Equipment)
                && Contains(layout.PageSurface,
                    layout.GrowthPage.Navigation.Cultivation)
                && Contains(layout.PageSurface,
                    layout.GrowthPage.EntryCard)
                && Contains(layout.PageSurface,
                    layout.GrowthPage.DetailPanel)
                && Contains(layout.GrowthPage.EntryCard,
                    layout.GrowthPage.EntryTitle)
                && Contains(layout.GrowthPage.EntryCard,
                    layout.GrowthPage.EntryStatus)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.DetailTitle)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.Description)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.Rank)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.Effect)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.Cost)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.Status)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.StateIndicator)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.EquipmentPrimaryAction)
                && Contains(layout.GrowthPage.DetailPanel,
                    layout.GrowthPage.CultivationPrimaryAction),
                caseName + " Hub chrome and every child page remain inside the safe area");
            Assert(layout.TopBar.width > 0f && layout.TopBar.height > 0f
                && layout.PageSurface.width > 0f && layout.PageSurface.height > 0f
                && layout.NavigationTray.width > 0f
                && layout.NavigationTray.height > 0f,
                caseName + " Hub owners retain usable positive geometry");
            Assert(!Overlaps(layout.TopBar, layout.PageSurface)
                && !Overlaps(layout.TopBar, layout.NavigationTray)
                && !Overlaps(layout.PageSurface, layout.NavigationTray)
                && !Overlaps(layout.PrimaryNavigation.Home,
                    layout.PrimaryNavigation.Activity)
                && !Overlaps(layout.PrimaryNavigation.Home,
                    layout.PrimaryNavigation.Growth)
                && !Overlaps(layout.PrimaryNavigation.Activity,
                    layout.PrimaryNavigation.Growth)
                && !Overlaps(layout.HomePage.Orchard01Card,
                    layout.HomePage.Orchard02Card)
                && !Overlaps(layout.HomePage.Orchard02Card,
                    layout.HomePage.Orchard03Card)
                && !Overlaps(layout.HomePage.Orchard03Card,
                    layout.HomePage.GrowthPreview)
                && !Overlaps(layout.GrowthPage.Navigation.Equipment,
                    layout.GrowthPage.Navigation.Cultivation)
                && !Overlaps(layout.GrowthPage.Navigation.Equipment,
                    layout.GrowthPage.EntryCard)
                && !Overlaps(layout.GrowthPage.Navigation.Cultivation,
                    layout.GrowthPage.EntryCard)
                && !Overlaps(layout.GrowthPage.EntryCard,
                    layout.GrowthPage.DetailPanel)
                && !Overlaps(layout.ActivityPage.Title,
                    layout.ActivityPage.Description)
                && !Overlaps(layout.ActivityPage.Description,
                    layout.ActivityPage.Illustration)
                && !Overlaps(layout.ActivityPage.Illustration,
                    layout.ActivityPage.RewardPanel)
                && !Overlaps(layout.ActivityPage.RewardTitle,
                    layout.ActivityPage.RewardEquipment)
                && !Overlaps(layout.ActivityPage.RewardEquipment,
                    layout.ActivityPage.RewardItem)
                && !Overlaps(layout.ActivityPage.RewardPanel,
                    layout.ActivityPage.Status)
                && !Overlaps(layout.ActivityPage.Status,
                    layout.ActivityPage.PrimaryAction)
                && !Overlaps(layout.GrowthPage.EntryTitle,
                    layout.GrowthPage.EntryStatus)
                && !Overlaps(layout.GrowthPage.DetailTitle,
                    layout.GrowthPage.Description)
                && !Overlaps(layout.GrowthPage.Description,
                    layout.GrowthPage.Rank)
                && !Overlaps(layout.GrowthPage.Rank,
                    layout.GrowthPage.Effect)
                && !Overlaps(layout.GrowthPage.Effect,
                    layout.GrowthPage.Cost)
                && !Overlaps(layout.GrowthPage.Cost,
                    layout.GrowthPage.Status)
                && !Overlaps(layout.GrowthPage.Status,
                    layout.GrowthPage.EquipmentPrimaryAction)
                && !Overlaps(layout.GrowthPage.Status,
                    layout.GrowthPage.CultivationPrimaryAction),
                caseName + " Hub chrome and child owners do not overlap");

            Assert(IsMinimumTarget(layout.PrimaryNavigation.Home, layout.Frame.Scale)
                && IsMinimumTarget(layout.PrimaryNavigation.Activity,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.PrimaryNavigation.Growth,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.HomePage.Orchard01Card,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.HomePage.Orchard02Card,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.HomePage.Orchard03Card,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.HomePage.StartButton,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.ActivityPage.PrimaryAction,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.GrowthPage.EntryCard,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.GrowthPage.EquipmentPrimaryAction,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.GrowthPage.CultivationPrimaryAction,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.GrowthPage.Navigation.Equipment,
                    layout.Frame.Scale)
                && IsMinimumTarget(layout.GrowthPage.Navigation.Cultivation,
                    layout.Frame.Scale),
                caseName + " Hub navigation and Home actions are at least 44 logical points");

            ValidateHomeCardAnatomy(layout.HomePage.Orchard01Card,
                layout.Frame.Scale, caseName + "/orchard-01");
            ValidateHomeCardAnatomy(layout.HomePage.Orchard02Card,
                layout.Frame.Scale, caseName + "/orchard-02");
            ValidateHomeCardAnatomy(layout.HomePage.Orchard03Card,
                layout.Frame.Scale, caseName + "/orchard-03");

            Assert(Equal(layout.PrimaryNavigation.RectFor(HubPageId.Home),
                    layout.PrimaryNavigation.Home)
                && Equal(layout.PrimaryNavigation.RectFor(HubPageId.Activity),
                    layout.PrimaryNavigation.Activity)
                && Equal(layout.PrimaryNavigation.RectFor(HubPageId.Growth),
                    layout.PrimaryNavigation.Growth)
                && Equal(layout.HomePage.LevelCardFor(
                        LobbyHubPresenter.Orchard01LevelId),
                    layout.HomePage.Orchard01Card)
                && Equal(layout.HomePage.LevelCardFor(
                        LobbyHubPresenter.Orchard02LevelId),
                    layout.HomePage.Orchard02Card)
                && Equal(layout.HomePage.LevelCardFor(
                        LobbyHubPresenter.Orchard03LevelId),
                    layout.HomePage.Orchard03Card)
                && Equal(layout.GrowthPage.Navigation.RectFor(
                        GrowthPageId.Equipment),
                    layout.GrowthPage.Navigation.Equipment)
                && Equal(layout.GrowthPage.Navigation.RectFor(
                        GrowthPageId.Cultivation),
                    layout.GrowthPage.Navigation.Cultivation)
                && Equal(layout.GrowthPage.PrimaryActionFor(
                        GrowthPageId.Equipment),
                    layout.GrowthPage.EquipmentPrimaryAction)
                && Equal(layout.GrowthPage.PrimaryActionFor(
                        GrowthPageId.Cultivation),
                    layout.GrowthPage.CultivationPrimaryAction),
                caseName + " Hub drawing resolves the authoritative navigation rectangles");

            Assert(PortraitHubLayout.HitTest(layout,
                        layout.PrimaryNavigation.Home.center, HubPageId.Home, false)
                    == HubHitTarget.Home
                && PortraitHubLayout.HitTest(layout,
                        layout.PrimaryNavigation.Activity.center, HubPageId.Home, false)
                    == HubHitTarget.Activity
                && PortraitHubLayout.HitTest(layout,
                        layout.PrimaryNavigation.Growth.center, HubPageId.Home, false)
                    == HubHitTarget.Growth
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.Orchard01Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard01
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.Orchard02Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard02
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.Orchard03Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard03
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.StartButton.center,
                        HubPageId.Home, false) == HubHitTarget.Start
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.Navigation.Equipment.center,
                        HubPageId.Growth, false) == HubHitTarget.Equipment
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.Navigation.Cultivation.center,
                        HubPageId.Growth, false) == HubHitTarget.Cultivation
                && PortraitHubLayout.HitTest(layout,
                        layout.ActivityPage.PrimaryAction.center,
                        HubPageId.Activity, false)
                    == HubHitTarget.ActivityClaim
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.EntryCard.center,
                        HubPageId.Growth, false, GrowthPageId.Equipment)
                    == HubHitTarget.EquipmentEntry
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.EntryCard.center,
                        HubPageId.Growth, false, GrowthPageId.Cultivation)
                    == HubHitTarget.CultivationEntry
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.EquipmentPrimaryAction.center,
                        HubPageId.Growth, false, GrowthPageId.Equipment)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.CultivationPrimaryAction.center,
                        HubPageId.Growth, false, GrowthPageId.Cultivation)
                    == HubHitTarget.GrowthPrimaryAction,
                caseName + " Hub hit testing consumes every drawn interactive rectangle");
            var hiddenGrowthNavigationHit = PortraitHubLayout.HitTest(layout,
                layout.GrowthPage.Navigation.Equipment.center,
                HubPageId.Home, false);
            var equipmentOnlyActionPoint = new Vector2(
                layout.GrowthPage.EquipmentPrimaryAction.xMin
                    + layout.Frame.Scale,
                layout.GrowthPage.EquipmentPrimaryAction.yMin
                    + layout.Frame.Scale);
            var cultivationOnlyActionPoint = new Vector2(
                layout.GrowthPage.CultivationPrimaryAction.center.x,
                layout.GrowthPage.CultivationPrimaryAction.yMax
                    - layout.Frame.Scale);
            Assert(hiddenGrowthNavigationHit != HubHitTarget.Equipment
                && hiddenGrowthNavigationHit != HubHitTarget.Cultivation
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.StartButton.center,
                        HubPageId.Activity, false) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        layout.ActivityPage.PrimaryAction.center,
                        HubPageId.Home, false) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.EntryCard.center,
                        HubPageId.Activity, false)
                    != HubHitTarget.EquipmentEntry
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.EntryCard.center,
                        HubPageId.Activity, false)
                    != HubHitTarget.CultivationEntry
                && PortraitHubLayout.HitTest(layout,
                        layout.PrimaryNavigation.Home.center,
                        HubPageId.Home, true) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        layout.HomePage.Orchard01Card.center,
                        HubPageId.Home, true) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        layout.GrowthPage.Navigation.Cultivation.center,
                        HubPageId.Growth, true) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        equipmentOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Equipment)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(layout,
                        equipmentOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Cultivation) == HubHitTarget.None
                && PortraitHubLayout.HitTest(layout,
                        cultivationOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Cultivation)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(layout,
                        cultivationOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Equipment) == HubHitTarget.None,
                caseName + " hidden, unavailable, transitioning, and other-page Hub targets reject input");
        }

        private static bool IsMinimumTarget(Rect rect, float scale)
        {
            var minimum = PortraitHubLayout.MinimumTargetLogical * scale;
            const float tolerance = .01f;
            return rect.width + tolerance >= minimum
                && rect.height + tolerance >= minimum;
        }

        private static void ValidateHomeCardAnatomy(Rect card, float scale,
            string caseName)
        {
            var anatomy = PortraitHubLayout.CreateHomeLevelCard(card, scale);
            var expectedFrame = new Vector2(
                136f * scale, Mathf.Min(102f * scale,
                    card.height - 20f * scale));
            Assert(Contains(card, anatomy.Frame)
                && Contains(anatomy.Frame, anatomy.Thumbnail)
                && Contains(card, anatomy.Title)
                && Contains(card, anatomy.Body)
                && Contains(card, anatomy.SelectedMarker)
                && Contains(card, anatomy.TransientIndicator),
                caseName + " card art, copy and state cues remain inside the original hit rect");
            Assert(ApproximatelyGeometry(anatomy.Frame.width, expectedFrame.x)
                && ApproximatelyGeometry(anatomy.Frame.height, expectedFrame.y)
                && ApproximatelyGeometry(anatomy.Frame.x - card.x, 12f * scale)
                && ApproximatelyGeometry(anatomy.Frame.center.y, card.center.y)
                && ApproximatelyGeometry(anatomy.Thumbnail.width,
                    anatomy.Frame.width)
                && ApproximatelyGeometry(anatomy.Thumbnail.height,
                    anatomy.Frame.height)
                && ApproximatelyGeometry(anatomy.Thumbnail.center.x,
                    anatomy.Frame.center.x)
                && ApproximatelyGeometry(anatomy.Thumbnail.center.y,
                    anatomy.Frame.center.y),
                caseName + " card centers and fills its complete reference-led level-art frame");
            Assert(ApproximatelyGeometry(anatomy.Title.x - anatomy.Frame.xMax, 10f * scale)
                && ApproximatelyGeometry(anatomy.Title.x - card.x, 158f * scale)
                && ApproximatelyGeometry(anatomy.Title.width,
                    card.width - 220f * scale)
                && ApproximatelyGeometry(anatomy.Title.y - card.y, 16f * scale)
                && ApproximatelyGeometry(anatomy.Title.height, 48f * scale)
                && ApproximatelyGeometry(anatomy.Body.y - card.y, 68f * scale)
                && ApproximatelyGeometry(anatomy.Body.height, 40f * scale)
                && !Overlaps(anatomy.Frame, anatomy.Title)
                && !Overlaps(anatomy.Frame, anatomy.Body)
                && !Overlaps(anatomy.Title, anatomy.SelectedMarker)
                && !Overlaps(anatomy.Body, anatomy.TransientIndicator),
                caseName + " card retains the reference copy gap and cue-safe two-line copy");
            Assert(ApproximatelyGeometry(anatomy.SelectedMarker.width, 48f * scale)
                && ApproximatelyGeometry(anatomy.SelectedMarker.height, 48f * scale),
                caseName + " selected source canvas preserves its approved 32-36px optical size");
        }

        private static void ValidateSettlement(float width, float height, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateSettlement(width, height, safeArea);
            if (Mathf.Approximately(width, PortraitShellLayout.ReferenceWidth)
                && Mathf.Approximately(height, PortraitShellLayout.ReferenceHeight)
                && Equal(safeArea, new Rect(0f, 0f, width, height)))
            {
                Assert(Equal(layout.Title, new Rect(16f, 42f, 370f, 64f))
                    && Equal(layout.ResultCard, new Rect(16f, 120f, 370f, 502f))
                    && Equal(layout.ResultBanner, new Rect(36f, 150f, 330f, 48f))
                    && Equal(layout.Outcome, new Rect(98f, 154f, 206f, 40f))
                    && Equal(layout.OrchardVista, new Rect(32f, 222f, 338f, 216f))
                    && Equal(layout.CompletedLevel, new Rect(32f, 450f, 338f, 48f))
                    && Equal(layout.ReachedWave, new Rect(32f, 506f, 338f, 48f))
                    && Equal(layout.RemainingLives, new Rect(32f, 562f, 338f, 48f))
                    && Equal(layout.ResultIndicator, new Rect(308f, 160f, 28f, 28f))
                    && Equal(layout.RetryButton, new Rect(16f, 638f, 370f, 72f))
                    && Equal(layout.ReturnButton, new Rect(16f, 722f, 370f, 64f))
                    && Equal(layout.Status, new Rect(16f, 798f, 370f, 58f)),
                    "Settlement reference composition matches the approved quality audit");
            }
            Assert(Contains(layout.Frame.SafeArea, layout.ResultCard)
                && Contains(layout.ResultCard, layout.Outcome)
                && Contains(layout.ResultCard, layout.CompletedLevel)
                && Contains(layout.ResultCard, layout.ReachedWave)
                && Contains(layout.ResultCard, layout.RemainingLives)
                && Contains(layout.ResultCard, layout.ResultBanner)
                && Contains(layout.ResultCard, layout.OrchardVista)
                && Contains(layout.ResultBanner, layout.ResultIndicator),
                "result values including completed level remain inside result card");
            Assert(!Overlaps(layout.ResultBanner, layout.OrchardVista)
                && Contains(layout.ResultBanner, layout.Outcome)
                && !Overlaps(layout.Outcome, layout.ResultIndicator)
                && !Overlaps(layout.OrchardVista, layout.CompletedLevel)
                && !Overlaps(layout.OrchardVista, layout.ReachedWave)
                && !Overlaps(layout.OrchardVista, layout.RemainingLives),
                "result art hierarchy leaves outcome copy and metrics unobscured");
            Assert(Contains(layout.Frame.SafeArea, layout.RetryButton)
                && Contains(layout.Frame.SafeArea, layout.ReturnButton)
                && !Overlaps(layout.RetryButton, layout.ReturnButton),
                "settlement actions remain usable and non-overlapping");
            Assert(PortraitShellLayout.HitTest(layout, layout.RetryButton.center, false)
                    == ShellHitTarget.Retry
                && PortraitShellLayout.HitTest(layout, layout.ReturnButton.center, false)
                    == ShellHitTarget.Return,
                "Settlement Retry and Return hit exactly their drawn rectangles");
            Assert(PortraitShellLayout.HitTest(layout, layout.RetryButton.center, true)
                    == ShellHitTarget.None
                && PortraitShellLayout.HitTest(layout, layout.ReturnButton.center, true)
                    == ShellHitTarget.None,
                "Settlement hit targets are disabled during transition");
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = .01f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool ApproximatelyGeometry(float first, float second)
        {
            const float tolerance = .01f;
            return Mathf.Abs(first - second) <= tolerance;
        }

        private static bool Overlaps(Rect first, Rect second)
        {
            const float tolerance = .01f;
            return Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin) > tolerance
                && Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin) > tolerance;
        }

        private static bool Equal(Rect first, Rect second)
        {
            return Mathf.Approximately(first.x, second.x)
                && Mathf.Approximately(first.y, second.y)
                && Mathf.Approximately(first.width, second.width)
                && Mathf.Approximately(first.height, second.height);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Shell layout validation failed: " + message);
        }

        private readonly struct HubViewportCase
        {
            public HubViewportCase(float width, float height, float topInset,
                float bottomInset)
            {
                Width = width;
                Height = height;
                TopInset = topInset;
                BottomInset = bottomInset;
            }

            public float Width { get; }
            public float Height { get; }
            public float TopInset { get; }
            public float BottomInset { get; }
        }
    }
}
