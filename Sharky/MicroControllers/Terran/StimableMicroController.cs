﻿using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    public class StimableMicroController : IndividualMicroController
    {
        public StimableMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected bool Stiming(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER);
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.STIMPACK))
            {
                if (Stiming(commander)) // don't double stim
                {
                    return false;
                }

                if (commander.UnitCalculation.EnemiesInRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 100) // stim if more than 100 hitpoints in range
                {
                    action = commander.Order(frame, Abilities.EFFECT_STIM);
                    return true;
                }
            }

            return false;
        }

        protected override bool GetInBunker(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var nearbyBunkers = commander.UnitCalculation.NearbyAllies.Take(25).Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && u.Unit.BuildProgress == 1);
            foreach (var bunker in nearbyBunkers)
            {
                if (bunker.Unit.CargoSpaceMax - bunker.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.SMART, targetTag: bunker.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        protected override float GetMovementSpeed(UnitCommander commander)
        {
            if (Stiming(commander))
            {
                return base.GetMovementSpeed(commander) + 1.57f;
            }
            return base.GetMovementSpeed(commander);
        }

        protected override bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            if (bestTarget != null && Stiming(commander))
            {
                if (MoveToAttackTarget(commander, bestTarget, formation, frame, out action)) { return true; }
                return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
            }

            if (!(formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count() > 5))
            {
                if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        protected override bool MoveToAttackTarget(UnitCommander commander, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (bestTarget != null && Stiming(commander))
            {
                var attackPoint = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                if (MapDataService.PathWalkable(attackPoint))
                {
                    action = commander.Order(frame, Abilities.MOVE, attackPoint);
                    return true;
                }
            }

            return base.MoveToAttackTarget(commander, bestTarget, formation, frame, out action);
        }
    }
}
