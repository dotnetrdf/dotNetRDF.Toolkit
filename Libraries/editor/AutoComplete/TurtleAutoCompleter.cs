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
using System.Text.RegularExpressions;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using VDS.RDF.Utilities.Editor.AutoComplete.Data;
using VDS.RDF.Utilities.Editor.AutoComplete.Vocabularies;

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// Auto-complete implementation for Turtle
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public class TurtleAutoCompleter<T>
        : BaseAutoCompleter<T>
    {
        private const string PrefixDeclaration = "prefix";
        private const string BaseDeclaration = "base";
        /// <summary>
        /// Regular expression pattern for prefix declarations
        /// </summary>
        protected string PrefixRegexPattern = @"@prefix\s+(\p{L}(\p{L}|\p{N}|-|_)*)?:\s+<((\\>|[^>])*)>\s*\.";
        /// <summary>
        /// Regular expression pattern for blank node IDs
        /// </summary>
        protected string BlankNodePattern = @"_:\p{L}(\p{L}|\p{N}|-|_)*";
        private LoadNamespaceTermsDelegate _namespaceLoader = new LoadNamespaceTermsDelegate(AutoCompleteManager.LoadNamespaceTerms);

        /// <summary>
        /// Keywords that the completer permits
        /// </summary>
        protected List<ICompletionData> _keywords = new List<ICompletionData>()
        {
            new KeywordData("a", "Shorthand for RDF type predicate - equivalent to the URI <" + RdfSpecsHelper.RdfType + ">"),
            new KeywordData("false", "Keyword representing false - equivalent to the literal \"false\"^^<" + XmlSpecsHelper.XmlSchemaDataTypeBoolean + ">"),
            new KeywordData("true", "Keyword representing true - equivalent to the literal \"true\"^^<" + XmlSpecsHelper.XmlSchemaDataTypeBoolean + ">")
        };

        private OffsetScopedNamespaceMapper _nsmap = new OffsetScopedNamespaceMapper(true);
        private Dictionary<string, List<NamespaceTerm>> _namespaceTerms = new Dictionary<string, List<NamespaceTerm>>();
        private HashSet<ICompletionData> _bnodes = new HashSet<ICompletionData>();
        private BlankNodeMapper _bnodemap = new BlankNodeMapper();

        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public TurtleAutoCompleter(ITextEditorAdaptor<T> editor)
            : base(editor) { }

        #region State Detection

        /// <summary>
        /// Detects the state of the auto-completer
        /// </summary>
        protected override void DetectStateInternal()
        {
            //Look for defined Prefixes - we have to clear the list of namespaces and prefixes since they might have been altered
            DetectNamespaces();
            DetectBlankNodes();
        }

        /// <summary>
        /// Detects declared namespaces
        /// </summary>
        protected virtual void DetectNamespaces()
        {
            _nsmap.Clear();

            foreach (Match m in Regex.Matches(_editor.Text, PrefixRegexPattern))
            {
                //Set the Offset for this Namespace so it gets properly scoped later on
                _nsmap.CurrentOffset = m.Index + m.Length;

                string prefix = m.Groups[1].Value;
                string nsUri = m.Groups[3].Value;
                try
                {
                    _nsmap.AddNamespace(prefix, new Uri(nsUri));
                    if (!_namespaceTerms.ContainsKey(nsUri))
                    {
                        _namespaceLoader.BeginInvoke(nsUri, LoadNamespaceTermsCallback, null);
                    }
                }
                catch (UriFormatException)
                {
                    //Ignore
                }
            }

            //TODO: Add auto-detection of declared classes and properties
            if (CanDeclareNewTerms)
            {
                //TODO: Need to get the relevant parser and then invoke it on this text using TermDetectionHandler
            }
        }

        /// <summary>
        /// Gets whether new terms can be declared
        /// </summary>
        protected virtual bool CanDeclareNewTerms
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Detects declared blank nodes
        /// </summary>
        protected virtual void DetectBlankNodes()
        {
            _bnodes.Clear();

            foreach (Match m in Regex.Matches(_editor.Text, BlankNodePattern))
            {
                string id = m.Value;
                if (_bnodes.Add(new BlankNodeData(id)))
                {
                    _bnodemap.CheckID(ref id);
                }                
            }
        }

        /// <summary>
        /// Delegate for requesting namespace terms
        /// </summary>
        /// <param name="namespaceUri">Namespace URI</param>
        /// <returns>Namespace Terms</returns>
        private delegate IEnumerable<NamespaceTerm> LoadNamespaceTermsDelegate(string namespaceUri);

        /// <summary>
        /// Callback to fire when namespace terms are received
        /// </summary>
        /// <param name="result">Async Result</param>
        private void LoadNamespaceTermsCallback(IAsyncResult result)
        {
            try
            {
                IEnumerable<NamespaceTerm> terms = (IEnumerable<NamespaceTerm>)_namespaceLoader.EndInvoke(result);

                if (terms.Any())
                {
                    string nsUri = terms.First().NamespaceUri;

                    if (!_namespaceTerms.ContainsKey(nsUri)) _namespaceTerms.Add(nsUri, new List<NamespaceTerm>());

                    _namespaceTerms[nsUri].AddRange(terms);
                }
            }
            catch (Exception ex)
            {
                //Ignore exceptions
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Start Auto-completion

        /// <summary>
        /// Start literal completion
        /// </summary>
        /// <param name="newText">New text</param>
        protected virtual void StartLiteralCompletion(string newText)
        {
            if (TemporaryState == AutoCompleteState.Literal || TemporaryState == AutoCompleteState.LongLiteral)
            {
                TemporaryState = AutoCompleteState.None;
                State = AutoCompleteState.None;
            }
            else
            {
                State = AutoCompleteState.Literal;
            }
        }

        /// <summary>
        /// Start comment completion
        /// </summary>
        /// <param name="newText">New text</param>
        protected virtual void StartCommentCompletion(string newText)
        {
            State = AutoCompleteState.Comment;
        }

        /// <summary>
        /// Start URI completion
        /// </summary>
        /// <param name="newText">New text</param>
        protected virtual void StartUriCompletion(string newText)
        {
            State = AutoCompleteState.Uri;
        }

        /// <summary>
        /// Start QName completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartQNameCompletion(string newText)
        {
            State = AutoCompleteState.QName;

            _editor.Suggest(GetQNameCompletionData());
        }

        /// <summary>
        /// Start Blank Node completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartBNodeCompletion(string newText)
        {
            State = AutoCompleteState.BNode;

            _editor.Suggest(new NewBlankNodeData(_bnodemap.GetNextID()).AsEnumerable<ICompletionData>().Concat(_bnodes));
        }

        /// <summary>
        /// Start Keyword or QName completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartKeywordOrQNameCompletion(string newText)
        {
            //Backtrack start point
            BacktrackStartOffset(s => IsValidPartialKeyword(s) || IsValidPartialQName(s));

            if (IsValidPartialKeyword(CurrentText))
            {
                State = AutoCompleteState.KeywordOrQName;
                _editor.Suggest(_keywords.Concat(GetQNameCompletionData()));
            }
            else if (IsValidPartialQName(CurrentText))
            {
                State = AutoCompleteState.QName;
                _editor.Suggest(GetQNameCompletionData());
            }
        }

        /// <summary>
        /// Start declaration completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartDeclarationCompletion(string newText)
        {
            State = AutoCompleteState.Declaration;

            //Build up the Possible Declarations
            List<ICompletionData> data = new List<ICompletionData>();

            //Add Base Declaration
            data.Add(new TurtleStyleBaseDeclarationData());

            //Add New Namespace Declarations
            data.Add(new TurtleStyleDefaultPrefixDeclarationData());
            string nextPrefix = "ns0";
            int nextPrefixID = 0;
            while (_nsmap.HasNamespace(nextPrefix))
            {
                nextPrefixID++;
                nextPrefix = "ns" + nextPrefixID;
            }
            data.Add(new TurtleStylePrefixDeclarationData(nextPrefix, "Enter new Namespace URI here"));

            //Add Existing Namespace Declarations
            foreach (VocabularyDefinition vocab in AutoCompleteManager.Vocabularies)
            {
                data.Add(new TurtleStylePrefixDeclarationData(vocab.Prefix, vocab.NamespaceUri));
            }

            _editor.Suggest(data);
        }

        #endregion

        #region Auto-completion

        /// <summary>
        /// Try to auto-complete
        /// </summary>
        /// <param name="newText">New Text</param>
        public override void TryAutoComplete(string newText)
        {
            //Don't do anything if auto-complete not currently active
            if (State == AutoCompleteState.Disabled || State == AutoCompleteState.Inserted) return;

            if (State == AutoCompleteState.None)
            {
                //If State is None then Start a New Completion

                //Actual Start Offset is always -1 from where we are as the user has typed a character
                StartOffset = _editor.CaretOffset - 1;

                if (newText == "@")
                {
                    StartDeclarationCompletion(newText);
                }
                else if (newText.Length == 1)
                {
                    char c = newText[0];
                    if (char.IsLetter(c))
                    {
                        StartKeywordOrQNameCompletion(newText);
                    }
                    else if (c == '_')
                    {
                        StartBNodeCompletion(newText);
                    }
                    else if (c == ':')
                    {
                        StartQNameCompletion(newText);
                    }
                    else if (c == '<')
                    {
                        StartUriCompletion(newText);
                    }
                    else if (c == '#')
                    {
                        StartCommentCompletion(newText);
                    }
                    else if (c == '"')
                    {
                        StartLiteralCompletion(newText);
                    }
                    else if (c == '.' || c == ',' || c == ';')
                    {
                        State = AutoCompleteState.None;
                    }
                }

                if (State == AutoCompleteState.None || State == AutoCompleteState.Disabled) return;
            }
            else
            {
                try
                {
                    //If Length is less than zero then user has moved the caret so we'll abort our completion and start a new one
                    if (Length < 0 || Length == 1)
                    {
                        _editor.EndSuggestion();
                        TryAutoComplete(newText);
                        return;
                    }

                    if (newText.Length > 0)
                    {
                        switch (State)
                        {
                            case AutoCompleteState.Declaration:
                                TryDeclarationCompletion(newText);
                                break;

                            case AutoCompleteState.Base:
                                TryBaseCompletion(newText);
                                break;

                            case AutoCompleteState.Prefix:
                                TryPrefixCompletion(newText);
                                break;

                            case AutoCompleteState.KeywordOrQName:
                                TryKeywordOrQNameCompletion(newText);
                                break;

                            case AutoCompleteState.QName:
                                TryQNameCompletion(newText);
                                break;

                            case AutoCompleteState.BNode:
                                TryBNodeCompletion(newText);
                                break;

                            case AutoCompleteState.Uri:
                                TryUriCompletion(newText);
                                break;

                            case AutoCompleteState.Literal:
                                TryLiteralCompletion(newText);
                                break;

                            case AutoCompleteState.LongLiteral:
                                TryLongLiteralCompletion(newText);
                                break;

                            case AutoCompleteState.Comment:
                                TryCommentCompletion(newText);
                                break;

                            default:
                                //Nothing to do as no other auto-completion is implemented by this completer
                                //Derived implementations may call us first and then add in their extra cases if we don't hit anything
                                break;
                        }
                    }
                }
                catch
                {
                    //If any kind of error occurs just abort auto-completion
                    State = AutoCompleteState.None;
                    _editor.EndSuggestion();
                }
            }
        }

        /// <summary>
        /// Try to auto-complete a long literal
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryLongLiteralCompletion(string newText)
        {
            if (newText == "\"")
            {
                //Is this an escaped "?
                if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\\""))
                {
                    //Not escaped so terminate the literal if the buffer ends in 3 " and the length is >= 6
                    if (CurrentText.Length >= 6 && CurrentText.Substring(CurrentText.Length - 3, 3).Equals("\"\"\""))
                    {
                        LastCompletion = AutoCompleteState.LongLiteral;
                        _editor.EndSuggestion();
                    }
                }
            }
        }

        /// <summary>
        /// Try to auto-complete a literal
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryLiteralCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
            }

            if (newText == "\"")
            {
                if (CurrentText.Length == 2)
                {
                    //Might be a long literal so have to wait and see
                }
                else if (CurrentText.Length == 3)
                {
                    if (CurrentText.ToString().Equals("\"\"\""))
                    {
                        //Switch to long literal mode
                        State = AutoCompleteState.LongLiteral;
                    }
                    else if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\\""))
                    {
                        //Not an escape so ends the literal
                        LastCompletion = AutoCompleteState.Literal;
                        _editor.EndSuggestion();
                    }
                }
                else
                {
                    //Is this an escaped "?
                    if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\\""))
                    {
                        //Not escaped so terminates the literal
                        LastCompletion = State;
                        _editor.EndSuggestion();
                    }
                }
            }
            else if (CurrentText.Length == 3)
            {
                char last = CurrentText[CurrentText.Length - 1];
                if (char.IsWhiteSpace(last) || char.IsPunctuation(last))
                {
                    //White Space/Punctuation means we've left the empty literal
                    LastCompletion = AutoCompleteState.Literal;
                    _editor.EndSuggestion();
                }
            }
        }

        /// <summary>
        /// Try to complete a URI
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryUriCompletion(string newText)
        {
            if (newText == ">")
            {
                if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\>"))
                {
                    //End of a URI so exit auto-complete
                    LastCompletion = AutoCompleteState.Uri;
                    _editor.EndSuggestion();
                }
            }
        }

        /// <summary>
        /// Try to complete a BNode
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryBNodeCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                LastCompletion = AutoCompleteState.BNode;
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c != '_' && c != '-' && c != ':'))
            {
                LastCompletion = AutoCompleteState.BNode;
                _editor.EndSuggestion();
                DetectBlankNodes();
                return;
            }

            if (!IsValidPartialBlankNodeID(CurrentText.ToString()))
            {
                //Not a BNode ID so close the window
                _editor.EndSuggestion();
                DetectBlankNodes();
            }
        }

        /// <summary>
        /// Try to complete a QName
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryQNameCompletion(string newText)
        {
            if (IsValidPartialKeyword(CurrentText))
            {
                //Can switch back to Keyword/QName mode if user backtracks
                State = AutoCompleteState.KeywordOrQName;
                _editor.Suggest(_keywords.Where(d => d is KeywordData));
                TryKeywordOrQNameCompletion(newText);
                return;
            }

            if (IsNewLine(newText) || !IsValidPartialQName(CurrentText))
            {
                //Not a QName so close the window
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }
        }

        /// <summary>
        /// Try to complete a Keyword or QName
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryKeywordOrQNameCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                LastCompletion = State;
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c != '_' && c != '-' && c != ':'))
            {
                _editor.EndSuggestion();
                return;
            }

            if (!IsValidPartialKeyword(CurrentText.ToString()) && !IsValidPartialQName(CurrentText.ToString()))
            {
                //Not a keyword/Qname so close the window
                _editor.EndSuggestion();
            }
            else if (!IsValidPartialKeyword(CurrentText.ToString()))
            {
                //No longer a possible keyword
                State = AutoCompleteState.QName;
            }
        }

        /// <summary>
        /// Try to complete a declaration
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryDeclarationCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                LastCompletion = AutoCompleteState.Declaration;
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
            {
                LastCompletion = AutoCompleteState.Declaration;
                _editor.EndSuggestion();
            }

            int testLength = Math.Min(PrefixDeclaration.Length, CurrentText.Length);
            if (PrefixDeclaration.Substring(0, testLength).Equals(CurrentText.Substring(0, testLength)))
            {
                State = AutoCompleteState.Prefix;
                return;
            }
            testLength = Math.Min(BaseDeclaration.Length, CurrentText.Length);
            if (BaseDeclaration.Substring(0, testLength).Equals(CurrentText.Substring(0, testLength)))
            {
                State = AutoCompleteState.Base;
            }
        }

        /// <summary>
        /// Try to complete a prefix declaration
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryPrefixCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                LastCompletion = AutoCompleteState.Prefix;
                _editor.EndSuggestion();
                DetectNamespaces();
            }

            if (newText.Equals("."))
            {
                string testText = _editor.GetText(StartOffset - 1, Length + 1);
                if (Regex.IsMatch(testText, PrefixRegexPattern))
                {
                    LastCompletion = AutoCompleteState.Declaration;
                    _editor.EndSuggestion();
                    DetectNamespaces();
                    return;
                }
            }

            int testLength = Math.Min(PrefixDeclaration.Length, CurrentText.Length);
            if (!PrefixDeclaration.Substring(0, testLength).Equals(CurrentText.Substring(0, testLength)))
            {
                //Not a prefix declaration so close the window
                _editor.EndSuggestion();
                DetectNamespaces();
            }
        }

        /// <summary>
        /// Try to complete a base declaration
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryBaseCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
            {
                _editor.EndSuggestion();
            }

            int testLength = Math.Min(BaseDeclaration.Length, CurrentText.Length);
            if (!BaseDeclaration.Substring(0, testLength).Equals(CurrentText.Substring(0, testLength)))
            {
                //Not a base declaration so close the window
                _editor.EndSuggestion();
            }
        }

        /// <summary>
        /// Try to complete a comment
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryCommentCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                LastCompletion = AutoCompleteState.Comment;
                _editor.EndSuggestion();
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Is something a valid partial keyword?
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        protected virtual bool IsValidPartialKeyword(string value)
        {
            foreach (KeywordData keyword in _keywords.OfType<KeywordData>())
            {
                int testLength = Math.Min(keyword.InsertionText.Length, value.Length);
                if (testLength > keyword.InsertionText.Length) continue;
                if (keyword.InsertionText.Substring(0, testLength).Equals(value.Substring(0, testLength))) return true;
            }
            return false;
        }

        /// <summary>
        /// Is something a valid partial QName?
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        protected virtual bool IsValidPartialQName(string value)
        {
            string ns, localname;
            if (value.Contains(':'))
            {
                ns = value.Substring(0, value.IndexOf(':'));
                localname = value.Substring(value.IndexOf(':') + 1);
            }
            else
            {
                ns = value;
                localname = string.Empty;
            }

            //Namespace Validation
            if (!ns.Equals(string.Empty))
            {
                //Allowed empty Namespace
                if (ns.StartsWith("-"))
                {
                    //Can't start with a -
                    return false;
                }
                else
                {
                    char[] nchars = ns.ToCharArray();
                    if (XmlSpecsHelper.IsNameStartChar(nchars[0]) && nchars[0] != '_')
                    {
                        if (nchars.Length > 1)
                        {
                            for (int i = 1; i < nchars.Length; i++)
                            {
                                //Not a valid Name Char
                                if (!XmlSpecsHelper.IsNameChar(nchars[i])) return false;
                                if (nchars[i] == '.') return false;
                            }
                            //If we reach here the Namespace is OK
                        }
                        else
                        {
                            //Only 1 Character which was valid so OK
                        }
                    }
                    else
                    {
                        //Doesn't start with a valid Name Start Char
                        return false;
                    }
                }
            }

            //Local Name Validation
            if (!localname.Equals(string.Empty))
            {
                //Allowed empty Local Name
                char[] lchars = localname.ToCharArray();

                if (XmlSpecsHelper.IsNameStartChar(lchars[0]))
                {
                    if (lchars.Length > 1)
                    {
                        for (int i = 1; i < lchars.Length; i++)
                        {
                            //Not a valid Name Char
                            if (!XmlSpecsHelper.IsNameChar(lchars[i])) return false;
                            if (lchars[i] == '.') return false;
                        }
                        //If we reach here the Local Name is OK
                    }
                    else
                    {
                        //Only 1 Character which was valid so OK
                    }
                }
                else
                {
                    //Not a valid Name Start Char
                    return false;
                }
            }

            //If we reach here then it's all valid
            return true;
        }

        /// <summary>
        /// Is something a valid partial Blank Node ID?
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        protected virtual bool IsValidPartialBlankNodeID(string value)
        {
            if (value.Equals(string.Empty))
            {
                //Can't be empty
                return false;
            }
            else if (value.Equals("_:"))
            {
                return true;
            }
            else if (value.Length > 2 && value.StartsWith("_:"))
            {
                value = value.Substring(2);
                char[] cs = value.ToCharArray();
                if (char.IsDigit(cs[0]) || cs[0] == '-' || cs[0] == '_')
                {
                    //Can't start with a Digit, Hyphen or Underscore
                    return false;
                }
                else
                {
                    //Otherwise OK
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Is something a new line
        /// </summary>
        /// <param name="text">Value</param>
        /// <returns></returns>
        protected bool IsNewLine(string text)
        {
            return text.Equals("\n") || text.Equals("\r") || text.Equals("\r\n") || text.Equals("\n\r");
        }

        /// <summary>
        /// Get QName completion data
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ICompletionData> GetQNameCompletionData()
        {
            //Set Current Offset to scope the Namespace Mapper properly
            _nsmap.CurrentOffset = StartOffset;

            //Generate all available QNames
            List<ICompletionData> qnames = new List<ICompletionData>();
            foreach (string prefix in _nsmap.Prefixes)
            {
                string nsUri = _nsmap.GetNamespaceUri(prefix).AbsoluteUri;
                if (_namespaceTerms.ContainsKey(nsUri))
                {
                    foreach (NamespaceTerm term in _namespaceTerms[nsUri])
                    {
                        if (term.Label.Equals(string.Empty))
                        {
                            qnames.Add(new QNameData(prefix + ":" + term.Term, "QName for the URI " + nsUri + term.Term));
                        }
                        else
                        {
                            qnames.Add(new QNameData(prefix + ":" + term.Term, term.Label));
                        }
                    }
                }
            }
            qnames.Sort();
            return qnames;
        }

        /// <summary>
        /// Backtrack the start offset while a function is true
        /// </summary>
        /// <param name="testFunc">Test function</param>
        protected void BacktrackStartOffset(Func<string, bool> testFunc)
        {
            int offset = StartOffset - 1;
            int backtracked = 1;
            if (offset >= 0)
            {
                string test = _editor.GetText(offset, Length + backtracked);
                while (testFunc(test))
                {
                    if (offset > 0)
                    {
                        offset--;
                        backtracked++;
                    }
                    else
                    {
                        break;
                    }
                    test = _editor.GetText(offset, Length + backtracked);
                }
                offset++;
                if (offset != StartOffset)
                {
                    StartOffset = offset;
                }
            }
        }

        #endregion
    }
}
