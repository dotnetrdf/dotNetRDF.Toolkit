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
using System.ComponentModel;
using System.Windows.Forms;
using VDS.RDF.Utilities.StoreManager.Connections;
using VDS.RDF.Utilities.StoreManager.Properties;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    public partial class StartPage : Form
    {
        public StartPage(IConnectionsGraph recent, IConnectionsGraph faves)
        {
            InitializeComponent();

            this.FillConnectionList(recent, this.lstRecent);
            this.FillConnectionList(faves, this.lstFaves);
            this.chkAlwaysEdit.Checked = Settings.Default.AlwaysEdit;
            this.chkAlwaysShow.Checked = Settings.Default.ShowStartPage;
        }

        private void StartPage_Load(object sender, EventArgs e)
        {
        }

        private void FillConnectionList(IConnectionsGraph connections, ListBox lbox)
        {
            foreach (Connection connection in connections.Connections)
            {
                lbox.Items.Add(connection);
            }
            lbox.DoubleClick += (sender, args) =>
                {
                    Connection connection = lbox.SelectedItem as Connection;
                    if (connection == null) return;
                    if (Settings.Default.AlwaysEdit)
                    {
                        if (connection.IsOpen)
                        {
                            MessageBox.Show(Resources.EditConnection_Forbidden_Text, Resources.EditConnection_Forbidden_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        EditConnectionForm edit = new EditConnectionForm(connection, false);
                        if (edit.ShowDialog() == DialogResult.OK)
                        {
                            connection = edit.Connection;
                            try
                            {
                                connection.Open();
                                Program.MainForm.ShowStoreManagerForm(connection);
                                this.Close();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(string.Format(Resources.StartPage_Open_Error_Text, connection.Name, ex.Message), Resources.ConnectionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            connection.Open();
                            Program.MainForm.ShowStoreManagerForm(connection);
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format(Resources.StartPage_Open_Error_Text, connection.Name, ex.Message), Resources.ConnectionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };
        }

        private void btnNewConnection_Click(object sender, EventArgs e)
        {
            NewConnectionForm newConn = new NewConnectionForm();
            if (newConn.ShowDialog() != DialogResult.OK) return;
            Connection connection = newConn.Connection;
            Program.MainForm.ShowStoreManagerForm(newConn.Connection);

            //Add to Recent Connections
            Program.MainForm.AddRecentConnection(connection);

            this.Close();
        }

        private void chkAlwaysShow_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.ShowStartPage = this.chkAlwaysShow.Checked;
            Settings.Default.Save();
        }

        private void chkAlwaysEdit_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.AlwaysEdit = this.chkAlwaysEdit.Checked;
            Settings.Default.Save();
        }


        private void mnuEditFave_Click(object sender, EventArgs e)
        {
            EditConnection(this.lstFaves);
        }

        private void mnuEditRecent_Click(object sender, EventArgs e)
        {
            EditConnection(this.lstRecent);
        }

        private void EditConnection(ListBox lbox)
        {
            Connection connection = lbox.SelectedItem as Connection;
            if (connection == null) return;

            if (connection.IsOpen)
            {
                MessageBox.Show("The selected connection is not editable", "Edit Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            EditConnectionForm edit = new EditConnectionForm(connection, false);
            if (edit.ShowDialog() != DialogResult.OK) return;
            connection = edit.Connection;
            try
            {
                connection.Open();
                Program.MainForm.ShowStoreManagerForm(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.StartPage_Open_Error_Text, connection.Name, ex.Message), Resources.ConnectionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Add to Recent Connections
            Program.MainForm.AddRecentConnection(connection);

            this.Close();
        }

        private static void CheckConnectionContext(ListBox lbox, CancelEventArgs e)
        {
            if (lbox == null || lbox.SelectedItem == null) e.Cancel = true;
        }

        private void mnuFaveConnections_Opening(object sender, CancelEventArgs e)
        {
            CheckConnectionContext(this.lstFaves, e);
        }

        private void mnuRecentConnections_Opening(object sender, CancelEventArgs e)
        {
            CheckConnectionContext(this.lstRecent, e);
        }
    }
}