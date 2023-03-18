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

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// Auto-completer for NTriples
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public class NTriplesAutoCompleter<T>
        : BaseAutoCompleter<T>
    {
        /// <summary>
        /// Regular Expression Pattern for valid blank node identifiers
        /// </summary>
        protected string BlankNodePattern = @"_:\p{L}(\p{L}|\p{N}|-|_)*";

        private HashSet<ICompletionData> _bnodes = new HashSet<ICompletionData>();
        private BlankNodeMapper _bnodemap = new BlankNodeMapper();

        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public NTriplesAutoCompleter(ITextEditorAdaptor<T> editor)
            : base(editor) { }

        #region State Detection

        /// <summary>
        /// Detects the auto-complete state
        /// </summary>
        protected override void DetectStateInternal()
        {
            //Look for Blank Nodes
            DetectBlankNodes();
        }

        /// <summary>
        /// Detect declared blank nodes
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

        #endregion

        #region Start Auto-completion

        /// <summary>
        /// Start literal completion
        /// </summary>
        /// <param name="newText">New Text</param>
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
        /// <param name="newText">New Text</param>
        protected virtual void StartCommentCompletion(string newText)
        {
            State = AutoCompleteState.Comment;
        }

        /// <summary>
        /// Start URI completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartUriCompletion(string newText)
        {
            State = AutoCompleteState.Uri;
        }

        /// <summary>
        /// Start BNode completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartBNodeCompletion(string newText)
        {
            State = AutoCompleteState.BNode;
            _editor.Suggest(new NewBlankNodeData(_bnodemap.GetNextID()).AsEnumerable<ICompletionData>().Concat(_bnodes));
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
                if (newText.Length == 1)
                {
                    char c = newText[0];
                    if (c == '_')
                    {
                        StartBNodeCompletion(newText);
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
                    if (Length < 0)
                    {
                        _editor.EndSuggestion();
                        State = AutoCompleteState.None;
                        TryAutoComplete(newText);
                        return;
                    }

                    if (newText.Length > 0)
                    {
                        switch (State)
                        {
                            case AutoCompleteState.BNode:
                                TryBNodeCompletion(newText);
                                break;

                            case AutoCompleteState.Uri:
                                TryUriCompletion(newText);
                                break;

                            case AutoCompleteState.Literal:
                                TryLiteralCompletion(newText);
                                break;

                            case AutoCompleteState.Comment:
                                TryCommentCompletion(newText);
                                break;

                            default:
                                //Nothing to do as no other auto-completion is implemented yet
                                break;
                        }
                    }
                }
                catch
                {
                    //If any kind of error occurs abort auto-completion
                    State = AutoCompleteState.None;
                    _editor.EndSuggestion();
                }
            }
        }

        /// <summary>
        /// Try literal completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryLiteralCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }

            if (newText == "\"")
            {
                if (CurrentText.Length == 2)
                {
                    //Possibly end of the Literal, have to wait and see
                }
                else
                {
                    //Is this an escaped "?
                    if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\\""))
                    {
                        //Not escaped so terminates the literal
                        LastCompletion = AutoCompleteState.Literal;
                        _editor.EndSuggestion();
                    }
                }
            }
            else if (CurrentText.Length == 3)
            {
                char last = CurrentText[CurrentText.Length - 1];
                if (char.IsWhiteSpace(last) || char.IsPunctuation(last))
                {
                    LastCompletion = AutoCompleteState.Literal;
                    _editor.EndSuggestion();
                }
            }
        }

        /// <summary>
        /// Try URI completion
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
        /// Try Blank Node completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryBNodeCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c != '_' && c != '-' && c != ':'))
            {
                LastCompletion = AutoCompleteState.BNode;
                DetectBlankNodes();
                _editor.EndSuggestion();
                return;
            }

            if (!IsValidPartialBlankNodeID(CurrentText.ToString()))
            {
                //Not a BNode ID so close the window
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
                DetectBlankNodes();
            }
        }

        /// <summary>
        /// Try comment completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryCommentCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Is something a valid partial blank node ID?
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
        /// Is something a new line?
        /// </summary>
        /// <param name="text">Value</param>
        /// <returns></returns>
        protected bool IsNewLine(string text)
        {
            return text.Equals("\n") || text.Equals("\r") || text.Equals("\r\n") || text.Equals("\n\r");
        }

        #endregion
    }
}
