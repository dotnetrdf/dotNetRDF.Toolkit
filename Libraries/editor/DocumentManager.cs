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
using System.Linq;
using System.Text;
using VDS.RDF.Parsing.Validation;
using VDS.RDF.Utilities.Editor.AutoComplete;

namespace VDS.RDF.Utilities.Editor
{
    /// <summary>
    /// The document manager is a manager for all the documents being managed by an editor
    /// </summary>
    /// <typeparam name="TControl">Control Type</typeparam>
    /// <typeparam name="TFont">Font Type</typeparam>
    /// <typeparam name="TColor">Colour Type</typeparam>
    public class DocumentManager<TControl, TFont, TColor>
        where TFont : class
        where TColor : struct
    {
        //General State
        private ITextEditorAdaptorFactory<TControl> _factory;
        private List<Document<TControl>> _documents = new List<Document<TControl>>();
        private int _current = 0;
        private ManagerOptions<TControl> _options = new ManagerOptions<TControl>();
        private VisualOptions<TFont, TColor> _visualOptions;
        private string _defaultTitle = "Untitled";
        private string _defaultSyntax = "None";
        private int _nextID = 0;

        //Default Callbacks
        private SaveChangesCallback<TControl> _defaultSaveChangesCallback = new SaveChangesCallback<TControl>(d => SaveChangesMode.Discard);
        private SaveAsCallback<TControl> _defaultSaveAsCallback = new SaveAsCallback<TControl>(d => null);

        /// <summary>
        /// Creates a new document manager
        /// </summary>
        /// <param name="factory">Text Editor factory</param>
        public DocumentManager(ITextEditorAdaptorFactory<TControl> factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            _factory = factory;

            if (_factory is IVisualTextEditorAdaptorFactory<TControl, TFont, TColor>)
            {
                _visualOptions = ((IVisualTextEditorAdaptorFactory<TControl, TFont, TColor>)_factory).GetDefaultVisualOptions();
            }

            //Wire Up Events
            _options.HighlightingToggled += HandleHighlightingToggled;
            _options.HighlightErrorsToggled += HandleHighlightErrorsToggled;
            _options.AutoCompleteToggled += HandleAutoCompleteToggled;
            _options.SymbolSelectorChanged += HandleSymbolSelectorChanged;
            if (_visualOptions != null)
            {
                _visualOptions.Changed += HandleVisualOptionsChanged;
            }
        }

        #region General State

        /// <summary>
        /// Gets/Sets the default title for documents
        /// </summary>
        public string DefaultTitle
        {
            get
            {
                return _defaultTitle;
            }
            set
            {
                _defaultTitle = value;
            }
        }

        /// <summary>
        /// Gets/Sets the default syntax for documents
        /// </summary>
        public string DefaultSyntax
        {
            get
            {
                return _defaultSyntax;
            }
            set
            {
                _defaultSyntax = value;
            }
        }

        /// <summary>
        /// Gets the manager options
        /// </summary>
        public ManagerOptions<TControl> Options
        {
            get
            {
                return _options;
            }
        }

        /// <summary>
        /// Gets/Sets the visual options
        /// </summary>
        public VisualOptions<TFont, TColor> VisualOptions
        {
            get
            {
                return _visualOptions;
            }
            set
            {
                _visualOptions = value;
            }
        }

        /// <summary>
        /// Gets the active document or null if there are no documents
        /// </summary>
        public Document<TControl> ActiveDocument
        {
            get
            {
                if (_documents.Count > 0)
                {
                    return _documents[_current];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the index of the active document or -1 if there are no documents
        /// </summary>
        public int ActiveDocumentIndex
        {
            get
            {
                if (_documents.Count > 0)
                {
                    return _current;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Gets a document by index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Document</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is outside the range of acceptable document indexes</exception>
        public Document<TControl> this[int index]
        {
            get
            {
                if (index >= 0 && index < _documents.Count)
                {
                    return _documents[index];
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets all the documents
        /// </summary>
        public IEnumerable<Document<TControl>> Documents
        {
            get
            {
                return _documents;
            }
        }

        /// <summary>
        /// Gets the count of documents
        /// </summary>
        public int Count
        {
            get
            {
                return _documents.Count;
            }
        }

        #endregion

        #region Default Callbacks

        /// <summary>
        /// Gets/Sets the default save changes callback
        /// </summary>
        public SaveChangesCallback<TControl> DefaultSaveChangesCallback
        {
            get
            {
                return _defaultSaveChangesCallback;
            }
            set
            {
                if (value != null)
                {
                    _defaultSaveChangesCallback = value;
                }
            }
        }

        /// <summary>
        /// Gets/Sets the default save as callback
        /// </summary>
        public SaveAsCallback<TControl> DefaultSaveAsCallback
        {
            get
            {
                return _defaultSaveAsCallback;
            }
            set
            {
                if (value != null)
                {
                    _defaultSaveAsCallback = value;
                }
            }
        }

        #endregion

        #region Document Management

        /// <summary>
        /// Helper method which adjusts the active document index appropriately
        /// <para>
        /// This includes handling the wrap around of the index when switching to the next/previous document and when closing the last document.
        /// </para>
        /// </summary>
        private void CorrectIndex()
        {
            if (_documents.Count > 0)
            {
                if (_current >= _documents.Count)
                {
                    _current = _documents.Count - 1;
                }
                else if (_current < 0)
                {
                    _current = _documents.Count - 1;
                }
                RaiseActiveDocumentChanged(ActiveDocument);
            }
        }

        /// <summary>
        /// Creates a new document
        /// </summary>
        /// <returns>New Document</returns>
        public Document<TControl> New()
        {
            return New(_defaultTitle + (++_nextID));
        }

        /// <summary>
        /// Creates a new document and makes it the active document if desired
        /// </summary>
        /// <param name="switchTo">Whether to make the new document the active document</param>
        /// <returns>New Document</returns>
        public Document<TControl> New(bool switchTo)
        {
            return New(_defaultTitle + (++_nextID), switchTo);
        }

        /// <summary>
        /// Creates a new document
        /// </summary>
        /// <param name="title">Title</param>
        /// <returns>New Document</returns>
        public Document<TControl> New(string title)
        {
            return New(title, false);
        }

        /// <summary>
        /// Creates a new document and makes it the active document if desired
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="switchTo">Whether to make the new document the active document</param>
        /// <returns>New Document</returns>
        public Document<TControl> New(string title, bool switchTo)
        {
            Document<TControl> doc = new Document<TControl>(_factory.CreateAdaptor(), null, title);

            //Add the document to the collection of documents
            //Register for appropriate events on the document
            _documents.Add(doc);
            RaiseDocumentCreated(doc);
            doc.ValidatorChanged += new DocumentChangedHandler<TControl>(HandleValidatorChanged);
            doc.Opened += new DocumentChangedHandler<TControl>(HandleTextChanged);
            doc.TextChanged += new DocumentChangedHandler<TControl>(HandleTextChanged);

            //Apply relevant global options to the Document
            doc.Syntax = _defaultSyntax;
            doc.IsHighlightingEnabled = _options.IsSyntaxHighlightingEnabled;
            doc.IsAutoCompleteEnabled = _options.IsAutoCompletionEnabled;
            if (doc.IsAutoCompleteEnabled && doc.TextEditor.CanAutoComplete)
            {
                doc.TextEditor.AutoCompleter = AutoCompleteManager.GetAutoCompleter<TControl>(doc.Syntax, doc.TextEditor);
            }
            else
            {
                doc.TextEditor.AutoCompleter = null;
            }
            if (_visualOptions != null) doc.TextEditor.Apply<TFont, TColor>(_visualOptions);
            doc.TextEditor.SymbolSelector = _options.CurrentSymbolSelector;

            //Switch to the new document if required
            if (switchTo)
            {
                _current = _documents.Count - 1;
                RaiseActiveDocumentChanged(ActiveDocument);
            }
            return doc;
        }

        /// <summary>
        /// Creates a new document from the active document
        /// </summary>
        /// <returns>New Document</returns>
        public Document<TControl> NewFromActive()
        {
            return NewFromExisting(ActiveDocument, false);
        }

        /// <summary>
        /// Creates a new document from the active document and makes it the active document if desired
        /// </summary>
        /// <param name="switchTo">Whether to make it the active document</param>
        /// <returns>New Document</returns>
        public Document<TControl> NewFromActive(bool switchTo)
        {
            return NewFromExisting(ActiveDocument, switchTo);
        }

        /// <summary>
        /// Creates a new document from an existing document
        /// </summary>
        /// <param name="doc">Existing Document</param>
        /// <returns>New Document</returns>
        public Document<TControl> NewFromExisting(Document<TControl> doc)
        {
            return NewFromExisting(doc, false);
        }

        /// <summary>
        /// Creates a new document from an existing document and makes it the active document 
        /// </summary>
        /// <param name="doc">Existing Document</param>
        /// <param name="switchTo">Whether to make it the active document</param>
        /// <returns>New Document</returns>
        public Document<TControl> NewFromExisting(Document<TControl> doc, bool switchTo)
        {
            Document<TControl> clonedDoc = New();
            clonedDoc.Text = doc.Text;
            clonedDoc.Syntax = doc.Syntax;
            if (switchTo)
            {
                _current = _documents.Count - 1;
                RaiseActiveDocumentChanged(ActiveDocument);
            }
            return clonedDoc;
        }

        /// <summary>
        /// Close the active document
        /// </summary>
        /// <returns>True if the document was closed, false otherwise.  May be false if the application cancels the close.</returns>
        public bool Close()
        {
            return Close(_defaultSaveChangesCallback, _defaultSaveAsCallback);
        }

        /// <summary>
        /// Close the active document
        /// </summary>
        /// <param name="saveChanges">Save Changes Callback</param>
        /// <param name="saveAs">Save As Callback</param>
        /// <returns>True if the document was closed, false otherwise.  May be false if the application cancels the close.</returns>
        public bool Close(SaveChangesCallback<TControl> saveChanges, SaveAsCallback<TControl> saveAs)
        {
            if (_documents.Count > 0)
            {
                if (!Close(ActiveDocument, saveChanges, saveAs)) return false;
                _documents.RemoveAt(_current);
                CorrectIndex();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Closes the specified document
        /// </summary>
        /// <param name="index">Document Index</param>
        /// <returns>True if the document was closed, false otherwise.  May be false if the application cancels the close.</returns>
        public bool Close(int index)
        {
            return Close(index, _defaultSaveChangesCallback, _defaultSaveAsCallback);
        }

        /// <summary>
        /// Close the specified document
        /// </summary>
        /// <param name="index">Document Index</param>
        /// <param name="saveChanges">Save Changes Callback</param>
        /// <param name="saveAs">Save As Callback</param>
        /// <returns>True if the document was closed, false otherwise.  May be false if the application cancels the close.</returns>
        public bool Close(int index, SaveChangesCallback<TControl> saveChanges, SaveAsCallback<TControl> saveAs)
        {
            if (index >= 0 && index < _documents.Count)
            {
                Document<TControl> doc = _documents[index];
                if (!Close(doc, saveChanges, saveAs)) return false;
                _documents.RemoveAt(index);
                CorrectIndex();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Close the specified document
        /// </summary>
        /// <param name="doc">Document to close</param>
        /// <param name="saveChanges">Save Changes Callback</param>
        /// <param name="saveAs">Save As Callback</param>
        /// <returns>True if the document was closed, false otherwise.  May be false if the application cancels the close.</returns>
        private bool Close(Document<TControl> doc, SaveChangesCallback<TControl> saveChanges, SaveAsCallback<TControl> saveAs)
        {
            //Get Confirmation to save/discard changes - allows application to cancel close
            if (doc.HasChanged)
            {
                switch (saveChanges(doc))
                {
                    case SaveChangesMode.Cancel:
                        return false;
                    case SaveChangesMode.Discard:
                        //Do nothing
                        return true;
                    case SaveChangesMode.Save:
                        if (doc.Filename != null && !doc.Filename.Equals(string.Empty))
                        {
                            doc.Save();
                        }
                        else
                        {
                            string filename = saveAs(doc);
                            if (filename != null && !filename.Equals(string.Empty))
                            {
                                doc.SaveAs(filename);
                            }
                        }
                        return true;
                    default:
                        return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Close all documents
        /// </summary>
        /// <returns>True if all documents are closed, false otherwise.  May be false if the application cancels one/more of the close operations</returns>
        public bool CloseAll()
        {
            return CloseAll(_defaultSaveChangesCallback, _defaultSaveAsCallback);
        }

        /// <summary>
        /// Closes all documents
        /// </summary>
        /// <param name="saveChanges">Save Changes callback</param>
        /// <param name="saveAs">Save As callback</param>
        /// <returns>True if all documents are closed, false otherwise.  May be false if the application cancels one/more of the close operations</returns>
        public bool CloseAll(SaveChangesCallback<TControl> saveChanges, SaveAsCallback<TControl> saveAs)
        {
            int i = 0;
            while (i < _documents.Count)
            {
                //Get Confirmation to save/discard changes - allows application to cancel close
                if (!Close(i, saveChanges, saveAs))
                {
                    //If the document was not closed increment the counter
                    //Otherwise we will be stuck trying to close the same document forever
                    i++;
                }
            }
            return _documents.Count == 0;
        }

        /// <summary>
        /// Reloads all documents
        /// </summary>
        public void ReloadAll()
        {
            _documents.ForEach(d => d.Reload());
        }

        /// <summary>
        /// Saves all documents
        /// </summary>
        public void SaveAll()
        {
            SaveAll(_defaultSaveAsCallback);
        }

        /// <summary>
        /// Saves all documents
        /// </summary>
        /// <param name="saveAs">Save As callback</param>
        public void SaveAll(SaveAsCallback<TControl> saveAs)
        {
            foreach (Document<TControl> doc in _documents)
            {
                if (doc.Filename != null && !doc.Filename.Equals(string.Empty))
                {
                    doc.Save();
                }
                else
                {
                    string filename = saveAs(doc);
                    if (filename != null && !filename.Equals(string.Empty))
                    {
                        doc.SaveAs(filename);
                    }
                }
            }
        }

        /// <summary>
        /// Switches to a specific document
        /// </summary>
        /// <param name="index">Document Index</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the given index is not valid</exception>
        public void SwitchTo(int index)
        {
            if (index >= 0 && index < _documents.Count)
            {
                if (index != _current)
                {
                    _current = index;
                    RaiseActiveDocumentChanged(ActiveDocument);
                }
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        /// <summary>
        /// Switch to the previous document
        /// </summary>
        public void PrevDocument()
        {
            if (_documents.Count > 1)
            {
                _current--;
                CorrectIndex();
            }
        }

        /// <summary>
        /// Switch to the next document
        /// </summary>
        public void NextDocument()
        {
            if (_documents.Count > 1)
            {
                _current++;
                CorrectIndex();
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Handles the validator changed event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        private void HandleValidatorChanged(object sender, DocumentChangedEventArgs<TControl> args)
        {
            //Update Syntax Validation if appropriate
            if (args.Document.SyntaxValidator != null && _options.IsValidateAsYouTypeEnabled)
            {
                ISyntaxValidationResults results = args.Document.Validate();
                if (results != null && _options.IsHighlightErrorsEnabled)
                {
                    if (results.Error != null && !results.IsValid)
                    {
                        args.Document.TextEditor.ClearErrorHighlights();
                        args.Document.TextEditor.AddErrorHighlight(results.Error);
                    }
                    else
                    {
                        args.Document.TextEditor.ClearErrorHighlights();
                    }
                }
                else
                {
                    args.Document.TextEditor.ClearErrorHighlights();
                }
            }
        }

        /// <summary>
        /// Handles the text changed event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        private void HandleTextChanged(object sender, DocumentChangedEventArgs<TControl> args)
        {
            //Update Syntax Validation if appropriate
            if (args.Document.SyntaxValidator != null && _options.IsValidateAsYouTypeEnabled)
            {
                args.Document.TextEditor.ClearErrorHighlights();
                ISyntaxValidationResults results = args.Document.Validate();
                if (results != null && _options.IsHighlightErrorsEnabled)
                {
                    if (results.Error != null && !results.IsValid)
                    {
                        args.Document.TextEditor.AddErrorHighlight(results.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handle the higlighting toggled event
        /// </summary>
        private void HandleHighlightingToggled()
        {
            foreach (Document<TControl> doc in _documents)
            {
                doc.IsHighlightingEnabled = _options.IsSyntaxHighlightingEnabled;
            }
        }

        /// <summary>
        /// Handle the highlight errors toggled event
        /// </summary>
        private void HandleHighlightErrorsToggled()
        {
            foreach (Document<TControl> doc in _documents)
            {
                doc.TextEditor.Refresh();
            }
        }

        /// <summary>
        /// Handle the auto-complete toggled event
        /// </summary>
        private void HandleAutoCompleteToggled()
        {
            foreach (Document<TControl> doc in _documents)
            {
                doc.IsAutoCompleteEnabled = _options.IsAutoCompletionEnabled;
            }
        }

        /// <summary>
        /// Handle the symbol selection toggled event
        /// </summary>
        private void HandleSymbolSelectionToggled()
        {
            foreach (Document<TControl> doc in _documents)
            {
                doc.TextEditor.SymbolSelector = (Options.IsSymbolSelectionEnabled ? Options.CurrentSymbolSelector : null);
            }
        }

        /// <summary>
        /// Handle the symbol selector changed event
        /// </summary>
        private void HandleSymbolSelectorChanged()
        {
            foreach (Document<TControl> doc in _documents)
            {
                doc.TextEditor.SymbolSelector = (Options.IsSymbolSelectionEnabled ? Options.CurrentSymbolSelector : null);
            }
        }

        /// <summary>
        /// Handle the visual options changed event
        /// </summary>
        private void HandleVisualOptionsChanged()
        {
            if (_visualOptions != null)
            {
                foreach (Document<TControl> doc in _documents)
                {
                    doc.TextEditor.Apply<TFont, TColor>(_visualOptions);
                }
            }
        }

        #endregion

        #region Events

        private void RaiseActiveDocumentChanged(Document<TControl> doc)
        {
            DocumentChangedHandler<TControl> d = ActiveDocumentChanged;
            if (d != null)
            {
                d(this, new DocumentChangedEventArgs<TControl>(doc));
            }
        }

        private void RaiseDocumentCreated(Document<TControl> doc)
        {
            DocumentChangedHandler<TControl> d = DocumentCreated;
            if (d != null)
            {
                d(this, new DocumentChangedEventArgs<TControl>(doc));
            }
        }

        /// <summary>
        /// Event which is raised when the active document changes
        /// </summary>
        public event DocumentChangedHandler<TControl> ActiveDocumentChanged;

        /// <summary>
        /// Event which is raised when a new document is created
        /// </summary>
        public event DocumentChangedHandler<TControl> DocumentCreated;

        #endregion
    }
}
