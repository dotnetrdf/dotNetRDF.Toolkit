/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Windows.Forms;
using VDS.RDF.GUI.WinForms.Controls;
using VDS.RDF.Query;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    /// <summary>
    /// Extension to form class which provides a bunch of useful methods for doing cross thread invokes
    /// </summary>
    public class CrossThreadForm 
        : Form
    {
        #region Cross Thread Messaging

        private delegate void CrossThreadMessageDelegate(string message, string title, MessageBoxIcon icon);

        protected void CrossThreadMessage(string message, string title, MessageBoxIcon icon)
        {
            if (InvokeRequired)
            {
                CrossThreadMessageDelegate d = CrossThreadMessage;
                Invoke(d, new object[] { message, title, icon });
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            }
        }

#endregion

        #region Cross Thread Set Text properties

        private delegate void CrossThreadSetTextDelegate(Control c, string text);

        private delegate void CrossThreadSetToolStripTextDelegate(ToolStripItem item, string text);

        protected void CrossThreadSetText(Control c, string text)
        {
            if (InvokeRequired)
            {
                CrossThreadSetTextDelegate d = CrossThreadSetText;
                Invoke(d, new object[] { c, text });
            }
            else
            {
                c.Text = text;
            }
        }

        protected void CrossThreadSetText(ToolStripItem item, string text)
        {
            if (InvokeRequired)
            {
                CrossThreadSetToolStripTextDelegate d = CrossThreadSetText;
                Invoke(d, new object[] { item, text });
            }
            else
            {
                item.Text = text;
            }
        }

#endregion

        #region Cross Thread Set Visibility properties

        private delegate void CrossThreadSetVisibilityDelegate(Control c, bool visible);

        protected void CrossThreadSetVisibility(Control c, bool visible)
        {
            if (InvokeRequired)
            {
                CrossThreadSetVisibilityDelegate d = CrossThreadSetVisibility;
                Invoke(d, new object[] { c, visible });
            }
            else
            {
                c.Visible = visible;
            }
        }

#endregion

        #region Cross Thread Refresh

        private delegate void CrossThreadRefreshDelegate(Control c);

        protected void CrossThreadRefresh(Control c)
        {
            if (InvokeRequired)
            {
                CrossThreadRefreshDelegate d = CrossThreadRefresh;
                Invoke(d, new object[] { c });
            }
            else
            {
                c.Refresh();
            }
        }

#endregion

        #region Cross Thread ListView manipulation

        private delegate void CrossThreadBeginUpdateDelegate(ListView lview);

        protected void CrossThreadBeginUpdate(ListView lview)
        {
            if (InvokeRequired)
            {
                CrossThreadBeginUpdateDelegate d = CrossThreadBeginUpdate;
                Invoke(d, new object[] { lview });
            }
            else
            {
                lview.BeginUpdate();
            }
        }

        private delegate void CrossThreadEndUpdateDelegate(ListView lview);

        protected void CrossThreadEndUpdate(ListView lview)
        {
            if (InvokeRequired)
            {
                CrossThreadEndUpdateDelegate d = CrossThreadEndUpdate;
                Invoke(d, new object[] { lview });
            }
            else
            {
                lview.EndUpdate();
            }
        }

        private delegate void CrossThreadClearDelegate(ListView lview);

        protected void CrossThreadClear(ListView lview)
        {
            if (InvokeRequired)
            {
                CrossThreadClearDelegate d = CrossThreadClear;
                Invoke(d, new object[] { lview });
            }
            else
            {
                lview.Items.Clear();
            }
        }

        private delegate void CrossThreadAddDelegate(ListView lview, string item);

        protected void CrossThreadAdd(ListView lview, string item)
        {
            if (InvokeRequired)
            {
                CrossThreadAddDelegate d = CrossThreadAdd;
                Invoke(d, new object[] { lview, item });
            }
            else
            {
                lview.Items.Add(item);
            }
        }

        private delegate void CrossThreadAddItemDelegate(ListView lview, ListViewItem item);

        protected void CrossThreadAddItem(ListView lview, ListViewItem item)
        {
            if (InvokeRequired)
            {
                CrossThreadAddItemDelegate d = CrossThreadAddItem;
                Invoke(d, new object[] { lview, item });
            }
            else
            {
                lview.Items.Add(item);
            }
        }

        private delegate void CrossThreadAlterSubItemDelegate(ListViewItem item, int index, string text);

        protected void CrossThreadAlterSubItem(ListViewItem item, int index, string text)
        {
            if (InvokeRequired)
            {
                CrossThreadAlterSubItemDelegate d = CrossThreadAlterSubItem;
                Invoke(d, new object[] { item, index, text });
            }
            else
            {
                item.SubItems[index] = new ListViewItem.ListViewSubItem(item, text);
            }
        }

        #endregion

        #region Cross Thead ListBox manipulation

        private delegate object CrossThreadGetSelectedItemDelegate(ListBox lbox);

        protected object CrossThreadGetSelectedItem(ListBox lbox)
        {
            if (InvokeRequired)
            {
                CrossThreadGetSelectedItemDelegate d = CrossThreadGetSelectedItem;
                return Invoke(d, new object[] { lbox });
            }
            return lbox.SelectedItem;
        }

        private delegate int CrossThreadGetSelectedIndexDelegate(ListControl c);

        protected int CrossThreadGetSelectedIndex(ListControl c)
        {
            if (InvokeRequired)
            {
                CrossThreadGetSelectedIndexDelegate d = CrossThreadGetSelectedIndex;
                return (int)Invoke(d, new object[] { c });
            }
            return c.SelectedIndex;
        }

        #endregion

        #region Cross Thread Set Enabled

        private delegate void CrossThreadSetEnabledDelegate(Control c, bool enabled);

        protected void CrossThreadSetEnabled(Control c, bool enabled)
        {
            if (InvokeRequired)
            {
                CrossThreadSetEnabledDelegate d = CrossThreadSetEnabled;
                Invoke(d, new object[] { c, enabled });
            }
            else
            {
                c.Enabled = enabled;
            }
        }

        #endregion

        #region Cross Thread Progress Bars

        private delegate void CrossThreadUpdateProgressDelegate(ProgressBar prg, int value);

        protected void CrossThreadUpdateProgress(ProgressBar prg, int value)
        {
            if (InvokeRequired)
            {
                CrossThreadUpdateProgressDelegate d = CrossThreadUpdateProgress;
                Invoke(d, new object[] { prg, value });
            }
            else
            {
                prg.Value = value;
            }
        }

        private delegate void CrossThreadSetProgressMaximumDelegate(ProgressBar prg, int value);

        protected void CrossThreadSetProgressMaximum(ProgressBar prg, int value)
        {
            if (InvokeRequired)
            {
                CrossThreadSetProgressMaximumDelegate d = CrossThreadSetProgressMaximum;
                Invoke(d, new object[] { prg, value });
            }
            else
            {
                prg.Maximum = value;
            }
        }

        #endregion

        #region Cross Thread Form Management

        private delegate void CrossThreadSetQueryDelegate(
           RichTextBox txtSparqlQuery,
           string newQuery);

        protected void CrossThreadSetQuery(
            RichTextBox txtSparqlQuery,
            string newQuery)
        {
            if (InvokeRequired)
            {
                CrossThreadSetQueryDelegate d = new CrossThreadSetQueryDelegate(CrossThreadSetQuery);
                Invoke(d, new object[] { txtSparqlQuery, newQuery});
            }
            else
            {
                txtSparqlQuery.Text = newQuery;
            }
        }

        private delegate void CrossThreadLoadResultSetDelegate(
            ResultSetViewerControl resultSetViewerControl,
            SparqlResultSet results,
            INamespaceMapper nsmap,
            UserControl controlToHide);

        protected void CrossThreadSetResultSet(
            ResultSetViewerControl resultSetViewerControl,
            SparqlResultSet results,
            INamespaceMapper nsmap,
            UserControl controlToHide)
        {
            if (InvokeRequired)
            {
                CrossThreadLoadResultSetDelegate d = new CrossThreadLoadResultSetDelegate(CrossThreadSetResultSet);
                Invoke(d, new object[] { resultSetViewerControl, results, nsmap, controlToHide });
            }
            else
            {
                controlToHide.Hide();
                resultSetViewerControl.Show();
                resultSetViewerControl.Dock = DockStyle.Fill;
                resultSetViewerControl.DisplayResultSet(results, nsmap);
            }
        }

        private delegate void CrossThreadLoadGraphDelegate(
            GraphViewerControl graphViewerControl, 
            IGraph graph,
            UserControl controlToHide);

        protected void CrossThreadSetResultGraph(GraphViewerControl graphViewerControl, IGraph results, UserControl controlToHide)
        {
            if (InvokeRequired)
            {
                CrossThreadLoadGraphDelegate d = new CrossThreadLoadGraphDelegate(CrossThreadSetResultGraph);
                Invoke(d, new object[] { graphViewerControl, results, controlToHide });
            }
            else
            {
                controlToHide.Hide();
                graphViewerControl.Show();
                graphViewerControl.Dock = DockStyle.Fill;
                graphViewerControl.DisplayGraph(results);
            }
        }

        private delegate void CrossThreadSetMdiParentDelegate(Form f);

        /// <summary>
        /// Sets the MDI Parent of the given Form to be same as this forms MDI Parent
        /// </summary>
        /// <param name="f">Form</param>
        protected void CrossThreadSetMdiParent(Form f)
        {
            if (InvokeRequired)
            {
                CrossThreadSetMdiParentDelegate d = CrossThreadSetMdiParent;
                Invoke(d, new object[] { f });
            }
            else
            {
                f.MdiParent = MdiParent;
            }
        }

        private delegate void CrossThreadSetMdiParentDelegate2(Form f, Form parent);

        /// <summary>
        /// Sets the MDI Parent of the given form to be the given parent
        /// </summary>
        /// <param name="f">Form</param>
        /// <param name="parent">Parent</param>
        protected void CrossThreadSetMdiParent(Form f, Form parent)
        {
            if (InvokeRequired)
            {
                CrossThreadSetMdiParentDelegate2 d = CrossThreadSetMdiParent;
                Invoke(d, new object[] {f, parent});
            }
            else
            {
                f.MdiParent = parent;
            }
        }

        private delegate void CrossThreadShowQueryPanelDelegate(SplitContainer splitContainer);

        protected void CrossThreadShowQueryPanel(SplitContainer splitContainer)
        {
            if (InvokeRequired)
            {
                CrossThreadShowQueryPanelDelegate d = new CrossThreadShowQueryPanelDelegate(CrossThreadShowQueryPanel);
                Invoke(d, new object[] { splitContainer });
            }
            else
            {

                splitContainer.Panel2Collapsed = false;
            }
        }



        private delegate void CrossThreadShowDelegate(Form f);

        protected void CrossThreadShow(Form f)
        {
            if (InvokeRequired)
            {
                CrossThreadShowDelegate d = CrossThreadShow;
                Invoke(d, new object[] { f });
            }
            else
            {

                f.Show();
            }
        }

        private delegate void CrossThreadCloseDelegate(Form f);

        protected void CrossThreadClose(Form f)
        {
            if (InvokeRequired)
            {
                CrossThreadCloseDelegate d = CrossThreadClose;
                Invoke(d, new object[] { f });
            }
            else
            {
                f.Close();
            }
        }

        protected void CrossThreadClose()
        {
            CrossThreadClose(this);
        }

        #endregion
    }
}
