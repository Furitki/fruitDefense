using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public CombatEntityState EntityById(int id)
        {
            return (CombatEntityState)PlantById(id) ?? ZombieById(id);
        }

        public IReadOnlyList<CombatEntityState> CombatEntities()
        {
            return State.Plants.Cast<CombatEntityState>().Concat(State.Zombies)
                .OrderBy(entity => entity.Id).ToArray();
        }

        public float GetEffectiveAttribute(CombatEntityState entity, CombatAttributeKind attribute, float baseValue)
        {
            return CombatAttributeResolver.Resolve(baseValue, entity, attribute, _content);
        }

        public int RemoveStatuses(int entityId, string definitionId = "",
            CombatStatusPolarity? polarity = null, string tag = "")
        {
            var entity = EntityById(entityId);
            return entity == null ? 0 : CombatStatusRuntime.Remove(entity, _content, definitionId, polarity, tag);
        }

        public bool HasStatus(int entityId, string definitionId)
        {
            return StatusStackCount(entityId, definitionId) > 0;
        }

        public int StatusStackCount(int entityId, string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return 0;
            var entity = EntityById(entityId);
            return entity == null ? 0 : entity.Statuses
                .Where(value => value.RemainingTicks > 0
                    && string.Equals(value.DefinitionId, definitionId, StringComparison.Ordinal))
                .Sum(value => Math.Max(1, value.StackCount));
        }
        public Vector2 PotPoint(Pot pot)
        {
            return pot == null ? DefaultBattleAnchor() : Map.CellToMap(pot.Cell);
        }

        public PlantDropStatus GetPlantDropStatus(int plantId, int potId)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new PlantDropStatus(false, PlantDropAction.Invalid, "植物不存在");
            var pot = PotById(potId);
            if (pot == null || !pot.Active) return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不是可用花盆");
            if (plant.PotId == potId) return new PlantDropStatus(false, PlantDropAction.Cancel, "植物已在这个花盆中");
            if (State.Phase == GamePhase.Playing && plant.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "移动冷却 " + plant.MoveCooldown.ToString("0.0") + " 秒");

            var target = PlantAtPot(potId);
            if (target == null)
            {
                var action = plant.NurseryIndex >= 0 ? PlantDropAction.Plant : PlantDropAction.Move;
                return new PlantDropStatus(true, action, action == PlantDropAction.Plant ? "可种植" : "可移动");
            }
            if (target.DefinitionId == plant.DefinitionId
                && target.Star == plant.Star
                && target.Star < _content.PlantMaximumTier(target.DefinitionId))
                return new PlantDropStatus(true, PlantDropAction.Merge, "可合成为 " + (target.Star + 1) + " 星");
            if (State.Phase == GamePhase.Playing && target.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid,
                    "目标植物移动冷却 " + target.MoveCooldown.ToString("0.0") + " 秒");
            return new PlantDropStatus(true, PlantDropAction.Swap, "可交换位置");
        }

        public PlantDropStatus GetNurseryDropStatus(int plantId, int slot)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new PlantDropStatus(false, PlantDropAction.Invalid, "植物不存在");
            if (slot < 0 || slot >= _ruleSet.NurserySlotCount) return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不是苗圃槽位");
            if (plant.NurseryIndex == slot) return new PlantDropStatus(false, PlantDropAction.Cancel, "植物已在这个苗圃槽位中");
            if (State.Phase == GamePhase.Playing && plant.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "移动冷却 " + plant.MoveCooldown.ToString("0.0") + " 秒");

            var target = PlantAtNursery(slot);
            if (target == null)
                return new PlantDropStatus(true, PlantDropAction.Move, plant.PotId >= 0 ? "可放回苗圃" : "可移动到此槽位");
            if (target.DefinitionId == plant.DefinitionId
                && target.Star == plant.Star
                && target.Star < _content.PlantMaximumTier(target.DefinitionId))
                return new PlantDropStatus(true, PlantDropAction.Merge, "可合成为 " + (target.Star + 1) + " 星");
            if (State.Phase == GamePhase.Playing && target.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid,
                    "目标植物移动冷却 " + target.MoveCooldown.ToString("0.0") + " 秒");
            return new PlantDropStatus(true, PlantDropAction.Swap, "可交换位置");
        }

        public bool MoveOrMergePlant(int plantId, int potId, out string reason)
        {
            var plant = PlantById(plantId);
            var pot = PotById(potId);
            var status = GetPlantDropStatus(plantId, potId);
            if (plant == null || pot == null || !status.Legal) { reason = status.Reason; return false; }
            var target = PlantAtPot(potId);
            if (target == null)
            {
                var wasPlanted = plant.PotId >= 0;
                plant.PotId = potId;
                plant.NurseryIndex = -1;
                plant.AbilityRuntimes.Clear();
                if (wasPlanted && State.Phase == GamePhase.Playing)
                    plant.MoveCooldown = _ruleSet.RelocationCooldownSeconds;
                reason = wasPlanted ? "水果已移动" : "水果已种下";
                _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                    BattleContentIds.BattleStates.PlantMoved, PotPoint(pot), plant.Id);
                return true;
            }
            if (status.Action == PlantDropAction.Swap)
            {
                SwapPlantLocations(plant, target);
                reason = "植物已交换位置";
                _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                    BattleContentIds.BattleStates.PlantMoved, PotPoint(pot), plant.Id, 2);
                return true;
            }
            if (!string.IsNullOrEmpty(plant.EquipmentId)) State.Inventory.Add(plant.EquipmentId, 1);
            target.Star++;
            target.AbilityRuntimes.Clear();
            State.Plants.Remove(plant);
            reason = _content.Plants[target.DefinitionId].displayName + "升至 " + target.Star + " 星";
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.PlantMerged, PotPoint(pot), target.Star);
            return true;
        }

        public bool MoveToNursery(int plantId, int slot, out string reason)
        {
            var plant = PlantById(plantId);
            var status = GetNurseryDropStatus(plantId, slot);
            if (plant == null || !status.Legal) { reason = status.Reason; return false; }
            var target = PlantAtNursery(slot);
            if (target == null)
            {
                var returningDuringBattle = plant.PotId >= 0 && State.Phase == GamePhase.Playing;
                plant.PotId = -1;
                plant.NurseryIndex = slot;
                plant.AbilityRuntimes.Clear();
                if (returningDuringBattle)
                    plant.MoveCooldown = _ruleSet.RelocationCooldownSeconds;
                reason = status.Reason == "可放回苗圃" ? "水果已放回刷新栏" : "水果已移动到新槽位";
                return true;
            }
            if (status.Action == PlantDropAction.Swap)
            {
                SwapPlantLocations(plant, target);
                reason = "植物已交换位置";
                return true;
            }
            if (!string.IsNullOrEmpty(plant.EquipmentId)) State.Inventory.Add(plant.EquipmentId, 1);
            target.Star++;
            target.AbilityRuntimes.Clear();
            State.Plants.Remove(plant);
            reason = _content.Plants[target.DefinitionId].displayName + "升至 " + target.Star + " 星";
            return true;
        }

        private void SwapPlantLocations(Plant first, Plant second)
        {
            var firstPotId = first.PotId;
            var firstNurseryIndex = first.NurseryIndex;
            var secondPotId = second.PotId;
            var secondNurseryIndex = second.NurseryIndex;

            first.PotId = secondPotId;
            first.NurseryIndex = secondNurseryIndex;
            second.PotId = firstPotId;
            second.NurseryIndex = firstNurseryIndex;

            ResetAfterRelocation(first, firstPotId >= 0);
            ResetAfterRelocation(second, secondPotId >= 0);
        }

        private void ResetAfterRelocation(Plant plant, bool movedFromBoard)
        {
            plant.AbilityRuntimes.Clear();
            if (movedFromBoard && State.Phase == GamePhase.Playing)
                plant.MoveCooldown = _ruleSet.RelocationCooldownSeconds;
        }

        public bool InstallEquipment(int plantId, string equipmentId, out string reason)
        {
            var plant = PlantById(plantId);
            var status = GetEquipmentInstallStatus(plantId, equipmentId);
            if (plant == null || !status.Legal) { reason = status.Reason; return false; }
            State.Inventory.Add(equipmentId, -1);
            plant.EquipmentId = equipmentId;
            plant.AbilityRuntimes.Clear();
            reason = _content.Equipment[equipmentId].displayName + "安装成功";
            var point = plant.PotId >= 0 ? PotPoint(PotById(plant.PotId)) : DefaultBattleAnchor();
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.EquipmentInstalled, point, plant.Id, 1);
            return true;
        }

        public InteractionStatus GetEquipmentInstallStatus(int plantId, string equipmentId)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new InteractionStatus(false, "找不到这株植物");
            EquipmentDefinitionDto equipment;
            if (string.IsNullOrEmpty(equipmentId)
                || !_content.Equipment.TryGetValue(equipmentId, out equipment)
                || State.Inventory.Get(equipmentId) <= 0)
                return new InteractionStatus(false, "武器库存不足");
            if (!string.IsNullOrEmpty(plant.EquipmentId))
                return new InteractionStatus(false, "这株植物已经装备武器");
            if (!equipment.compatiblePlantIds.Contains(plant.DefinitionId))
                return new InteractionStatus(false, "武器与该植物不兼容");
            return new InteractionStatus(true, "可安装" + equipment.displayName);
        }

        public bool CanExpand(Vector2Int cell)
        {
            if (State.Inventory.Pots <= 0 || !Map.IsPlantable(cell)) return false;
            if (State.Pots.Any(pot => pot.Active && pot.Cell == cell)) return false;
            return State.Pots.Any(pot => pot.Active && Map.Topology.AreCardinalNeighbors(pot.Cell, cell));
        }

        public bool ExpandPot(Vector2Int cell, out string reason)
        {
            if (!CanExpand(cell)) { reason = "只能扩建到现有花盆的上下左右"; return false; }
            State.Inventory.Pots--;
            var potId = State.NextId;
            AddPot(cell);
            reason = "花盆扩建完成";
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.PotExpanded, Map.CellToMap(cell), potId);
            return true;
        }

    }
}
