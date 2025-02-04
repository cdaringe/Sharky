﻿using SC2APIProtocol;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public interface IMicroTask
    {
        float Priority { get; set; }
        List<UnitCommander> UnitCommanders { get; set; }
        void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders);
        IEnumerable<Action> PerformActions(int frame);
        void ResetClaimedUnits();
        void Enable();
        void Disable();
        bool Enabled { get; }
        double LongestFrame { get; set; }
        double TotalFrameTime { get; set; }

        void RemoveDeadUnits(List<ulong> deadUnits);
        void StealUnit(UnitCommander commander);
    }
}
