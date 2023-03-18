using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VDS.RDF.Utilities.GraphBenchmarker.Test.Actual;

namespace VDS.RDF.Utilities.GraphBenchmarker.Test
{
    public delegate void TestSuiteProgressHandler();

    public class TestSuite
    {
        private BindingList<ITest> _tests = new BindingList<ITest>();
        private BindingList<TestResult> _results = new BindingList<TestResult>();
        private BindingList<TestCase> _cases = new BindingList<TestCase>();
        private int _currTest = 0, _currTestCase = 0;
        private bool _cancelled = false;
        private string _data;
        private int _iterations;

        public TestSuite(IEnumerable<TestCase> testCases, string data, int iterations, TestSet set)
        {
            _data = data;
            _iterations = iterations;

            foreach (TestCase c in testCases)
            {
                _cases.Add(c);
            }

            //Firstly add the Initial Memory Usage Check
            _tests.Add(new InitialMemoryUsageCheck());

            //Then do a Load Test and a further Memory Usage Check
            _tests.Add(new LoadDataTest(data));
            _tests.Add(new CountTriplesTest());
            _tests.Add(new MemoryUsageCheck());

            //Then add the actual tests
            if (set == TestSet.Standard)
            {
                _tests.Add(new EnumerateTriplesTest(iterations));
                _tests.Add(new SubjectLookupTest(iterations));
                _tests.Add(new PredicateLookupTest(iterations));
                _tests.Add(new ObjectLookupTest(iterations));

                //Do an Enumerate Test again to see if index population has changed performance
                _tests.Add(new EnumerateTriplesTest(iterations));

                //Finally add the final Memory Usage Check
                _tests.Add(new MemoryUsageCheck());
            }
        }

        public string Data
        {
            get
            {
                return _data;
            }
        }

        public int Iterations
        {
            get
            {
                return _iterations;
            }
        }

        public BindingList<TestCase> TestCases
        {
            get
            {
                return _cases;
            }
        }

        public BindingList<ITest> Tests
        {
            get
            {
                return _tests;
            }
        }

        public int CurrentTest
        {
            get
            {
                return _currTest;
            }
        }

        public int CurrentTestCase
        {
            get
            {
                return _currTestCase;
            }
        }

        public void Run()
        {
            for (int c = 0; c < _cases.Count; c++)
            {
                if (_cancelled) break;

                _currTestCase = c;
                _currTest = 0;
                _cases[c].Reset(true);

                RaiseProgress();

                //Get the Initial Memory Usage allowing the GC to clean up as necessary
                _cases[c].InitialMemory = GC.GetTotalMemory(true);

                //Do this to ensure we've created the Graph instance
                IGraph temp = _cases[c].Instance;

                RaiseProgress();

                for (int t = 0; t < _tests.Count; t++)
                {
                    if (_cancelled) break;

                    _currTest = t;
                    RaiseProgress();

                    //Run the Test and remember the Results
                    TestResult r = _tests[t].Run(_cases[c]);
                    _cases[c].Results.Add(r);
                }

                //Clear URI Factory after Tests to return memory usage to base levels
                UriFactory.Clear();
            }
            RaiseProgress();
            if (_cancelled)
            {
                RaiseCancelled();
            }
            _cancelled = false;

            foreach (TestCase c in _cases)
            {
                c.Reset(false);
            }
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        public event TestSuiteProgressHandler Progress;

        public event TestSuiteProgressHandler Cancelled;

        private void RaiseProgress()
        {
            TestSuiteProgressHandler d = Progress;
            if (d != null)
            {
                d();
            }
        }

        private void RaiseCancelled()
        {
            TestSuiteProgressHandler d = Cancelled;
            if (d != null)
            {
                d();
            }
        }
    }
}
