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
using VDS.RDF.Utilities.StoreManager.Connections;
using VDS.RDF.Utilities.StoreManager.Controls;
using VDS.RDF.Utilities.StoreManager.Properties;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    /// <summary>
    /// Form for editing connection definitions
    /// </summary>
    public partial class EditConnectionForm
        : Form
    {
        private bool _editing = false;

        /// <summary>
        /// Creates a new Edit Connection Form
        /// </summary>
        /// <param name="connection">Connection</param>
        /// <param name="copy">Whether to take a copy of the connection</param>
        public EditConnectionForm(Connection connection, bool copy)
        {
            InitializeComponent();
            if (!copy && connection.IsOpen) throw new ArgumentException("Cannot edit an open connection");
            Connection = copy ? connection.Copy() : connection;
            connSettings.Definition = Connection.Definition;
            connSettings.Connected += HandleConnected;

            if (copy)
            {
                Text = Resources.NewFromExisting;
            }
            else
            {
                _editing = true;
            }
        }

        /// <summary>
        /// Handles successfull connections
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Connection Event arguments</param>
        private void HandleConnected(object sender, ConnectedEventArgs e)
        {
            if (!_editing)
            {
                // When not editing need to take the newly created connection
                Connection = e.Connection;
            }
            else
            {
                // Otherwise take just the definition and update the last modified
                Connection.Definition = e.Connection.Definition;
                Connection.LastModified = DateTimeOffset.UtcNow;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Gets the Connection if it has been created
        /// </summary>
        public Connection Connection { get; private set; }
    }
}