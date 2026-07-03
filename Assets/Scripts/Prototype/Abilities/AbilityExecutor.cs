using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class AbilityExecutor
    {
        private readonly Action<string> log;

        public AbilityExecutor(Action<string> log)
        {
            this.log = log;
        }

        public bool Execute(Actor caster, AbilityDefinition ability, GridMap map)
        {
            if (caster == null || ability == null || map == null || !caster.IsAlive)
            {
                return false;
            }

            if (!caster.SpendMp(ability.MpCost))
            {
                log?.Invoke($"{caster.ActorName} does not have enough MP.");
                return false;
            }

            switch (ability.Kind)
            {
                case AbilityKind.ForwardAttack:
                    ExecuteForwardAttack(caster, ability, map);
                    return true;
                case AbilityKind.AreaAttack:
                    ExecuteAreaAttack(caster, ability, map);
                    return true;
                case AbilityKind.Heal:
                    ExecuteHeal(caster, ability);
                    return true;
                default:
                    return false;
            }
        }

        private void ExecuteForwardAttack(Actor caster, AbilityDefinition ability, GridMap map)
        {
            GridPosition offset = caster.Direction.ToGridOffset();
            int range = Mathf.Max(1, ability.Range);
            for (int step = 1; step <= range; step++)
            {
                GridPosition targetPosition = new GridPosition(
                    caster.Position.X + offset.X * step,
                    caster.Position.Y + offset.Y * step);
                Actor target = map.GetActorAt(targetPosition);
                if (target == null || !target.IsOpponentOf(caster))
                {
                    continue;
                }

                DealDamage(caster, target, ability);
                return;
            }

            log?.Invoke($"{caster.ActorName} uses {ability.DisplayName}, but hits nothing.");
        }

        private void ExecuteAreaAttack(Actor caster, AbilityDefinition ability, GridMap map)
        {
            List<Actor> targets = map.GetOpponentsInArea(caster, caster.Position, Mathf.Max(1, ability.Radius)).ToList();
            if (targets.Count == 0)
            {
                log?.Invoke($"{caster.ActorName} uses {ability.DisplayName}, but no target is nearby.");
                return;
            }

            foreach (Actor target in targets)
            {
                DealDamage(caster, target, ability);
            }
        }

        private void ExecuteHeal(Actor caster, AbilityDefinition ability)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(caster.GetTotalStats().Attack * (ability.PowerPercent / 100f)));
            int applied = caster.Heal(healAmount);
            log?.Invoke($"{caster.ActorName} uses {ability.DisplayName} and heals {applied} HP.");
        }

        private void DealDamage(Actor caster, Actor target, AbilityDefinition ability)
        {
            DamageResult result = DamageCalculator.Calculate(caster, target, ability.PowerPercent, ability.ElementType);
            if (result.IsEvaded)
            {
                log?.Invoke($"{target.ActorName} evades {ability.DisplayName}.");
                return;
            }

            target.TakeDamage(result.Amount);
            string criticalText = result.IsCritical ? " Critical!" : string.Empty;
            log?.Invoke($"{caster.ActorName} uses {ability.DisplayName} on {target.ActorName}: {result.Amount} damage.{criticalText}");
        }
    }
}
