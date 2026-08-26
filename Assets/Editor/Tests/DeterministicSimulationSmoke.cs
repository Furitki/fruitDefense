using System;
using System.IO;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class DeterministicSimulationSmoke
    {
        private static readonly string[] GameSimulationSourceOrder =
        {
            "GameSimulation.cs",
            "GameSimulation.Commands.cs",
            "GameSimulation.SimulationLoop.cs",
            "GameSimulation.Combat.cs",
            "GameSimulationMapIdentity.cs",
            "GameSimulationDeterministicProjection.cs",
            "GameSimulationCurrentSnapshotSource.cs",
            "GameSimulationCurrentSnapshotExport.cs",
            "GameSimulationCurrentSnapshotValidation.cs",
            "GameSimulationCurrentSnapshotRuntime.cs",
            "GameSimulationCurrentSnapshotRestore.cs",
        };

        public static void Run()
        {
            ValidateGameSimulationSourceAuthority();
            ValidateRandomState();
            ValidateSameSeedCommandReplay();
            ValidateFramePartitioning();
            ValidateSpeedEquivalence();
            ValidateBoundedCatchUpAndPause();
            ValidateSeededResetReplay();
            Debug.Log("Fruit Defense deterministic simulation validation passed.");
        }

        private static void ValidateGameSimulationSourceAuthority()
        {
            var sourceDirectory = Path.Combine(Application.dataPath, "Scripts", "Core");
            var physicalPaths = Directory.GetFiles(
                sourceDirectory,
                "GameSimulation*.cs",
                SearchOption.TopDirectoryOnly);
            var physicalNames = new string[physicalPaths.Length];
            for (var index = 0; index < physicalPaths.Length; index++)
                physicalNames[index] = Path.GetFileName(physicalPaths[index]);
            Array.Sort(physicalNames, StringComparer.Ordinal);

            var expectedNames = (string[])GameSimulationSourceOrder.Clone();
            Array.Sort(expectedNames, StringComparer.Ordinal);
            Assert(physicalNames.Length == expectedNames.Length,
                "GameSimulation source set matches the fixed authority whitelist");
            for (var index = 0; index < expectedNames.Length; index++)
            {
                Assert(string.Equals(physicalNames[index], expectedNames[index], StringComparison.Ordinal),
                    "GameSimulation source whitelist entry " + index + " is " + expectedNames[index]);
            }

            const string partialDeclaration = "public sealed partial class GameSimulation";
            for (var index = 0; index < GameSimulationSourceOrder.Length; index++)
            {
                var fileName = GameSimulationSourceOrder[index];
                var path = Path.Combine(sourceDirectory, fileName);
                var lines = File.ReadAllLines(path);
                Assert(lines.Length <= 900, fileName + " stays at or below 900 lines");
                Assert(RuntimeUiSourceAuthority.CountCSharpPartialDeclarations(
                        File.ReadAllText(path), partialDeclaration) == 1,
                    fileName + " contains exactly one GameSimulation partial declaration");
            }
        }

        private static void ValidateRandomState()
        {
            var zero = new DeterministicRandom(0);
            Assert(zero.State == DeterministicRandom.ZeroSeedState,
                "seed zero maps to the documented non-zero state");
            var random = new DeterministicRandom(123456);
            random.NextUInt();
            random.NextInt(3, 17);
            random.NextUnitDouble();
            var captured = random.State;
            var expected = random.NextUInt();
            random.RestoreState(captured);
            Assert(random.NextUInt() == expected, "captured random state restores the sequence");
        }

        private static void ValidateSameSeedCommandReplay()
        {
            var first = CreateCommandScenario(741852);
            var second = CreateCommandScenario(741852);
            for (var frame = 0; frame < 160; frame++)
            {
                var delta = frame % 3 == 0 ? .02f : .01f;
                first.AdvanceFrame(delta);
                second.AdvanceFrame(delta);
            }
            AssertChecksum(first, second, "same seed and command sequence replay");
        }

        private static void ValidateFramePartitioning()
        {
            var fine = CreateCommandScenario(112233);
            var coarse = CreateCommandScenario(112233);
            for (var frame = 0; frame < 100; frame++) fine.Tick(.01f);
            var coarseSteps = 0;
            for (var frame = 0; frame < 20; frame++) coarseSteps += coarse.AdvanceFrame(.05f);
            Assert(coarseSteps == 20, "twenty frames consume twenty fixed steps");
            AssertChecksum(fine, coarse, "frame partitioning");
        }

        private static void ValidateSpeedEquivalence()
        {
            var normal = CreateCommandScenario(334455);
            var fast = CreateCommandScenario(334455);
            fast.SetSpeed(2);
            for (var frame = 0; frame < 200; frame++) normal.AdvanceFrame(.01f);
            for (var frame = 0; frame < 100; frame++) fast.AdvanceFrame(.01f);
            fast.SetSpeed(1);
            AssertChecksum(normal, fast, "fixed-step speed equivalence");
        }

        private static void ValidateBoundedCatchUpAndPause()
        {
            var bounded = new GameSimulation(86420);
            bounded.SetSpeed(2);
            Assert(bounded.AdvanceFrame(10f) == GameSimulation.MaxStepsPerFrame,
                "long frames execute only the bounded number of steps");
            Assert(bounded.AdvanceFrame(0f) == 0 && ApproximatelyZero(bounded.FrameAccumulatorSeconds),
                "discarded stall time does not leak into later frames");

            var paused = new GameSimulation(97531);
            paused.AdvanceFrame(.04f);
            paused.TogglePause();
            Assert(paused.AdvanceFrame(.25f) == 0
                && ApproximatelyZero(paused.FrameAccumulatorSeconds)
                && Mathf.Approximately(paused.PresentationInterpolationFraction, 0f),
                "pause clears interpolation state");
        }

        private static void ValidateSeededResetReplay()
        {
            const int seed = 24681357;
            var reset = new GameSimulation(seed);
            reset.State.Sun = 999;
            Assert(reset.RefreshNursery(out _), "pre-reset nursery refresh succeeds");
            reset.AdvanceFrame(.03f);
            reset.Reset(seed);
            var fresh = new GameSimulation(seed);
            AssertChecksum(reset, fresh, "reset reconstructs seeded initial state");
        }

        private static GameSimulation CreateCommandScenario(int seed)
        {
            var simulation = new GameSimulation(seed);
            simulation.State.Sun = 999;
            Assert(simulation.RefreshNursery(out _), "deterministic nursery command succeeds");
            Assert(simulation.StartWave(out _), "deterministic wave command succeeds");
            return simulation;
        }

        private static void AssertChecksum(GameSimulation first, GameSimulation second, string label)
        {
            var left = first.OutcomeStateChecksum();
            var right = second.OutcomeStateChecksum();
            Assert(left == right, label + " (" + left + " != " + right + ")");
        }

        private static bool ApproximatelyZero(double value) => Math.Abs(value) < .0000001d;

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Deterministic simulation validation failed: " + message);
        }
    }
}
