using FruitDefense.Development.GmStress;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmMultiLaneStressBattleSmoke
    {
        public static void Run()
        {
            GmMultiRouteDeterminismSmoke.Validate(GmStressBattleFactory.CreateMap);
            GmStressBattleLayoutSmoke.Validate();
            GmStressBattleControllerSmoke.Validate();
            GmStressBattleSessionSmoke.Validate();
            GmStressBattleIsolationSmoke.Validate(
                GmStressBattleIds.LevelId,
                GmStressBattleIds.MapId,
                WebBuild.GmStressOutputDirectory);
            Debug.Log("FRUIT_DEFENSE_GM_MULTI_LANE_STRESS_BATTLE_OK");
        }
    }
}
