using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VDS.RDF.Utilities.GraphBenchmarker.Test;
using VDS.RDF.Utilities.StoreManager.Forms;

namespace VDS.RDF.Utilities.GraphBenchmarker
{
    internal delegate void RunTestSuiteDelegate();

    public partial class fclsTestRunner : CrossThreadForm
    {
        private TestSuite _suite;
        private bool _cancelled = false, _hasCancelled = false, _hasFinished = false;
        private RunTestSuiteDelegate _d;

        public fclsTestRunner(TestSuite suite)
        {
            InitializeComponent();
            _suite = suite;
            _suite.Progress += new TestSuiteProgressHandler(_suite_Progress);
            _suite.Cancelled += new TestSuiteProgressHandler(_suite_Cancelled);

            lstTestCases.DataSource = _suite.TestCases;
            lstTests.DataSource = _suite.Tests;
            lstTests.DisplayMember = "Name";

            prgTests.Minimum = 0;
            prgTests.Maximum = _suite.TestCases.Count * _suite.Tests.Count;
            prgTests.Value = 0;

            Shown += new EventHandler(fclsTestRunner_Shown);
        }

        void _suite_Cancelled()
        {
            _hasCancelled = true;
            CrossThreadSetEnabled(btnCancel, true);
            CrossThreadSetText(lblProgress, "Test Suite cancelled");
        }

        void _suite_Progress()
        {
            int progress = (_suite.CurrentTestCase * _suite.Tests.Count) + _suite.CurrentTest;
            CrossThreadUpdateProgress(prgTests, progress);
            if (!_cancelled)
            {
                CrossThreadSetText(lblProgress, "Running Test " + (_suite.CurrentTest + 1) + " of " + _suite.Tests.Count + " for Test Case " + (_suite.CurrentTestCase + 1) + " of " + _suite.TestCases.Count);
            }
            ShowTestResult();
        }

        void fclsTestRunner_Shown(object sender, EventArgs e)
        {
            _d = new RunTestSuiteDelegate(_suite.Run);
            _d.BeginInvoke(new AsyncCallback(TestsCompleteCallback), null);
        }

        private void lstTestCases_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowTestResult();
        }

        private void lstTests_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowTestInformation();
            ShowTestResult();
        }

        private void ShowTestInformation()
        {
            if (lstTests.SelectedItem != null)
            {
                ITest test = (ITest)lstTests.SelectedItem;
                grpTestInfo.Text = "Test Information - " + test.Name;
                txtDescription.Text = test.Description;
            }
            else
            {
                grpTestInfo.Text = "Test Information";
                txtDescription.Text = "No Test currently selected";
            }
        }

        private void ShowTestResult()
        {
            if (CrossThreadGetSelectedItem(lstTests) != null)
            {
                object item = CrossThreadGetSelectedItem(lstTestCases);
                if (item != null)
                {
                    TestCase testCase = (TestCase)item;
                    int i = CrossThreadGetSelectedIndex(lstTests);
                    if (i < testCase.Results.Count)
                    {
                        TestResult r = testCase.Results[i];
                        StringBuilder output = new StringBuilder();
                        output.AppendLine("Total Elapsed Time: " + r.Elapsed.ToString());
                        output.AppendLine("Performance Metric: " + r.ToString());
                        CrossThreadSetText(txtResults, output.ToString());
                    }
                    else
                    {
                        CrossThreadSetText(txtResults, "Test has not yet been run for the currently selected Test Case");
                    }
                }
                else
                {
                    CrossThreadSetText(txtResults, "No Test Case currently selected");
                }
            }
            else
            {
                CrossThreadSetText(txtResults, "No Test currently selected");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (!_cancelled && !_hasFinished)
            {
                _cancelled = true;
                _suite.Cancel();
                lblProgress.Text = "Waiting for Tests to cancel...";
                btnCancel.Text = "Close";
                btnCancel.Enabled = false;
            }
            else if (_hasCancelled || _hasFinished)
            {
                Close();
            }
        }

        private void TestsCompleteCallback(IAsyncResult result)
        {
            _hasFinished = true;
            try
            {
                _d.EndInvoke(result);
                if (_cancelled)
                {
                    CrossThreadMessage("Test Suite cancelled", "Tests Cancelled", MessageBoxIcon.Information);
                }
                else
                {
                    CrossThreadUpdateProgress(prgTests, prgTests.Maximum);
                    CrossThreadMessage("Test Suite completed successfully", "Tests Completed", MessageBoxIcon.Information);
                }
                CrossThreadSetText(btnCancel, "Close");
            }
            catch (Exception ex)
            {
                CrossThreadMessage("Test Suite failed due to the following error: " + ex.Message, "Tests Failed", MessageBoxIcon.Error);
            }
            CrossThreadSetText(btnCancel, "Close");
            CrossThreadSetEnabled(btnExport, true);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                string file = sfdExport.FileName;
                try
                {
                    using (StreamWriter writer = new StreamWriter(file))
                    {
                        //First write a couple of Rows detailing the Test Setup
                        writer.WriteLine("Test Data," + _suite.Data);
                        writer.WriteLine("Test Iterations," + _suite.Iterations);
                        writer.WriteLine(",");

                        //Then write Header Row listing the Test Cases
                        writer.WriteLine(",Test Cases");
                        writer.Write(',');
                        foreach (TestCase c in _suite.TestCases)
                        {
                            writer.Write(c.ToString());
                            writer.Write(',');
                        }
                        writer.WriteLine();

                        //Then for each Test dump the results
                        for (int i = 0; i < _suite.Tests.Count; i++)
                        {
                            //First remember to show the Test Name in the Leftmost column
                            writer.Write(_suite.Tests[i].Name);
                            writer.Write(',');

                            //Then show a Result for each Test Case
                            foreach (TestCase c in _suite.TestCases)
                            {
                                if (i < c.Results.Count)
                                {
                                    TestResult r = c.Results[i];
                                    if (r.Actions > 0)
                                    {
                                        writer.Write(r.Speed);
                                    }
                                    else
                                    {
                                        writer.Write(r.Memory);
                                    }
                                }
                                else
                                {
                                    writer.Write("N/A,");
                                }
                                writer.Write(',');
                            }
                            writer.WriteLine();
                        }

                        writer.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Exporting Results: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
