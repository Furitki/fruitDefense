using System;
using System.Collections.Generic;
using UnityEngine;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public enum PlantKind { Pea, Watermelon, Banana, Durian, Sunflower }
    public enum WeaponKind { None, Gatling, Ice, Chili }
    public enum ZombieKind { Normal, Runner, Armored, Boss }
    public enum GamePhase { Ready, Playing, BetweenWaves, Victory, Defeat }
    public enum PlantDropAction { Invalid, Cancel, Plant, Move, Merge }
    public enum CombatEffectKind { PeaImpact, WatermelonBlast, DurianDrop, SunBurst, GatlingMuzzle, IceImpact, ChiliImpact, HitSpark }

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
        public PlantKind Kind;
        public int Star = 1;
        public int PotId = -1;
        public int NurseryIndex = -1;
        public WeaponKind Weapon;
        public float AttackCooldown;
        public float ProductionProgress;
        public float MoveCooldown;
        public int BurstShotsRemaining;
        public float BurstShotCooldown;
        public Vector2 Facing = Vector2.right;
        public float ActionStartedAt;
        public float ActionUntil;
        public string EquipmentId = string.Empty;
        public override CombatFaction Faction { get { return CombatFaction.Player; } }
        public override bool IsAlive { get { return true; } }
    }

    [Serializable]
    public sealed class SkillRuntimeState
    {
        public string SkillId = string.Empty;
        public int CooldownTicks;
        public int PeriodicProgressTicks;
        public int BurstShotsRemaining;
        public int BurstIntervalTicks;
    }

    [Serializable]
    public sealed class Pot
    {
        public int Id;
        public Vector2Int Cell;
        public bool Active = true;
    }

    [Serializable]
    public sealed class BurnStack
    {
        public float Remaining;
        public float DamagePerSecond;
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
        public ZombieKind Kind;
        public float Hp;
        public float MaxHp;
        public float Speed;
        public float PathProgress;
        public int Reward;
        public int Threat;
        public float SlowUntil;
        public float FreezeUntil;
        public float HitStunUntil;
        public int IceHits;
        public readonly List<BurnStack> Burns = new List<BurnStack>();
        public override CombatFaction Faction { get { return CombatFaction.Enemy; } }
        public override bool IsAlive { get { return Hp > 0f; } }
    }

    [Serializable]
    public sealed class ProjectileFlash
    {
        public int Id;
        public int PlantId;
        public int TargetId = -1;
        public PlantKind Kind;
        public WeaponKind Weapon;
        public Vector2 Origin;
        public Vector2 Position;
        public Vector2 TargetPoint;
        public Vector2 Direction;
        public float MaxDistance;
        public float Progress;
        public bool Returning;
        public float Damage;
        public float Ttl;
        public readonly List<int> HitIds = new List<int>();
        public string ProjectileId = string.Empty;
        public string VisualId = string.Empty;
        public string ImpactCueId = string.Empty;
        public BattleProjectileMode Mode;
        public int TicksRemaining;
        public int FlightTicks;
    }

    [Serializable]
    public sealed class Inventory
    {
        public int Gatling;
        public int Ice;
        public int Chili;
        public int Pots;

        public int Get(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.Gatling: return Gatling;
                case WeaponKind.Ice: return Ice;
                case WeaponKind.Chili: return Chili;
                default: return 0;
            }
        }

        public void Add(WeaponKind kind, int amount)
        {
            switch (kind)
            {
                case WeaponKind.Gatling: Gatling += amount; break;
                case WeaponKind.Ice: Ice += amount; break;
                case WeaponKind.Chili: Chili += amount; break;
            }
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
        public int NextStatusSequence = 1;
        public long NextCombatEventSequence = 1;
        public readonly List<Plant> Plants = new List<Plant>();
        public readonly List<Pot> Pots = new List<Pot>();
        public readonly List<Zombie> Zombies = new List<Zombie>();
        public readonly List<ProjectileFlash> Projectiles = new List<ProjectileFlash>();
        public readonly Inventory Inventory = new Inventory();
    }
}
