using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using VDS.RDF.Utilities.GraphBenchmarker.Test;

namespace VDS.RDF.Utilities.GraphBenchmarker
{
    public partial class fclsGraphBenchmarker : Form
    {
        private BindingList<Type> _graphTypes = new BindingList<Type>();
        private BindingList<Type> _tripleCollectionTypes = new BindingList<Type>();
        private BindingList<TestCase> _testCases = new BindingList<TestCase>();
        private BindingList<string> _dataFiles = new BindingList<string>();

        public fclsGraphBenchmarker()
        {
            InitializeComponent();

            lstIGraphImpl.DataSource = _graphTypes;
            lstIGraphImpl.DisplayMember = "FullName";
            lstTripleCollectionImpl.DataSource = _tripleCollectionTypes;
            lstTripleCollectionImpl.DisplayMember = "FullName";
            lstTestCases.DataSource = _testCases;
            lstTestData.DataSource = _dataFiles;

            FindTypes(Assembly.GetAssembly(typeof(IGraph)));
            FindTestData("Data\\");
        }

        private void FindTypes(Assembly assm)
        {
            Type igraph = typeof(IGraph);
            Type tcol = typeof(BaseTripleCollection);

            foreach (Type t in assm.GetTypes())
            {
                if (t.GetInterfaces().Contains(igraph))
                {
                    if (IsTestableGraphType(t)) _graphTypes.Add(t);
                }
                else if (t.IsSubclassOf(tcol))
                {
                    if (IsTestableCollectionType(t)) _tripleCollectionTypes.Add(t);
                }
            }
        }

        private bool IsTestableGraphType(Type t)
        {
            return !t.IsAbstract && t.IsPublic && t.GetConstructors().Any(c => c.GetParameters().Length == 0 || (c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.Equals(typeof(BaseTripleCollection))));
        }

        private bool IsTestableCollectionType(Type t)
        {
            return !t.IsAbstract && t.IsPublic && t.GetConstructors().Any(c => c.GetParameters().Length == 0);
        }

        private void FindTestData(string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    string ext = MimeTypesHelper.GetTrueFileExtension(file);
                    if (MimeTypesHelper.Definitions.Any(d => d.SupportsFileExtension(ext)))
                    {
                        _dataFiles.Add(file);
                    }
                }
            }
        }

        private void chkUseDefault_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseDefault.Checked)
            {
                //Check whether it can be enabled
                if (lstIGraphImpl.SelectedItem != null)
                {
                    Type t = (Type)lstIGraphImpl.SelectedItem;
                    if (t.GetConstructors().Any(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.Equals(typeof(BaseTripleCollection))))
                    {
                        lstTripleCollectionImpl.Enabled = true;
                    }
                    else
                    {
                        chkUseDefault.Checked = false;
                        lstTripleCollectionImpl.Enabled = false;
                    }
                }
                else
                {
                    chkUseDefault.Checked = false;
                    lstTripleCollectionImpl.Enabled = false;
                }
            }
            else
            {
                lstTripleCollectionImpl.Enabled = false;
            }
        }

        private void lstIGraphImpl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkUseDefault.Checked)
            {
                chkUseDefault_CheckedChanged(sender, e);
            }
        }

        private void btnAddTestCase_Click(object sender, EventArgs e)
        {
            if (lstIGraphImpl.SelectedItem != null)
            {
                if (chkUseDefault.Checked && lstTripleCollectionImpl.SelectedItem != null)
                {
                    _testCases.Add(new TestCase((Type)lstIGraphImpl.SelectedItem, (Type)lstTripleCollectionImpl.SelectedItem));
                }
                else
                {
                    _testCases.Add(new TestCase((Type)lstIGraphImpl.SelectedItem));
                }
            }
        }

        private void btnRemoveTestCase_Click(object sender, EventArgs e)
        {
            if (lstTestCases.SelectedItem != null)
            {
                _testCases.Remove((TestCase)lstTestCases.SelectedItem);
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (_testCases.Count > 0)
            {
                if (lstTestData.SelectedItem != null)
                {
                    TestSet set = TestSet.Standard;
                    if (radLoadAndMem.Checked) set = TestSet.LoadAndMemory;

                    TestSuite suite = new TestSuite(_testCases, (string)lstTestData.SelectedItem, (int)numIterations.Value, set);
                    fclsTestRunner runner = new fclsTestRunner(suite);
                    runner.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Please selected Test Data to use...", "Test Data Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please create one/more Test Cases...", "Test Case(s) Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
