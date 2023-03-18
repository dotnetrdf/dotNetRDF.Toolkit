using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VDS.RDF.Utilities.GraphBenchmarker.Test
{
    public enum TestMetricType
    {
        MemoryUsage,
        Speed,
        Count
    }

    public class TestResult
    {
        private TimeSpan _elapsed;
        private int _actions;
        private string _unit;
        private double _memory = 0d;
        private TestMetricType _metric;

        public TestResult(TimeSpan elapsed, int actions, string unit, TestMetricType metric)
        {
            _elapsed = elapsed;
            _actions = actions;
            _unit = unit;
            _metric = metric;
        }

        public TestResult(TimeSpan elapsed, long memory)
            : this(elapsed, 0, "Bytes", TestMetricType.MemoryUsage)
        {
            _memory = (double)memory;

            //Convert up to KB if possible
            if (_memory > 1024d)
            {
                _memory /= 1024d;
                _unit = "Kilobytes";

                //Convert up to MB if possible
                if (_memory > 1024d)
                {
                    _memory /= 1024d;
                    _unit = "Megabytes";

                    //Convert up to GB if possible
                    if (_memory > 1024d)
                    {
                        _memory /= 1024d;
                        _unit = "Gigabytes";
                    }
                }
            }
        }

        public TimeSpan Elapsed
        {
            get
            {
                return _elapsed;
            }
        }

        public int Actions
        {
            get
            {
                return _actions;
            }
        }

        public string Unit
        {
            get
            {
                return _unit;
            }
        }

        public double Speed
        {
            get
            {
                if (_actions > 0)
                {
                    double seconds = ((double)_elapsed.TotalMilliseconds) / 1000d;
                    return ((double)_actions) / seconds;
                }
                else
                {
                    return double.NaN;
                }
            }
        }

        public double Memory
        {
            get
            {
                return _memory;
            }
        }

        public override string ToString()
        {
            switch (_metric)
            {
                case TestMetricType.Speed:
                    return Speed.ToString("N3") + " " + Unit;
                case TestMetricType.MemoryUsage:
                    return Memory.ToString("F3") + " " + Unit + "";
                case TestMetricType.Count:
                    return Actions.ToString("F3") + " " + Unit + "";
                default:
                    return "Unknown";
            }
        }
    }
}
