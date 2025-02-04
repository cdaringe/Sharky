﻿using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class MicroManager : SharkyManager
    {      
        ActiveUnitData ActiveUnitData;
        MicroTaskData MicroTaskData;
        SharkyOptions SharkyOptions;

        public MicroManager(ActiveUnitData activeUnitData, MicroTaskData microTaskData, SharkyOptions sharkyOptions)
        {
            ActiveUnitData = activeUnitData;
            MicroTaskData = microTaskData;
            SharkyOptions = sharkyOptions;
        }

        public override bool NeverSkip { get => true; }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            var actions = new List<Action>();
            foreach (var microTask in MicroTaskData.Values.Where(m => m.Enabled).OrderBy(m => m.Priority))
            {
                var begin = System.DateTime.UtcNow;
                microTask.RemoveDeadUnits(ActiveUnitData.DeadUnits);

                microTask.ClaimUnits(ActiveUnitData.Commanders);
                if (!SkipFrame)
                {
                    LogMissingCommanders(observation, microTask);

                    var taskActions = microTask.PerformActions(frame);

                    taskActions = FilterActions(observation, microTask, taskActions);

                    actions.AddRange(taskActions);
                }
                var end = System.DateTime.UtcNow;
                var time = (end - begin).TotalMilliseconds;
                microTask.TotalFrameTime += time;

                if (frame > 10 && SharkyOptions.LogPerformance && time > 1 && time > microTask.LongestFrame)
                {
                    microTask.LongestFrame = time;
                    System.Console.WriteLine($"{frame} {microTask.GetType().Name} {time} ms, average: {microTask.TotalFrameTime / frame} ms");
                }
            }
            if (SkipFrame)
            {
                SkipFrame = false;
            }
            return actions;
        }

        private static List<Action> FilterActions(ResponseObservation observation, MicroTasks.IMicroTask microTask, IEnumerable<Action> taskActions)
        {
            var filteredActions = new List<SC2APIProtocol.Action>();
            var tags = new List<ulong>();
            foreach (var action in taskActions)
            {
                if (action?.ActionRaw?.UnitCommand?.UnitTags == null)
                {
                    filteredActions.Add(action);
                }
                else if (action.ActionRaw.UnitCommand.UnitTags.All(tag => !observation.Observation.RawData.Units.Any(u => u.Tag == tag)))
                {
                    if (microTask.GetType().Name != "MiningTask") 
                    {
                        // System.Console.WriteLine($"{observation.Observation.GameLoop} {microTask.GetType().Name}, ignored uncontrollable unit order {action.ActionRaw.UnitCommand.AbilityId} for tags {string.Join(" ", action.ActionRaw.UnitCommand.UnitTags)}");
                    }
                }
                else if (!action.ActionRaw.UnitCommand.QueueCommand)
                {
                    if (!tags.Any(tag => action.ActionRaw.UnitCommand.UnitTags.Any(t => t == tag)))
                    {
                        filteredActions.Add(action);
                        tags.AddRange(action.ActionRaw.UnitCommand.UnitTags);
                    }
                    else
                    {
                        // System.Console.WriteLine($"{observation.Observation.GameLoop} {microTask.GetType().Name}, ignored conflicting order {action.ActionRaw.UnitCommand.AbilityId} for tags {string.Join(" ", action.ActionRaw.UnitCommand.UnitTags)}");
                    }
                }
                else
                {
                    filteredActions.Add(action);
                }
            }

            return filteredActions;
        }

        void LogMissingCommanders(ResponseObservation observation, MicroTasks.IMicroTask microTask)
        {
            return;
            if (microTask.GetType().Name == "MiningTask") { return; }
            var missingCommanders = microTask.UnitCommanders.Where(c => !observation.Observation.RawData.Units.Any(u => u.Tag == c.UnitCalculation.Unit.Tag));
            foreach (var missingCommander in missingCommanders)
            {
                System.Console.WriteLine($"{observation.Observation.GameLoop} {microTask.GetType().Name}, missing {missingCommander.UnitCalculation.Unit.UnitType}, tag {missingCommander.UnitCalculation.Unit.Tag}");
            }
        }
    }
}
