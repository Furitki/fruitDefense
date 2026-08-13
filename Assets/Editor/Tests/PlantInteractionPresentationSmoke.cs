using System;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class PlantInteractionPresentationSmoke
    {
        public static void Run()
        {
            ValidateMergePriorityAndBoardSwap();
            ValidateCrossLocationSwapAndCooldown();
            Assert(FruitDefenseGame.ValidatePlantPresentationResources(out var reason),
                "presentation resource contract: " + reason);
            Debug.Log("FRUIT_DEFENSE_PLANT_INTERACTION_PRESENTATION_OK");
        }

        private static void ValidateMergePriorityAndBoardSwap()
        {
            var simulation = new GameSimulation(7401);
            simulation.State.Plants.Clear();
            var firstPot = simulation.State.Pots[0];
            var secondPot = simulation.State.Pots[1];
            var thirdPot = simulation.State.Pots[2];
            var fourthPot = simulation.State.Pots[3];
            var first = Plant(9101, PlantKind.Pea, 2, firstPot.Id, -1, WeaponKind.Ice);
            var second = Plant(9102, PlantKind.Watermelon, 3, secondPot.Id, -1, WeaponKind.Chili);
            var mergeSource = Plant(9103, PlantKind.Banana, 1, thirdPot.Id, -1, WeaponKind.None);
            var mergeTarget = Plant(9104, PlantKind.Banana, 1, fourthPot.Id, -1, WeaponKind.Gatling);
            simulation.State.Plants.AddRange(new[] { first, second, mergeSource, mergeTarget });

            var swap = simulation.GetPlantDropStatus(first.Id, secondPot.Id);
            Assert(swap.Legal && swap.Action == PlantDropAction.Swap,
                "different occupied board plants resolve to swap");
            Assert(simulation.MoveOrMergePlant(first.Id, secondPot.Id, out _),
                "board swap commits");
            Assert(first.PotId == secondPot.Id && second.PotId == firstPot.Id
                && first.Star == 2 && second.Star == 3
                && first.Weapon == WeaponKind.Ice && second.Weapon == WeaponKind.Chili
                && simulation.State.Plants.Count == 4,
                "board swap preserves plant identity, stars, equipment, and count");

            var merge = simulation.GetPlantDropStatus(mergeSource.Id, fourthPot.Id);
            Assert(merge.Legal && merge.Action == PlantDropAction.Merge,
                "compatible occupied plants retain merge priority");
            Assert(simulation.MoveOrMergePlant(mergeSource.Id, fourthPot.Id, out _)
                && simulation.PlantById(mergeSource.Id) == null
                && mergeTarget.Star == 2,
                "compatible plant merge retains existing result");
        }

        private static void ValidateCrossLocationSwapAndCooldown()
        {
            var simulation = new GameSimulation(7402);
            simulation.State.Plants.Clear();
            var pot = simulation.State.Pots[0];
            var boardPlant = Plant(9201, PlantKind.Pea, 1, pot.Id, -1, WeaponKind.None);
            var nurseryPlant = Plant(9202, PlantKind.Durian, 3, -1, 0, WeaponKind.Ice);
            simulation.State.Plants.AddRange(new[] { boardPlant, nurseryPlant });

            var toNursery = simulation.GetNurseryDropStatus(boardPlant.Id, 0);
            Assert(toNursery.Legal && toNursery.Action == PlantDropAction.Swap,
                "board-to-nursery occupied destination resolves to swap");
            Assert(simulation.MoveToNursery(boardPlant.Id, 0, out _)
                && boardPlant.NurseryIndex == 0 && boardPlant.PotId < 0
                && nurseryPlant.PotId == pot.Id && nurseryPlant.NurseryIndex < 0,
                "cross-location swap exchanges complete locations");

            simulation.State.Phase = GamePhase.Playing;
            boardPlant.MoveCooldown = 0f;
            nurseryPlant.MoveCooldown = 1f;
            var blocked = simulation.GetPlantDropStatus(boardPlant.Id, pot.Id);
            Assert(!blocked.Legal && blocked.Action == PlantDropAction.Invalid,
                "target cooldown blocks an active-wave swap");
            Assert(boardPlant.NurseryIndex == 0 && nurseryPlant.PotId == pot.Id,
                "blocked swap leaves both positions unchanged");

            nurseryPlant.MoveCooldown = 0f;
            var allowed = simulation.GetPlantDropStatus(boardPlant.Id, pot.Id);
            Assert(allowed.Legal && allowed.Action == PlantDropAction.Swap
                && simulation.MoveOrMergePlant(boardPlant.Id, pot.Id, out _),
                "cross-location swap commits after cooldown clears");
            Assert(boardPlant.PotId == pot.Id && Mathf.Approximately(boardPlant.MoveCooldown, 0f)
                && nurseryPlant.NurseryIndex == 0 && Mathf.Approximately(nurseryPlant.MoveCooldown, 2f),
                "active-wave cooldown applies only to the plant moved from the board");
        }

        private static Plant Plant(int id, PlantKind kind, int star, int potId,
            int nurseryIndex, WeaponKind weapon)
        {
            return new Plant
            {
                Id = id,
                Kind = kind,
                Star = star,
                PotId = potId,
                NurseryIndex = nurseryIndex,
                Weapon = weapon,
            };
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Plant interaction presentation validation failed: " + message);
        }
    }
}
