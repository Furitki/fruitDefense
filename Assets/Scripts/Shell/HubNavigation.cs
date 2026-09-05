using System;

namespace FruitDefense.Shell
{
    public enum HubPageId
    {
        Home = 0,
        Activity = 1,
        Growth = 2,
    }

    public enum GrowthPageId
    {
        Equipment = 0,
        Cultivation = 1,
    }

    public enum HubActivityState
    {
        Available = 0,
        Claimable = 1,
        Claiming = 2,
        Claimed = 3,
        Locked = 4,
        Error = 5,
        InsufficientContext = 6,
    }

    public enum HubGrowthState
    {
        Owned = 0,
        Equipped = 1,
        Upgradeable = 2,
        Insufficient = 3,
        Locked = 4,
        Maximum = 5,
        Loading = 6,
        Success = 7,
        Error = 8,
    }

    public enum HubGrowthPrimaryAction
    {
        None = 0,
        Equip = 1,
        UpgradeEquipment = 2,
        UpgradeCultivation = 3,
    }

    /// <summary>
    /// Pure, finite navigation state owned by one Lobby lifetime. It has no
    /// reference to application routing, scenes, content, or player data.
    /// </summary>
    public sealed class HubPageRouter
    {
        public HubPageRouter()
        {
            CurrentPage = HubPageId.Home;
            CurrentGrowthPage = GrowthPageId.Equipment;
        }

        public HubPageId CurrentPage { get; private set; }
        public GrowthPageId CurrentGrowthPage { get; private set; }
        public int Revision { get; private set; }

        public bool TrySelectPage(HubPageId page)
        {
            if (!Enum.IsDefined(typeof(HubPageId), page) || page == CurrentPage)
                return false;

            CurrentPage = page;
            Revision++;
            return true;
        }

        public bool TrySelectGrowthPage(GrowthPageId page)
        {
            if (!Enum.IsDefined(typeof(GrowthPageId), page)
                || page == CurrentGrowthPage)
            {
                return false;
            }

            CurrentGrowthPage = page;
            Revision++;
            return true;
        }

        public bool ResetToHome()
        {
            if (CurrentPage == HubPageId.Home) return false;
            CurrentPage = HubPageId.Home;
            Revision++;
            return true;
        }
    }
}
