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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using VDS.RDF.GUI;
using VDS.RDF.GUI.WinForms;
using VDS.RDF.GUI.WinForms.Forms;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.FullText.Indexing;
using VDS.RDF.Query.FullText.Indexing.Lucene;
using VDS.RDF.Query.FullText.Schema;
using VDS.RDF.Query.FullText.Search;
using VDS.RDF.Query.FullText.Search.Lucene;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Utilities.Sparql
{
    public partial class fclsSparqlGui : Form
    {
        private TripleStore _store;
        private ISparqlDataset _dataset;
        private LeviathanQueryProcessor _processor;
        private IRdfWriter _rdfwriter = new CompressingTurtleWriter(WriterCompressionLevel.High);
        private ISparqlResultsWriter _resultswriter = new SparqlHtmlWriter();
        private string _rdfext = ".ttl";
        private string _resultsext = ".html";
        private bool _noDataWarning = true, _logExplain = false;
        private string _logfile;
        private long _tripleCount = 0;
        private SparqlQuerySyntax _querySyntax = SparqlQuerySyntax.Sparql_1_1;

        //Full Text Indexing stuff
        private Lucene.Net.Store.Directory _ftIndex;
        private IFullTextIndexer _ftIndexer;
        private IFullTextSearchProvider _ftSearcher;
        private FullTextOptimiser _ftOptimiser;

        public fclsSparqlGui()
        {
            InitializeComponent();
            Constants.WindowIcon = Icon;
            _store = new TripleStore();
            _dataset = new InMemoryQuadDataset(_store);
            _processor = new LeviathanQueryProcessor(_dataset, GetProcessorOptions());

            // Enable UTF-8 BOM setting if user set
            if (Properties.Settings.Default.UseUtf8Bom)
            {
                chkUseUtf8Bom.Checked = true;
            }

            string temp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sep = new string(new char[] { Path.DirectorySeparatorChar });
            if (!temp.EndsWith(sep)) temp += sep;
            temp = Path.Combine(temp, @"dotNetRDF\");
            if (!System.IO.Directory.Exists(temp)) System.IO.Directory.CreateDirectory(temp);
            temp = Path.Combine(temp, @"SparqlGUI\");
            if (!System.IO.Directory.Exists(temp)) System.IO.Directory.CreateDirectory(temp);
            _logfile = Path.Combine(temp, "SparqlGui-" + DateTime.Now.ToString("MMM-yyyy") + ".log");

            ofdBrowse.Filter = MimeTypesHelper.GetFilenameFilter(true, true, false, false, false, true);
            ofdQuery.Filter = MimeTypesHelper.GetFilenameFilter(false, false, false, true, false, true);
        }

        private void fclsSparqlGui_Load(object sender, EventArgs e)
        {
            if (File.Exists("default.rq"))
            {
                StreamReader reader = new StreamReader("default.rq");
                string defaultQuery = reader.ReadToEnd();
                txtQuery.Text = defaultQuery;
                reader.Close();
            }
            cboGraphFormat.SelectedIndex = 5;
            cboResultsFormat.SelectedIndex = 2;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofdBrowse.ShowDialog() == DialogResult.OK)
            {
                txtSourceFile.Text = ofdBrowse.FileName;
            }
        }

        private void btnImportFile_Click(object sender, EventArgs e)
        {
            if (txtSourceFile.Text.Equals(string.Empty))
            {
                MessageBox.Show("Please enter a File you wish to import RDF from...", "No File Specified");
            }
            else
            {
                try
                {
                    //Try and get a Graph Parser and load
                    IRdfReader parser = MimeTypesHelper.GetParserByFileExtension(MimeTypesHelper.GetTrueFileExtension(txtSourceFile.Text));
                    Graph g = new Graph();
                    FileLoader.Load(g, txtSourceFile.Text);
                    LogImportSuccess(txtSourceFile.Text, 1, g.Triples.Count);

                    //Add to Store
                    try
                    {
                        _tripleCount += g.Triples.Count;
                        _dataset.AddGraph(g);
                    }
                    catch (Exception ex)
                    {
                        LogImportFailure(txtSourceFile.Text, ex);
                        MessageBox.Show("An error occurred trying to add the RDF Graph to the Dataset:\n" + ex.Message, "File Import Error");
                        return;
                    }
                }
                catch (RdfParserSelectionException)
                {
                    try
                    {
                        //Try and get a Store Parser and load
                        IStoreReader storeparser = MimeTypesHelper.GetStoreParserByFileExtension(MimeTypesHelper.GetTrueFileExtension(txtSourceFile.Text));
                        TripleStore store = new TripleStore();
                        storeparser.Load(store, txtSourceFile.Text);

                        foreach (IGraph g in store.Graphs)
                        {
                            if (_dataset.HasGraph(g.Name))
                            {
                                int triplesBefore = _dataset[g.Name].Triples.Count;
                                _dataset[g.Name].Merge(g);
                                _tripleCount += _dataset[g.Name].Triples.Count - triplesBefore;
                            }
                            else
                            {
                                _dataset.AddGraph(g);
                                _tripleCount += g.Triples.Count;
                            }
                        }

                        LogImportSuccess(txtSourceFile.Text, store.Graphs.Count, store.Graphs.Sum(g => g.Triples.Count));
                    }
                    catch (RdfParserSelectionException selEx)
                    {
                        LogImportFailure(txtSourceFile.Text, selEx);
                        MessageBox.Show("The given file does not appear to be an RDF Graph/Dataset File Format the tool understands", "File Import Error");
                        return;
                    }
                    catch (Exception ex)
                    {
                        LogImportFailure(txtSourceFile.Text, ex);
                        MessageBox.Show("An error occurred trying to read an RDF Dataset from the file:\n" + ex.Message, "File Import Error");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogImportFailure(txtSourceFile.Text, ex);
                    MessageBox.Show("An error occurred trying to read an RDF Graph from the file:\n" + ex.Message, "File Import Error");
                    return;
                }

                _dataset.Flush();
                stsGraphs.Text = _dataset.GraphNames.Count() + " Graphs";
                stsTriples.Text = _tripleCount + " Triples";
                MessageBox.Show("RDF added to the Dataset OK", "File Import Done");
            }
        }

        private void btnImportUri_Click(object sender, EventArgs e)
        {
            if (txtSourceUri.Text.Equals(string.Empty))
            {
                MessageBox.Show("Please enter a URI you wish to import RDF from...", "No URI Specified");
            }
            else
            {
                Graph g = new Graph();
                try
                {
                    var loader = new Loader();
                    loader.LoadGraph(g, new Uri(txtSourceUri.Text));
                    try
                    {
                        if (_dataset.HasGraph(g.Name))
                        {
                            int triplesBefore = _dataset[g.Name].Triples.Count;
                            _dataset[g.Name].Merge(g);
                            _tripleCount += _dataset[g.Name].Triples.Count - triplesBefore;
                        }
                        else
                        {
                            _dataset.AddGraph(g);
                            _tripleCount += g.Triples.Count;
                        }

                        LogImportSuccess(new Uri(txtSourceUri.Text), 1, g.Triples.Count);
                    }
                    catch (Exception ex)
                    {
                        LogImportFailure(new Uri(txtSourceUri.Text), ex);
                        MessageBox.Show("An error occurred trying to add the RDF Graph to the Dataset:\n" + ex.Message, "URI Import Error");
                        return;
                    }
                }
                catch (UriFormatException uriEx)
                {
                    MessageBox.Show("The URI you have entered is malformed:\n" + uriEx.Message, "Malformed URI");
                }
                catch (Exception ex)
                {
                    LogImportFailure(new Uri(txtSourceUri.Text), ex);
                    MessageBox.Show("An error occurred while loading RDF from the given URI:\n" + ex.Message, "URI Import Error");
                    return;
                }

                _dataset.Flush();
                stsGraphs.Text = _dataset.GraphNames.Count() + " Graphs";
                stsTriples.Text = _tripleCount + " Triples";
                MessageBox.Show("RDF added to the Dataset OK", "URI Import Done");
            }
        }

        private void btnClearDataset_Click(object sender, EventArgs e)
        {
            _dataset = new InMemoryQuadDataset();
            _processor = new LeviathanQueryProcessor(_dataset, GetProcessorOptions());
            if (chkFullTextIndexing.Checked) EnableFullTextIndex();
            _tripleCount = 0;
            stsGraphs.Text = _dataset.GraphNames.Count() + " Graphs";
            stsTriples.Text = _tripleCount + " Triples";
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            try
            {
                SparqlQueryParser parser = getQueryParser();
                SparqlQuery query = parser.ParseFromString(txtQuery.Text);
                query.Timeout = (long)numTimeout.Value;
                query.PartialResultsOnTimeout = chkPartialResults.Checked;

                if (_tripleCount == 0 && _noDataWarning)
                {
                    switch (MessageBox.Show("You have no data loaded to query over - do you wish to run this query anyway?  Press Abort if you'd like to load data first, Retry to continue anyway or Ignore to continue anyway and suppress this message during this session", "Continue Query without Data", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Question))
                    {
                        case DialogResult.Abort:
                            return;
                        case DialogResult.Ignore:
                            //Set the Ignore flag then continue anyway
                            _noDataWarning = false;
                            break;
                        default:
                            //Continue anyway
                            break;
                    }
                }

                LogStartQuery(query);

                //Evaluate the Query
                object results;
                if (_logExplain)
                {
                    using (StreamWriter writer = new StreamWriter(_logfile, true, Encoding.UTF8))
                    {
                        ExplainQueryProcessor explainer = new ExplainQueryProcessor(_dataset, (ExplanationLevel.OutputToTrace | ExplanationLevel.ShowAll | ExplanationLevel.AnalyseAll ) ^ ExplanationLevel.ShowThreadID);
                        TextWriterTraceListener listener = new TextWriterTraceListener(writer, "SparqlGUI");
                        Trace.Listeners.Add(listener);
                        try
                        {
                            results = explainer.ProcessQuery(query);
                        }
                        finally
                        {
                            Trace.Listeners.Remove(listener);
                        }

                        writer.Close();
                    }
                }
                else
                {
                    results = _processor.ProcessQuery(query);
                }

                //Process the Results
                if (results is IGraph)
                {
                    LogEndQuery(query, (IGraph)results);

                    if (chkViewResultsInApp.Checked)
                    {
                        GraphViewerForm graphViewer = new GraphViewerForm((IGraph)results, "SPARQL GUI");
                        graphViewer.Show();
                    }
                    else
                    {
                        _rdfwriter.Save((IGraph)results, getTextWriter("temp" + _rdfext));
                        System.Diagnostics.Process.Start("temp" + _rdfext);
                    }
                }
                else if (results is SparqlResultSet)
                {
                    LogEndQuery(query, (SparqlResultSet)results);

                    if (chkViewResultsInApp.Checked)
                    {
                        ResultSetViewerForm resultSetViewer = new ResultSetViewerForm((SparqlResultSet)results,query.NamespaceMap, "SPARQL GUI");
                        resultSetViewer.Show();
                    }
                    else
                    {
                        _resultswriter.Save((SparqlResultSet)results, getTextWriter("temp" + _resultsext));
                        System.Diagnostics.Process.Start("temp" + _resultsext);
                    }
                }
                else
                {
                    throw new RdfException("Unexpected Result Type");
                }
                stsLastQuery.Text = "Last Query took " + query.QueryExecutionTime;
            }
            catch (RdfParseException parseEx)
            {
                LogMalformedQuery(parseEx);
                MessageBox.Show("Query failed to parse:\n" + parseEx.Message, "Query Failed");
            }
            catch (RdfQueryException queryEx)
            {
                LogFailedQuery(queryEx);
                MessageBox.Show("Query failed during Execution:\n" + queryEx.Message, "Query Failed");
            }
            catch (Exception ex)
            {
                LogFailedQuery(ex);
                MessageBox.Show("Query failed:\n" + ex.Message + "\n" + ex.StackTrace, "Query Failed");
            }
        }

        private TextWriter getTextWriter(string outputPath)
        {
            return new StreamWriter(outputPath, false, new UTF8Encoding(chkUseUtf8Bom.Checked));
        }

        private void btnInspect_Click(object sender, EventArgs e)
        {
            try
            {
                SparqlQueryParser parser = getQueryParser();
                Stopwatch timer = new Stopwatch();
                timer.Start();
                SparqlQuery query = parser.ParseFromString(txtQuery.Text);
                timer.Stop();

                fclsInspect inspect = new fclsInspect(query, timer.ElapsedMilliseconds, txtQuery.Text);
                inspect.Show();
            }
            catch (RdfParseException parseEx)
            {
                MessageBox.Show("Query failed to parse:\n" + parseEx.Message, "Query Failed");
            }
            catch (RdfQueryException queryEx)
            {
                MessageBox.Show("Query failed during Execution:\n" + queryEx.Message, "Query Failed");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Query failed:\n" + ex.Message + "\n" + ex.StackTrace, "Query Failed");
            }
        }

        private void radSparql10_CheckedChanged(object sender, EventArgs e)
        {
            if (radSparql10.Checked) _querySyntax = SparqlQuerySyntax.Sparql_1_0;
        }

        private void radSparql11_CheckedChanged(object sender, EventArgs e)
        {
            if (radSparql11.Checked) _querySyntax = SparqlQuerySyntax.Sparql_1_1;
        }

        private void radSparqlExtended_CheckedChanged(object sender, EventArgs e)
        {
            if (radSparqlExtended.Checked) _querySyntax = SparqlQuerySyntax.Extended;
        }

        private void chkWebDemand_CheckedChanged(object sender, EventArgs e)
        {
            if (chkWebDemand.Checked)
            {
                EnableWebDemand();
            }
            else
            {
                DisableWebDemand();
            }
        }

        private void cboGraphFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboGraphFormat.SelectedIndex)
            {
                case 0:
                    _rdfwriter = new CsvWriter();
                    _rdfext = ".csv";
                    break;
                case 1:
                    fclsStylesheetPicker stylesheetPicker = new fclsStylesheetPicker("CSS (Optional)");
                    if (stylesheetPicker.ShowDialog() == DialogResult.OK)
                    {
                        HtmlWriter temp = new HtmlWriter();
                        temp.Stylesheet = stylesheetPicker.StylesheetUri;
                        _rdfwriter = temp;
                    }
                    else
                    {
                        _rdfwriter = new HtmlWriter();
                    }
                    _rdfext = ".html";
                    break;
                case 2:
                    _rdfwriter = new Notation3Writer();
                    _rdfext = ".n3";
                    break;
                case 3:
                    _rdfwriter = new NTriplesWriter();
                    _rdfext = ".nt";
                    break;
                case 4:
                    _rdfwriter = new RdfJsonWriter();
                    _rdfext = ".json";
                    break;
                case 5:
                    _rdfwriter = new RdfXmlWriter();
                    _rdfext = ".rdf";
                    break;
                case 6:
                    _rdfwriter = new CompressingTurtleWriter();
                    _rdfext = ".ttl";
                    break;
                case 7:
                    _rdfwriter = new TsvWriter();
                    _rdfext = ".tsv";
                    break;
            }

            if (_rdfwriter is ICompressingWriter)
            {
                ((ICompressingWriter)_rdfwriter).CompressionLevel = WriterCompressionLevel.High;
            }

            if (cboResultsFormat.SelectedIndex == 1)
            {
                _resultswriter = new SparqlRdfWriter(_rdfwriter);
                _resultsext = _rdfext;
            }
        }

        private void cboResultsFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            fclsStylesheetPicker stylesheetPicker;

            switch (cboResultsFormat.SelectedIndex)
            {
                case 0:
                    _resultswriter = new SparqlCsvWriter();
                    _resultsext = ".csv";
                    break;
                case 1:
                    _resultswriter = new SparqlRdfWriter(_rdfwriter);
                    _resultsext = _rdfext;
                    break;
                case 2:
                    _resultswriter = new SparqlHtmlWriter();
                    _resultsext = ".html";
                    break;
                case 3:
                    stylesheetPicker = new fclsStylesheetPicker("CSS");
                    if (stylesheetPicker.ShowDialog() == DialogResult.OK) 
                    {
                        SparqlHtmlWriter temp = new SparqlHtmlWriter();
                        temp.Stylesheet = stylesheetPicker.StylesheetUri;
                        _resultswriter = temp;
                    } 
                    else 
                    {
                        _resultswriter = new SparqlHtmlWriter();
                    }
                    _resultsext = ".html";
                    break;
                    
                case 4:
                    _resultswriter = new SparqlJsonWriter();
                    _resultsext = ".json";
                    break;
                case 5:
                    _resultswriter = new SparqlTsvWriter();
                    _resultsext = ".tsv";
                    break;
                case 6:
                    _resultswriter = new SparqlXmlWriter();
                    _resultsext = ".srx";
                    break;
                case 7:
                    stylesheetPicker = new fclsStylesheetPicker("XSLT");
                    if (stylesheetPicker.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            _resultswriter = new SparqlXsltWriter(stylesheetPicker.StylesheetUri);
                            _resultsext = ".xml";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Unable to use the selected XSLT Stylesheet due to the following error:\n" + ex.Message, "Invalid Stylesheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            cboResultsFormat.SelectedIndex = 2;
                            _resultswriter = new SparqlHtmlWriter();
                            _resultsext = ".html";
                        }
                    }
                    else
                    {
                        cboResultsFormat.SelectedIndex = 2;
                        _resultswriter = new SparqlHtmlWriter();
                        _resultsext = ".html";
                    }
                    break;
            }
        }

        private void btnSaveQuery_Click(object sender, EventArgs e)
        {
            if (sfdQuery.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(sfdQuery.FileName))
                {
                    writer.Write(txtQuery.Text);
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (ofdQuery.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(ofdQuery.FileName))
                {
                    txtQuery.Text = reader.ReadToEnd();
                }
            }
        }

        private void Log(string action, string information)
        {
            using (StreamWriter writer = new StreamWriter(_logfile, true, System.Text.Encoding.UTF8))
            {
                writer.Write("[" + DateTime.Now + "] " + action);
                if (information.Contains('\n') || information.Contains('\r'))
                {
                    writer.WriteLine();
                    writer.Write(information);
                }
                else
                {
                    writer.WriteLine(' ');
                    writer.WriteLine(information);
                }
                writer.Close();
            }
        }

        private void LogImportSuccess(string file, int graphs, int triples)
        {
            Log("IMPORT", "Import from File '" + file + "' - " + graphs + " Graphs with " + triples + " Triples");
        }

        private void LogImportSuccess(Uri u, int graphs, int triples)
        {
            Log("IMPORT", "Import from URI '" + u.AbsoluteUri + "' - " + graphs + " Graphs with " + triples + " Triples");
        }

        private void LogImportFailure(string file, Exception ex)
        {
            Log("IMPORT FAILURE", "Import from File '" + file + "' failed\n" + GetFullErrorTrace(ex));
        }

        private void LogImportFailure(Uri u, Exception ex)
        {
            Log("IMPORT FAILURE", "Import from URI '" + u.AbsoluteUri + "' failed\n" + GetFullErrorTrace(ex));
        }

        private void LogMalformedQuery(Exception ex)
        {
            Log("QUERY PARSING FAILURE", "Failed to Parse Query\n" + GetFullErrorTrace(ex));
        }

        private void LogStartQuery(SparqlQuery q)
        {
            SparqlFormatter formatter = new SparqlFormatter(q.NamespaceMap);
            Log("QUERY START", formatter.Format(q));
        }

        private void LogFailedQuery(Exception ex)
        {
            Log("QUERY FAILED", "Query Failed during Execution\n" + GetFullErrorTrace(ex));
        }

        private void LogEndQuery(SparqlQuery q, SparqlResultSet results)
        {
            if (results.ResultsType == SparqlResultsType.Boolean)
            {
                Log("QUERY END", "Query Finished in " + q.QueryExecutionTime + " producing a Boolean Result of " + results.Result);
            }
            else
            {
                Log("QUERY END", "Query Finished in " + q.QueryExecutionTime + " producing a Result Set containing " + results.Count + " Results");
            }
        }

        private void LogEndQuery(SparqlQuery q, IGraph g)
        {
            Log("QUERY END", "Query Finished in " + q.QueryExecutionTime + " producing a Graph contaning " + g.Triples.Count + " Triples");
        }

        private string GetFullErrorTrace(Exception ex)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(ex.Message);
            output.AppendLine(ex.StackTrace);

            while (ex.InnerException != null)
            {
                output.AppendLine();
                output.AppendLine(ex.InnerException.Message);
                output.AppendLine(ex.InnerException.StackTrace);
                ex = ex.InnerException;
            }

            return output.ToString();
        }

        private void btnViewLog_Click(object sender, EventArgs e)
        {
            if (File.Exists(_logfile))
            {
                try
                {
                    Process.Start(_logfile);
                }
                catch
                {
                    MessageBox.Show("Error opening log file");
                }
            }
            else
            {
                MessageBox.Show("Log File not found!");
            }
        }

        private void chkLogExplanation_CheckedChanged(object sender, EventArgs e)
        {
            _logExplain = chkLogExplanation.Checked;
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            if (File.Exists(_logfile))
            {
                try
                {
                    File.Delete(_logfile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error clearing Log File - " + ex.Message, "Clear Log Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExplain_Click(object sender, EventArgs e)
        {
            try
            {
                SparqlQueryParser parser = getQueryParser();
                Stopwatch timer = new Stopwatch();
                timer.Start();
                SparqlQuery query = parser.ParseFromString(txtQuery.Text);
                timer.Stop();

                fclsExplanation explain = new fclsExplanation(query, timer.ElapsedMilliseconds);
                explain.Show();
            }
            catch (RdfParseException parseEx)
            {
                MessageBox.Show("Query failed to parse:\n" + parseEx.Message, "Query Failed");
            }
            catch (RdfQueryException queryEx)
            {
                MessageBox.Show("Query failed during Execution:\n" + queryEx.Message, "Query Failed");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Query failed:\n" + ex.Message + "\n" + ex.StackTrace, "Query Failed");
            }
        }

        private SparqlQueryParser getQueryParser()
        {
            return new SparqlQueryParser(_querySyntax) { 
                AllowUnknownFunctions = chkAllowUnknownFunctions.Checked,
                QueryOptimisation = chkQueryOptimisation.Checked
            };
        }
        private void chkFullTextIndexing_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFullTextIndexing.Checked)
            {
                EnableFullTextIndex();
            }
            else
            {
                DisableFullTextIndex();
            }
        }

        #region Feature Configuration

        private void EnableFullTextIndex()
        {
            if (_dataset is FullTextIndexedDataset)
            {
                //Nothing to do
            }
            else if (_dataset is WebDemandDataset)
            {
                WebDemandDataset ds = (WebDemandDataset)_dataset;
                _dataset = ds.UnderlyingDataset;
                EnableFullTextIndex();
                _dataset = new WebDemandDataset(_dataset);
            }
            else
            {
                //Create and ensure index ready for use
                _ftIndex = new RAMDirectory();

                var writer =
                    new IndexWriter(_ftIndex, new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)));

                writer.Dispose();

                //Create Indexer and wrap dataset
                _ftIndexer = new LuceneObjectsIndexer(_ftIndex, new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), new DefaultIndexSchema());
                if (_dataset is WebDemandDataset)
                {
                    //Web Demand needs to go around Full Text as we want to index on demand loaded content
                    _dataset = new WebDemandDataset(new FullTextIndexedDataset(((WebDemandDataset)_dataset).UnderlyingDataset, _ftIndexer, true));
                }
                else
                {
                    _dataset = new FullTextIndexedDataset(_dataset, _ftIndexer, true);
                }

                //Create and Register Optimizer
                _ftSearcher = new LuceneSearchProvider(Lucene.Net.Util.LuceneVersion.LUCENE_48, _ftIndex);
                _ftOptimiser = new FullTextOptimiser(_ftSearcher);
            }
            _processor = new LeviathanQueryProcessor(_dataset, GetProcessorOptions());
        }

        private void DisableFullTextIndex()
        {
            if (_dataset is WebDemandDataset)
            {
                WebDemandDataset ds = (WebDemandDataset)_dataset;
                if (ds.UnderlyingDataset is FullTextIndexedDataset)
                {
                    _dataset = ds.UnderlyingDataset;
                    DisableFullTextIndex();
                    _dataset = new WebDemandDataset(_dataset);
                }
            }
            else if (_dataset is FullTextIndexedDataset)
            {
                _ftOptimiser = null;
                _ftSearcher.Dispose();
                _ftSearcher = null;
                _dataset = ((FullTextIndexedDataset)_dataset).UnderlyingDataset;
                _ftIndexer.Dispose();
                _ftIndexer = null;
                _ftIndex.Dispose();
                _ftIndex = null;
            }
            _processor = new LeviathanQueryProcessor(_dataset, GetProcessorOptions());
        }

        private void EnableWebDemand()
        {
            if (_dataset is WebDemandDataset)
            {
                //Nothing to do
            }
            else
            {
                //Wrap dataset in a WebDemandDataset
                _dataset = new WebDemandDataset(_dataset);
            }
            _processor = new LeviathanQueryProcessor(_dataset);
        }

        private void DisableWebDemand()
        {
            if (_dataset is WebDemandDataset)
            {
                _dataset = ((WebDemandDataset)_dataset).UnderlyingDataset;
            }
            _processor = new LeviathanQueryProcessor(_dataset);
        }

        #endregion

        private void chkUnsafeOptimisation_CheckedChanged(object sender, EventArgs e)
        {
            // TODO: This option no longer has any effect
            // Options.UnsafeOptimisation = this.chkUnsafeOptimisation.Checked;
        }

        private LeviathanQueryOptions GetProcessorOptions()
        {
            LeviathanQueryOptions options = new LeviathanQueryOptions();
            options.UsePLinqEvaluation = chkParallelEval.Checked;
            options.AlgebraOptimisation = chkAlgebraOptimisation.Checked;
            if (_ftOptimiser != null)
            {
                var optimisers = options.AlgebraOptimisers.ToList();
                if (!optimisers.OfType<FullTextOptimiser>().Any()) { 
                    optimisers.Add(_ftOptimiser);
                    options.AlgebraOptimisers = optimisers;
                }
            }
            return options;
        }

        private void chkDefaultUnionGraph_CheckedChanged(object sender, EventArgs e)
        {
            bool unionDefaultGraph = chkDefaultUnionGraph.Checked;
            if (_dataset is WebDemandDataset)
            {
                if (((WebDemandDataset)_dataset).UnderlyingDataset is FullTextIndexedDataset)
                {
                    _dataset = new FullTextIndexedDataset(new WebDemandDataset(new InMemoryQuadDataset(_store, unionDefaultGraph)), _ftIndexer, false);
                }
                else
                {
                    _dataset = new WebDemandDataset(new InMemoryQuadDataset(_store, unionDefaultGraph));
                }
            }
            else if (_dataset is FullTextIndexedDataset)
            {
                _dataset = new FullTextIndexedDataset(new InMemoryQuadDataset(_store, unionDefaultGraph), _ftIndexer, false);
            }
            else
            {
                _dataset = new InMemoryQuadDataset(_store, unionDefaultGraph);
            }
            _processor = new LeviathanQueryProcessor(_dataset);
        }
    }
}
