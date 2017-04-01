﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Utilities.GraphBenchmarker.Test.Actual
{
    public class InitialMemoryUsageCheck : SingleRunTest
    {
        public InitialMemoryUsageCheck()
            : base("Initial Memory Usage", "Checks how much memory is currently allocated to determine how much memory the empty Graph and Triple Collection required")
        { }

        public override TestResult Run(TestCase testCase)
        {
            long init = testCase.InitialMemory;
            long current = GC.GetTotalMemory(false);
            return new TestResult(TimeSpan.Zero, current - init);
        }
    }

    public class MemoryUsageCheck : SingleRunTest
    {
        public MemoryUsageCheck()
            : base("Memory Usage", "Checks how much memory is currently allocated to determine how much memory a Test Case used up to the current point")
        { }

        public override TestResult Run(TestCase testCase)
        {
            long init = testCase.InitialMemory;
            long current = GC.GetTotalMemory(false);
            return new TestResult(TimeSpan.Zero, current - init);
        }
    }
}
