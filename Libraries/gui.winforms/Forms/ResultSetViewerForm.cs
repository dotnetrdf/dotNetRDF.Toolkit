/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Windows.Forms;
using VDS.RDF.Query;

namespace VDS.RDF.GUI.WinForms.Forms
{
    /// <summary>
    /// A Form that displays a SPARQL Result Set using a DataGridView
    /// </summary>
    public partial class ResultSetViewerForm : Form
    {
        private readonly SparqlResultSet _results;
        private readonly INamespaceMapper _nsmap;

        /// <summary>
        /// Displays the given SPARQL Result Set
        /// </summary>
        /// <param name="results">SPARQL Result to display</param>
        public ResultSetViewerForm(SparqlResultSet results)
            : this(results, null, null) { }

        /// <summary>
        /// Displays the given SPARQL Result Set and prefixes the form title with the given title
        /// </summary>
        /// <param name="results">SPARQL Result Set to display</param>
        /// <param name="title">Title prefix</param>
        public ResultSetViewerForm(SparqlResultSet results, string title)
            : this(results, null, title) { }

        /// <summary>
        /// Creates a new Result Set viewer form
        /// </summary>
        /// <param name="results">Result Set</param>
        /// <param name="nsmap">Namespace Map to use for display</param>
        /// <param name="title">Title prefix</param>
        public ResultSetViewerForm(SparqlResultSet results, INamespaceMapper nsmap, string title)
        {
            InitializeComponent();
            if (Constants.WindowIcon != null)
            {
                Icon = Constants.WindowIcon;
            }
            _results = results;
            Text = GetTitle(title, results);
            _nsmap = nsmap;

            resultsViewer.UriClicked += (sender, uri) => RaiseUriClicked(uri);
            Load += (sender, args) => resultsViewer.DisplayResultSet(_results, _nsmap);
        }

        private static string GetTitle(SparqlResultSet results)
        {
            if (results.ResultsType == SparqlResultsType.Boolean)
                return "SPARQL Results Viewer - Boolean Result";
            return results.ResultsType == SparqlResultsType.VariableBindings ? string.Format("SPARQL Results Viewer - {0} Result(s)", results.Count) : "SPARQL Results Viewer";
        }

        private static string GetTitle(string title, SparqlResultSet results)
        {
            if (title == null) return GetTitle(results);
            return string.Format("{0} - " + GetTitle(results), title);
        }

        /// <summary>
        /// Event which is raised when the User clicks a URI
        /// </summary>
        public event UriClickedEventHandler UriClicked;

        private void RaiseUriClicked(Uri u)
        {
            UriClickedEventHandler d = UriClicked;
            if (d != null)
            {
                d(this, u);
            }
        }
    }
}