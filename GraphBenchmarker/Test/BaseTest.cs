using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Utilities.GraphBenchmarker.Test
{
    public abstract class BaseTest : ITest
    {
        private TestType _type;

        public BaseTest(string name, string description, TestType type)
        {
            Name = name;
            Description = description;
            _type = type;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public TestType Type
        {
            get
            {
                return _type;
            }
        }

        public abstract TestResult Run(TestCase testCase);
    }

    public abstract class SingleRunTest : BaseTest
    {
        public SingleRunTest(string name, string description)
            : base(name, description, TestType.SingleRun)
        { }
    }

    public abstract class IterationTest : BaseTest
    {
        private int _iterations;
        private string _unit;

        public IterationTest(string name, string description, int iterations, string unit)
            : base(name, description, TestType.Iterations)
        {
            _iterations = Math.Max(1000, iterations);
            _unit = unit;
        }

        /// <summary>
        /// Allows for actions to be taken prior to iterations which don't count towards the Benchmarked score
        /// </summary>
        /// <param name="testCase"></param>
        protected virtual void PreIterationSetup(TestCase testCase)
        {

        }

        public override TestResult Run(TestCase testCase)
        {
            PreIterationSetup(testCase);

            DateTime start = DateTime.Now;
            int actions = 0;
            for (int i = 0; i < _iterations; i++)
            {
                actions += RunIteration(testCase);
            }
            TimeSpan elapsed = DateTime.Now - start;

            return new TestResult(elapsed, actions, _unit, TestMetricType.Speed);
        }

        protected abstract int RunIteration(TestCase testCase);
    }
}
