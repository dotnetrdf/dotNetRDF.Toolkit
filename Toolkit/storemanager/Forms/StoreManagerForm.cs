/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VDS.RDF.GUI.WinForms.Controls;
using VDS.RDF.GUI.WinForms.Forms;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Storage.Management.Provisioning;
using VDS.RDF.Utilities.StoreManager.Connections;
using VDS.RDF.Utilities.StoreManager.Dialogues;
using VDS.RDF.Utilities.StoreManager.Properties;
using VDS.RDF.Utilities.StoreManager.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    /// <summary>
    /// Form for managing stores
    /// </summary>
    public partial class StoreManagerForm
        : CrossThreadForm
    {
        private readonly EventHandler _copyGraphHandler, _moveGraphHandler;
        private bool _codeFormatInProgress = false;
        private readonly List<HighLight> _highLights = new List<HighLight>();
        private readonly Timer _highlightsUpdateTimer = new Timer();
        private bool _codeHighLightingInProgress = false;
        private int _taskId;
        private readonly System.Timers.Timer _timStartup;
        private bool _closing = true;
        private int _queryId = 0;

        /// <summary>
        /// Creates a new Store Manager form
        /// </summary>
        /// <param name="connection">Connection</param>
        public StoreManagerForm(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (!connection.IsOpen) throw new ArgumentException(Resources.ConnectionMustBeOpen, "connection");

            InitializeComponent();
            Closing += OnClosing;

            splitQueryResults.Panel2Collapsed = true;
            ActivateHighlighting();

            // Configure Connection
            Connection = connection;
            StorageProvider = connection.StorageProvider;
            Text = connection.Name;

            // Subscribe to events on the connection
            Connection.PropertyChanged += ConnectionOnPropertyChanged;

            // Configure Tasks List
            lvwTasks.ListViewItemSorter = new SortTasksById();

            // Configure Graphs List
            lvwGraphs.ItemDrag += lvwGraphs_ItemDrag;
            lvwGraphs.DragEnter += lvwGraphs_DragEnter;
            lvwGraphs.DragDrop += lvwGraphs_DragDrop;
            _copyGraphHandler = CopyGraphClick;
            _moveGraphHandler = MoveGraphClick;

            // Startup Timer
            _timStartup = new System.Timers.Timer(250);
            _timStartup.Elapsed += timStartup_Tick;

            // Apply Editor Options
            ApplyEditorOptions();

            // Set highlight delay for 2 secs
            _highlightsUpdateTimer.Interval = (int) TimeSpan.FromSeconds(2).TotalMilliseconds;
            _highlightsUpdateTimer.Tick += HighlightsUpdateTimerOnTick;
        }

        private void fclsGenericStoreManager_Load(object sender, EventArgs e)
        {
            //Determine whether SPARQL Query is supported
            if (!(StorageProvider is IQueryableStorage))
            {
                tabFunctions.TabPages.Remove(tabSparqlQuery);
            }

            //Determine what SPARQL Update mode if any is supported
            if (StorageProvider is IUpdateableStorage)
            {
                lblUpdateMode.Text = Resources.UpdateModeNative;
            }
            else if (!StorageProvider.IsReadOnly)
            {
                lblUpdateMode.Text = Resources.UpdarteModeApproximated;
            }
            else
            {
                tabFunctions.TabPages.Remove(tabSparqlUpdate);
            }

            //Disable Import for Read-Only stores
            if (StorageProvider.IsReadOnly)
            {
                tabFunctions.TabPages.Remove(tabImport);
            }

            //Disable Server Management for non Storage Servers
            if (StorageProvider.ParentServer == null)
            {
                tabFunctions.TabPages.Remove(tabServer);
            }

            //Show Connection Information
            propInfo.SelectedObject = Connection.Information;

            //Run Startup Timer
            _timStartup.Start();
        }

        #region Connection Management

        private void ConnectionOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            string property = propertyChangedEventArgs.PropertyName;
            if (property.Equals("IsOpen"))
            {
                // If the connection gets closed then close the form
                if (!Connection.IsOpen && !_closing)
                    Close();
            }
            else if (property.Equals("Name"))
            {
                // If the connection is renamed update the form title
                Text = Connection.Name;
            }
        }

        /// <summary>
        /// Gets/Sets the storage provider
        /// </summary>
        private IStorageProvider StorageProvider { get; set; }

        /// <summary>
        /// Gets the connection
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// Gets/Sets whether to force close the connection when this form closes
        /// </summary>
        private bool ForceClose { get; set; }

        #endregion

        #region Editor Options

        /// <summary>
        /// Method that should be called whenever editor options are changed so that all editors are updated
        /// </summary>
        public void ApplyEditorOptions()
        {
            rtbSparqlQuery.WordWrap = Settings.Default.EditorWordWrap;
            rtbSparqlQuery.DetectUrls = Settings.Default.EditorDetectUrls;
            if (Settings.Default.EditorHighlighting)
            {
                ActivateHighlighting();
            }
            else
            {
                ClearHighlighting();
            }
        }

        #endregion

        #region Store Operations

        /// <summary>
        /// Requests that the graphs be listed
        /// </summary>
        public void ListGraphs()
        {
            ListGraphsTask task = new ListGraphsTask(StorageProvider);
            AddTask(task, ListGraphsCallback);
        }

        /// <summary>
        /// Requests that the stores be listed
        /// </summary>
        public void ListStores()
        {
            ListStoresTask task = new ListStoresTask(StorageProvider.ParentServer);
            AddTask(task, ListStoresCallback);
        }

        /// <summary>
        /// Requests that the view of a graph be returned
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        private void ViewGraph(string graphUri)
        {
            ViewGraphTask task = new ViewGraphTask(StorageProvider, graphUri);
            AddTask(task, ViewGraphCallback);
        }

        /// <summary>
        /// Requests the preview of a graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        private void PreviewGraph(string graphUri)
        {
            PreviewGraphTask task = new PreviewGraphTask(StorageProvider, graphUri, Settings.Default.PreviewSize);
            AddTask(task, PreviewGraphCallback);
        }

        /// <summary>
        /// Requests the count of triples for a graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        private void CountTriples(string graphUri)
        {
            CountTriplesTask task = new CountTriplesTask(StorageProvider, graphUri);
            AddTask(task, CountTriplesCallback);
        }

        /// <summary>
        /// Requests the deletion of a graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        private void DeleteGraph(string graphUri)
        {
            DeleteGraphTask task = new DeleteGraphTask(StorageProvider, graphUri);
            AddTask(task, DeleteGraphCallback);
        }

        /// <summary>
        /// Runs a Query
        /// </summary>
        private void Query()
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNotReady_Query_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (StorageProvider is IQueryableStorage)
            {
                if (chkPageQuery.Checked)
                {
                    QueryTask task = new QueryTask((IQueryableStorage) StorageProvider, rtbSparqlQuery.Text, (int) numPageSize.Value);
                    AddTask(task, QueryCallback);
                }
                else
                {
                    QueryTask task = new QueryTask((IQueryableStorage) StorageProvider, rtbSparqlQuery.Text);
                    AddTask(task, QueryCallback);
                }
            }
            else
            {
                MessageBox.Show(Resources.Query_Unsupported, Resources.Query_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Runs a GenerateEntitiesQueryTask
        /// </summary>
        public void GenerateEntitiesQuery(string query, int predicateLimitCount)
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNotReady_Query_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (StorageProvider is IQueryableStorage)
            {
                GenerateEntitiesQueryTask task = new GenerateEntitiesQueryTask((IQueryableStorage) StorageProvider, query, predicateLimitCount);
                AddTask(task, GenerateEntitiesQueryCallback);
            }
            else
            {
                MessageBox.Show(Resources.Query_Unsupported, Resources.Query_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Runs an Update
        /// </summary>
        private void SparqlUpdate()
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNotReady_Update_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UpdateTask task = new UpdateTask(StorageProvider, txtSparqlUpdate.Text);
            AddTask(task, UpdateCallback);
        }

        /// <summary>
        /// Imports a File
        /// </summary>
        private void ImportFile()
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNoteReady_Import_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (txtImportFile.Text.Equals(string.Empty))
            {
                MessageBox.Show(Resources.ImportData_NoFile_Text, Resources.ImportData_NoFile_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Uri targetUri = null;
            try
            {
                if (chkImportDefaultUri.Checked)
                {
                    targetUri = new Uri(txtImportDefaultGraph.Text);
                }
            }
            catch (UriFormatException uriEx)
            {
                MessageBox.Show(string.Format(Resources.ImportData_InvalidTarget_Text, uriEx.Message), Resources.ImportData_InvalidTarget_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ImportFileTask task = new ImportFileTask(StorageProvider, txtImportFile.Text, targetUri, (int) numBatchSize.Value);
            AddTask(task, ImportCallback);
        }

        /// <summary>
        /// Imports  URI
        /// </summary>
        private void ImportUri()
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNoteReady_Import_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (txtImportUri.Text.Equals(string.Empty))
            {
                MessageBox.Show(Resources.ImportData_NoUri_Text, Resources.ImportData_NoUri_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Uri targetUri = null;
            try
            {
                if (chkImportDefaultUri.Checked)
                {
                    targetUri = new Uri(txtImportDefaultGraph.Text);
                }
            }
            catch (UriFormatException uriEx)
            {
                MessageBox.Show(string.Format(Resources.ImportData_InvalidTarget_Text, uriEx.Message), Resources.ImportData_InvalidTarget_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ImportUriTask task = new ImportUriTask(StorageProvider, new Uri(txtImportUri.Text), targetUri, (int) numBatchSize.Value);
                AddTask(task, ImportCallback);
            }
            catch (UriFormatException uriEx)
            {
                MessageBox.Show(string.Format(Resources.ImportData_InvalidSource_Text, uriEx.Message), Resources.ImportData_InvalidSource_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Exports data
        /// </summary>
        private void Export()
        {
            if (!StorageProvider.IsReady)
            {
                MessageBox.Show(Resources.StoreNoteReady_Import_Text, Resources.StoreNotReady_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (txtExportFile.Text.Equals(string.Empty))
            {
                MessageBox.Show(Resources.ExportData_NoFile_Text, Resources.ExportData_NoFile_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExportTask task = new ExportTask(StorageProvider, txtExportFile.Text);
            AddTask(task, ExportCallback);
        }

        /// <summary>
        /// Copies a Graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <param name="target">Target</param>
        public void CopyGraph(string graphUri, Connection target)
        {
            if (target == null) return;

            Uri source = graphUri.Equals("Default Graph") ? null : new Uri(graphUri);
            if (ReferenceEquals(Connection, target))
            {
                CopyMoveRenameGraphForm rename = new CopyMoveRenameGraphForm("Copy");

                if (rename.ShowDialog() == DialogResult.OK)
                {
                    CopyMoveTask task = new CopyMoveTask(Connection, target, source, rename.Uri, ReferenceEquals(Connection, target));
                    AddTask(task, CopyMoveRenameCallback);
                }
            }
            else
            {
                CopyMoveTask task = new CopyMoveTask(Connection, target, source, source, true);
                AddTask(task, CopyMoveRenameCallback);
            }
        }

        /// <summary>
        /// Renames a Graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        private void RenameGraph(string graphUri)
        {
            CopyMoveRenameGraphForm rename = new CopyMoveRenameGraphForm("Rename");
            Uri source = graphUri.Equals("Default Graph") ? null : new Uri(graphUri);
            if (rename.ShowDialog() != DialogResult.OK) return;
            CopyMoveTask task = new CopyMoveTask(Connection, Connection, source, rename.Uri, false);
            AddTask(task, CopyMoveRenameCallback);
        }

        /// <summary>
        /// Moves a Graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <param name="target">Target</param>
        public void MoveGraph(string graphUri, Connection target)
        {
            if (target == null) return;

            if (ReferenceEquals(Connection, target))
            {
                RenameGraph(graphUri);
            }
            else
            {
                Uri source = graphUri.Equals("Default Graph") ? null : new Uri(graphUri);
                CopyMoveTask task = new CopyMoveTask(Connection, target, source, source, false);
                AddTask(task, CopyMoveRenameCallback);
            }
        }

        #region Server Operations

        /// <summary>
        /// Requests a Store be retrieved
        /// </summary>
        /// <param name="id">Store ID</param>
        public void GetStore(string id)
        {
            GetStoreTask task = new GetStoreTask(StorageProvider.ParentServer, id);
            AddTask(task, GetStoreCallback);
        }

        /// <summary>
        /// Requests a Store be deleted
        /// </summary>
        /// <param name="id">Store ID</param>
        public void DeleteStore(string id)
        {
            DeleteStoreTask task = new DeleteStoreTask(StorageProvider.ParentServer, id);
            AddTask(task, DeleteStoreCallback);
        }

        /// <summary>
        /// Requests a store be created
        /// </summary>
        /// <param name="template">Template</param>
        public void CreateStore(IStoreTemplate template)
        {
            CreateStoreTask task = new CreateStoreTask(StorageProvider.ParentServer, template);
            AddTask(task, CreateStoreCallback);
        }

        #endregion

        #endregion

        #region Control Event Handlers

        private void btnSparqlQuery_Click(object sender, EventArgs e)
        {
            Query();
        }

        private void btnGraphRefresh_Click(object sender, EventArgs e)
        {
            ListGraphs();
        }

        private void btnRefreshStores_Click(object sender, EventArgs e)
        {
            ListStores();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            ofdImport.Filter = MimeTypesHelper.GetFilenameFilter(true, true, false, false, false, false);
            if (ofdImport.ShowDialog() == DialogResult.OK)
            {
                txtImportFile.Text = ofdImport.FileName;
            }
        }

        private void btnImportFile_Click(object sender, EventArgs e)
        {
            ImportFile();
        }

        private void btnImportUri_Click(object sender, EventArgs e)
        {
            ImportUri();
        }

        private void lvwGraphs_DoubleClick(object sender, EventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                if (graphUri.Equals("Default Graph")) graphUri = null;

                ViewGraph(graphUri);
            }
        }

        private void lvwGraphs_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                CopyMoveDragInfo info = new CopyMoveDragInfo(this, graphUri);
                DragDropEffects effects = DragDropEffects.Copy;
                if (StorageProvider.DeleteSupported) effects = effects | DragDropEffects.Move; //Move only possible if this storage supports DeleteGraph()

                lvwGraphs.DoDragDrop(info, effects);
            }
        }

        private void lvwGraphs_DragEnter(object sender, DragEventArgs e)
        {
            if ((e.AllowedEffect & DragDropEffects.Copy) == 0 && (e.AllowedEffect & DragDropEffects.Move) == 0) return;
            if (!e.Data.GetDataPresent(typeof (CopyMoveDragInfo))) return;

            //Cannot Copy/Move if a read-only storage is the target
            if (StorageProvider.IsReadOnly) return;

            CopyMoveDragInfo info = e.Data.GetData(typeof (CopyMoveDragInfo)) as CopyMoveDragInfo;
            if (info == null) return;

            DragDropEffects effects = DragDropEffects.Copy;
            if (info.Source.StorageProvider.DeleteSupported) effects = effects | DragDropEffects.Move; //Move only possible if the source storage supports DeleteGraph()
            e.Effect = effects;
        }

        private void lvwGraphs_DragDrop(object sender, DragEventArgs e)
        {
            if ((e.AllowedEffect & DragDropEffects.Copy) != 0 || (e.AllowedEffect & DragDropEffects.Move) != 0)
            {
                if (e.Data.GetDataPresent(typeof (CopyMoveDragInfo)))
                {
                    CopyMoveDragInfo info = e.Data.GetData(typeof (CopyMoveDragInfo)) as CopyMoveDragInfo;
                    if (info == null) return;

                    //Check whether Move is permitted?
                    if ((e.Effect & DragDropEffects.Move) != 0)
                    {
                        CopyMoveDialogue copyMoveConfirm = new CopyMoveDialogue(info, Connection);
                        if (copyMoveConfirm.ShowDialog() == DialogResult.OK)
                        {
                            if (copyMoveConfirm.IsMove)
                            {
                                info.Form.MoveGraph(info.SourceUri, Connection);
                            }
                            else if (copyMoveConfirm.IsCopy)
                            {
                                info.Form.CopyGraph(info.SourceUri, Connection);
                            }
                        }
                    }
                    else
                    {
                        //Just do a Copy
                        info.Form.CopyGraph(info.SourceUri, Connection);
                    }
                }
            }
        }

        private void timStartup_Tick(object sender, EventArgs e)
        {
            if (!StorageProvider.IsReady) return;
            CrossThreadSetText(stsCurrent, Resources.Status_Ready);
            ListGraphs();
            if (StorageProvider.ParentServer != null)
            {
                ListStores();
            }
            _timStartup.Stop();
        }

        private void btnSaveQuery_Click(object sender, EventArgs e)
        {
            sfdQuery.Filter = MimeTypesHelper.GetFilenameFilter(false, false, false, true, false, true);
            if (sfdQuery.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(sfdQuery.FileName))
                {
                    writer.Write(rtbSparqlQuery.Text);
                }
            }
        }

        private void btnLoadQuery_Click(object sender, EventArgs e)
        {
            ofdQuery.Filter = MimeTypesHelper.GetFilenameFilter(false, false, false, true, false, true);
            if (ofdQuery.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(ofdQuery.FileName))
                {
                    rtbSparqlQuery.Text = reader.ReadToEnd();
                }
            }
        }

        private void btnSparqlUpdate_Click(object sender, EventArgs e)
        {
            SparqlUpdate();
        }

        private void chkImportDefaultUri_CheckedChanged(object sender, EventArgs e)
        {
            txtImportDefaultGraph.Enabled = chkImportDefaultUri.Checked;
        }

        private void btnBrowseExport_Click(object sender, EventArgs e)
        {
            sfdExport.Filter = MimeTypesHelper.GetFilenameFilter(false, true, false, false, false, false);
            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                txtExportFile.Text = sfdExport.FileName;
            }
        }

        private void btnExportStore_Click(object sender, EventArgs e)
        {
            Export();
        }

        #region Graphs Context Menu

        private void mnuGraphs_Opening(object sender, CancelEventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                mnuDeleteGraph.Enabled = StorageProvider.DeleteSupported;
                mnuPreviewGraph.Text = string.Format("Preview first {0} Triples", Settings.Default.PreviewSize);
                mnuMoveGraphTo.Enabled = StorageProvider.DeleteSupported;
                mnuCopyGraph.Enabled = !StorageProvider.IsReadOnly;
                mnuRenameGraph.Enabled = !StorageProvider.IsReadOnly && StorageProvider.DeleteSupported;

                //Fill Copy To and Move To menus
                while (mnuCopyGraphTo.DropDownItems.Count > 2)
                {
                    mnuCopyGraphTo.DropDownItems.RemoveAt(2);
                }
                while (mnuMoveGraphTo.DropDownItems.Count > 2)
                {
                    mnuMoveGraphTo.DropDownItems.RemoveAt(2);
                }
                foreach (Connection connection in Program.ActiveConnections)
                {
                    if (!ReferenceEquals(connection.StorageProvider, StorageProvider) && !connection.StorageProvider.IsReadOnly)
                    {
                        //Copy To entry
                        ToolStripMenuItem item = new ToolStripMenuItem(connection.Name);
                        item.Tag = connection;
                        item.Click += _copyGraphHandler;
                        mnuCopyGraphTo.DropDownItems.Add(item);

                        //Move To entry
                        item = new ToolStripMenuItem(connection.Name);
                        item.Tag = connection;
                        item.Click += _moveGraphHandler;
                        mnuMoveGraphTo.DropDownItems.Add(item);
                    }
                }
                if (mnuCopyGraphTo.DropDownItems.Count == 2 && StorageProvider.IsReadOnly)
                {
                    mnuCopyGraphTo.Enabled = false;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void mnuViewGraph_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                if (graphUri.Equals("Default Graph")) graphUri = null;

                ViewGraph(graphUri);
            }
        }

        private void mnuDeleteGraph_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count <= 0) return;
            string graphUri = lvwGraphs.SelectedItems[0].Text;
            if (MessageBox.Show(string.Format(Resources.DeleteGraph_Confirm_Text, graphUri), Resources.DeleteGraph_Confirm_Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            if (graphUri.Equals("Default Graph")) graphUri = null;
            DeleteGraph(graphUri);
        }

        private void mnuPreviewGraph_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.Items.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                if (graphUri.Equals("Default Graph")) graphUri = null;

                PreviewGraph(graphUri);
            }
        }

        private void mnuCountTriples_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.Items.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                if (graphUri.Equals("Default Graph")) graphUri = null;

                CountTriples(graphUri);
            }
        }

        private void mnuCopyGraph_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                CopyGraph(graphUri, Connection);
            }
        }

        private void mnuRenameGraph_Click(object sender, EventArgs e)
        {
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                RenameGraph(graphUri);
            }
        }

        private void CopyGraphClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;
            if (!(item.Tag is Connection)) return;
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                CopyGraph(graphUri, item.Tag as Connection);
            }
        }

        private void MoveGraphClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;
            if (!(item.Tag is Connection)) return;
            if (lvwGraphs.SelectedItems.Count > 0)
            {
                string graphUri = lvwGraphs.SelectedItems[0].Text;
                MoveGraph(graphUri, item.Tag as Connection);
            }
        }

        #endregion

        #region Tasks Context Menu

        private void mnuTasks_Opening(object sender, CancelEventArgs e)
        {
            if (lvwTasks.SelectedItems.Count > 0)
            {
                ListViewItem item = lvwTasks.SelectedItems[0];
                object tag = item.Tag;
                if (tag != null)
                {
                    if (tag is QueryTask)
                    {
                        QueryTask qTask = (QueryTask) tag;
                        mnuViewErrors.Enabled = qTask.Error != null;
                        mnuViewResults.Enabled = (qTask.State == TaskState.Completed && qTask.Result != null);
                        mnuCancel.Enabled = qTask.IsCancellable;
                    }
                    else if (tag is BaseImportTask)
                    {
                        BaseImportTask importTask = (BaseImportTask) tag;
                        mnuViewErrors.Enabled = importTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = importTask.IsCancellable;
                    }
                    else if (tag is ListGraphsTask)
                    {
                        ListGraphsTask graphsTask = (ListGraphsTask) tag;
                        mnuViewErrors.Enabled = graphsTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = graphsTask.IsCancellable;
                    }
                    else if (tag is ListStoresTask)
                    {
                        ListStoresTask storesTask = (ListStoresTask) tag;
                        mnuViewErrors.Enabled = storesTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = storesTask.IsCancellable;
                    }
                    else if (tag is GetStoreTask)
                    {
                        GetStoreTask getStoreTask = (GetStoreTask) tag;
                        mnuViewErrors.Enabled = getStoreTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = getStoreTask.IsCancellable;
                    }
                    else if (tag is CountTriplesTask)
                    {
                        CountTriplesTask countTask = (CountTriplesTask) tag;
                        mnuViewErrors.Enabled = countTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = countTask.IsCancellable;
                    }
                    else if (tag is GenerateEntitiesQueryTask)
                    {
                        GenerateEntitiesQueryTask genTask = (GenerateEntitiesQueryTask) tag;
                        mnuViewErrors.Enabled = genTask.Error != null;
                        mnuViewResults.Enabled = genTask.State == TaskState.Completed;
                        mnuCancel.Enabled = genTask.IsCancellable;
                    }
                    else if (tag is ITask<IGraph>)
                    {
                        ITask<IGraph> graphTask = (ITask<IGraph>) tag;
                        mnuViewErrors.Enabled = graphTask.Error != null;
                        mnuViewResults.Enabled = (graphTask.State == TaskState.Completed && graphTask.Result != null);
                        mnuCancel.Enabled = graphTask.IsCancellable;
                    }
                    else if (tag is ITask<TaskResult>)
                    {
                        ITask<TaskResult> basicTask = (ITask<TaskResult>) tag;
                        mnuViewErrors.Enabled = basicTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = basicTask.IsCancellable;
                    }
                    else if (tag is ITask<TaskValueResult<bool>>)
                    {
                        ITask<TaskValueResult<bool>> boolTask = (ITask<TaskValueResult<bool>>) tag;
                        mnuViewErrors.Enabled = boolTask.Error != null;
                        mnuViewResults.Enabled = false;
                        mnuCancel.Enabled = boolTask.IsCancellable;
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void mnuCancel_Click(object sender, EventArgs e)
        {
            if (lvwTasks.SelectedItems.Count > 0)
            {
                ListViewItem item = lvwTasks.SelectedItems[0];
                object tag = item.Tag;

                if (tag is CancellableTask<TaskResult>)
                {
                    ((CancellableTask<TaskResult>) tag).Cancel();
                }
            }
        }

        private void mnuViewDetail_Click(object sender, EventArgs e)
        {
            if (lvwTasks.SelectedItems.Count > 0)
            {
                ListViewItem item = lvwTasks.SelectedItems[0];
                object tag = item.Tag;

                if (tag is QueryTask)
                {
                    TaskInformationForm<object> queryInfo = new TaskInformationForm<object>((QueryTask) tag, Connection.Name);
                    queryInfo.MdiParent = MdiParent;
                    queryInfo.Show();
                }
                else if (tag is UpdateTask)
                {
                    TaskInformationForm<TaskResult> updateInfo = new TaskInformationForm<TaskResult>((UpdateTask) tag, Connection.Name);
                    updateInfo.MdiParent = MdiParent;
                    updateInfo.Show();
                }
                else if (tag is ListGraphsTask)
                {
                    TaskInformationForm<IEnumerable<Uri>> listInfo = new TaskInformationForm<IEnumerable<Uri>>((ListGraphsTask) tag, Connection.Name);
                    listInfo.MdiParent = MdiParent;
                    listInfo.Show();
                }
                else if (tag is ListStoresTask)
                {
                    TaskInformationForm<IEnumerable<string>> storeInfo = new TaskInformationForm<IEnumerable<string>>((ListStoresTask) tag, Connection.Name);
                    storeInfo.MdiParent = MdiParent;
                    storeInfo.Show();
                }
                else if (tag is GetStoreTask)
                {
                    TaskInformationForm<IStorageProvider> getStoreInfo = new TaskInformationForm<IStorageProvider>((GetStoreTask) tag, Connection.Name);
                    getStoreInfo.MdiParent = MdiParent;
                    getStoreInfo.Show();
                }
                else if (tag is CountTriplesTask)
                {
                    TaskInformationForm<TaskValueResult<int>> countInfo = new TaskInformationForm<TaskValueResult<int>>((CountTriplesTask) tag, Connection.Name);
                    countInfo.MdiParent = MdiParent;
                    countInfo.Show();
                }
                else if (tag is GenerateEntitiesQueryTask)
                {
                    TaskInformationForm<string> genEntitiesQueryInfo = new TaskInformationForm<string>((GenerateEntitiesQueryTask) tag, Connection.Name);
                    genEntitiesQueryInfo.MdiParent = MdiParent;
                    genEntitiesQueryInfo.Show();
                }
                else if (tag is ITask<IGraph>)
                {
                    TaskInformationForm<IGraph> graphInfo = new TaskInformationForm<IGraph>((ITask<IGraph>) tag, Connection.Name);
                    graphInfo.MdiParent = MdiParent;
                    graphInfo.Show();
                }
                else if (tag is ITask<TaskResult>)
                {
                    TaskInformationForm<TaskResult> simpleInfo = new TaskInformationForm<TaskResult>((ITask<TaskResult>) tag, Connection.Name);
                    simpleInfo.MdiParent = MdiParent;
                    simpleInfo.Show();
                }
                else if (tag is ITask<TaskValueResult<bool>>)
                {
                    TaskInformationForm<TaskValueResult<bool>> boolInfo = new TaskInformationForm<TaskValueResult<bool>>((ITask<TaskValueResult<bool>>) tag, Connection.Name);
                    boolInfo.MdiParent = MdiParent;
                    boolInfo.Show();
                }
                else
                {
                    MessageBox.Show(Resources.TaskInfo_Unavailable_Text, Resources.TaskInfo_Unavailable_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void mnuViewErrors_Click(object sender, EventArgs e)
        {
            if (lvwTasks.SelectedItems.Count > 0)
            {
                ListViewItem item = lvwTasks.SelectedItems[0];
                object tag = item.Tag;

                if (tag is QueryTask)
                {
                    TaskErrorTraceForm<object> queryInfo = new TaskErrorTraceForm<object>((ITask<object>) tag, Connection.Name);
                    queryInfo.MdiParent = MdiParent;
                    queryInfo.Show();
                }
                else if (tag is ListGraphsTask)
                {
                    TaskErrorTraceForm<IEnumerable<Uri>> listInfo = new TaskErrorTraceForm<IEnumerable<Uri>>((ITask<IEnumerable<Uri>>) tag, Connection.Name);
                    listInfo.MdiParent = MdiParent;
                    listInfo.Show();
                }
                else if (tag is ListStoresTask)
                {
                    TaskErrorTraceForm<IEnumerable<string>> storeInfo = new TaskErrorTraceForm<IEnumerable<string>>((ListStoresTask) tag, Connection.Name);
                    storeInfo.MdiParent = MdiParent;
                    storeInfo.Show();
                }
                else if (tag is GetStoreTask)
                {
                    TaskErrorTraceForm<IStorageProvider> getStoreInfo = new TaskErrorTraceForm<IStorageProvider>((GetStoreTask) tag, Connection.Name);
                    getStoreInfo.MdiParent = MdiParent;
                    getStoreInfo.Show();
                }
                else if (tag is ITask<IGraph>)
                {
                    TaskErrorTraceForm<IGraph> graphInfo = new TaskErrorTraceForm<IGraph>((ITask<IGraph>) tag, Connection.Name);
                    graphInfo.MdiParent = MdiParent;
                    graphInfo.Show();
                }
                else if (tag is ITask<TaskResult>)
                {
                    TaskErrorTraceForm<TaskResult> simpleInfo = new TaskErrorTraceForm<TaskResult>((ITask<TaskResult>) tag, Connection.Name);
                    simpleInfo.MdiParent = MdiParent;
                    simpleInfo.Show();
                }
                else if (tag is ITask<TaskValueResult<bool>>)
                {
                    TaskErrorTraceForm<TaskValueResult<bool>> boolInfo = new TaskErrorTraceForm<TaskValueResult<bool>>((ITask<TaskValueResult<bool>>) tag, Connection.Name);
                    boolInfo.MdiParent = MdiParent;
                    boolInfo.Show();
                }
                else if (tag is ITask<string>)
                {
                    TaskErrorTraceForm<string> stringInfo = new TaskErrorTraceForm<string>((ITask<string>) tag, Connection.Name);
                    stringInfo.MdiParent = MdiParent;
                    stringInfo.Show();
                }
                else
                {
                    MessageBox.Show(Resources.TaskInfo_Unavailable_Text, Resources.TaskInfo_Unavailable_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void mnuViewResults_Click(object sender, EventArgs e)
        {
            if (lvwTasks.SelectedItems.Count <= 0) return;
            ListViewItem item = lvwTasks.SelectedItems[0];
            object tag = item.Tag;

            if (tag is QueryTask)
            {
                QueryTask qTask = (QueryTask) tag;
                if (qTask.State == TaskState.Completed && qTask.Result != null)
                {
                    object result = qTask.Result;

                    if (result is IGraph)
                    {
                        GraphViewerForm graphViewer = new GraphViewerForm((IGraph) result, Connection.Name);
                        CrossThreadSetMdiParent(graphViewer);
                        CrossThreadShow(graphViewer);
                    }
                    else if (result is SparqlResultSet)
                    {
                        ResultSetViewerForm resultsViewer = new ResultSetViewerForm((SparqlResultSet) result, qTask.Query != null ? qTask.Query.NamespaceMap : null, Connection.Name);
                        CrossThreadSetMdiParent(resultsViewer);
                        CrossThreadShow(resultsViewer);
                    }
                    else
                    {
                        CrossThreadMessage(Resources.QueryResults_NotViewable_Text, Resources.QueryResults_NotViewable_Title, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    CrossThreadMessage(Resources.QueryResults_Unavailable_Text, Resources.QueryResults_Unavailable_Title, MessageBoxIcon.Error);
                }
            }
            else if (tag is GenerateEntitiesQueryTask)
            {
                GenerateEntitiesQueryTask genTask = (GenerateEntitiesQueryTask) tag;
                StringResultDialogue dialogue = new StringResultDialogue(string.Format("Generated Entity Query on {0}", Connection.Name), genTask.Result, rtbSparqlQuery, "Query Editor");
                CrossThreadSetMdiParent(dialogue);
                CrossThreadShow(dialogue);
            }
            else if (tag is ITask<IGraph>)
            {
                ITask<IGraph> graphTask = (ITask<IGraph>) tag;
                if (graphTask.Result != null)
                {
                    GraphViewerForm graphViewer = new GraphViewerForm(graphTask.Result, Connection.Name);
                    CrossThreadSetMdiParent(graphViewer);
                    CrossThreadShow(graphViewer);
                }
                else
                {
                    CrossThreadMessage(Resources.Graph_Unavailable_Text, Resources.Graph_Unavailable_Title, MessageBoxIcon.Error);
                }
            }
            else if (tag is ITask<string>)
            {
            }
        }

        #endregion

        #region Stores Context Menu

        private void mnuStores_Opening(object sender, CancelEventArgs e)
        {
            IStorageServer server = StorageProvider.ParentServer;
            if (server == null)
            {
                e.Cancel = true;
                return;
            }

            mnuNewStore.Enabled = (server.IOBehaviour & IOBehaviour.CanCreateStores) != 0;
            if (lvwStores.SelectedItems.Count > 0)
            {
                mnuOpenStore.Enabled = true;
                mnuDeleteStore.Enabled = (server.IOBehaviour & IOBehaviour.CanDeleteStores) != 0;
            }
            else
            {
                mnuOpenStore.Enabled = false;
                mnuDeleteStore.Enabled = false;
            }
        }

        private void mnuNewStore_Click(object sender, EventArgs e)
        {
            NewStoreForm newStore = new NewStoreForm(StorageProvider.ParentServer);
            if (newStore.ShowDialog() == DialogResult.OK)
            {
                CreateStore(newStore.Template);
            }
        }

        private void mnuOpenStore_Click(object sender, EventArgs e)
        {
            if (lvwStores.SelectedItems.Count > 0)
            {
                string id = lvwStores.SelectedItems[0].Text;
                GetStore(id);
            }
            else
            {
                MessageBox.Show(Resources.OpenStore_Error_Text, Resources.OpenStore_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void mnuDeleteStore_Click(object sender, EventArgs e)
        {
            if (lvwStores.SelectedItems.Count > 0)
            {
                string id = lvwStores.SelectedItems[0].Text;
                if (MessageBox.Show(string.Format(Resources.DeleteStore_Confirm_Text, id), Resources.DeleteStore_Confirm_Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DeleteStore(id);
                }
            }
        }

        #endregion

        #endregion

        #region Task Management

        private void AddTask<T>(ITask<T> task, TaskCallback<T> callback) where T : class
        {
            string[] items = new string[]
                {
                    (++_taskId).ToString(System.Globalization.CultureInfo.CurrentUICulture),
                    task.Name,
                    task.State.GetStateDescription(),
                    task.Information
                };
            ListViewItem item = new ListViewItem(items);
            item.Tag = task;
            CrossThreadAddItem(lvwTasks, item);

            //Ensure that the Task Information gets updated automatically when the Task State changes
            TaskStateChanged d = delegate
                {
                    CrossThreadAlterSubItem(item, 2, task.State.GetStateDescription());
                    CrossThreadAlterSubItem(item, 3, task.Information);
                    CrossThreadRefresh(lvwTasks);
                };
            task.StateChanged += d;

            //Clear old Tasks if necessary and enabled
            if (chkRemoveOldTasks.Checked)
            {
                if (lvwTasks.Items.Count > 10)
                {
                    int i = lvwTasks.Items.Count - 1;
                    do
                    {
                        ListViewItem oldItem = lvwTasks.Items[i];
                        if (oldItem.Tag is ITaskBase)
                        {
                            ITaskBase t = (ITaskBase) oldItem.Tag;
                            if (t.State == TaskState.Completed || t.State == TaskState.CompletedWithErrors)
                            {
                                lvwTasks.Items.RemoveAt(i);
                                i--;
                            }
                        }

                        i--;
                    } while (lvwTasks.Items.Count > 10 && i >= 0);
                }
            }

            //Start the Task
            task.RunTask(callback);
        }

        private void ListGraphsCallback(ITask<IEnumerable<Uri>> task)
        {
            if (task.State == TaskState.Completed && task.Result != null)
            {
                CrossThreadSetText(stsCurrent, Resources.Status_RenderingGraphList);
                CrossThreadSetVisibility(lvwGraphs, true);
                CrossThreadBeginUpdate(lvwGraphs);
                CrossThreadClear(lvwGraphs);

                foreach (Uri u in task.Result)
                {
                    CrossThreadAdd(lvwGraphs, u != null ? u.AbsoluteUri : "Default Graph");
                }

                CrossThreadEndUpdate(lvwGraphs);

                CrossThreadSetText(stsCurrent, Resources.Status_Ready);
                CrossThreadSetEnabled(btnGraphRefresh, true);

                task.Information = string.Format(Resources.ListGraphs_Information, lvwGraphs.Items.Count);
            }
            else
            {
                CrossThreadSetText(stsCurrent, Resources.Status_GraphListingUnavailable);
                if (task.Error != null)
                {
                    CrossThreadMessage(string.Format(Resources.ListGraphs_Error_Text, task.Error.Message), Resources.ListGraphs_Error_Title, MessageBoxIcon.Warning);
                }
                CrossThreadSetVisibility(lvwGraphs, false);
                CrossThreadSetVisibility(lblGraphListUnavailable, true);
                CrossThreadRefresh(tabGraphs);
            }
        }

        private void ListStoresCallback(ITask<IEnumerable<string>> task)
        {
            if (task.State == TaskState.Completed && task.Result != null)
            {
                CrossThreadSetText(stsCurrent, Resources.Status_RenderingStoreList);
                CrossThreadSetVisibility(lvwStores, true);
                CrossThreadBeginUpdate(lvwStores);
                CrossThreadClear(lvwStores);

                foreach (string id in task.Result)
                {
                    if (id != null)
                    {
                        CrossThreadAdd(lvwStores, id);
                    }
                }

                CrossThreadEndUpdate(lvwStores);

                CrossThreadSetText(stsCurrent, Resources.Status_Ready);
                CrossThreadSetEnabled(btnRefreshStores, true);

                task.Information = string.Format(Resources.ListStores_Information, lvwStores.Items.Count);
            }
            else
            {
                CrossThreadSetText(stsCurrent, Resources.Status_StoreListUnavailable);
                if (task.Error != null)
                {
                    CrossThreadMessage(string.Format(Resources.ListStores_Error_Text, task.Error.Message), Resources.ListStores_Error_Title, MessageBoxIcon.Warning);
                }
                CrossThreadRefresh(tabServer);
            }
        }

        private void ViewGraphCallback(ITask<IGraph> task)
        {
            if (task.State == TaskState.Completed && task.Result != null)
            {
                GraphViewerForm graphViewer = new GraphViewerForm(task.Result, Connection.Name);
                CrossThreadSetMdiParent(graphViewer);
                CrossThreadShow(graphViewer);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage(string.Format(Resources.ViewGraph_Error_Text, task.Error.Message), Resources.ViewGraph_Error_Title, MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage(Resources.ViewGraph_UnknownError_Text, Resources.ViewGraph_Error_Title, MessageBoxIcon.Error);
                }
            }
        }

        private void PreviewGraphCallback(ITask<IGraph> task)
        {
            if (task.State == TaskState.Completed && task.Result != null)
            {
                GraphViewerForm graphViewer = new GraphViewerForm(task.Result, Connection.Name);
                CrossThreadSetMdiParent(graphViewer);
                CrossThreadShow(graphViewer);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Preview Graph Failed due to the following error: " + task.Error.Message, "Preview Graph Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Preview Graph Failed due to an unknown error", "Preview Graph Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void CountTriplesCallback(ITask<TaskValueResult<int>> task)
        {
            if (task.State == TaskState.Completed && task.Result != null)
            {
                CrossThreadMessage(task.Information, Resources.TriplesCounted, MessageBoxIcon.Information);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Count Triples Failed due to the following error: " + task.Error.Message, "Count Triples Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Count Triples Failed due to an unknown error", "Count Triples Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteGraphCallback(ITask<TaskResult> task)
        {
            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage(task.Information, "Deleted Graph OK", MessageBoxIcon.Information);
                ListGraphs();
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Delete Graph Failed due to the following error: " + task.Error.Message, "Delete Graph Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Delete Graph Failed due to an unknown error", "Delete Graph Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void QueryCallback(ITask<object> task)
        {
            QueryTask qTask;
            if (task is QueryTask)
            {
                qTask = (QueryTask) task;
                if (qTask.Query != null)
                {
                    try
                    {
                        if (task.State == TaskState.Completed)
                        {
                            CrossThreadSetText(stsCurrent, "Query Completed OK (Took " + qTask.Query.QueryExecutionTime.Value.ToString() + ")");
                        }
                        else
                        {
                            CrossThreadSetText(stsCurrent, "Query Failed (Took " + qTask.Query.QueryExecutionTime.Value.ToString() + ")");
                        }
                    }
                    catch
                    {
                        CrossThreadSetText(stsCurrent, qTask.State == TaskState.Completed ? "Query Completed OK" : "Query Failed");
                    }
                }
            }
            else
            {
                CrossThreadMessage("Unexpected Error - QueryCallback was invoked but the given task was not a QueryTask", "Unexpected Error", MessageBoxIcon.Exclamation);
                return;
            }

            if (task.State == TaskState.Completed)
            {
                object result = task.Result;

                if (result is IGraph)
                {
                    CrossThreadShowQueryPanel(splitQueryResults);
                    DisplayQueryResults(qTask);
                }
                else if (result is SparqlResultSet)
                {
                    CrossThreadShowQueryPanel(splitQueryResults);
                    DisplayQueryResults(qTask);
                }
                else
                {
                    CrossThreadMessage("Unable to show Query Results as did not get a Graph/Result Set as expected", "Unable to Show Results", MessageBoxIcon.Error);
                }
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Query Failed due to the following error: " + task.Error.Message, "Query Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Query Failed due to an unknown error", "Query Failed", MessageBoxIcon.Error);
                }
            }
        }

        private delegate void DisplayQueryResultsDelegate(QueryTask task);

        private void DisplayQueryResults(QueryTask task)
        {
            if (InvokeRequired)
            {
                DisplayQueryResultsDelegate d = DisplayQueryResults;
                Invoke(d, new object[] {task});
            }
            else
            {
                TabPage tabPage = new TabPage();
                tabPage.Text = "Query " + ++_queryId;
                QueryResultsControl control = new QueryResultsControl();
                control.Namespaces = task.Query != null ? task.Query.NamespaceMap : null;
                control.QueryString = task.QueryString;
                control.DataSource = task.Result;
                control.CloseRequested += delegate
                    {
                        tabResults.TabPages.Remove(tabPage);
                        if (tabResults.TabPages.Count == 0) splitQueryResults.Panel2Collapsed = true;
                    };
                control.DetachRequested += delegate
                    {
                        if (control.DataSource is SparqlResultSet)
                        {
                            ResultSetViewerForm resultsViewer = new ResultSetViewerForm((SparqlResultSet) control.DataSource, control.Namespaces, Connection.Name);
                            CrossThreadSetMdiParent(resultsViewer);
                            CrossThreadShow(resultsViewer);
                        }
                        else if (control.DataSource is IGraph)
                        {
                            GraphViewerForm graphViewer = new GraphViewerForm((IGraph)control.DataSource, Connection.Name);
                            CrossThreadSetMdiParent(graphViewer);
                            CrossThreadShow(graphViewer);
                        }
                        tabResults.TabPages.Remove(tabPage);
                        if (tabResults.TabPages.Count == 0) splitQueryResults.Panel2Collapsed = true;
                    };
                tabPage.SuspendLayout();
                tabPage.Controls.Add(control);
                tabResults.TabPages.Add(tabPage);
                tabResults.SelectTab(tabPage);
                tabPage.ResumeLayout();

                // Try and get control to take up all available space
                control.SuspendLayout();
                control.Anchor = AnchorStyles.Bottom |
                                 AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                control.Height = tabPage.Height;
                control.Width = tabPage.Width;
                control.ResumeLayout();
                
            }
        }

        private void GenerateEntitiesQueryCallback(ITask<string> task)
        {
            if (task.State == TaskState.Completed)
            {
                StringResultDialogue dialogue = new StringResultDialogue(string.Format("Generated Entity Query on {0}", Connection.Name), task.Result, rtbSparqlQuery, "Query Editor");
                CrossThreadSetMdiParent(dialogue);
                CrossThreadShow(dialogue);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Generating an entities query failed due to the following error: " + task.Error.Message, "Generate Entities Query Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Generating an entities query failed due to an unknown error", "Generate Entities Query Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateCallback(ITask<TaskResult> task)
        {
            if (task is UpdateTask)
            {
                UpdateTask uTask = (UpdateTask) task;
                if (uTask.Updates != null)
                {
                    try
                    {
                        if (task.State == TaskState.Completed)
                        {
                            CrossThreadSetText(stsCurrent, "Updates Completed OK (Took " + uTask.Updates.UpdateExecutionTime.Value.ToString() + ")");
                        }
                        else
                        {
                            CrossThreadSetText(stsCurrent, "Updates Failed (Took " + uTask.Updates.UpdateExecutionTime.Value.ToString() + ")");
                        }
                    }
                    catch
                    {
                        CrossThreadSetText(stsCurrent, uTask.State == TaskState.Completed ? "Updates Completed OK" : "Updates Failed");
                    }
                }
            }

            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage("Updates Completed successfully", "Updates Completed", MessageBoxIcon.Information);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Updates Failed due to the following error: " + task.Error.Message, "Updates Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Updates Failed due to an unknown error", "Updates Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void ImportCallback(ITask<TaskResult> task)
        {
            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage("Import Completed OK\n" + task.Information, "Import Completed", MessageBoxIcon.Information);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Import Failed due to the following error: " + task.Error.Message, "Import Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Import Failed due to an unknown error", "Import Failed", MessageBoxIcon.Error);
                }
            }
            ListGraphs();
        }

        private void ExportCallback(ITask<TaskResult> task)
        {
            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage("Export Completed OK - " + task.Information, "Export Completed", MessageBoxIcon.Information);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage("Export Failed due to the following error: " + task.Error.Message, "Export Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage("Export Failed due to an unknown error", "Export Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void CopyMoveRenameCallback(ITask<TaskResult> task)
        {
            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage(task.Name + " Completed OK - " + task.Information, task.Name + " Completed", MessageBoxIcon.Information);
                ListGraphs();

                if (task is CopyMoveTask)
                {
                    CopyMoveTask cmTask = (CopyMoveTask) task;
                    if (!ReferenceEquals(Connection, cmTask.Target))
                    {
                        foreach (StoreManagerForm managerForm in Program.MainForm.MdiChildren.OfType<StoreManagerForm>())
                        {
                            if (ReferenceEquals(this, managerForm) || !ReferenceEquals(cmTask.Target, managerForm.Connection)) continue;
                            managerForm.ListGraphs();
                            break;
                        }
                    }
                }
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage(task.Name + " Failed due to the following error: " + task.Error.Message, task.Name + " Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage(task.Name + " Failed due to an unknown error", task.Name + " Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void GetStoreCallback(ITask<IStorageProvider> task)
        {
            if (task.State == TaskState.Completed)
            {
                // TODO Need a better way to modify the connection definition appropriately to set the relevant Store ID - currently this is done via a hack in the Connection constructor
                IConnectionDefinition definition = Connection.Definition.Copy();
                Connection connection = new Connection(definition, task.Result);
                Program.MainForm.ShowStoreManagerForm(connection);
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage(task.Name + " Failed due to the following error: " + task.Error.Message, task.Name + " Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage(task.Name + " Failed due to an unknown error", task.Name + " Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteStoreCallback(ITask<TaskResult> task)
        {
            if (task.State == TaskState.Completed)
            {
                CrossThreadMessage(task.Name + " Completed OK - " + task.Information, task.Name + " Completed", MessageBoxIcon.Information);
                ListStores();
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage(task.Name + " Failed due to the following error: " + task.Error.Message, task.Name + " Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage(task.Name + " Failed due to an unknown error", task.Name + " Failed", MessageBoxIcon.Error);
                }
            }
        }

        private void CreateStoreCallback(ITask<TaskValueResult<bool>> task)
        {
            if (task.State == TaskState.Completed)
            {
                if (task.Result.Value == true)
                {
                    CrossThreadMessage(task.Name + " Completed OK - " + task.Information, task.Name + " Completed", MessageBoxIcon.Information);
                    ListStores();
                }
                else
                {
                    CrossThreadMessage(task.Name + " Failed - Underlying Server returned that a Store was not created", task.Name + " Failed", MessageBoxIcon.Warning);
                }
            }
            else
            {
                if (task.Error != null)
                {
                    CrossThreadMessage(task.Name + " Failed due to the following error: " + task.Error.Message, task.Name + " Failed", MessageBoxIcon.Error);
                }
                else
                {
                    CrossThreadMessage(task.Name + " Failed due to an unknown error", task.Name + " Failed", MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (Connection.ActiveUsers > 1)
            {
                CloseConnectionDialogue closeConnection = new CloseConnectionDialogue();
                if (closeConnection.ShowDialog() == DialogResult.Cancel)
                {
                    cancelEventArgs.Cancel = true;
                    return;
                }
                ForceClose = closeConnection.ForceClose;
            }
            else
            {
                ForceClose = false;
            }
            _closing = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            Connection.Close(ForceClose);
            base.OnClosed(e);
        }

        private void btnChangeOrientation_Click(object sender, EventArgs e)
        {
            if (splitQueryResults.Orientation == Orientation.Horizontal)
            {
                splitQueryResults.Orientation = Orientation.Vertical;
            }
            else
            {
                splitQueryResults.Orientation = Orientation.Horizontal;
            }
        }

        private void btnOpenEntityGeneratorForm_Click(object sender, EventArgs e)
        {
            EntityQueryGeneratorDialogue queryGeneratorDialogue = new EntityQueryGeneratorDialogue(rtbSparqlQuery.Text);
            if (queryGeneratorDialogue.ShowDialog() == DialogResult.OK)
            {
                GenerateEntitiesQuery(queryGeneratorDialogue.QueryString, queryGeneratorDialogue.MinPredicateUsageLimit);
            }
        }

        private void btnFormatQuery_Click(object sender, EventArgs e)
        {
            var rtb = rtbSparqlQuery;

            if (_codeFormatInProgress) return;
            try
            {
                rtb.Text = ReformatText(rtb.Text);
            }
            finally
            {
                _codeFormatInProgress = false;
            }
        }

        private string ReformatText(string text)
        {
            _codeFormatInProgress = true;
            string[] currentText = Regex.Split(text, "\r?\n");

            int lvl = 0;
            string newString = "";
            bool lineAdded = false;
            foreach (string line in currentText)
            {
                if (line.Contains("{"))
                {
                    newString += ApplyIndentation(lvl) + line.TrimStart(' ') + "\r\n";
                    lineAdded = true;
                    lvl += line.Count(f => f == '{');
                }
                if (line.Contains("}"))
                {
                    lvl -= line.Count(f => f == '}');
                    if (!lineAdded)
                    {
                        newString += ApplyIndentation(lvl) + line.TrimStart(' ') + "\r\n";
                        lineAdded = true;
                    }
                }

                if (!lineAdded)
                {
                    newString += ApplyIndentation(lvl) + line.TrimStart(' ') + "\r\n";
                }

                lineAdded = false;
            }

            return newString.TrimEnd('\n').TrimEnd('\r');
        }

        private static string ApplyIndentation(int indentLevel)
        {
            string space = "";
            if (indentLevel > 0)
            {
                for (int lvl = 0; lvl < indentLevel; lvl++)
                {
                    space += " ".PadLeft(2);
                }
            }

            return space;
        }

        private void rtbSparqlQuery_TextChanged(object sender, EventArgs e)
        {
            // Reset timer
            _highlightsUpdateTimer.Stop();
            if (!_codeHighLightingInProgress)
            {
                _highlightsUpdateTimer.Start();
            }
        }

        private void HighlightsUpdateTimerOnTick(object sender, EventArgs eventArgs)
        {
            _highlightsUpdateTimer.Stop();
            ActivateHighlighting();
        }

        private void ActivateHighlighting()
        {
            if (_codeHighLightingInProgress) return;
            _codeHighLightingInProgress = true;

            rtbSparqlQuery.BeginUpdate();
            int initialSelectionStart = rtbSparqlQuery.SelectionStart;
            ClearHighlighting();
            HighlightText("prefix", Color.DarkBlue);

            HighlightText("select", Color.Blue);
            HighlightText("FROM", Color.Blue);
            HighlightText("FROM NAMED", Color.Blue);
            HighlightText("GRAPH", Color.Blue);

            HighlightText("describe", Color.Blue);
            HighlightText("ask", Color.Blue);
            HighlightText("construct", Color.Blue);

            HighlightText("where", Color.Blue);
            HighlightText("filter", Color.Blue);
            HighlightText("distinct", Color.Blue);
            HighlightText("optional", Color.Blue);

            HighlightText("order by", Color.Blue);
            HighlightText("limit", Color.Blue);
            HighlightText("offset", Color.Blue);
            HighlightText("REDUCED", Color.Blue);


            HighlightText("GROUP BY", Color.Blue);
            HighlightText("HAVING", Color.Blue);

            rtbSparqlQuery.SelectionStart = initialSelectionStart;
            rtbSparqlQuery.SelectionLength = 0;
            rtbSparqlQuery.EndUpdate();
            rtbSparqlQuery.Invalidate();
            _codeHighLightingInProgress = false;
        }

        private void HighlightText(string text, Color color)
        {
            if (text.Length <= 0) return;
            int startPosition = 0;
            int foundPosition = 0;
            while (foundPosition > -1)
            {
                foundPosition = rtbSparqlQuery.Find(text, startPosition, RichTextBoxFinds.WholeWord);
                if (foundPosition < 0) continue;
                rtbSparqlQuery.Select(foundPosition, text.Length);
                rtbSparqlQuery.SelectionColor = color;
                startPosition = foundPosition + text.Length;
                _highLights.Add(new HighLight() {Start = foundPosition, End = text.Length});
            }
        }

        private void ClearHighlighting()
        {
            rtbSparqlQuery.Select(0, rtbSparqlQuery.TextLength - 1);
            rtbSparqlQuery.SelectionColor = rtbSparqlQuery.ForeColor;
            _highLights.Clear();
        }
    }

    internal class HighLight
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    /// <summary>
    /// Comparer for sorting tasks in ascending order by their IDs
    /// </summary>
    internal class SortTasksById
        : IComparer, IComparer<ListViewItem>
    {
        /// <summary>
        /// Compares two tasks
        /// </summary>
        /// <param name="x">Task</param>
        /// <param name="y">Task</param>
        /// <returns></returns>
        public int Compare(ListViewItem x, ListViewItem y)
        {
            int a;
            if (!int.TryParse(x.SubItems[0].Text, out a)) return -1;
            int b;
            if (int.TryParse(y.SubItems[0].Text, out b)) return -1*a.CompareTo(b);
            return 1;
        }

        #region IComparer Members

        /// <summary>
        /// Compares two tasks
        /// </summary>
        /// <param name="x">Task</param>
        /// <param name="y">Task</param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            if (x is ListViewItem && y is ListViewItem)
            {
                return Compare((ListViewItem) x, (ListViewItem) y);
            }
            return 0;
        }

        #endregion
    }
}