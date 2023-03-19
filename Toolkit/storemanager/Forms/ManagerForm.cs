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
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VDS.RDF.GUI;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Utilities.StoreManager.Connections;
using VDS.RDF.Utilities.StoreManager.Dialogues;
using VDS.RDF.Utilities.StoreManager.Properties;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    /// <summary>
    /// A form which provides an interface for managing connections to multiple stores
    /// </summary>
    public partial class ManagerForm
        : CrossThreadForm
    {
        /// <summary>
        /// Creates a new form
        /// </summary>
        public ManagerForm()
        {
            InitializeComponent();
            Closing += OnClosing;
            Constants.WindowIcon = Icon;

            //Ensure we upgrade settings if user has come from an older version of the application
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
                Settings.Default.Reload();
            }

            //Ensure Configuration Loader has known required Object Factorires registered
            ConfigurationLoader.AddObjectFactory(new FullTextObjectFactory());

            //Prepare Connection Definitions so users don't get a huge lag the first time they use these
            ConnectionDefinitionManager.GetDefinitions().Count();

            //Check whether we have a Recent and Favourites Connections Graph
            LoadConnections();
        }

        #region Connection Management

        /// <summary>
        /// Loads in favourite, recent and active connections
        /// </summary>
        private void LoadConnections()
        {
            try
            {
                string appDataDir = Program.GetApplicationDataDirectory();
                string recentConnectionsFile = Path.Combine(appDataDir, "recent.ttl");
                string faveConnectionsFile = Path.Combine(appDataDir, "favourite.ttl");
                string activeConnectionsFile = Path.Combine(appDataDir, "active.ttl");

                // Load Favourite Connections
                IGraph faves = new Graph();
                if (File.Exists(faveConnectionsFile)) faves.LoadFromFile(faveConnectionsFile);
                FavouriteConnections = new ConnectionsGraph(faves, faveConnectionsFile);
                FillConnectionsMenu(mnuFavouriteConnections, FavouriteConnections, 0);

                // Subscribe to collection changed events
                FavouriteConnections.CollectionChanged += FavouriteConnectionsOnCollectionChanged;

                // Load Recent Connections
                IGraph recent = new Graph();
                if (File.Exists(recentConnectionsFile)) recent.LoadFromFile(recentConnectionsFile);
                RecentConnections = new RecentConnectionsesGraph(recent, recentConnectionsFile, Settings.Default.MaxRecentConnections);
                FillConnectionsMenu(mnuRecentConnections, RecentConnections, Settings.Default.MaxRecentConnections);

                // Subscribe to collection changed events
                RecentConnections.CollectionChanged += RecentConnectionsOnCollectionChanged;

                // Load Active Connections
                IGraph active = new Graph();
                if (File.Exists(activeConnectionsFile)) active.LoadFromFile(activeConnectionsFile);
                ActiveConnections = new ActiveConnectionsGraph(active, activeConnectionsFile);
            }
            catch (Exception ex)
            {
                Program.HandleInternalError(Resources.LoadConnections_Error, ex);
            }
        }

        /// <summary>
        /// Gets the favourite connections (may be null)
        /// </summary>
        public IConnectionsGraph FavouriteConnections { get; private set; }

        /// <summary>
        /// Gets the recent connections (may be null)
        /// </summary>
        public IConnectionsGraph RecentConnections { get; private set; }

        /// <summary>
        /// Gets the active connections (may be null)
        /// </summary>
        public ActiveConnectionsGraph ActiveConnections { get; private set; }

        /// <summary>
        /// Gets the first form associated with a given connection (if any)
        /// </summary>
        /// <param name="connection">Connection</param>
        /// <returns>Form if found, null otherwise</returns>
        public StoreManagerForm GetStoreManagerForm(Connection connection)
        {
            return MdiChildren.OfType<StoreManagerForm>().FirstOrDefault(form => ReferenceEquals(form.Connection, connection));
        }

        private void FavouriteConnectionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            HandleConnectionsGraphChanged(notifyCollectionChangedEventArgs, FavouriteConnections, mnuFavouriteConnections, 0);
        }

        private void RecentConnectionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            HandleConnectionsGraphChanged(notifyCollectionChangedEventArgs, RecentConnections, mnuRecentConnections, Settings.Default.MaxRecentConnections);
        }

        private void HandleConnectionsGraphChanged(NotifyCollectionChangedEventArgs args, IConnectionsGraph connections, ToolStripMenuItem item, int maxItems)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Connection connection in args.NewItems.OfType<Connection>())
                    {
                        AddConnectionToMenu(connection, item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Connection connection in args.OldItems.OfType<Connection>())
                    {
                        RemoveConnectionFromMenu(connection, item);
                    }
                    break;
                default:
                    FillConnectionsMenu(item, connections, maxItems);
                    break;
            }
        }

        public void AddRecentConnection(Connection connection)
        {
            try
            {
                if (RecentConnections != null) RecentConnections.Add(connection);
                if (ActiveConnections != null) ActiveConnections.Add(connection);
            }
            catch (Exception ex)
            {
                Program.HandleInternalError(Resources.RecentConnections_Update_Error, ex);
            }
        }

        public void AddFavouriteConnection(Connection connection)
        {
            if (FavouriteConnections == null)
            {
                Program.HandleInternalError(Resources.FavouriteConnections_NoFile);
                return;
            }
            try
            {
                FavouriteConnections.Add(connection);
            }
            catch (Exception ex)
            {
                Program.HandleInternalError(Resources.FavouriteConnections_Update_Error, ex);
            }
        }

        private void AddConnectionToMenu(Connection connection, ToolStripDropDownItem parentItem)
        {
            if (connection == null) return;
            ToolStripMenuItem item = new ToolStripMenuItem {Text = connection.Name, Tag = connection};
            item.Click += QuickConnectClick;

            ToolStripMenuItem edit = new ToolStripMenuItem();
            edit.Text = Resources.EditConnection;
            edit.Tag = item.Tag;
            edit.Click += QuickEditClick;
            item.DropDownItems.Add(edit);

            parentItem.DropDownItems.Add(item);
        }

        private static void RemoveConnectionFromMenu(Connection connection, ToolStripDropDownItem parentItem)
        {
            if (connection == null) return;
            for (int i = 0; i < parentItem.DropDownItems.Count; i++)
            {
                if (!ReferenceEquals(parentItem.DropDownItems[i].Tag, connection)) continue;
                parentItem.DropDownItems.RemoveAt(i);
                i--;
            }
        }

        private void ClearRecentConnections()
        {
            ClearConnections(mnuRecentConnections, RecentConnections);
        }

        private void ClearFavouriteConnections()
        {
            if (MessageBox.Show(Resources.FavouriteConnections_ConfirmClear_Text, Resources.FavouriteConnections_ConfirmClear_Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ClearConnections(mnuFavouriteConnections, FavouriteConnections);
            }
        }

        private static void ClearConnections(ToolStripMenuItem menu, IConnectionsGraph connections)
        {
            if (connections == null) return;
            try
            {
                connections.Clear();
            }
            catch (Exception ex)
            {
                Program.HandleInternalError(Resources.ClearConnections_Error, ex);
            }

            if (menu == null) return;
            while (menu.DropDownItems.Count > 2)
            {
                menu.DropDownItems.RemoveAt(2);
            }
        }

        private void FillConnectionsMenu(ToolStripDropDownItem menu, IConnectionsGraph config, int maxItems)
        {
            // Clear existing items (except the items that are the clear options)
            while (menu.DropDownItems.Count > 2)
            {
                menu.DropDownItems.RemoveAt(2);
            }
            if (config == null || config.Count == 0) return;

            int count = 0;
            foreach (Connection connection in config.Connections)
            {
                ToolStripMenuItem item = new ToolStripMenuItem {Text = connection.Name, Tag = connection};
                item.Click += QuickConnectClick;

                ToolStripMenuItem edit = new ToolStripMenuItem {Text = Resources.EditConnection, Tag = item.Tag};
                edit.Click += QuickEditClick;
                item.DropDownItems.Add(edit);

                menu.DropDownItems.Add(item);

                count++;
                if (maxItems > 0 && count >= maxItems) break;
            }
        }

        private void QuickConnectClick(object sender, EventArgs e)
        {
            if (sender == null) return;
            object tag = null;
            if (sender is Control)
            {
                tag = ((Control) sender).Tag;
            }
            else if (sender is ToolStripItem)
            {
                tag = ((ToolStripItem) sender).Tag;
            }
            else if (sender is Menu)
            {
                tag = ((Menu) sender).Tag;
            }
            if (tag == null) return;
            if (!(tag is Connection)) return;
            Connection connection = (Connection) tag;
            try
            {
                connection.Open();
                ShowStoreManagerForm(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.QuickConnect_Error_Text, ex.Message), Resources.QuickConnect_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void QuickEditClick(object sender, EventArgs e)
        {
            if (sender == null) return;
            object tag = null;
            if (sender is Control)
            {
                tag = ((Control) sender).Tag;
            }
            else if (sender is ToolStripItem)
            {
                tag = ((ToolStripItem) sender).Tag;
            }
            else if (sender is Menu)
            {
                tag = ((Menu) sender).Tag;
            }

            if (tag != null)
            {
                if (tag is Connection)
                {
                    Connection connection = (Connection) tag;
                    if (connection.IsOpen)
                    {
                        MessageBox.Show(Resources.EditConnection_Forbidden_Text, Resources.EditConnection_Forbidden_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    try
                    {
                        EditConnectionForm editConn = new EditConnectionForm(connection, false);
                        if (editConn.ShowDialog() == DialogResult.OK)
                        {
                            connection = editConn.Connection;
                            connection.Open();
                            ShowStoreManagerForm(connection);

                            Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format(Resources.QuickEdit_Error_Text, ex.Message), Resources.QuickEdit_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void PromptRestoreConnections()
        {
            if (ActiveConnections == null) return;
            if (Settings.Default.PromptRestoreActiveConnections)
            {
                if (!ActiveConnections.IsClosed && ActiveConnections.Count > 0 && ActiveConnections.Connections.Any(c => c.IsOpen))
                {
                    try
                    {
                        RestoreConnectionsDialogue restoreConnections = new RestoreConnectionsDialogue();
                        DialogResult result = restoreConnections.ShowDialog();
                        switch (result)
                        {
                            case DialogResult.Cancel:
                                return;
                            case DialogResult.Yes:
                                Settings.Default.RestoreActiveConnections = true;
                                ActiveConnections.Close();
                                break;
                            default:
                                Settings.Default.RestoreActiveConnections = false;
                                ActiveConnections.Clear();
                                break;
                        }
                        Settings.Default.Save();
                        return;
                    }
                    catch (Exception ex)
                    {
                        Program.HandleInternalError(Resources.ActiveConnections_Update_Error, ex);
                    }
                }
            }
            else if (Settings.Default.AlwaysRestoreActiveConnections)
            {
                if (!ActiveConnections.IsClosed && ActiveConnections.Count > 0 && ActiveConnections.Connections.Any(c => c.IsOpen))
                {
                    Settings.Default.RestoreActiveConnections = true;
                    ActiveConnections.Close();
                }
            }
            else
            {
                ActiveConnections.Clear();
            }
        }

        private void RestoreConnections()
        {
            if (!Settings.Default.RestoreActiveConnections) return;
            foreach (Connection connection in ActiveConnections.Connections.ToList())
            {
                try
                {
                    connection.Open();
                    ShowStoreManagerForm(connection);
                }
                catch (Exception ex)
                {
                    Program.HandleInternalError(string.Format(Resources.RestoreConnection_Error, connection.Name), ex);
                }
            }
            LayoutMdi(MdiLayout.Cascade);
        }

        #endregion

        #region Event Handlers

        private void fclsManager_Load(object sender, EventArgs e)
        {
            RestoreConnections();

            if (!Settings.Default.ShowStartPage) return;
            StartPage start = new StartPage(RecentConnections, FavouriteConnections);
            start.ShowDialog();
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            PromptRestoreConnections();
        }

        #endregion

        #region Menu Event Handlers

        private void mnuStrip_MenuActivate(object sender, EventArgs e)
        {
            if (ActiveMdiChild != null)
            {
                Form activeChild = ActiveMdiChild;
                ActivateMdiChild(null);
                ActivateMdiChild(activeChild);

                if (ActiveMdiChild is StoreManagerForm)
                {
                    mnuSaveConnection.Enabled = true;
                    mnuAddFavourite.Enabled = true;
                    mnuNewFromExisting.Enabled = true;
                }
                else
                {
                    mnuSaveConnection.Enabled = false;
                    mnuAddFavourite.Enabled = false;
                    mnuNewFromExisting.Enabled = false;
                }
            }
            else
            {
                mnuSaveConnection.Enabled = false;
                mnuAddFavourite.Enabled = false;
                mnuNewFromExisting.Enabled = false;
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            // Prompt for restoring connections
            PromptRestoreConnections();

            // Close children
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
            Application.Exit();
        }

        private void mnuCascade_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void mnuTileVertical_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void mnuTileHorizontal_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void mnuArrangeIcons_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void mnuCloseAll_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void mnuSaveConnection_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild != null)
            {
                if (ActiveMdiChild is StoreManagerForm)
                {
                    try
                    {
                        Connection connection = ((StoreManagerForm) ActiveMdiChild).Connection;
                        sfdConnection.Filter = MimeTypesHelper.GetFilenameFilter(true, false, false, false, false, false);
                        if (sfdConnection.ShowDialog() == DialogResult.OK)
                        {
                            //Append to existing configuration file or overwrite?
                            IGraph cs = new Graph();
                            if (File.Exists(sfdConnection.FileName))
                            {
                                DialogResult result = MessageBox.Show(Resources.SaveConnection_Append_Text, Resources.SaveConnection_Append_Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                                switch (result)
                                {
                                    case DialogResult.Yes:
                                        // Load in existing connections
                                        cs.LoadFromFile(sfdConnection.FileName);
                                        break;
                                    case DialogResult.No:
                                        File.Delete(sfdConnection.FileName);
                                        break;
                                    default:
                                        return;
                                }
                            }

                            // Open the connections file and add to it which automatically causes it to be saved
                            IConnectionsGraph connections = new ConnectionsGraph(cs, sfdConnection.FileName);
                            connections.Add(connection);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.HandleInternalError(Resources.SaveConnection_Error, ex);
                    }
                }
                else
                {
                    mnuSaveConnection.Enabled = false;
                }
            }
            else
            {
                mnuSaveConnection.Enabled = false;
            }
        }

        private void mnuOpenConnection_Click(object sender, EventArgs e)
        {
            ofdConnection.Filter = MimeTypesHelper.GetFilenameFilter(true, false, false, false, false, false);
            if (ofdConnection.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    IGraph cs = new Graph();
                    cs.LoadFromFile(ofdConnection.FileName);
                    IConnectionsGraph connections = new ConnectionsGraph(cs, ofdConnection.FileName);

                    OpenConnectionForm openConnections = new OpenConnectionForm(connections);
                    openConnections.MdiParent = this;
                    if (openConnections.ShowDialog() == DialogResult.OK)
                    {
                        Connection connection = openConnections.Connection;
                        ShowStoreManagerForm(connection);
                    }
                }
                catch (RdfParseException)
                {
                    MessageBox.Show(Resources.OpenConnection_InvalidFile_Text, Resources.OpenConnection_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Resources.OpenConnection_Error_Text, ex.Message), Resources.OpenConnection_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void mnuClearRecentConnections_Click(object sender, EventArgs e)
        {
            ClearRecentConnections();
        }

        private void mnuClearFavouriteConnections_Click(object sender, EventArgs e)
        {
            ClearFavouriteConnections();
        }

        private void mnuAddFavourite_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild == null) return;
            if (!(ActiveMdiChild is StoreManagerForm)) return;
            Connection connection = ((StoreManagerForm) ActiveMdiChild).Connection;
            AddFavouriteConnection(connection);
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.ShowDialog();
        }

        private void mnuNewConnection_Click(object sender, EventArgs e)
        {
            NewConnectionForm newConn = new NewConnectionForm();
            newConn.StartPosition = FormStartPosition.CenterParent;
            if (newConn.ShowDialog() != DialogResult.OK) return;
            Connection connection = newConn.Connection;
            ShowStoreManagerForm(connection);
        }

        private void mnuNewFromExisting_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild != null)
            {
                if (ActiveMdiChild is StoreManagerForm)
                {
                    Connection connection = ((StoreManagerForm) ActiveMdiChild).Connection;
                    EditConnectionForm editConn = new EditConnectionForm(connection, true);
                    if (editConn.ShowDialog() == DialogResult.OK)
                    {
                        editConn.Connection.Open();
                        ShowStoreManagerForm(editConn.Connection);
                    }
                    return;
                }
            }
            MessageBox.Show(Resources.NewFromExisting_Error_Text, Resources.NewFromExisting_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowStoreManagerForm(Connection connection)
        {
            try
            {
                StoreManagerForm storeManagerForm = new StoreManagerForm(connection);
                CrossThreadSetMdiParent(storeManagerForm, this);
                CrossThreadShow(storeManagerForm);

                // Update Quick Jump Bar
                ToolStripButton quickJumpButton = new ToolStripButton(connection.Name);
                connection.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
                    {
                        if (args.PropertyName.Equals("Name")) CrossThreadSetText(quickJumpButton, connection.Name);
                        if (args.PropertyName.Equals("IsOpen") && !connection.IsOpen)
                        {
                            quickJumpBar.Items.Remove(quickJumpButton);
                            if (quickJumpBar.Items.Count == 1) quickJumpBar.Visible = false;
                        }
                    };
                quickJumpButton.Click += delegate(object sender, EventArgs args)
                    {
                        StoreManagerForm form = GetStoreManagerForm(connection);
                        if (form == null) return;
                        form.Show();
                        form.Focus();
                    };
                quickJumpBar.Items.Add(quickJumpButton);
                quickJumpBar.Visible = true;

                AddRecentConnection(connection);
            }
            catch (Exception ex)
            {
                Program.HandleInternalError("Unable to display a Store Manager form for the connection " + connection.Name, ex);
            }
        }

        private void mnuStartPage_Click(object sender, EventArgs e)
        {
            StartPage start = new StartPage(RecentConnections, FavouriteConnections);
            start.Owner = this;
            start.ShowDialog();
        }

        private void mnuManageConnections_Click(object sender, EventArgs e)
        {
            ManageConnectionsForm manageConnectionsForm = new ManageConnectionsForm();
            manageConnectionsForm.ActiveConnections = ActiveConnections;
            manageConnectionsForm.RecentConnections = RecentConnections;
            manageConnectionsForm.FavouriteConnections = FavouriteConnections;
            manageConnectionsForm.MdiParent = this;
            manageConnectionsForm.Show();
        }

        private void mnuOptions_Click(object sender, EventArgs e)
        {
            // Show options dialogue
            OptionsDialogue dialogue = new OptionsDialogue();
            dialogue.MdiParent = this;
            dialogue.Show();

            // Want to apply updated editor options when the dialogue is closed
            dialogue.Closed += (o, args) => UpdateEditors();
        }

        private void UpdateEditors()
        {
            foreach (StoreManagerForm storeManagerForm in MdiChildren.OfType<StoreManagerForm>())
            {
                storeManagerForm.ApplyEditorOptions();
            }
        }

        #endregion
    }
}