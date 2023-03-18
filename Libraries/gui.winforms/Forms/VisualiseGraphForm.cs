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
using System.IO;
using System.Windows.Forms;
using VDS.RDF.Writing;

namespace VDS.RDF.GUI.WinForms.Forms
{
    /// <summary>
    /// A Form that can be used to Visualise a Graph using GraphViz (or produce DOT output for use with GraphViz)
    /// </summary>
    public partial class VisualiseGraphForm : Form
    {
        private IGraph _g;

        /// <summary>
        /// Creates a new Form for Visualising Graphs
        /// </summary>
        /// <param name="g">Graph to visualise</param>
        public VisualiseGraphForm(IGraph g)
        {
            InitializeComponent();

            _g = g;
        }

        private void btnVisualise_Click(object sender, EventArgs e)
        {
            if (txtFile.Text.Equals(string.Empty))
            {
                MessageBox.Show("You must enter a filename you wish to visualise the Graph to", "Filename Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                try
                {
                    switch (Path.GetExtension(txtFile.Text))
                    {
                        case ".dot":
                        case "dot":
                            GraphVizWriter dotwriter = new GraphVizWriter();
                            dotwriter.Save(_g, txtFile.Text);
                            break;
                        case ".png":
                        case "png":
                            GraphVizGenerator pnggenerator = new GraphVizGenerator("png");
                            pnggenerator.Generate(_g, txtFile.Text, true);
                            break;
                        case ".svg":
                        case "svg":
                        default:
                            GraphVizGenerator svggenerator = new GraphVizGenerator("svg");
                            svggenerator.Generate(_g, txtFile.Text, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An Error occurred while trying to visualise the selected Graph:\n" + ex.Message, "Visualisation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (sfdVisualise.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = sfdVisualise.FileName;
            }
        }

    }
}
