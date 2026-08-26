using System;
using System.Collections.Generic;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    /// <summary>
    /// View-local previous/current samples for moving battle entities. A catch-up
    /// frame that completes multiple logic steps safely snaps instead of blending
    /// across an unknown intermediate path.
    /// </summary>
    public sealed class BattleRenderInterpolationSamples
    {
        private readonly Dictionary<int, float> _previousEnemyProgress =
            new Dictionary<int, float>();
        private readonly Dictionary<int, float> _currentEnemyProgress =
            new Dictionary<int, float>();
        private readonly Dictionary<int, Vector2> _previousProjectilePositions =
            new Dictionary<int, Vector2>();
        private readonly Dictionary<int, Vector2> _currentProjectilePositions =
            new Dictionary<int, Vector2>();
        private readonly List<int> _staleIds = new List<int>();
        private int _logicTick = -1;

        public int EnemySampleCount { get { return _currentEnemyProgress.Count; } }
        public int ProjectileSampleCount { get { return _currentProjectilePositions.Count; } }
        public int LogicTick { get { return _logicTick; } }

        public void Capture(GameSimulation simulation, int completedSteps)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            Capture(simulation.State, completedSteps);
        }

        public void Capture(GameState state, int completedSteps)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (_logicTick < 0)
            {
                SnapTo(state);
                return;
            }

            if (state.Paused || state.Phase == GamePhase.Victory
                || state.Phase == GamePhase.Defeat || completedSteps > 1)
            {
                SnapTo(state);
                return;
            }

            if (completedSteps <= 0 || state.LogicTick == _logicTick) return;
            ShiftEnemies(state);
            ShiftProjectiles(state);
            _logicTick = state.LogicTick;
        }

        public float EnemyPathProgress(int entityId, float authoritativeProgress,
            float interpolationFraction)
        {
            float previous;
            float current;
            return _previousEnemyProgress.TryGetValue(entityId, out previous)
                && _currentEnemyProgress.TryGetValue(entityId, out current)
                ? Mathf.Lerp(previous, current, SafeFraction(interpolationFraction))
                : authoritativeProgress;
        }

        public Vector2 ProjectilePosition(int entityId, Vector2 authoritativePosition,
            float interpolationFraction)
        {
            Vector2 previous;
            Vector2 current;
            return _previousProjectilePositions.TryGetValue(entityId, out previous)
                && _currentProjectilePositions.TryGetValue(entityId, out current)
                ? Vector2.Lerp(previous, current, SafeFraction(interpolationFraction))
                : authoritativePosition;
        }

        public void SnapTo(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _previousEnemyProgress.Clear();
            _currentEnemyProgress.Clear();
            foreach (var zombie in state.Zombies)
            {
                _previousEnemyProgress[zombie.Id] = zombie.PathProgress;
                _currentEnemyProgress[zombie.Id] = zombie.PathProgress;
            }
            _previousProjectilePositions.Clear();
            _currentProjectilePositions.Clear();
            foreach (var projectile in state.Projectiles)
            {
                _previousProjectilePositions[projectile.Id] = projectile.Position;
                _currentProjectilePositions[projectile.Id] = projectile.Position;
            }
            _logicTick = state.LogicTick;
        }

        public void Clear()
        {
            _previousEnemyProgress.Clear();
            _currentEnemyProgress.Clear();
            _previousProjectilePositions.Clear();
            _currentProjectilePositions.Clear();
            _logicTick = -1;
        }

        private void ShiftEnemies(GameState state)
        {
            _staleIds.Clear();
            foreach (var pair in _currentEnemyProgress)
                _staleIds.Add(pair.Key);
            foreach (var zombie in state.Zombies)
            {
                float current;
                if (!_currentEnemyProgress.TryGetValue(zombie.Id, out current))
                    current = zombie.PathProgress;
                _previousEnemyProgress[zombie.Id] = current;
                _currentEnemyProgress[zombie.Id] = zombie.PathProgress;
                _staleIds.Remove(zombie.Id);
            }
            foreach (var id in _staleIds)
            {
                _previousEnemyProgress.Remove(id);
                _currentEnemyProgress.Remove(id);
            }
        }

        private void ShiftProjectiles(GameState state)
        {
            _staleIds.Clear();
            foreach (var pair in _currentProjectilePositions)
                _staleIds.Add(pair.Key);
            foreach (var projectile in state.Projectiles)
            {
                Vector2 current;
                if (!_currentProjectilePositions.TryGetValue(projectile.Id, out current))
                    current = projectile.Position;
                _previousProjectilePositions[projectile.Id] = current;
                _currentProjectilePositions[projectile.Id] = projectile.Position;
                _staleIds.Remove(projectile.Id);
            }
            foreach (var id in _staleIds)
            {
                _previousProjectilePositions.Remove(id);
                _currentProjectilePositions.Remove(id);
            }
        }

        private static float SafeFraction(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Clamp01(value);
        }
    }
}
