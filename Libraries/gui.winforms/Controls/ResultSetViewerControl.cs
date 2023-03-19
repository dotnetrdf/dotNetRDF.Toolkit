﻿using System;
using System.Data;
using System.Windows.Forms;
using VDS.RDF.Data.DataTables;
using VDS.RDF.GUI.WinForms.Forms;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.GUI.WinForms.Controls
{
    /// <summary>
    /// A Form that displays a SPARQL Result Set using a DataGridView
    /// </summary>
    public partial class ResultSetViewerControl : UserControl
    {
        private SparqlResultSet _results;
        private Formatter _lastFormatter = null;
        private INodeFormatter _formatter = new SparqlFormatter();
        private INamespaceMapper _nsmap;

        /// <summary>
        /// Creates a new control
        /// </summary>
        public ResultSetViewerControl()
        {
            InitializeComponent();
            dgvResults.CellFormatting += dgvTriples_CellFormatting;
            dgvResults.CellContentClick += dgvTriples_CellClick;
            fmtSelector.DefaultFormatter = typeof (SparqlFormatter);
            fmtSelector.FormatterChanged += fmtSelector_FormatterChanged;
        }

        private void fmtSelector_FormatterChanged(object sender, Formatter formatter)
        {
            if (ReferenceEquals(formatter, _lastFormatter)) return;
            _lastFormatter = formatter;
            Reformat();
        }

        private void Reformat()
        {
            if (ReferenceEquals(_lastFormatter, null)) return;
            _formatter = _lastFormatter.CreateInstance(_nsmap);

            if (dgvResults.DataSource == null) return;
            DataTable tbl = (DataTable) dgvResults.DataSource;
            dgvResults.DataSource = null;
            dgvResults.Refresh();
            dgvResults.DataSource = tbl;
        }

        /// <summary>
        /// Displays the given SPARQL Result Set
        /// </summary>
        /// <param name="results">SPARQL Result to display</param>
        public void DisplayResultSet(SparqlResultSet results)
        {
            _results = results;

            Text = GetTitle();

            // Load Results and Populate Form Fields
            LoadInternal();
        }

        /// <summary>
        /// Creates a new Result Set viewer form
        /// </summary>
        /// <param name="results">Result Set</param>
        /// <param name="nsmap">Namespace Map to use for display</param>
        public void DisplayResultSet(SparqlResultSet results, INamespaceMapper nsmap)
        {
            _nsmap = nsmap;
            if (nsmap != null) _formatter = new SparqlFormatter(nsmap);

            DisplayResultSet(results);
        }

        private string GetTitle()
        {
            if (_results.ResultsType == SparqlResultsType.Boolean)
                return "SPARQL Results Viewer - Boolean Result";
            return _results.ResultsType == SparqlResultsType.VariableBindings ? string.Format("SPARQL Results Viewer - {0} Result(s)", _results.Count) : "SPARQL Results Viewer";
        }

        /// <summary>
        /// Event which is raised when the User clicks a URI
        /// </summary>
        public event UriClickedEventHandler UriClicked;

        #region Internal Implementation

        private void dgvTriples_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!(e.Value is INode)) return;
            INode n = (INode) e.Value;
            e.Value = _formatter.Format(n);
            e.FormattingApplied = true;
            switch (n.NodeType)
            {
                case NodeType.Uri:
                    e.CellStyle.Font = new System.Drawing.Font(e.CellStyle.Font, System.Drawing.FontStyle.Underline);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 0, 0, 255);
                    break;
            }
        }

        private void dgvTriples_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            object value = dgvResults[e.ColumnIndex, e.RowIndex].Value;
            if (value == null) return;
            if (!(value is INode)) return;
            INode n = (INode) value;
            if (n.NodeType == NodeType.Uri)
            {
                RaiseUriClicked(((IUriNode) n).Uri);
            }
        }

        private void LoadInternal()
        {
            //Show Results
            if (_results.ResultsType == SparqlResultsType.Boolean)
            {
                DataTable table = new DataTable();
                table.Columns.Add(new DataColumn("ASK", typeof (string)));
                DataRow row = table.NewRow();
                row["ASK"] = _results.Result.ToString();
                table.Rows.Add(row);
                dgvResults.DataSource = table;
            }
            else
            {
                dgvResults.DataSource = _results.ToDataTable();
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportResultSetOptionsForm exporter = new ExportResultSetOptionsForm();
            if (exporter.ShowDialog() != DialogResult.OK) return;
            ISparqlResultsWriter writer = exporter.Writer;
            string file = exporter.File;

            try
            {
                writer.Save(_results, file);

                MessageBox.Show("Successfully exported the SPARQL Results to the file '" + file + "'", "SPARQL Results Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred attempting to export the SPARQL Results:\n" + ex.Message, "SPARQL Results Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RaiseUriClicked(Uri u)
        {
            UriClickedEventHandler d = UriClicked;
            if (d != null)
            {
                d(this, u);
            }
        }

        #endregion
    }
}