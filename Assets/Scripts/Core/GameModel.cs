using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public enum GamePhase { Ready, Playing, BetweenWaves, Victory, Defeat }
    public enum PlantDropAction { Invalid, Cancel, Plant, Move, Swap, Merge }
    public enum AbilityRuntimePhase { Idle, Windup, Recovery }

    public struct InteractionStatus
    {
        public bool Legal;
        public string Reason;

        public InteractionStatus(bool legal, string reason)
        {
            Legal = legal;
            Reason = reason;
        }
    }

    public struct PlantDropStatus
    {
        public bool Legal;
        public PlantDropAction Action;
        public string Reason;

        public PlantDropStatus(bool legal, PlantDropAction action, string reason)
        {
            Legal = legal;
            Action = action;
            Reason = reason;
        }
    }

    [Serializable]
    public sealed class Plant : CombatEntityState
    {
        public int Star = 1;
        public int PotId = -1;
        public int NurseryIndex = -1;
        public float MoveCooldown;
        public string EquipmentId = string.Empty;
        public override CombatFaction Faction { get { return CombatFaction.Player; } }
        public override bool IsAlive { get { return true; } }
    }

    [Serializable]
    public sealed class AbilityRuntimeState
    {
        public string AbilityId = string.Empty;
        public AbilityRuntimePhase Phase;
        public int CooldownTicks;
        public int PeriodicProgressTicks;
        public int WindupTicksRemaining;
        public int RecoveryTicksRemaining;
        public int BurstShotsRemaining;
        public int BurstIntervalTicks;
        public int PendingSourceEntityId;
        public int PendingTargetEntityId;
        public float PendingEventMagnitude;
        public long PendingRootEventSequence;
        public long LastRootEventSequence;
    }

    [Serializable]
    public sealed class Pot
    {
        public int Id;
        public Vector2Int Cell;
        public bool Active = true;
    }

    [Serializable]
    public sealed class StatusInstance
    {
        public string DefinitionId = string.Empty;
        public int SourceEntityId;
        public int RemainingTicks;
        public int StackCount = 1;
        public float Magnitude;
        public int Sequence;
        public int TickProgress;
    }

    [Serializable]
    public sealed class Zombie : CombatEntityState
    {
        public string RouteId = string.Empty;
        public float Hp;
        public float MaxHp;
        public float Speed;
        public float PathProgress;
        public int Reward;
        public int Threat;
        public override CombatFaction Faction { get { return CombatFaction.Enemy; } }
        public override bool IsAlive { get { return Hp > 0f; } }
    }

    [Serializable]
    public sealed class ProjectileFlash
    {
        public int Id;
        public int SourceEntityId;
        public int TargetId = -1;
        public string SourceDefinitionId = string.Empty;
        public string SourceEquipmentId = string.Empty;
        public string AbilityId = string.Empty;
        public int DeliveryIndex;
        public Vector2 Origin;
        public Vector2 Position;
        public Vector2 TargetPoint;
        public Vector2 Direction;
        public float MaxDistance;
        public float Progress;
        public bool Returning;
        public float DamageBasis;
        public readonly List<int> HitIds = new List<int>();
        public string ProjectileId = string.Empty;
        public int TicksRemaining;
        public int FlightTicks;
    }

    [Serializable]
    public sealed class Inventory
    {
        private readonly Dictionary<string, int> equipment =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public int Pots;

        public int Gatling
        {
            get { return Get(BattleContentIds.Equipment.Gatling); }
            set { Set(BattleContentIds.Equipment.Gatling, value); }
        }

        public int Ice
        {
            get { return Get(BattleContentIds.Equipment.Ice); }
            set { Set(BattleContentIds.Equipment.Ice, value); }
        }

        public int Chili
        {
            get { return Get(BattleContentIds.Equipment.Chili); }
            set { Set(BattleContentIds.Equipment.Chili, value); }
        }

        public IReadOnlyList<KeyValuePair<string, int>> Equipment
        {
            get
            {
                return equipment.OrderBy(value => value.Key, StringComparer.Ordinal).ToArray();
            }
        }

        public int Get(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return 0;
            int count;
            return equipment.TryGetValue(definitionId, out count) ? count : 0;
        }

        public void Set(string definitionId, int count)
        {
            if (string.IsNullOrEmpty(definitionId))
                throw new ArgumentException("Equipment definition ID is required.", nameof(definitionId));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) equipment.Remove(definitionId);
            else equipment[definitionId] = count;
        }

        public void Add(string definitionId, int amount)
        {
            var next = Get(definitionId) + amount;
            if (next < 0) throw new InvalidOperationException(
                "Equipment inventory cannot become negative for '" + definitionId + "'.");
            Set(definitionId, next);
        }

    }

    [Serializable]
    public sealed class GameState
    {
        public GamePhase Phase;
        public bool Paused;
        public int Speed = 1;
        public float Elapsed;
        public int Sun;
        public int Lives;
        public int RefreshCount;
        public int WaveIndex;
        public int WaveSpawned;
        public int WaveTotal;
        public float SpawnCooldown;
        public float BetweenTimer;
        public int NextId = 1;
        public int RandomSeed;
        public int LogicTick;
        public int EscapedEnemies;
        public int NextStatusSequence = 1;
        public long NextCombatEventSequence = 1;
        public readonly List<Plant> Plants = new List<Plant>();
        public readonly List<Pot> Pots = new List<Pot>();
        public readonly List<Zombie> Zombies = new List<Zombie>();
        public readonly List<ProjectileFlash> Projectiles = new List<ProjectileFlash>();
        public readonly Inventory Inventory = new Inventory();
    }
}
