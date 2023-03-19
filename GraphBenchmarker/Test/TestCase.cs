using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;

namespace VDS.RDF.Utilities.GraphBenchmarker.Test
{
    public class TestCase
    {
        private Type _graphType, _tripleCollectionType, _nodeCollectionType;
        private IGraph _instance;
        private BindingList<TestResult> _results = new BindingList<TestResult>();
        private long _initMemory = 0;

        public TestCase(Type graphType)
        {
            _graphType = graphType;
        }

        public TestCase(Type graphType, Type tripleCollectionType)
            : this(graphType)
        {
            _tripleCollectionType = tripleCollectionType;
        }

        public TestCase(Type graphType, Type tripleCollectionType, Type nodeCollectionType)
            : this(graphType, tripleCollectionType)
        {
            _nodeCollectionType = nodeCollectionType;
        }

        public IGraph Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_tripleCollectionType != null)
                    {
                        if (_nodeCollectionType == null)
                        {
                            _instance = (IGraph)Activator.CreateInstance(_graphType, new object[] { Activator.CreateInstance(_tripleCollectionType) });
                        }
                        else
                        {
                            _instance = (IGraph)Activator.CreateInstance(_graphType, new object[] { Activator.CreateInstance(_tripleCollectionType), Activator.CreateInstance(_nodeCollectionType) });
                        }
                    }
                    else if (_nodeCollectionType != null)
                    {
                        _instance = (IGraph)Activator.CreateInstance(_graphType, new object[] { Activator.CreateInstance(_nodeCollectionType) });
                    }
                    else
                    {
                        _instance = (IGraph)Activator.CreateInstance(_graphType);
                    }
                }
                return _instance;
            }
        }

        public BindingList<TestResult> Results
        {
            get
            {
                return _results;
            }
        }

        public long InitialMemory
        {
            get
            {
                return _initMemory;
            }
            set
            {
                _initMemory = value;
            }
        }

        public void Reset(bool clearResults)
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
                _initMemory = 0;
            }
            if (clearResults) _results.Clear();
        }

        public override string ToString()
        {
            if (_tripleCollectionType != null)
            {
                if (_nodeCollectionType != null)
                {
                    return _graphType.Name + " with " + _tripleCollectionType.Name + " and " + _nodeCollectionType.Name;
                }
                else
                {
                    return _graphType.Name + " with " + _tripleCollectionType.Name;
                }
            }
            else if (_nodeCollectionType != null)
            {
                return _graphType.Name + " with " + _nodeCollectionType.Name;
            }
            else
            {
                return _graphType.Name;
            }
        }
    }
}
