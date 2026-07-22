using System;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class DeterministicSimulationSmoke
    {
        [MenuItem("Fruit Defense/Validate Deterministic Simulation")]
        public static void Run()
        {
            ValidateRandomState();
            ValidateSameSeedCommandReplay();
            ValidateFramePartitioning();
            ValidateSpeedEquivalence();
            ValidateBoundedCatchUpAndPause();
            ValidateSeededResetReplay();
            Debug.Log("Fruit Defense deterministic simulation validation passed.");
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

            random.RestoreState(0u);
            Assert(random.State == DeterministicRandom.ZeroSeedState,
                "restoring zero cannot enter the absorbing xorshift state");
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

            Assert(coarseSteps == 20, "twenty 0.05 second frames consume twenty fixed steps");
            Assert(Mathf.Approximately(fine.State.Elapsed, 1f),
                "one hundred 0.01 second compatibility ticks consume one simulated second");
            AssertChecksum(fine, coarse, "100x0.01 and 20x0.05 frame partitioning");
        }

        private static void ValidateSpeedEquivalence()
        {
            var normal = CreateCommandScenario(334455);
            var fast = CreateCommandScenario(334455);
            normal.SetSpeed(1);
            fast.SetSpeed(2);
            for (var frame = 0; frame < 200; frame++) normal.AdvanceFrame(.01f);
            for (var frame = 0; frame < 100; frame++) fast.AdvanceFrame(.01f);
            fast.SetSpeed(1);

            Assert(Mathf.Approximately(normal.State.Elapsed, 2f)
                && Mathf.Approximately(fast.State.Elapsed, 2f),
                "1x for two seconds and 2x for one second consume forty fixed steps");
            AssertChecksum(normal, fast, "fixed-step speed equivalence");
        }

        private static void ValidateBoundedCatchUpAndPause()
        {
            var bounded = new GameSimulation(86420);
            bounded.SetSpeed(2);
            var steps = bounded.AdvanceFrame(10f);
            Assert(steps == GameSimulation.MaxStepsPerFrame
                && Mathf.Approximately(bounded.State.Elapsed,
                    GameSimulation.FixedStepSeconds * GameSimulation.MaxStepsPerFrame),
                "long frames execute at most five fixed steps");
            Assert(bounded.AdvanceFrame(0f) == 0 && ApproximatelyZero(bounded.FrameAccumulatorSeconds),
                "discarded stall time does not leak into later frames");
            Assert(bounded.AdvanceFrame(float.NaN) == 0 && bounded.AdvanceFrame(-1f) == 0,
                "invalid and negative frame deltas contribute no time");

            var paused = new GameSimulation(97531);
            Assert(paused.AdvanceFrame(.04f) == 0 && paused.FrameAccumulatorSeconds > 0d,
                "partial frame time is retained before pause");
            paused.TogglePause();
            Assert(paused.AdvanceFrame(.25f) == 0 && ApproximatelyZero(paused.FrameAccumulatorSeconds),
                "paused frames clear pending time");
            paused.TogglePause();
            Assert(paused.AdvanceFrame(.01f) == 0,
                "unpause does not consume stale pre-pause time");
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
            Assert(ApproximatelyZero(reset.FrameAccumulatorSeconds), "reset clears the frame accumulator");
            AssertChecksum(reset, fresh, "reset reconstructs seeded initial state");

            reset.State.Sun = 999;
            fresh.State.Sun = 999;
            Assert(reset.RefreshNursery(out _) && fresh.RefreshNursery(out _),
                "post-reset nursery refreshes succeed");
            AssertChecksum(reset, fresh, "reset replays flowerpot and nursery randomness");
        }

        private static GameSimulation CreateCommandScenario(int seed)
        {
            var simulation = new GameSimulation(seed);
            simulation.State.Sun = 999;
            Assert(simulation.RefreshNursery(out _), "deterministic nursery command succeeds");
            Assert(simulation.StartWave(out _), "deterministic wave command succeeds");
            return simulation;
        }

        private static void AssertChecksum(GameSimulation first, GameSimulation second, string message)
        {
            var firstChecksum = GameplayChecksum(first);
            var secondChecksum = GameplayChecksum(second);
            Assert(firstChecksum == secondChecksum,
                message + " (" + firstChecksum.ToString("X16") + " != " + secondChecksum.ToString("X16") + ")");
        }

        private static ulong GameplayChecksum(GameSimulation simulation)
        {
            var state = simulation.State;
            var hash = 14695981039346656037UL;
            Add(ref hash, (int)state.Phase);
            Add(ref hash, state.Paused);
            Add(ref hash, state.Speed);
            Add(ref hash, state.Elapsed);
            Add(ref hash, state.Sun);
            Add(ref hash, state.Lives);
            Add(ref hash, state.RefreshCount);
            Add(ref hash, state.WaveIndex);
            Add(ref hash, state.WaveSpawned);
            Add(ref hash, state.WaveTotal);
            Add(ref hash, state.SpawnCooldown);
            Add(ref hash, state.BetweenTimer);
            Add(ref hash, state.NextId);
            Add(ref hash, state.RandomSeed);
            Add(ref hash, state.LogicTick);
            Add(ref hash, state.NextStatusSequence);
            Add(ref hash, state.NextCombatEventSequence);
            Add(ref hash, simulation.RandomState);
            Add(ref hash, simulation.FrameAccumulatorSeconds);
            Add(ref hash, state.Inventory.Gatling);
            Add(ref hash, state.Inventory.Ice);
            Add(ref hash, state.Inventory.Chili);
            Add(ref hash, state.Inventory.Pots);

            foreach (var slot in simulation.LastNurseryPotSlots) Add(ref hash, slot);
            foreach (var pot in state.Pots)
            {
                Add(ref hash, pot.Id);
                Add(ref hash, pot.Cell.x);
                Add(ref hash, pot.Cell.y);
                Add(ref hash, pot.Active);
            }
            foreach (var plant in state.Plants)
            {
                Add(ref hash, plant.Id);
                Add(ref hash, (int)plant.Kind);
                Add(ref hash, plant.Star);
                Add(ref hash, plant.PotId);
                Add(ref hash, plant.NurseryIndex);
                Add(ref hash, (int)plant.Weapon);
                Add(ref hash, plant.AttackCooldown);
                Add(ref hash, plant.ProductionProgress);
                Add(ref hash, plant.MoveCooldown);
                Add(ref hash, plant.BurstShotsRemaining);
                Add(ref hash, plant.BurstShotCooldown);
                Add(ref hash, plant.Facing.x);
                Add(ref hash, plant.Facing.y);
                Add(ref hash, plant.ActionStartedAt);
                Add(ref hash, plant.ActionUntil);
                Add(ref hash, plant.ContentId);
                Add(ref hash, plant.EquipmentId);
                foreach (var runtime in plant.SkillRuntimes)
                {
                    Add(ref hash, runtime.SkillId);
                    Add(ref hash, runtime.CooldownTicks);
                    Add(ref hash, runtime.PeriodicProgressTicks);
                    Add(ref hash, runtime.BurstShotsRemaining);
                    Add(ref hash, runtime.BurstIntervalTicks);
                }
                AddEntityRuntime(ref hash, plant);
            }
            foreach (var zombie in state.Zombies)
            {
                Add(ref hash, zombie.Id);
                Add(ref hash, (int)zombie.Kind);
                Add(ref hash, zombie.Hp);
                Add(ref hash, zombie.MaxHp);
                Add(ref hash, zombie.Speed);
                Add(ref hash, zombie.PathProgress);
                Add(ref hash, zombie.Reward);
                Add(ref hash, zombie.Threat);
                Add(ref hash, zombie.SlowUntil);
                Add(ref hash, zombie.FreezeUntil);
                Add(ref hash, zombie.HitStunUntil);
                Add(ref hash, zombie.IceHits);
                Add(ref hash, zombie.ContentId);
                foreach (var runtime in zombie.PassiveRuntimes)
                {
                    Add(ref hash, runtime.PassiveId);
                    Add(ref hash, runtime.CooldownTicks);
                    Add(ref hash, runtime.LastRootEventSequence);
                }
                foreach (var status in zombie.Statuses)
                {
                    Add(ref hash, status.DefinitionId);
                    Add(ref hash, status.SourceEntityId);
                    Add(ref hash, status.RemainingTicks);
                    Add(ref hash, status.StackCount);
                    Add(ref hash, status.Magnitude);
                    Add(ref hash, status.Sequence);
                    Add(ref hash, status.TickProgress);
                }
                foreach (var burn in zombie.Burns)
                {
                    Add(ref hash, burn.Remaining);
                    Add(ref hash, burn.DamagePerSecond);
                }
            }
            foreach (var projectile in state.Projectiles)
            {
                Add(ref hash, projectile.Id);
                Add(ref hash, projectile.PlantId);
                Add(ref hash, projectile.TargetId);
                Add(ref hash, (int)projectile.Kind);
                Add(ref hash, (int)projectile.Weapon);
                Add(ref hash, projectile.Origin.x);
                Add(ref hash, projectile.Origin.y);
                Add(ref hash, projectile.Position.x);
                Add(ref hash, projectile.Position.y);
                Add(ref hash, projectile.TargetPoint.x);
                Add(ref hash, projectile.TargetPoint.y);
                Add(ref hash, projectile.Direction.x);
                Add(ref hash, projectile.Direction.y);
                Add(ref hash, projectile.MaxDistance);
                Add(ref hash, projectile.Progress);
                Add(ref hash, projectile.Returning);
                Add(ref hash, projectile.Damage);
                Add(ref hash, projectile.Ttl);
                Add(ref hash, projectile.ProjectileId);
                Add(ref hash, projectile.VisualId);
                Add(ref hash, projectile.ImpactCueId);
                Add(ref hash, (int)projectile.Mode);
                Add(ref hash, projectile.TicksRemaining);
                Add(ref hash, projectile.FlightTicks);
                foreach (var id in projectile.HitIds) Add(ref hash, id);
            }
            return hash;
        }

        private static void AddEntityRuntime(ref ulong hash, CombatEntityState entity)
        {
            foreach (var runtime in entity.PassiveRuntimes)
            {
                Add(ref hash, runtime.PassiveId);
                Add(ref hash, runtime.CooldownTicks);
                Add(ref hash, runtime.LastRootEventSequence);
            }
            foreach (var status in entity.Statuses)
            {
                Add(ref hash, status.DefinitionId);
                Add(ref hash, status.SourceEntityId);
                Add(ref hash, status.RemainingTicks);
                Add(ref hash, status.StackCount);
                Add(ref hash, status.Magnitude);
                Add(ref hash, status.Sequence);
                Add(ref hash, status.TickProgress);
            }
        }

        private static void Add(ref ulong hash, bool value) { Add(ref hash, value ? 1 : 0); }
        private static void Add(ref ulong hash, int value) { Add(ref hash, BitConverter.GetBytes(value)); }
        private static void Add(ref ulong hash, uint value) { Add(ref hash, BitConverter.GetBytes(value)); }
        private static void Add(ref ulong hash, long value) { Add(ref hash, BitConverter.GetBytes(value)); }
        private static void Add(ref ulong hash, float value) { Add(ref hash, BitConverter.GetBytes(value)); }
        private static void Add(ref ulong hash, double value) { Add(ref hash, BitConverter.GetBytes(value)); }
        private static void Add(ref ulong hash, string value)
        {
            value = value ?? string.Empty;
            Add(ref hash, value.Length);
            foreach (var character in value) Add(ref hash, (int)character);
        }

        private static void Add(ref ulong hash, byte[] bytes)
        {
            unchecked
            {
                foreach (var value in bytes)
                {
                    hash ^= value;
                    hash *= 1099511628211UL;
                }
            }
        }

        private static bool ApproximatelyZero(double value)
        {
            return Math.Abs(value) < 0.0000001;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Deterministic simulation validation failed: " + message);
        }
    }
}
