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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Validation;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Utilities.Editor.AutoComplete;
using VDS.RDF.Utilities.Editor.Selection;
using VDS.RDF.Utilities.Editor.Syntax;
using VDS.RDF.Utilities.Editor.Wpf.Syntax;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow
        : Window
    {
        private readonly string FileFilterRdf, FileFilterSparql, FileFilterRdfDataset, FileFilterAll;

        private OpenFileDialog _ofd = new OpenFileDialog();
        private SaveFileDialog _sfd = new SaveFileDialog();
        private Editor<TextEditor, FontFamily, Color> _editor;
        private bool _saveWindowSize = false;
        private FindReplace _findReplace;

        public EditorWindow()
        {
            InitializeComponent();

            //rdfEditor must disable URI interning otherwise it will eat memory as the user edits lots of files
            UriFactory.InternUris = false;

            //Generate Filename Filters
            FileFilterRdf = MimeTypesHelper.GetFilenameFilter(true, false, false, false, false, true);
            FileFilterSparql = MimeTypesHelper.GetFilenameFilter(false, false, true, false, false, true);
            FileFilterRdfDataset = MimeTypesHelper.GetFilenameFilter(false, true, false, false, false, true);
            FileFilterAll = MimeTypesHelper.GetFilenameFilter();

            //Initialise Highlighting and Auto-Completion
            WpfHighlightingManager.Initialise(Properties.Settings.Default.UseCustomisedXshdFiles);
            AutoCompleteManager.Initialise();

            //Create the Editor Manager
            WpfEditorFactory factory = new WpfEditorFactory();
            _editor = new Editor<TextEditor, FontFamily, Color>(factory);
            _editor.DocumentManager.DefaultSaveChangesCallback = new SaveChangesCallback<TextEditor>(SaveChangesCallback);
            _editor.DocumentManager.DefaultSaveAsCallback = new SaveAsCallback<TextEditor>(SaveAsCallback);
            _editor.DocumentManager.DocumentCreated += new DocumentChangedHandler<TextEditor>(HandleDocumentCreated);
          
            //Set up the Editor Options
            if (_editor.DocumentManager.VisualOptions == null) _editor.DocumentManager.VisualOptions = new WpfVisualOptions();
            _editor.DocumentManager.VisualOptions.EnableClickableUris = Properties.Settings.Default.EnableClickableUris;
            _editor.DocumentManager.VisualOptions.ShowEndOfLine = Properties.Settings.Default.ShowEndOfLine;
            _editor.DocumentManager.VisualOptions.ShowLineNumbers = Properties.Settings.Default.ShowLineNumbers;
            _editor.DocumentManager.VisualOptions.ShowSpaces = Properties.Settings.Default.ShowSpaces;
            _editor.DocumentManager.VisualOptions.ShowTabs = Properties.Settings.Default.ShowTabs;
            if (Properties.Settings.Default.EditorFontFace != null)
            {
                _editor.DocumentManager.VisualOptions.FontFace = Properties.Settings.Default.EditorFontFace;
            }
            _editor.DocumentManager.VisualOptions.FontSize = Math.Round(Properties.Settings.Default.EditorFontSize, 0);
            _editor.DocumentManager.VisualOptions.Foreground = Properties.Settings.Default.EditorForeground;
            _editor.DocumentManager.VisualOptions.Background = Properties.Settings.Default.EditorBackground;
            _editor.DocumentManager.VisualOptions.ErrorBackground = Properties.Settings.Default.ErrorHighlightBackground;
            _editor.DocumentManager.VisualOptions.ErrorDecoration = Properties.Settings.Default.ErrorHighlightDecoration;
            _editor.DocumentManager.VisualOptions.ErrorFontFace = Properties.Settings.Default.ErrorHighlightFontFamily;
            _editor.DocumentManager.VisualOptions.ErrorForeground = Properties.Settings.Default.ErrorHighlightForeground;

            //If custom highlighting colours have been used this call forces them to be used
            AppearanceSettings.UpdateHighlightingColours();
            
            //Setup Options based on the User Config file
            Options.UseBomForUtf8 = false;
            if (Properties.Settings.Default.UseUtf8Bom)
            {
                mnuUseBomForUtf8.IsChecked = true;
                Options.UseBomForUtf8 = true;
                GlobalOptions.UseBomForUtf8 = true;
            }
            if (Properties.Settings.Default.SaveWithOptionsPrompt)
            {
                mnuSaveWithPromptOptions.IsChecked = true;
            }
            if (!Properties.Settings.Default.EnableSymbolSelection)
            {
                _editor.DocumentManager.Options.IsSymbolSelectionEnabled = false;
                mnuSymbolSelectEnabled.IsChecked = false;
            }
            if (!Properties.Settings.Default.IncludeSymbolBoundaries)
            {
                _editor.DocumentManager.Options.IncludeBoundaryInSymbolSelection = false;
                mnuSymbolSelectIncludeBoundary.IsChecked = false;
            }
            switch (Properties.Settings.Default.SymbolSelectionMode)
            {
                case "Punctuation":
                    _editor.DocumentManager.Options.CurrentSymbolSelector = new PunctuationSelector<TextEditor>();
                    mnuBoundariesPunctuation.IsChecked = true;
                    break;
                case "WhiteSpace":
                    _editor.DocumentManager.Options.CurrentSymbolSelector = new WhiteSpaceSelector<TextEditor>();
                    mnuBoundariesWhiteSpace.IsChecked = true;
                    break;
                case "All":
                    _editor.DocumentManager.Options.CurrentSymbolSelector = new WhiteSpaceOrPunctuationSelection<TextEditor>();
                    mnuBoundariesAll.IsChecked = true;
                    break;
                case "Default":
                default:
                    mnuBoundariesDefault.IsChecked = true;
                    break;
            }
            if (!Properties.Settings.Default.EnableAutoComplete) 
            {
                _editor.DocumentManager.Options.IsAutoCompletionEnabled = false;
                mnuAutoComplete.IsChecked = false;
            }
            if (!Properties.Settings.Default.EnableHighlighting)
            {
                _editor.DocumentManager.Options.IsSyntaxHighlightingEnabled = false;
                mnuEnableHighlighting.IsChecked = false;
            }
            if (!Properties.Settings.Default.EnableValidateAsYouType)
            {
                _editor.DocumentManager.Options.IsValidateAsYouTypeEnabled = false;
                mnuValidateAsYouType.IsChecked = false;
            }
            if (!Properties.Settings.Default.ShowLineNumbers)
            {
                _editor.DocumentManager.VisualOptions.ShowLineNumbers = false;
                mnuShowLineNumbers.IsChecked = false;
            }
            if (Properties.Settings.Default.WordWrap)
            {
                _editor.DocumentManager.VisualOptions.WordWrap = true;
                mnuWordWrap.IsChecked = true;
            }
            if (Properties.Settings.Default.EnableClickableUris)
            {
                _editor.DocumentManager.VisualOptions.EnableClickableUris = true;
                mnuClickableUris.IsChecked = true;
            }
            if (Properties.Settings.Default.ShowEndOfLine)
            {
                _editor.DocumentManager.VisualOptions.ShowEndOfLine = true;
                mnuShowSpecialEOL.IsChecked = true;
            }
            if (Properties.Settings.Default.ShowSpaces)
            {
                _editor.DocumentManager.VisualOptions.ShowSpaces = true;
                mnuShowSpecialSpaces.IsChecked = true;
            }
            if (Properties.Settings.Default.ShowTabs)
            {
                _editor.DocumentManager.VisualOptions.ShowTabs = true;
                mnuShowSpecialTabs.IsChecked = true;
            }
            _editor.DocumentManager.DefaultSyntax = Properties.Settings.Default.DefaultHighlighter;

            //Add an initial document for editing
            AddTextEditor();
            tabDocuments.SelectedIndex = 0;
            _editor.DocumentManager.ActiveDocument.TextEditor.Control.Focus();

            //Create our Dialogs
            _ofd.Title = "Open RDF/SPARQL File";
            _ofd.DefaultExt = ".rdf";
            _ofd.Filter = FileFilterAll;
            _ofd.Multiselect = true;
            _sfd.Title = "Save RDF/SPARQL File";
            _sfd.DefaultExt = ".rdf";
            _sfd.Filter = _ofd.Filter;

            //Setup dropping of files
            AllowDrop = true;
            Drop += new DragEventHandler(EditorWindow_Drop);
        }

        #region Text Editor Management

        private void AddTextEditor()
        {
            AddTextEditor(new TabItem());
        }

        private void AddTextEditor(TabItem tab)
        {
            Document<TextEditor> doc = _editor.DocumentManager.New();
            AddTextEditor(tab, doc);
        }

        private void AddTextEditor(TabItem tab, Document<TextEditor> doc)
        {
            //Register for relevant events on the document
            doc.FilenameChanged +=
                new DocumentChangedHandler<TextEditor>((sender, e) =>
                {
                    if (e.Document.Filename != null && !e.Document.Filename.Equals(string.Empty))
                    {
                        tab.Header = System.IO.Path.GetFileName(e.Document.Filename);
                    }
                });
            doc.TitleChanged += new DocumentChangedHandler<TextEditor>((sender, e) =>
            {
                if (e.Document.Title != null && !e.Document.Title.Equals(string.Empty))
                {
                    tab.Header = e.Document.Title;
                }
            });
            doc.SyntaxChanged += new DocumentChangedHandler<TextEditor>((sender, e) =>
            {
                if (ReferenceEquals(_editor.DocumentManager.ActiveDocument, e.Document))
                {
                    stsCurrSyntax.Content = "Syntax: " + e.Document.Syntax;
                }
            });
            doc.Validated += new DocumentValidatedHandler<TextEditor>(HandleValidation);
            doc.ValidatorChanged += new DocumentChangedHandler<TextEditor>(HandleValidatorChanged);
            doc.TextChanged += new DocumentChangedHandler<TextEditor>(HandleTextChanged);

            //Set Tab title where appropriate
            if (doc.Filename != null && !doc.Filename.Equals(string.Empty))
            {
                tab.Header = System.IO.Path.GetFileName(doc.Filename);
            }
            else if (doc.Title != null && !doc.Title.Equals(string.Empty))
            {
                tab.Header = doc.Title;
            }

            //Add to Tabs
            tabDocuments.Items.Add(tab);
            tab.Content = doc.TextEditor.Control;

            //Add appropriate event handlers on tabs
            //tab.Enter +=
            //    new EventHandler((sender, e) =>
            //    {
            //        var page = ((TabPage)sender);
            //        if (page.Controls.Count > 0)
            //        {
            //            page.BeginInvoke(new Action<TabPage>(p => p.Controls[0].Focus()), page);
            //        }
            //    });
        }

        #endregion

        #region File Menu

        private void mnuFile_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            bool hasDoc = _editor.DocumentManager.ActiveDocument != null;
            mnuNewFromActive.IsEnabled = hasDoc;
            mnuSave.IsEnabled = hasDoc;
            mnuSaveAs.IsEnabled = hasDoc;
            mnuSaveAll.IsEnabled = hasDoc;
            mnuSaveWith.IsEnabled = hasDoc;
            mnuPageSetup.IsEnabled = hasDoc;
            mnuPrint.IsEnabled = hasDoc;
            mnuPrintNoHighlighting.IsEnabled = hasDoc;
            mnuPrintPreview.IsEnabled = hasDoc;
            mnuPrintPreviewNoHighlighting.IsEnabled = hasDoc;
            mnuClose.IsEnabled = hasDoc;
            mnuCloseAll.IsEnabled = hasDoc;
        }

        private void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            AddTextEditor();
            _editor.DocumentManager.SwitchTo(_editor.DocumentManager.Count - 1);
            tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
        }

        private void mnuNewFromActive_Click(object sender, RoutedEventArgs e)
        {
            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            if (doc != null)
            {
                Document<TextEditor> newDoc = _editor.DocumentManager.NewFromActive(true);

                TabItem tab = new TabItem();
                tab.Header = newDoc.Title;
                AddTextEditor(tab, newDoc);
                tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
            }
            else
            {
                AddTextEditor();
            }
        }

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_ofd.ShowDialog() == true)
                {
                    if (_ofd.FileNames.Length == 1)
                    {
                        Document<TextEditor> doc, active;
                        active = _editor.DocumentManager.ActiveDocument;
                        if (active.TextLength == 0 && (active.Filename == null || active.Filename.Equals(string.Empty)))
                        {
                            doc = active;
                            doc.Filename = _ofd.FileName;
                            UpdateMruList(doc.Filename);
                        }
                        else
                        {
                            doc = _editor.DocumentManager.New(System.IO.Path.GetFileName(_ofd.FileName), true);
                        }

                        //Open the file and display in new tab if necessary
                        doc.Open(_ofd.FileName);
                        if (!ReferenceEquals(active, doc))
                        {
                            AddTextEditor(new TabItem(), doc);
                            tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
                        }
                    }
                    else
                    {
                        foreach (string filename in _ofd.FileNames)
                        {
                            Document<TextEditor> doc = _editor.DocumentManager.New(System.IO.Path.GetFileName(filename), false);
                            try
                            {
                                doc.Open(filename);
                                AddTextEditor(new TabItem(), doc);
                                UpdateMruList(doc.Filename);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show("An error occurred while opening the selected file(s): " + ex.Message, "Open File Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                                _editor.DocumentManager.Close(_editor.DocumentManager.Count - 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occurred while opening the selected file(s): " + ex.Message, "Open File(s) Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuOpenUri_Click(object sender, RoutedEventArgs e)
        {
            Document<TextEditor> doc, active;
            active = _editor.DocumentManager.ActiveDocument;
            if (active != null)
            {
                if (active.TextLength == 0 && (active.Filename == null || active.Filename.Equals(string.Empty)))
                {
                    doc = active;
                }
                else
                {
                    doc = _editor.DocumentManager.New(true);
                    AddTextEditor(new TabItem(), doc);
                }
            }
            else
            {
                doc = _editor.DocumentManager.New(true);
                AddTextEditor(new TabItem(), doc);
            }

            OpenUri diag = new OpenUri();
            if (diag.ShowDialog() == true)
            {
                doc.Text = diag.RetrievedData;
                if (diag.Parser != null)
                {
                    doc.Syntax = diag.Parser.GetSyntaxName();
                }
                else
                {
                    doc.AutoDetectSyntax();
                }
            }
        }

        private void mnuOpenQueryResults_Click(object sender, RoutedEventArgs e)
        {
            string queryText = string.Empty;
            if (_editor.DocumentManager.ActiveDocument != null && _editor.DocumentManager.ActiveDocument.Syntax.StartsWith("SparqlQuery"))
            {
                queryText = _editor.DocumentManager.ActiveDocument.Text;
            }

            OpenQueryResults diag = new OpenQueryResults(_editor.DocumentManager.VisualOptions);
            if (!queryText.Equals(string.Empty)) diag.Query = queryText;
            if (diag.ShowDialog() == true)
            {
                Document<TextEditor> doc = _editor.DocumentManager.New(true);
                AddTextEditor(new TabItem(), doc);
                tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
                doc.Text = diag.RetrievedData;
                doc.AutoDetectSyntax();
            }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            if (doc != null)
            {
                if (doc.Filename == null || doc.Filename.Equals(string.Empty))
                {
                    mnuSaveAs_Click(sender, e);
                }
                else
                {
                    doc.Save();
                }
            }
        }

        private void mnuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            if (doc != null)
            {
                string filename = SaveAsCallback(doc);
                if (filename != null)
                {
                    doc.SaveAs(_sfd.FileName);
                }
            }
        }

        private void mnuSaveAll_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.SaveAll();
        }

        private void SaveWith(IRdfWriter writer)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;

            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            IRdfReader parser = SyntaxManager.GetParser(doc.Syntax);
            if (parser == null)
            {
                MessageBox.Show("To use Save With the source document must be in a RDF Graph Syntax.  If the document is in a RDF Graph Syntax please change the syntax setting to the relevant format under Options > Syntax", "Save With Unavailable");
                return;
            }

            Graph g = new Graph();
            try
            {
                StringParser.Parse(g, doc.Text, parser);
            }
            catch
            {
                MessageBox.Show("Unable to Save With an RDF Writer as the current document is not a valid RDF document when parsed with the " + parser.GetType().Name + ".  If you believe this is a valid RDF document please select the correct Syntax Highlighting from the Options Menu and retry", "Save With Failed");
                return;
            }

            try
            {
                //Check whether the User wants to set advanced options?
                if (Properties.Settings.Default.SaveWithOptionsPrompt)
                {
                    RdfWriterOptionsWindow optPrompt = new RdfWriterOptionsWindow(writer);
                    optPrompt.Owner = this;
                    if (optPrompt.ShowDialog() != true) return;
                }

                //Do the actual save
                System.IO.StringWriter strWriter = new System.IO.StringWriter();
                writer.Save(g, strWriter);
                Document<TextEditor> newDoc = _editor.DocumentManager.New(true);
                newDoc.Text = strWriter.ToString();
                newDoc.AutoDetectSyntax();
                AddTextEditor(new TabItem(), newDoc);
                tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving: " + ex.Message, "Save With Failed");
            }
        }

        private void mnuSaveWithNTriples_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new NTriplesWriter());
        }

        private void mnuSaveWithTurtle_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new CompressingTurtleWriter(WriterCompressionLevel.High));
        }

        private void mnuSaveWithN3_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new Notation3Writer());
        }

        private void mnuSaveWithRdfXml_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new PrettyRdfXmlWriter());
        }

        private void mnuSaveWithRdfJson_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new RdfJsonWriter());
        }

        private void mnuSaveWithRdfa_Click(object sender, RoutedEventArgs e)
        {
            SaveWith(new HtmlWriter());
        }

        private void mnuSaveWithPromptOptions_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SaveWithOptionsPrompt = mnuSaveWithPromptOptions.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void mnuUseBomForUtf8_Click(object sender, RoutedEventArgs e)
        {
            Options.UseBomForUtf8 = mnuUseBomForUtf8.IsChecked;
            GlobalOptions.UseBomForUtf8 = Options.UseBomForUtf8;
            Properties.Settings.Default.UseUtf8Bom = Options.UseBomForUtf8;
            Properties.Settings.Default.Save();
        }

        private void mnuPageSetup_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Control.PageSetupDialog();
            }
        }

        private void mnuPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Control.PrintPreviewDialog(_editor.DocumentManager.ActiveDocument.Title);
            }
        }

        private void mnuPrintPreviewNoHighlighting_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Control.PrintPreviewDialog(_editor.DocumentManager.ActiveDocument.Title, false);
            }
        }

        private void mnuPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Control.PrintDialog(_editor.DocumentManager.ActiveDocument.Title);
            }
        }

        private void mnuPrintNoHighlighting_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Control.PrintDialog(_editor.DocumentManager.ActiveDocument.Title, false);
            }
        }

        private void mnuClose_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                if (_editor.DocumentManager.Close())
                {
                    int index = _editor.DocumentManager.ActiveDocumentIndex;
                    try
                    {
                        tabDocuments.Items.RemoveAt(tabDocuments.SelectedIndex);
                        tabDocuments.SelectedIndex = index;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        //Ignore as may be possible to get into this state without intending
                        //to
                    }
                }
            }
        }

        private void mnuCloseAll_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.CloseAll();
            foreach (TabItem tab in tabDocuments.Items)
            {
                tab.Content = null;
            }
            tabDocuments.Items.Clear();

            //Recreate new Tabs for any Documents that were not closed
            foreach (Document<TextEditor> doc in _editor.DocumentManager.Documents)
            {
                AddTextEditor(new TabItem(), doc);
            }
            try
            {
                _editor.DocumentManager.SwitchTo(0);
                tabDocuments.SelectedIndex = 0;
            }
            catch (IndexOutOfRangeException)
            {
                //Ignore as if there are no documents left this may be thrown
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            mnuCloseAll_Click(sender, e);
            if (tabDocuments.Items.Count == 0)
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Edit Menu

        private void mnuEdit_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            bool hasDoc = _editor.DocumentManager.ActiveDocument != null;
            mnuUndo.IsEnabled = hasDoc;
            mnuRedo.IsEnabled = hasDoc;
            mnuCut.IsEnabled = hasDoc;
            mnuCopy.IsEnabled = hasDoc;
            mnuPaste.IsEnabled = hasDoc;
            mnuFind.IsEnabled = hasDoc;
            mnuFindNext.IsEnabled = hasDoc;
            mnuReplace.IsEnabled = hasDoc;
            mnuGoToLine.IsEnabled = hasDoc;
            mnuCommentSelection.IsEnabled = hasDoc;
            mnuUncommentSelection.IsEnabled = hasDoc;
            mnuSymbolBoundaries.IsEnabled = _editor.DocumentManager.Options.IsSymbolSelectionEnabled;
        }

        private void mnuUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument.TextEditor.Control.CanUndo)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Undo();
            }
        }

        private void mnuRedo_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                if (_editor.DocumentManager.ActiveDocument.TextEditor.Control.CanRedo)
                {
                    _editor.DocumentManager.ActiveDocument.TextEditor.Redo();
                }
            }
        }

        private void mnuCut_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Cut();
            }
        }

        private void mnuCopy_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Copy();
            }
        }

        private void mnuPaste_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument != null)
            {
                _editor.DocumentManager.ActiveDocument.TextEditor.Paste();
            }
        }

        private void mnuFind_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;
            if (_findReplace == null)
            {
                _findReplace = new FindReplace();
            }
            _findReplace.Editor = _editor.DocumentManager.ActiveDocument.TextEditor;
            if (_findReplace.Visibility != Visibility.Visible)
            {
                _findReplace.Mode = FindReplaceMode.Find;
                _findReplace.Show();
            }
            _findReplace.BringIntoView();
            _findReplace.Focus();
        }

        private void mnuFindNext_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;
            if (_findReplace == null)
            {
                mnuFind_Click(sender, e);
            }
            else
            {
                _findReplace.Editor = _editor.DocumentManager.ActiveDocument.TextEditor;
                _findReplace.FindNext();
            }
        }

        private void mnuReplace_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;
            if (_findReplace == null)
            {
                _findReplace = new FindReplace();
            }
            _findReplace.Mode = FindReplaceMode.FindAndReplace;
            _findReplace.Editor = _editor.DocumentManager.ActiveDocument.TextEditor;
            if (_findReplace.Visibility != Visibility.Visible) _findReplace.Show();
            _findReplace.BringIntoView();
            _findReplace.Focus();
        }

        private void mnuGoToLine_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;
            ITextEditorAdaptor<TextEditor> editor = _editor.DocumentManager.ActiveDocument.TextEditor;
            GoToLine gotoLine = new GoToLine(editor);
            gotoLine.Owner = this;
            if (gotoLine.ShowDialog() == true)
            {
                editor.ScrollToLine(gotoLine.Line);
            }
        }

        private void mnuCommentSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;

            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            TextEditor textEditor = doc.TextEditor.Control;
            if (textEditor.SelectionLength == 0) return;

            string syntax = doc.Syntax;
            SyntaxDefinition def = SyntaxManager.GetDefinition(syntax);
            if (def != null)
            {
                if (def.CanComment)
                {
                    string selection = textEditor.SelectedText;
                    int startLine = textEditor.Document.GetLineByOffset(textEditor.SelectionStart).LineNumber;
                    int endLine = textEditor.Document.GetLineByOffset(textEditor.SelectionStart + textEditor.SelectionLength).LineNumber;

                    if (startLine == endLine && def.SingleLineComment != null)
                    {
                        //Single Line Comment
                        textEditor.Document.Replace(textEditor.SelectionStart, textEditor.SelectionLength, def.SingleLineComment + selection);
                    }
                    else
                    {
                        //Multi Line Comment
                        if (def.MultiLineCommentStart != null && def.MultiLineCommentEnd != null)
                        {
                            textEditor.Document.Replace(textEditor.SelectionStart, textEditor.SelectionLength, def.MultiLineCommentStart + selection + def.MultiLineCommentEnd);
                        }
                        else
                        {
                            //Multi-Line Comment but only supports single line comments
                            textEditor.BeginChange();
                            for (int i = startLine; i <= endLine; i++)
                            {
                                DocumentLine line = textEditor.Document.GetLineByNumber(i);
                                int startOffset = Math.Max(textEditor.SelectionStart, line.Offset);
                                textEditor.Document.Insert(startOffset, def.SingleLineComment);
                            }
                            textEditor.EndChange();
                            if (textEditor.SelectionStart > 0) textEditor.SelectionStart--;
                            if (textEditor.SelectionStart + textEditor.SelectionLength < textEditor.Text.Length) textEditor.SelectionLength++;
                        }
                    }
                }
            }
        }

        private void mnuUncommentSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;

            Document<TextEditor> doc = _editor.DocumentManager.ActiveDocument;
            TextEditor textEditor = doc.TextEditor.Control;
            if (textEditor.SelectionLength == 0) return;

            string syntax = doc.Syntax;
            SyntaxDefinition def = SyntaxManager.GetDefinition(syntax);
            if (def != null)
            {
                if (def.CanComment)
                {
                    string selection = textEditor.SelectedText;
                    int startLine = textEditor.Document.GetLineByOffset(textEditor.SelectionStart).LineNumber;
                    int endLine = textEditor.Document.GetLineByOffset(textEditor.SelectionStart + textEditor.SelectionLength).LineNumber;

                    if (startLine == endLine && def.SingleLineComment != null)
                    {
                        //Single Line Comment
                        int index = selection.IndexOf(def.SingleLineComment);
                        if (index > -1)
                        {
                            textEditor.Document.Remove(textEditor.SelectionStart + index, def.SingleLineComment.Length);
                        }
                    }
                    else
                    {
                        //Multi Line Comment
                        if (def.MultiLineCommentStart != null && def.MultiLineCommentEnd != null)
                        {
                            int startIndex = selection.IndexOf(def.MultiLineCommentStart);
                            int endIndex = selection.LastIndexOf(def.MultiLineCommentEnd);
                            textEditor.BeginChange();
                            textEditor.Document.Remove(textEditor.SelectionStart + startIndex, def.MultiLineCommentStart.Length);
                            textEditor.Document.Remove(textEditor.SelectionStart + endIndex - def.MultiLineCommentStart.Length, def.MultiLineCommentEnd.Length);
                            textEditor.EndChange();
                        }
                        else
                        {
                            textEditor.BeginChange();
                            for (int i = startLine; i <= endLine; i++)
                            {
                                DocumentLine line = textEditor.Document.GetLineByNumber(i);
                                int startOffset = Math.Max(textEditor.SelectionStart, line.Offset);
                                int endOffset = Math.Min(textEditor.SelectionStart + textEditor.SelectionLength, line.EndOffset);
                                string lineText = textEditor.Document.GetText(startOffset, endOffset - startOffset);
                                int index = lineText.IndexOf(def.SingleLineComment);
                                textEditor.Document.Remove(startOffset + index, def.SingleLineComment.Length);
                            }
                            textEditor.EndChange();
                        }
                    }
                }
            }
        }

        private void mnuSymbolSelectEnabled_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IsSymbolSelectionEnabled = mnuSymbolSelectEnabled.IsChecked;
            Properties.Settings.Default.EnableSymbolSelection = mnuSymbolSelectEnabled.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void mnuSymbolSelectIncludeBoundary_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IncludeBoundaryInSymbolSelection = mnuSymbolSelectIncludeBoundary.IsChecked;
            Properties.Settings.Default.IncludeSymbolBoundaries = mnuSymbolSelectIncludeBoundary.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void SymbolSelectorMode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selected = sender as MenuItem;
            if (selected == null) return;
            string tag = (string)selected.Tag;
            if (selected.IsChecked == false) tag = "Default";

            foreach (MenuItem item in mnuSymbolBoundaries.Items.OfType<MenuItem>())
            {
                if (tag.Equals((string)item.Tag))
                {
                    item.IsChecked = true;
                }
                else
                {
                    item.IsChecked = false;
                }
            }

            ISymbolSelector<TextEditor> current = _editor.DocumentManager.Options.CurrentSymbolSelector;
            switch (tag)
            {
                case "Punctuation":
                    if (!(current is PunctuationSelector<TextEditor>))
                    {
                        _editor.DocumentManager.Options.CurrentSymbolSelector = new PunctuationSelector<TextEditor>();
                        Properties.Settings.Default.SymbolSelectionMode = tag;
                        Properties.Settings.Default.Save();
                    }
                    break;
                case "WhiteSpace":
                    if (!(current is WhiteSpaceSelector<TextEditor>))
                    {
                        _editor.DocumentManager.Options.CurrentSymbolSelector = new WhiteSpaceSelector<TextEditor>();
                    }
                    break;
                case "All":
                    if (!(current is WhiteSpaceOrPunctuationSelection<TextEditor>))
                    {
                        _editor.DocumentManager.Options.CurrentSymbolSelector = new WhiteSpaceOrPunctuationSelection<TextEditor>();
                    }
                    break;
                case "Default":
                default:
                    tag = "Default";
                    if (!(current is DefaultSelector<TextEditor>))
                    {
                        _editor.DocumentManager.Options.CurrentSymbolSelector = new DefaultSelector<TextEditor>();
                    }
                    break;
            }

            //Update default Symbol Selection Mode
            Properties.Settings.Default.SymbolSelectionMode = tag;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region View Menu

        private void mnuShowLineNumbers_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.ShowLineNumbers = mnuShowLineNumbers.IsChecked;
            Properties.Settings.Default.ShowLineNumbers = _editor.DocumentManager.VisualOptions.ShowLineNumbers;
            Properties.Settings.Default.Save();
        }

        private void mnuWordWrap_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.WordWrap = mnuWordWrap.IsChecked;
            Properties.Settings.Default.WordWrap = _editor.DocumentManager.VisualOptions.WordWrap;
            Properties.Settings.Default.Save();
        }

        private void mnuClickableUris_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.EnableClickableUris = mnuClickableUris.IsChecked;
            Properties.Settings.Default.EnableClickableUris = _editor.DocumentManager.VisualOptions.EnableClickableUris;
            Properties.Settings.Default.Save();
        }

        private void mnuShowSpecialAll_Click(object sender, RoutedEventArgs e)
        {
            bool all = mnuShowSpecialAll.IsChecked;
            mnuShowSpecialEOL.IsChecked = all;
            mnuShowSpecialSpaces.IsChecked = all;
            mnuShowSpecialTabs.IsChecked = all;
            mnuShowSpecialEOL_Click(sender, e);
            mnuShowSpecialSpaces_Click(sender, e);
            mnuShowSpecialTabs_Click(sender, e);
        }

        private void mnuShowSpecialEOL_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.ShowEndOfLine = mnuShowSpecialEOL.IsChecked;
            Properties.Settings.Default.ShowEndOfLine = _editor.DocumentManager.VisualOptions.ShowEndOfLine;
            Properties.Settings.Default.Save();
        }

        private void mnuShowSpecialSpaces_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.ShowSpaces = mnuShowSpecialSpaces.IsChecked;
            Properties.Settings.Default.ShowSpaces = _editor.DocumentManager.VisualOptions.ShowSpaces;
            Properties.Settings.Default.Save();
        }

        private void mnuShowSpecialTabs_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.ShowTabs = mnuShowSpecialTabs.IsChecked;
            Properties.Settings.Default.ShowTabs = _editor.DocumentManager.VisualOptions.ShowTabs;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Options Menu

        private void mnuEnableHighlighting_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IsSyntaxHighlightingEnabled = mnuEnableHighlighting.IsChecked;
            Properties.Settings.Default.EnableHighlighting = _editor.DocumentManager.Options.IsSyntaxHighlightingEnabled;
            Properties.Settings.Default.Save();
        }

        private void mnuCurrentHighlighter_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            string currSyntax = _editor.DocumentManager.ActiveDocument != null ? _editor.DocumentManager.ActiveDocument.Syntax : "None";
            foreach (MenuItem item in mnuCurrentHighlighter.Items.OfType<MenuItem>())
            {
                if (item.Tag != null)
                {
                    if (item.Tag.Equals(currSyntax))
                    {
                        item.IsChecked = true;
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                    string header = (string)item.Header;
                    if (header.EndsWith(" (Default)"))
                    {
                        if (!item.Tag.Equals(_editor.DocumentManager.DefaultSyntax))
                        {
                            item.Header = header.Substring(0, header.Length - 10);
                        }
                    }
                    else if (item.Tag.Equals(_editor.DocumentManager.DefaultSyntax))
                    {
                        item.Header += " (Default)";
                    }
                }
            }
        }

        private void mnuSetDefaultHighlighter_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.DefaultSyntax = _editor.DocumentManager.ActiveDocument != null ? _editor.DocumentManager.ActiveDocument.Syntax : "None";
            Properties.Settings.Default.DefaultHighlighter = _editor.DocumentManager.DefaultSyntax;
            Properties.Settings.Default.Save();
        }

        private void mnuSetHighlighter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item == null) return;
            if (item.Tag == null) return;
            if (_editor.DocumentManager.ActiveDocument == null) return;
            _editor.DocumentManager.ActiveDocument.Syntax = (string)item.Tag;
        }

        private void mnuValidateAsYouType_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IsValidateAsYouTypeEnabled = mnuValidateAsYouType.IsChecked;
            Properties.Settings.Default.EnableValidateAsYouType = _editor.DocumentManager.Options.IsValidateAsYouTypeEnabled;
            Properties.Settings.Default.Save();
            if (_editor.DocumentManager.Options.IsValidateAsYouTypeEnabled)
            {
                if (_editor.DocumentManager.ActiveDocument != null)
                {
                    _editor.DocumentManager.ActiveDocument.Validate();
                }
            }
            else
            {
                stsSyntaxValidation.Content = "Validate as you Type disabled, go to Tools > Validate Syntax to check your syntax";
                stsSyntaxValidation.ToolTip = string.Empty;
            }
        }

        private void mnuHighlightErrors_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IsHighlightErrorsEnabled = mnuHighlightErrors.IsChecked;
            Properties.Settings.Default.EnableErrorHighlighting = _editor.DocumentManager.Options.IsHighlightErrorsEnabled;
            Properties.Settings.Default.Save();
        }

        private void mnuAutoComplete_Click(object sender, RoutedEventArgs e)
        {
            _editor.DocumentManager.Options.IsAutoCompletionEnabled = mnuAutoComplete.IsChecked;
            Properties.Settings.Default.EnableAutoComplete = _editor.DocumentManager.Options.IsAutoCompletionEnabled;
            Properties.Settings.Default.Save();
        }

        private void mnuCustomiseFileAssociations_Click(object sender, RoutedEventArgs e)
        {
            FileAssociations diag = new FileAssociations();
            diag.ShowDialog();
        }

        private void mnuCustomiseAppearance_Click(object sender, RoutedEventArgs e)
        {
            AppearanceSettings settings = new AppearanceSettings(_editor.DocumentManager.VisualOptions);
            settings.Owner = this;
            if (settings.ShowDialog() == true)
            {
                if (Properties.Settings.Default.EditorFontFace != null)
                {
                    _editor.DocumentManager.VisualOptions.FontFace = Properties.Settings.Default.EditorFontFace;
                }
                _editor.DocumentManager.VisualOptions.FontSize = Math.Round(Properties.Settings.Default.EditorFontSize, 0);
                _editor.DocumentManager.VisualOptions.Foreground = Properties.Settings.Default.EditorForeground;
                _editor.DocumentManager.VisualOptions.Background = Properties.Settings.Default.EditorBackground;

                _editor.DocumentManager.VisualOptions.ErrorBackground = Properties.Settings.Default.ErrorHighlightBackground;
                _editor.DocumentManager.VisualOptions.ErrorDecoration = Properties.Settings.Default.ErrorHighlightDecoration;
                if (Properties.Settings.Default.ErrorHighlightFontFamily != null)
                {
                    _editor.DocumentManager.VisualOptions.ErrorFontFace = Properties.Settings.Default.ErrorHighlightFontFamily;
                }
                _editor.DocumentManager.VisualOptions.ErrorForeground = Properties.Settings.Default.ErrorHighlightForeground;
            }
        }

        #endregion

        #region Tools Menu

        private void mnuTools_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            bool hasDoc = _editor.DocumentManager.ActiveDocument != null;
            mnuValidateSyntax.IsEnabled = hasDoc;
            mnuStructureView.IsEnabled = hasDoc;
        }

        private void mnuValidateSyntax_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;

            ISyntaxValidator validator = _editor.DocumentManager.ActiveDocument.SyntaxValidator;
            if (validator != null)
            {
                ISyntaxValidationResults results = _editor.DocumentManager.ActiveDocument.Validate();
                string caption = results.IsValid ? "Valid Syntax" : "Invalid Syntax";
                MessageBox.Show(results.Message, caption);
            }
            else
            {
                MessageBox.Show("Validation is not possible as there is no Syntax Validator registered for your currently selected Syntax Highlighting", "Validation Unavailable");
            }
        }

        private void mnuStructureView_Click(object sender, RoutedEventArgs e)
        {
            if (_editor.DocumentManager.ActiveDocument == null) return;
            ISyntaxValidator validator = _editor.DocumentManager.ActiveDocument.SyntaxValidator;
            if (validator != null)
            {
                ISyntaxValidationResults results = validator.Validate(_editor.DocumentManager.ActiveDocument.Text);
                if (results.IsValid)
                {
                    if (!_editor.DocumentManager.ActiveDocument.Syntax.Equals("None"))
                    {
                        try
                        {
                            SyntaxDefinition def = SyntaxManager.GetDefinition(_editor.DocumentManager.ActiveDocument.Syntax);
                            if (def.DefaultParser != null)
                            {
                                NonIndexedGraph g = new NonIndexedGraph();
                                def.DefaultParser.Load(g, new StringReader(_editor.DocumentManager.ActiveDocument.Text));
                                TriplesWindow window = new TriplesWindow(g);
                                window.ShowDialog();
                            }
                            //else if (def.Validator is RdfDatasetSyntaxValidator)
                            //{
                            //    TripleStore store = new TripleStore();
                            //    StringParser.ParseDataset(store, textEditor.Text);
                            //}
                            else if (def.Validator is SparqlResultsValidator)
                            {
                                SparqlResultSet sparqlResults = new SparqlResultSet();
                                StringParser.ParseResultSet(sparqlResults, _editor.DocumentManager.ActiveDocument.Text);
                                if (sparqlResults.ResultsType == SparqlResultsType.VariableBindings)
                                {
                                    ResultSetWindow window = new ResultSetWindow(sparqlResults);
                                    window.ShowDialog();
                                }
                                else
                                {
                                    MessageBox.Show("Cannot open Structured View since this form of SPARQL Results is not structured");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Cannot open Structured View since this is not a syntax for which Structure view is available");
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Unable to open Structured View as could not parse the Syntax successfully for structured display");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Cannot open Structured View since this is not a syntax for which Structure view is available");
                    }
                }
                else
                {
                    MessageBox.Show("Cannot open Structured View as the Syntax is not valid");
                }
            }
            else
            {
                MessageBox.Show("Cannot open Structured View as you have not selected a Syntax");
            }
        }

        #endregion

        #region Help Menu

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        #endregion

        #region Command Bindings for creating Keyboard Shortcuts

        private void NewCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuNew_Click(sender, e);
        }

        private void NewFromActiveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuNewFromActive_Click(sender, e);
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuOpen_Click(sender, e);
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSave_Click(sender, e);
        }

        private void SaveAsCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveAs_Click(sender, e);
        }

        private void SaveAllCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveAll_Click(sender, e);
        }

        private void SaveWithNTriplesExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithNTriples_Click(sender, e);
        }

        private void SaveWithTurtleExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithTurtle_Click(sender, e);
        }

        private void SaveWithN3Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithN3_Click(sender, e);
        }

        private void SaveWithRdfXmlExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithRdfXml_Click(sender, e);
        }

        private void SaveWithRdfJsonExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithRdfJson_Click(sender, e);
        }

        private void SaveWithXHtmlRdfAExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuSaveWithRdfa_Click(sender, e);
        }

        private void CloseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuClose_Click(sender, e);
        }

        private void CloseAllCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuCloseAll_Click(sender, e);
        }

        private void UndoCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuUndo_Click(sender, e);
        }

        private void RedoCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuRedo_Click(sender, e);
        }

        private void CutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuCut_Click(sender, e);
        }

        private void CopyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuCopy_Click(sender, e);
        }

        private void PasteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuPaste_Click(sender, e);
        }

        private void FindCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuFind_Click(sender, e);
        }

        private void FindNextCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuFindNext_Click(sender, e);
        }

        private void ReplaceCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuReplace_Click(sender, e);
        }

        private void GoToLineCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuGoToLine_Click(sender, e);
        }

        private void CommentSelectionExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuCommentSelection_Click(sender, e);
        }

        private void UncommentSelectionExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuUncommentSelection_Click(sender, e);
        }

        private void ToggleLineNumbersExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuShowLineNumbers.IsChecked = !mnuShowLineNumbers.IsChecked;
            mnuShowLineNumbers_Click(sender, e);
        }

        private void ToggleWordWrapExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuWordWrap.IsChecked = !mnuWordWrap.IsChecked;
            mnuWordWrap_Click(sender, e);
        }

        private void ToggleClickableUrisExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuClickableUris.IsChecked = !mnuClickableUris.IsChecked;
            mnuClickableUris_Click(sender, e);
        }

        private void IncreaseTextSizeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.FontSize = Math.Round(_editor.DocumentManager.VisualOptions.FontSize + 1.0, 0);
            Properties.Settings.Default.EditorFontSize = _editor.DocumentManager.VisualOptions.FontSize;
            Properties.Settings.Default.Save();
        }

        private void DecreaseTextSizeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (_editor.DocumentManager.VisualOptions.FontSize >= 5.0d)
            {
                _editor.DocumentManager.VisualOptions.FontSize = Math.Round(_editor.DocumentManager.VisualOptions.FontSize - 1.0, 0);
                Properties.Settings.Default.EditorFontSize = _editor.DocumentManager.VisualOptions.FontSize;
                Properties.Settings.Default.Save();
            }
        }

        private void ResetTextSizeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _editor.DocumentManager.VisualOptions.FontSize = 13.0d;
            Properties.Settings.Default.EditorFontSize = _editor.DocumentManager.VisualOptions.FontSize;
            Properties.Settings.Default.Save();
        }

        private void ToggleHighlightingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuEnableHighlighting.IsChecked = !mnuEnableHighlighting.IsChecked;
            mnuEnableHighlighting_Click(sender, e);
        }

        private void ToggleValidateAsYouTypeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuValidateAsYouType.IsChecked = !mnuValidateAsYouType.IsChecked;
            mnuValidateAsYouType_Click(sender, e);
        }

        private void ToggleValidationErrorHighlighting(object sender, ExecutedRoutedEventArgs e)
        {
            mnuHighlightErrors.IsChecked = !mnuHighlightErrors.IsChecked;
            mnuHighlightErrors_Click(sender, e);
        }

        private void ToggleAutoCompletionExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuAutoComplete.IsChecked = !mnuAutoComplete.IsChecked;
            mnuAutoComplete_Click(sender, e);
        }

        private void ValidateSyntaxExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            mnuValidateSyntax_Click(sender, e);
        }

        #endregion

        #region Other Event Handlers

        void HandleDocumentCreated(object sender, DocumentChangedEventArgs<TextEditor> args)
        {
            args.Document.TextEditor.Control.TextArea.TextView.ElementGenerators.Add(new ValidationErrorElementGenerator(args.Document.TextEditor as WpfEditorAdaptor, _editor.DocumentManager.VisualOptions));
        }

        private void HandleValidatorChanged(object sender, DocumentChangedEventArgs<TextEditor> args)
        {
            if (ReferenceEquals(args.Document, _editor.DocumentManager.ActiveDocument))
            {
                if (args.Document.SyntaxValidator == null)
                {
                    stsSyntaxValidation.Content = "No Syntax Validator available for the currently selected syntax";
                }
                else
                {
                    stsSyntaxValidation.Content = "Syntax Validation available, enable Validate as you Type or select Tools > Validate to validate";
                    _editor.DocumentManager.ActiveDocument.Validate();
                }
            }
        }

        private void HandleValidation(object sender, DocumentValidatedEventArgs<TextEditor> args)
        {
            if (ReferenceEquals(args.Document, _editor.DocumentManager.ActiveDocument))
            {
                stsSyntaxValidation.ToolTip = string.Empty;
                if (args.ValidationResults != null)
                {
                    stsSyntaxValidation.Content = args.ValidationResults.Message;
                    //Build a TextBlock with wrapping for the ToolTip
                    TextBlock block = new TextBlock();
                    block.TextWrapping = TextWrapping.Wrap;
                    block.Width = 800;
                    block.Text = args.ValidationResults.Message;
                    stsSyntaxValidation.ToolTip = block;
                    if (args.ValidationResults.Warnings.Any())
                    {
                        stsSyntaxValidation.ToolTip += "\n" + string.Join("\n", args.ValidationResults.Warnings.ToArray());
                    }
                }
                else
                {
                    stsSyntaxValidation.Content = "Syntax Validation unavailable";
                }
            }
        }

        private void HandleTextChanged(object sender, DocumentChangedEventArgs<TextEditor> args)
        {
            mnuUndo.IsEnabled = _editor.DocumentManager.ActiveDocument != null && _editor.DocumentManager.ActiveDocument.TextEditor.Control.CanUndo;
            mnuRedo.IsEnabled = _editor.DocumentManager.ActiveDocument != null && _editor.DocumentManager.ActiveDocument.TextEditor.Control.CanRedo;
        }

        private void tabDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabDocuments.SelectedIndex >= 0 && tabDocuments.Items.Count > 0)
            {
                try
                {
                    _editor.DocumentManager.SwitchTo(tabDocuments.SelectedIndex);
                    stsCurrSyntax.Content = "Current Syntax: " + _editor.DocumentManager.ActiveDocument.Syntax;
                    stsSyntaxValidation.Content = string.Empty;
                    stsSyntaxValidation.ToolTip = string.Empty;
                    _editor.DocumentManager.ActiveDocument.Validate();
                }
                catch (IndexOutOfRangeException)
                {
                    //Ignore this since we may get this because of events firing after objects have already
                    //been thrown away
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Open a File if we've been asked to do so
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                if (File.Exists(args[1]))
                {
                    Document<TextEditor> doc = _editor.DocumentManager.New();
                    try
                    {
                        doc.Open(args[1]);
                        AddTextEditor(new TabItem(), doc);
                        _editor.DocumentManager.Close(0);
                        tabDocuments.Items.RemoveAt(0);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occurred while opening the selected file: " + ex.Message, "Unable to Open File");
                        _editor.DocumentManager.Close(_editor.DocumentManager.Count - 1);
                    }
                }
            }

            //Check File Associations
            if (Properties.Settings.Default.AlwaysCheckFileAssociations)
            {
                if (Properties.Settings.Default.FirstRun)
                {
                    Properties.Settings.Default.AlwaysCheckFileAssociations = false;
                    Properties.Settings.Default.FirstRun = false;
                    Properties.Settings.Default.Save();
                }

                FileAssociations diag = new FileAssociations();
                if (!diag.AllAssociated) diag.ShowDialog(); //Don't show if all associations are already set
            }

            //Set Window size
            if (Properties.Settings.Default.WindowHeight > 0 && Properties.Settings.Default.WindowWidth > 0)
            {
                Height = Properties.Settings.Default.WindowHeight;
                Width = Properties.Settings.Default.WindowWidth;
            }
            _saveWindowSize = true;

            //Fill The MRU List
            ShowMruList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Application.Current.ShutdownMode != ShutdownMode.OnExplicitShutdown )
            {
                mnuCloseAll_Click(sender, new RoutedEventArgs());
                if (tabDocuments.Items.Count == 0)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_saveWindowSize) return;

            if (Width > 0 && Height > 0)
            {
                Properties.Settings.Default.WindowHeight = Height;
                Properties.Settings.Default.WindowWidth = Width;
                Properties.Settings.Default.Save();
            }
        }

        void EditorWindow_Drop(object sender, DragEventArgs e)
        {
            //Is the data FileDrop data?
            string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, false) as string[];
            if (droppedFilePaths == null) return;

            e.Handled = true;

            foreach (string file in droppedFilePaths)
            {
                Document<TextEditor> doc = _editor.DocumentManager.New();
                try
                {
                    doc.Open(file);
                    AddTextEditor(new TabItem(), doc);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("The dropped file '" + file + "' could not be opened due to an error: " + ex.Message, "Open File Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    _editor.DocumentManager.Close(_editor.DocumentManager.Count - 1);
                }
            }
        }

        #endregion

        #region Callbacks

        private SaveChangesMode SaveChangesCallback(Document<TextEditor> doc)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(doc.Title + " has unsaved changes, do you wish to save these changes before closing the document?", "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    return SaveChangesMode.Save;
                case MessageBoxResult.Cancel:
                    return SaveChangesMode.Cancel;
                case MessageBoxResult.No:
                default:
                    return SaveChangesMode.Discard;
            }
        }

        private string SaveAsCallback(Document<TextEditor> doc)
        {
            _sfd.Filter = FileFilterAll;
            if (doc.Filename == null || doc.Filename.Equals(string.Empty))
            {
                _sfd.Title = "Save " + doc.Title + " As...";
            }
            else
            {
                _sfd.Title = "Save " + System.IO.Path.GetFileName(doc.Filename) + " As...";
                _sfd.InitialDirectory = System.IO.Path.GetDirectoryName(doc.Filename);
                _sfd.FileName = doc.Filename;
            }

            if (_sfd.ShowDialog() == true)
            {
                return _sfd.FileName;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region MRU List

        private void ShowMruList()
        {
            if (VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles != null)
            {
                while (mnuRecentFiles.Items.Count > 2)
                {
                    mnuRecentFiles.Items.RemoveAt(2);
                }

                int i = 0;
                foreach (string file in VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles.Files)
                {
                    i++;
                    MenuItem item = new MenuItem();
                    item.Header = i + ": " + MruList.ShortenFilename(file);
                    item.Tag = file;
                    item.Click += new RoutedEventHandler(MruListFileClicked);
                    mnuRecentFiles.Items.Add(item);
                }
            }
        }

        private void UpdateMruList(string file)
        {
            if (VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles != null)
            {
                VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles.Add(file);
                ShowMruList();
            }
        }

        private void MruListFileClicked(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                MenuItem item = (MenuItem)sender;
                if (item.Tag != null)
                {
                    string file = item.Tag as string;
                    if (file != null)
                    {
                        if (File.Exists(file))
                        {
                            Document<TextEditor> doc;
                            bool add = false;
                            if (_editor.DocumentManager.ActiveDocument != null && _editor.DocumentManager.ActiveDocument.TextLength == 0 && string.IsNullOrEmpty(_editor.DocumentManager.ActiveDocument.Filename))
                            {
                                doc = _editor.DocumentManager.ActiveDocument;
                            } 
                            else 
                            {
                                doc = _editor.DocumentManager.New();
                                add = true;
                            }
                            try
                            {
                                doc.Open(file);
                                if (add)
                                {
                                    AddTextEditor(new TabItem(), doc);
                                    _editor.DocumentManager.SwitchTo(_editor.DocumentManager.Count - 1);
                                    tabDocuments.SelectedIndex = tabDocuments.Items.Count - 1;
                                    tabDocuments.SelectedItem = tabDocuments.Items[tabDocuments.Items.Count - 1];
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("An error occurred while opening the selected file: " + ex.Message, "Open File Failed");
                                _editor.DocumentManager.Close(_editor.DocumentManager.Count - 1);
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Cannot Open the Recent File '" + file + "' as it no longer exists!", "Unable to Open File", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }

        private void mnuClearRecentFiles_Click(object sender, RoutedEventArgs e)
        {
            if (VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles != null)
            {
                VDS.RDF.Utilities.Editor.Wpf.App.RecentFiles.Clear();
            }
        }

        #endregion
    }
}
