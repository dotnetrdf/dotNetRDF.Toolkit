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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using VDS.RDF.Writing;

namespace VDS.RDF.GUI.WinForms.Forms
{
    /// <summary>
    /// A Form that can be used to select options for Exporting a Graph to a File
    /// </summary>
    public partial class ExportGraphOptionsForm : Form
    {
        /// <summary>
        /// Creates a new Export Graph Options Form
        /// </summary>
        public ExportGraphOptionsForm()
        {
            InitializeComponent();

            //Load Writers
            Type targetType = typeof(IRdfWriter);
            List<IRdfWriter> writers = new List<IRdfWriter>();
            foreach (Type t in Assembly.GetAssembly(targetType).GetTypes())
            {
                if (t.Namespace == null) continue;

                if (t.Namespace.Equals("VDS.RDF.Writing"))
                {
                    if (t.GetInterfaces().Contains(targetType))
                    {
                        try
                        {
                            IRdfWriter writer = (IRdfWriter)Activator.CreateInstance(t);
                            writers.Add(writer);
                        }
                        catch
                        {
                            //Ignore this Formatter
                        }
                    }
                }
            }
            writers.Sort(new ToStringComparer<IRdfWriter>());
            cboWriter.DataSource = writers;
            if (cboWriter.Items.Count > 0) cboWriter.SelectedIndex = 0;

            cboWriter.SelectedIndex = 0;
            cboCompression.SelectedIndex = 1;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (txtFile.Text.Equals(string.Empty))
            {
                MessageBox.Show("You must enter a filename you wish to export the Graph to", "Filename Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Gets the RDF Writer the user selected
        /// </summary>
        public IRdfWriter Writer
        {
            get
            {
                IRdfWriter writer = cboWriter.SelectedItem as IRdfWriter;
                if (writer == null) writer = new NTriplesWriter();

                //Configure Options on the Writer
                if (writer is IPrettyPrintingWriter)
                {
                    ((IPrettyPrintingWriter)writer).PrettyPrintMode = chkPrettyPrinting.Checked;
                }
                if (writer is IHighSpeedWriter)
                {
                    ((IHighSpeedWriter)writer).HighSpeedModePermitted = chkHighSpeed.Checked;
                }
                if (writer is ICompressingWriter)
                {
                    int c = WriterCompressionLevel.Default;
                    switch (cboCompression.SelectedIndex)
                    {
                        case 0:
                            c = WriterCompressionLevel.None;
                            break;
                        case 1:
                            c = WriterCompressionLevel.Default;
                            break;
                        case 2:
                            c = WriterCompressionLevel.Minimal;
                            break;
                        case 3:
                            c = WriterCompressionLevel.Medium;
                            break;
                        case 4:
                            c = WriterCompressionLevel.More;
                            break;
                        case 5:
                            c = WriterCompressionLevel.High;
                            break;
                        default:
                            c = WriterCompressionLevel.Default;
                            break;
                    }
                    ((ICompressingWriter)writer).CompressionLevel = c;
                }

                return writer;
            }
        }

        /// <summary>
        /// Gets the Target Filename the user selected
        /// </summary>
        public string File
        {
            get
            {
                return txtFile.Text;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            sfdExport.Filter = MimeTypesHelper.GetFilenameFilter(true, false, false, false, false, false);
            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = sfdExport.FileName;
            }
        }

    }
}
