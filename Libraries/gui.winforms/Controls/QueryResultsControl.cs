using System;
using System.ComponentModel;
using System.Windows.Forms;
using VDS.RDF.Query;

namespace VDS.RDF.GUI.WinForms.Controls
{
    /// <summary>
    /// A control for displaying query results
    /// </summary>
    public partial class QueryResultsControl : UserControl
    {
        private object _dataSource;
        private bool _allowDetach = true;

        /// <summary>
        /// Query Results Control
        /// </summary>
        public QueryResultsControl()
        {
            InitializeComponent();
            splPanel.Panel1Collapsed = true;
            splResults.Visible = false;

            // Only detachable if allowed and not already top level control on the form
            btnDetach.Visible = AllowDetach && !(Parent is Form);
        }

        private void btnToggleQuery_Click(object sender, EventArgs e)
        {
            splPanel.Panel1Collapsed = !splPanel.Panel1Collapsed;
            btnToggleQuery.Text = splPanel.Panel1Collapsed ? "Show &Query" : "Hide &Query";
            btnToggleResults.Text = splPanel.Panel2Collapsed ? "Show &Results" : "Hide &Results";
        }

        private void btnToggleResults_Click(object sender, EventArgs e)
        {
            splPanel.Panel2Collapsed = !splPanel.Panel2Collapsed;
            btnToggleQuery.Text = splPanel.Panel1Collapsed ? "Show &Query" : "Hide &Query";
            btnToggleResults.Text = splPanel.Panel2Collapsed ? "Show &Results" : "Hide &Results";
        }

        /// <summary>
        /// Gets/Sets whether detaching results is allowing
        /// </summary>
        [DefaultValue(true)]
        public bool AllowDetach
        {
            get { return _allowDetach; }
            set
            {
                _allowDetach = value;
                btnDetach.Visible = _allowDetach;
            }
        }

        /// <summary>
        /// Get/Sets the namespaces to use
        /// </summary>
        public INamespaceMapper Namespaces { get; set; }

        /// <summary>
        /// Gets/Sets the query string
        /// </summary>
        public string QueryString
        {
            get { return txtQuery.Text; }
            set { txtQuery.Text = value.Replace("\n", "\r\n"); }
        }

        /// <summary>
        /// Gets/Sets the data source
        /// </summary>
        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                if (value == null)
                {
                    splResults.Visible = false;
                    return;
                }
                if (value is SparqlResultSet)
                {
                    _dataSource = value;
                    splResults.SuspendLayout();
                    splResults.Panel1Collapsed = false;
                    splResults.Panel2Collapsed = true;
                    resultsViewer.DisplayResultSet((SparqlResultSet) value, Namespaces);
                    splResults.ResumeLayout();
                    splResults.Visible = true;
                }
                else if (value is IGraph)
                {
                    _dataSource = value;
                    splResults.SuspendLayout();
                    splResults.Panel1Collapsed = true;
                    splResults.Panel2Collapsed = false;
                    IGraph g = (IGraph) value;
                    graphViewer.DisplayGraph(g, MergeNamespaceMaps(g.NamespaceMap, Namespaces));
                    splResults.ResumeLayout();
                    splResults.Visible = true;
                }
                else
                {
                    throw new ArgumentException("Only SparqlResultSet and IGraph may be used as the DataSource for this control");
                }
            }
        }

        private static INamespaceMapper MergeNamespaceMaps(INamespaceMapper main, INamespaceMapper secondary)
        {
            NamespaceMapper nsmap = new NamespaceMapper(true);
            if (main != null) nsmap.Import(main);
            if (secondary != null) nsmap.Import(secondary);
            return nsmap;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            RaiseCloseRequested();
        }


        private void btnDetach_Click(object sender, EventArgs e)
        {
            RaiseDetachRequested();
        }

        protected void RaiseCloseRequested()
        {
            ResultCloseRequested d = CloseRequested;
            if (d == null) return;
            d(this);
        }

        /// <summary>
        /// Event which is raised when the user has clicked the close button
        /// </summary>
        public event ResultCloseRequested CloseRequested;

        /// <summary>
        /// Event which is raised when the user has clicked the detach button
        /// </summary>
        public event ResultDetachRequested DetachRequested;

        protected void RaiseDetachRequested()
        {
            ResultDetachRequested d = DetachRequested;
            if (d == null) return;
            d(this);
        }
    }
}