using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public enum HubHitTarget
    {
        None = 0,
        Home = 1,
        Activity = 2,
        Growth = 3,
        Equipment = 4,
        Cultivation = 5,
        LevelOrchard01 = 6,
        LevelOrchard02 = 7,
        LevelOrchard03 = 8,
        Start = 9,
        ActivityClaim = 10,
        EquipmentEntry = 11,
        CultivationEntry = 12,
        GrowthPrimaryAction = 13,
    }

    public readonly struct HubTopBarLayout
    {
        public HubTopBarLayout(Rect title, Rect resourceBalance)
        {
            Title = title;
            ResourceBalance = resourceBalance;
        }

        public Rect Title { get; }
        public Rect ResourceBalance { get; }
    }

    public readonly struct HubPrimaryNavigationLayout
    {
        public HubPrimaryNavigationLayout(Rect home, Rect activity, Rect growth)
        {
            Home = home;
            Activity = activity;
            Growth = growth;
        }

        public Rect Home { get; }
        public Rect Activity { get; }
        public Rect Growth { get; }

        public Rect RectFor(HubPageId page)
        {
            switch (page)
            {
                case HubPageId.Home: return Home;
                case HubPageId.Activity: return Activity;
                case HubPageId.Growth: return Growth;
                default: return default;
            }
        }
    }

    public readonly struct GrowthSecondaryNavigationLayout
    {
        public GrowthSecondaryNavigationLayout(Rect equipment, Rect cultivation)
        {
            Equipment = equipment;
            Cultivation = cultivation;
        }

        public Rect Equipment { get; }
        public Rect Cultivation { get; }

        public Rect RectFor(GrowthPageId page)
        {
            switch (page)
            {
                case GrowthPageId.Equipment: return Equipment;
                case GrowthPageId.Cultivation: return Cultivation;
                default: return default;
            }
        }
    }

    public readonly struct HubHomePageLayout
    {
        public HubHomePageLayout(Rect orchard01Card, Rect orchard02Card,
            Rect orchard03Card, Rect growthPreview, Rect startButton)
        {
            Orchard01Card = orchard01Card;
            Orchard02Card = orchard02Card;
            Orchard03Card = orchard03Card;
            GrowthPreview = growthPreview;
            StartButton = startButton;
        }

        public Rect Orchard01Card { get; }
        public Rect Orchard02Card { get; }
        public Rect Orchard03Card { get; }
        public Rect GrowthPreview { get; }
        public Rect StartButton { get; }

        public Rect LevelCardFor(string levelId)
        {
            switch (levelId)
            {
                case LobbyHubPresenter.Orchard01LevelId: return Orchard01Card;
                case LobbyHubPresenter.Orchard02LevelId: return Orchard02Card;
                case LobbyHubPresenter.Orchard03LevelId: return Orchard03Card;
                default: return default;
            }
        }
    }

    public readonly struct HubLevelCardLayout
    {
        public HubLevelCardLayout(Rect frame, Rect thumbnail, Rect title, Rect body,
            Rect selectedMarker, Rect transientIndicator)
        {
            Frame = frame;
            Thumbnail = thumbnail;
            Title = title;
            Body = body;
            SelectedMarker = selectedMarker;
            TransientIndicator = transientIndicator;
        }

        public Rect Frame { get; }
        public Rect Thumbnail { get; }
        public Rect Title { get; }
        public Rect Body { get; }
        public Rect SelectedMarker { get; }
        public Rect TransientIndicator { get; }
    }

    public readonly struct HubActivityPageLayout
    {
        public HubActivityPageLayout(Rect card, Rect title, Rect description,
            Rect rewardPanel, Rect rewardTitle, Rect rewardEquipment,
            Rect rewardItem, Rect status, Rect illustration, Rect primaryAction,
            Rect stateIndicator)
        {
            Card = card;
            Title = title;
            Description = description;
            RewardPanel = rewardPanel;
            RewardTitle = rewardTitle;
            RewardEquipment = rewardEquipment;
            RewardItem = rewardItem;
            Status = status;
            Illustration = illustration;
            PrimaryAction = primaryAction;
            StateIndicator = stateIndicator;
        }

        public Rect Card { get; }
        public Rect Title { get; }
        public Rect Description { get; }
        public Rect RewardPanel { get; }
        public Rect RewardTitle { get; }
        public Rect RewardEquipment { get; }
        public Rect RewardItem { get; }
        public Rect Status { get; }
        public Rect Illustration { get; }
        public Rect PrimaryAction { get; }
        public Rect StateIndicator { get; }
    }

    public readonly struct HubGrowthPageLayout
    {
        public HubGrowthPageLayout(GrowthSecondaryNavigationLayout navigation,
            Rect entryCard, Rect entryTitle, Rect entryStatus,
            Rect detailPanel, Rect detailTitle, Rect description, Rect rank,
            Rect effect, Rect cost, Rect status, Rect equipmentPrimaryAction,
            Rect cultivationPrimaryAction, Rect stateIndicator)
        {
            Navigation = navigation;
            EntryCard = entryCard;
            EntryTitle = entryTitle;
            EntryStatus = entryStatus;
            DetailPanel = detailPanel;
            DetailTitle = detailTitle;
            Description = description;
            Rank = rank;
            Effect = effect;
            Cost = cost;
            Status = status;
            EquipmentPrimaryAction = equipmentPrimaryAction;
            CultivationPrimaryAction = cultivationPrimaryAction;
            StateIndicator = stateIndicator;
        }

        public GrowthSecondaryNavigationLayout Navigation { get; }
        public Rect EntryCard { get; }
        public Rect EntryTitle { get; }
        public Rect EntryStatus { get; }
        public Rect DetailPanel { get; }
        public Rect DetailTitle { get; }
        public Rect Description { get; }
        public Rect Rank { get; }
        public Rect Effect { get; }
        public Rect Cost { get; }
        public Rect Status { get; }
        public Rect EquipmentPrimaryAction { get; }
        public Rect CultivationPrimaryAction { get; }
        public Rect StateIndicator { get; }

        public Rect PrimaryActionFor(GrowthPageId page)
        {
            switch (page)
            {
                case GrowthPageId.Equipment:
                    return EquipmentPrimaryAction;
                case GrowthPageId.Cultivation:
                    return CultivationPrimaryAction;
                default:
                    throw new ArgumentOutOfRangeException(nameof(page), page,
                        null);
            }
        }
    }

    public readonly struct PortraitHubResolvedLayout
    {
        public PortraitHubResolvedLayout(PortraitShellFrame frame, Rect topBar,
            HubTopBarLayout topBarContent,
            Rect pageSurface, Rect navigationTray,
            HubPrimaryNavigationLayout primaryNavigation,
            HubHomePageLayout homePage,
            HubActivityPageLayout activityPage,
            HubGrowthPageLayout growthPage)
        {
            Frame = frame;
            TopBar = topBar;
            TopBarContent = topBarContent;
            PageSurface = pageSurface;
            NavigationTray = navigationTray;
            PrimaryNavigation = primaryNavigation;
            HomePage = homePage;
            ActivityPage = activityPage;
            GrowthPage = growthPage;
        }

        public PortraitShellFrame Frame { get; }
        public Rect TopBar { get; }
        public HubTopBarLayout TopBarContent { get; }
        public Rect PageSurface { get; }
        public Rect NavigationTray { get; }
        public HubPrimaryNavigationLayout PrimaryNavigation { get; }
        public HubHomePageLayout HomePage { get; }
        public HubActivityPageLayout ActivityPage { get; }
        public HubGrowthPageLayout GrowthPage { get; }
    }

    /// <summary>
    /// Hub geometry projected from the approved 402 x 874 construction grid.
    /// Drawing, pointer tracking, hit testing, and validation consume these same
    /// resolved rectangles; visual motion never mutates an input target.
    /// </summary>
    public static class PortraitHubLayout
    {
        public const float MinimumTargetLogical = 44f;

        private const float ReferenceWidth = PortraitShellLayout.ReferenceWidth;
        private const float ReferenceHeight = PortraitShellLayout.ReferenceHeight;

        public static HubLevelCardLayout CreateHomeLevelCard(Rect card,
            float scale)
        {
            if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
            {
                throw new ArgumentOutOfRangeException(nameof(scale),
                    "Hub level-card scale must be finite and positive.");
            }

            var frameHeight = Mathf.Min(102f * scale,
                card.height - 20f * scale);
            var frame = LogicalRect(card.x + 12f * scale,
                card.center.y - frameHeight * .5f, 136f * scale,
                frameHeight);
            var thumbnail = frame;
            var textX = card.x + 158f * scale;
            var textWidth = Mathf.Max(0f, card.xMax - textX - 10f * scale);
            var selectedMarker = LogicalRect(card.xMax - 54f * scale,
                card.y + 6f * scale, 48f * scale, 48f * scale);
            var transientIndicator = LogicalRect(frame.xMax - 28f * scale,
                frame.yMax - 28f * scale, 28f * scale, 28f * scale);
            return new HubLevelCardLayout(frame, thumbnail,
                LogicalRect(textX, card.y + 16f * scale,
                    Mathf.Max(0f, textWidth - 52f * scale), 48f * scale),
                LogicalRect(textX, card.y + 68f * scale,
                    textWidth, 40f * scale),
                selectedMarker, transientIndicator);
        }

        public static PortraitHubResolvedLayout Create(float viewportWidth,
            float viewportHeight, Rect safeArea)
        {
            var frame = PortraitShellLayout.CreateFrame(
                viewportWidth, viewportHeight, safeArea);
            var projection = new DesignProjection(frame);

            var topBar = projection.Rect(7f, 15f, 388f, 80f);
            var topBarContent = new HubTopBarLayout(
                projection.Rect(24f, 27f, 194f, 48f),
                projection.Rect(235f, 29f, 142f, 46f));
            var pageSurface = projection.Rect(11f, 103f, 386f, 690f);
            var navigationTray = projection.Rect(0f, 794f, 402f, 80f);
            var primaryNavigation = new HubPrimaryNavigationLayout(
                projection.Rect(16f, 794f, 118f, 80f),
                projection.Rect(142f, 794f, 118f, 80f),
                projection.Rect(268f, 794f, 118f, 80f));
            var homePage = new HubHomePageLayout(
                projection.Rect(28f, 122f, 350f, 132f),
                projection.Rect(27f, 267f, 351f, 124f),
                projection.Rect(27f, 404f, 351f, 124f),
                projection.Rect(24f, 555f, 354f, 221f),
                projection.Rect(57f, 700f, 289f, 56f));
            var activityPage = new HubActivityPageLayout(
                projection.Rect(27f, 122f, 351f, 654f),
                projection.Rect(58f, 130f, 286f, 54f),
                projection.Rect(58f, 188f, 286f, 60f),
                projection.Rect(43f, 382f, 319f, 176f),
                projection.Rect(59f, 388f, 287f, 36f),
                projection.Rect(59f, 428f, 138f, 118f),
                projection.Rect(207f, 428f, 139f, 118f),
                projection.Rect(62f, 566f, 278f, 52f),
                projection.Rect(52f, 252f, 302f, 124f),
                projection.Rect(66f, 641f, 270f, 57f),
                projection.Rect(72f, 576f, 32f, 32f));
            var growthNavigation = new GrowthSecondaryNavigationLayout(
                projection.Rect(24f, 106f, 173f, 52f),
                projection.Rect(205f, 106f, 173f, 52f));
            var growthPage = new HubGrowthPageLayout(growthNavigation,
                projection.Rect(27f, 174f, 351f, 116f),
                projection.Rect(47f, 190f, 211f, 36f),
                projection.Rect(47f, 234f, 287f, 36f),
                projection.Rect(27f, 302f, 351f, 474f),
                projection.Rect(47f, 322f, 287f, 36f),
                projection.Rect(47f, 366f, 311f, 60f),
                projection.Rect(47f, 438f, 311f, 40f),
                projection.Rect(47f, 486f, 311f, 48f),
                projection.Rect(47f, 546f, 311f, 48f),
                projection.Rect(47f, 606f, 311f, 56f),
                projection.Rect(109f, 707f, 184f, 55f),
                projection.Rect(115f, 723f, 171f, 51f),
                projection.Rect(334f, 314f, 28f, 28f));

            return new PortraitHubResolvedLayout(frame, topBar, topBarContent,
                pageSurface,
                navigationTray, primaryNavigation, homePage, activityPage,
                growthPage);
        }

        public static HubHitTarget HitTest(PortraitHubResolvedLayout layout,
            Vector2 guiPoint, HubPageId visiblePage, bool appTransitioning,
            GrowthPageId visibleGrowthPage = GrowthPageId.Equipment)
        {
            if (appTransitioning) return HubHitTarget.None;
            if (layout.PrimaryNavigation.Home.Contains(guiPoint))
                return HubHitTarget.Home;
            if (layout.PrimaryNavigation.Activity.Contains(guiPoint))
                return HubHitTarget.Activity;
            if (layout.PrimaryNavigation.Growth.Contains(guiPoint))
                return HubHitTarget.Growth;

            switch (visiblePage)
            {
                case HubPageId.Home:
                    if (layout.HomePage.Orchard01Card.Contains(guiPoint))
                        return HubHitTarget.LevelOrchard01;
                    if (layout.HomePage.Orchard02Card.Contains(guiPoint))
                        return HubHitTarget.LevelOrchard02;
                    if (layout.HomePage.Orchard03Card.Contains(guiPoint))
                        return HubHitTarget.LevelOrchard03;
                    if (layout.HomePage.StartButton.Contains(guiPoint))
                        return HubHitTarget.Start;
                    break;
                case HubPageId.Activity:
                    if (layout.ActivityPage.PrimaryAction.Contains(guiPoint))
                        return HubHitTarget.ActivityClaim;
                    break;
                case HubPageId.Growth:
                    if (layout.GrowthPage.Navigation.Equipment.Contains(guiPoint))
                        return HubHitTarget.Equipment;
                    if (layout.GrowthPage.Navigation.Cultivation.Contains(guiPoint))
                        return HubHitTarget.Cultivation;
                    if (layout.GrowthPage.EntryCard.Contains(guiPoint))
                        return visibleGrowthPage == GrowthPageId.Equipment
                            ? HubHitTarget.EquipmentEntry
                            : HubHitTarget.CultivationEntry;
                    if (layout.GrowthPage.PrimaryActionFor(
                            visibleGrowthPage).Contains(guiPoint))
                        return HubHitTarget.GrowthPrimaryAction;
                    break;
            }

            return HubHitTarget.None;
        }

        private static Rect LogicalRect(float x, float y, float width,
            float height)
        {
            return new Rect(x, y, Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private readonly struct DesignProjection
        {
            private readonly float _originX;
            private readonly float _originY;
            private readonly float _scale;

            public DesignProjection(PortraitShellFrame frame)
            {
                _scale = frame.Scale;
                _originX = frame.SafeArea.x
                    + (frame.SafeArea.width - ReferenceWidth * _scale) * .5f;
                _originY = frame.SafeArea.y
                    + (frame.SafeArea.height - ReferenceHeight * _scale) * .5f;
            }

            public Rect Rect(float x, float y, float width, float height)
            {
                return LogicalRect(_originX + x * _scale,
                    _originY + y * _scale, width * _scale, height * _scale);
            }
        }
    }
}
