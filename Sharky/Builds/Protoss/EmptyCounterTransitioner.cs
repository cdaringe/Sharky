﻿using Sharky.Builds.BuildChoosing;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class EmptyCounterTransitioner : ICounterTransitioner
    {
        public EmptyCounterTransitioner()
        {
        }

        public List<string> DefaultCounterTransition(int frame)
        {
            return null;
        }
    }
}
