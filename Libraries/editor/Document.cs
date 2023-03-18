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
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Validation;
using VDS.RDF.Utilities.Editor.AutoComplete;
using VDS.RDF.Utilities.Editor.Syntax;

namespace VDS.RDF.Utilities.Editor
{
    /// <summary>
    /// Represents a document in the editor
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public class Document<T>
    {
        //General State
        private bool _changed = false;
        private bool _enableHighlighting = true, _enableAutoCompletion = true;
        private string _filename, _title;
        private string _syntax = "None";
        private ITextEditorAdaptor<T> _editor;
        private Encoding _encoding = Encoding.UTF8;

        //Validation
        private ISyntaxValidator _validator;
        private Exception _lastError = null;

        /// <summary>
        /// Creates a document
        /// </summary>
        /// <param name="editor">Text Editor</param>
        internal Document(ITextEditorAdaptor<T> editor)
            : this(editor, null, null) { }

        /// <summary>
        /// Creates a document
        /// </summary>
        /// <param name="editor">Text Editor</param>
        /// <param name="filename">Filename</param>
        internal Document(ITextEditorAdaptor<T> editor, string filename)
            : this(editor, filename, Path.GetFileName(filename)) { }

        /// <summary>
        /// Creates a document
        /// </summary>
        /// <param name="editor">Text Editor</param>
        /// <param name="filename">Filename</param>
        /// <param name="title">Title</param>
        internal Document(ITextEditorAdaptor<T> editor, string filename, string title)
        {
            if (editor == null) throw new ArgumentNullException("editor");
            _editor = editor;
            _filename = filename;
            _title = title;

            //Subscribe to relevant events on the Editor
            _editor.TextChanged += new TextEditorEventHandler<T>(HandleTextChanged);
            _editor.DoubleClick += new TextEditorEventHandler<T>(HandleDoubleClick);
        }

        #region General State

        /// <summary>
        /// Gets the text editor for the document
        /// </summary>
        public ITextEditorAdaptor<T> TextEditor
        {
            get
            {
                return _editor;
            }
        }

        /// <summary>
        /// Gets/Sets whether the document has changed
        /// </summary>
        public bool HasChanged
        {
            get
            {
                return _changed;
            }
            private set
            {
                _changed = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Current Filename of the Document
        /// </summary>
        public string Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    HasChanged = true;
                    RaiseEvent(FilenameChanged);
                }
            }
        }

        /// <summary>
        /// Gets/Sets the Title of the Document, if a filename is present that is always returned instead of any set title
        /// </summary>
        public string Title
        {
            get
            {
                if (_filename != null && !_filename.Equals(string.Empty))
                {
                    return Path.GetFileName(_filename);
                }
                else
                {
                    return _title;
                }
            }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    RaiseEvent(TitleChanged);
                }
            }
        }

        /// <summary>
        /// Gets/Sets the text of the document
        /// </summary>
        public string Text
        {
            get
            {
                return _editor.Text;
            }
            set
            {
                _editor.Text = value;
            }
        }

        /// <summary>
        /// Gets the length of the document
        /// </summary>
        public int TextLength
        {
            get
            {
                return _editor.TextLength;
            }
        }

        /// <summary>
        /// Gets the current caret position in the document
        /// </summary>
        public int CaretOffset
        {
            get
            {
                return _editor.CaretOffset;
            }
        }

        /// <summary>
        /// Gets the current selection start
        /// </summary>
        public int SelectionStart
        {
            get
            {
                return _editor.SelectionStart;
            }
        }

        /// <summary>
        /// Gets the current selection length
        /// </summary>
        public int SelectionLength
        {
            get
            {
                return _editor.SelectionLength;
            }
        }

        #endregion

        #region Syntax Highlighting and Validation

        /// <summary>
        /// Gets/Sets the syntax for the document
        /// </summary>
        public string Syntax
        {
            get
            {
                return _syntax;
            }
            set
            {
                if (_syntax != value)
                {
                    _syntax = value;
                    SetSyntax(_syntax);
                }
            }
        }

        /// <summary>
        /// Requests that the document auto-detect its syntax
        /// </summary>
        public void AutoDetectSyntax()
        {
            if (_filename != null && !_filename.Equals(string.Empty))
            {
                try
                {
                    //Try filename based syntax detection
                    MimeTypeDefinition def = MimeTypesHelper.GetDefinitionsByFileExtension(MimeTypesHelper.GetTrueFileExtension(_filename)).FirstOrDefault();
                    if (def != null)
                    {
                        Syntax = def.SyntaxName.GetSyntaxName();
                        return;
                    }
                }
                catch (RdfParserSelectionException)
                {
                    //Ignore and use string based detection instead
                }
            }

            //Otherwise try and use string based detection
            //First take a guess at it being a SPARQL Results format
            string text = Text;
            try
            {
                ISparqlResultsReader resultsReader = StringParser.GetResultSetParser(text);
                Syntax = resultsReader.GetSyntaxName();
            }
            catch (RdfParserSelectionException)
            {
                //Then see whether it may be a SPARQL query
                if (text.Contains("SELECT") || text.Contains("CONSTRUCT") || text.Contains("DESCRIBE") || text.Contains("ASK"))
                {
                    //Likely a SPARQL Query
                    Syntax = "SparqlQuery11";
                }
                else
                {
                    //Then take a guess at it being a RDF format
                    try
                    {
                        IRdfReader rdfReader = StringParser.GetParser(text);
                        Syntax = rdfReader.GetSyntaxName();
                    }
                    catch (RdfParserSelectionException)
                    {
                        //Finally take a guess at it being a RDF Dataset format
                        IStoreReader datasetReader = StringParser.GetDatasetParser(text);
                        Syntax = datasetReader.GetSyntaxName();
                    }
                }
            }
        }

        /// <summary>
        /// Sets the syntax configuring the associated text editor as appropriate
        /// </summary>
        /// <param name="syntax">Syntax</param>
        private void SetSyntax(string syntax)
        {
            if (_enableHighlighting)
            {
                _editor.SetHighlighter(syntax);
            }
            SyntaxValidator = SyntaxManager.GetValidator(syntax);
            if (_editor.CanAutoComplete)
            {
                if (IsAutoCompleteEnabled)
                {
                    TextEditor.AutoCompleter = AutoCompleteManager.GetAutoCompleter<T>(Syntax, TextEditor);
                }
                else
                {
                    TextEditor.AutoCompleter = null;
                }
            }

            RaiseEvent(SyntaxChanged);
        }

        /// <summary>
        /// Validates the document
        /// </summary>
        /// <returns>Syntax Validation Results if available, null otherwise</returns>
        public ISyntaxValidationResults Validate()
        {
            if (_validator != null)
            {
                ISyntaxValidationResults results = _validator.Validate(Text);
                _lastError = results.Error;
                RaiseValidated(results);
                return results;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets/Sets the Current Validator
        /// </summary>
        public ISyntaxValidator SyntaxValidator
        {
            get
            {
                return _validator;
            }
            set
            {
                if (!ReferenceEquals(_validator, value))
                {
                    _validator = value;
                    RaiseEvent(ValidatorChanged);
                }
            }
        }

        /// <summary>
        /// Gets the Last Validation Error
        /// </summary>
        public Exception LastValidationError
        {
            get
            {
                return _lastError;
            }
        }

        /// <summary>
        /// Gets/Sets whether highlighting is enabled
        /// </summary>
        public bool IsHighlightingEnabled
        {
            get
            {
                return _enableHighlighting;
            }
            set
            {
                if (value != _enableHighlighting)
                {
                    _enableHighlighting = value;
                    if (value)
                    {
                        TextEditor.SetHighlighter(Syntax);
                    }
                    else
                    {
                        TextEditor.SetHighlighter(null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether auto-completion is enabled
        /// </summary>
        public bool IsAutoCompleteEnabled
        {
            get
            {
                return _enableAutoCompletion;
            }
            set
            {
                if (value != _enableAutoCompletion)
                {
                    _enableAutoCompletion = value;
                    if (value)
                    {
                        TextEditor.AutoCompleter = AutoCompleteManager.GetAutoCompleter<T>(Syntax, TextEditor);
                    }
                    else
                    {
                        TextEditor.AutoCompleter = null;
                    }
                }
            }
        }

        #endregion

        #region File Actions

        /// <summary>
        /// Gets the encoding in which the document should be saved
        /// </summary>
        /// <returns></returns>
        private Encoding GetEncoding()
        {
            if (_encoding.Equals(Encoding.UTF8))
            {
                return new UTF8Encoding(GlobalOptions.UseBomForUtf8);
            }
            else
            {
                return _encoding;
            }
        }

        /// <summary>
        /// Saves the document assuming it has a file associated with it
        /// </summary>
        public void Save()
        {
            if (_filename != null && !_filename.Equals(string.Empty))
            {
                using (StreamWriter writer = new StreamWriter(_filename, false, GetEncoding()))
                {
                    writer.Write(Text);
                    writer.Close();
                }
                RaiseEvent(Saved);
                HasChanged = false;
            }
        }

        /// <summary>
        /// Saves the document with the given filename
        /// </summary>
        /// <param name="filename">Filename</param>
        public void SaveAs(string filename)
        {
            if (filename == null) throw new ArgumentNullException("filename");
            if (filename.Equals(string.Empty)) throw new ArgumentException("filename", "Filename cannot be empty");
            _filename = filename;
            RaiseEvent(FilenameChanged);
            Save();
        }

        /// <summary>
        /// Opens the document from a file
        /// </summary>
        /// <param name="filename">Filename</param>
        public void Open(string filename)
        {
            Filename = filename;
            using (StreamReader reader = new StreamReader(_filename))
            {
                _encoding = reader.CurrentEncoding;
                Text = reader.ReadToEnd();
                reader.Close();
            }
            AutoDetectSyntax();
            HasChanged = false;
            RaiseEvent(Opened);
        }

        /// <summary>
        /// Reloads the document
        /// </summary>
        public void Reload()
        {

        }

        #endregion

        #region Text Editor Events

        /// <summary>
        /// Handler for the TextChanged event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        private void HandleTextChanged(object sender, TextEditorEventArgs<T> args)
        {
            HasChanged = true;
            RaiseEvent(sender, TextChanged);
        }

        /// <summary>
        /// Handler for the DoubleClick event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Arguments</param>
        private void HandleDoubleClick(object sender, TextEditorEventArgs<T> args)
        {
            if (_editor.SymbolSelector != null)
            {
                _editor.SymbolSelector.SelectSymbol(this);
            }
        }

        #endregion

        #region Document Events

        private void RaiseEvent(DocumentChangedHandler<T> evt)
        {
            RaiseEvent(this, evt);
        }

        private void RaiseEvent(object sender, DocumentChangedHandler<T> evt)
        {
            if (evt != null)
            {
                evt(sender, new DocumentChangedEventArgs<T>(this));
            }
        }

        private void RaiseValidated(ISyntaxValidationResults results)
        {
            DocumentValidatedHandler<T> d = Validated;
            if (d != null)
            {
                d(this, new DocumentValidatedEventArgs<T>(this, results));
            }
        }

        /// <summary>
        /// Event which is raised when the document text changes
        /// </summary>
        public event DocumentChangedHandler<T> TextChanged;

        /// <summary>
        /// Event which is raised when the document is reloaded
        /// </summary>
        public event DocumentChangedHandler<T> Reloaded;

        /// <summary>
        /// Event which is raised when the document is opened
        /// </summary>
        public event DocumentChangedHandler<T> Opened;

        /// <summary>
        /// Event which is raised when the syntax for the document is changed
        /// </summary>
        public event DocumentChangedHandler<T> SyntaxChanged;

        /// <summary>
        /// Event which is raised when the filename for the document is changed
        /// </summary>
        public event DocumentChangedHandler<T> FilenameChanged;

        /// <summary>
        /// Event which is raised when the title of the document is changed
        /// </summary>
        public event DocumentChangedHandler<T> TitleChanged;

        /// <summary>
        /// Event which is raised when the document is saved
        /// </summary>
        public event DocumentChangedHandler<T> Saved;

        /// <summary>
        /// Event which is raised when the syntax validator for the document is changed
        /// </summary>
        public event DocumentChangedHandler<T> ValidatorChanged;

        /// <summary>
        /// Event which is raised when the document is validated
        /// </summary>
        public event DocumentValidatedHandler<T> Validated;

        #endregion
    }
}
