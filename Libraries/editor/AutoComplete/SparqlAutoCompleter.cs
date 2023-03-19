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
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Utilities.Editor.AutoComplete.Data;
using VDS.RDF.Utilities.Editor.AutoComplete.Vocabularies;

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// Auto-completer implementation for SPARQL
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SparqlAutoCompleter<T>
        : TurtleAutoCompleter<T>
    {
        private SparqlQuerySyntax _syntax;
        private HashSet<ICompletionData> _vars = new HashSet<ICompletionData>();

        /// <summary>
        /// Regular expression for Variables
        /// </summary>
        protected string VariableRegexPattern = @"[?$](_|\p{L}|\d)(_|-|\p{L}|\p{N})*";

        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public SparqlAutoCompleter(ITextEditorAdaptor<T> editor)
            : this(editor, SparqlQuerySyntax.Sparql_1_1) { }

        /// <summary>
        /// Creates a new auto-complete for the specific SPARQL syntax
        /// </summary>
        /// <param name="editor">Text Editor</param>
        /// <param name="syntax">SPARQL Syntax</param>
        public SparqlAutoCompleter(ITextEditorAdaptor<T> editor, SparqlQuerySyntax? syntax)
            : base(editor)
        {
            //Alter the Regex patterns
            PrefixRegexPattern = PrefixRegexPattern.Substring(1, PrefixRegexPattern.Length-6);
            BlankNodePattern = @"_:(\p{L}|\d)(\p{L}|\p{N}|-|_)*";

            //Add Prefix Definitions to Keywords
            _keywords.Add(new SparqlStyleBaseDeclarationData());
            _keywords.Add(new SparqlStyleDefaultPrefixDeclarationData());
            foreach (VocabularyDefinition vocab in AutoCompleteManager.Vocabularies)
            {
                _keywords.Add(new SparqlStylePrefixDeclarationData(vocab.Prefix, vocab.NamespaceUri));
            }

            //If not Query Syntax don't add any Query Keywords
            if (syntax == null) return;

            //Add Keywords relevant to the Syntax
            _syntax = (SparqlQuerySyntax)syntax;
            foreach (string keyword in SparqlSpecsHelper.SparqlQuery10Keywords)
            {
                _keywords.Add(new KeywordData(keyword));
                _keywords.Add(new KeywordData(keyword.ToLower()));
            }

            if (syntax != SparqlQuerySyntax.Sparql_1_0)
            {
                foreach (string keyword in SparqlSpecsHelper.SparqlQuery11Keywords)
                {
                    _keywords.Add(new KeywordData(keyword));
                    _keywords.Add(new KeywordData(keyword.ToLower()));
                }
            }

            //Sort the Keywords
            _keywords.Sort();
        }

        /// <summary>
        /// Indicates that new terms cannot be declared
        /// </summary>
        protected override bool CanDeclareNewTerms
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Detects the auto-complete state
        /// </summary>
        protected override void DetectStateInternal()
        {
            base.DetectStateInternal();
            DetectVariables();
        }

        /// <summary>
        /// Detects declared variables
        /// </summary>
        protected virtual void DetectVariables()
        {
            _vars.Clear();
            foreach (Match m in Regex.Matches(_editor.Text, VariableRegexPattern))
            {
                _vars.Add(new VariableData(m.Value));
            }
        }

        /// <summary>
        /// Starts declaration completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected override void StartDeclarationCompletion(string newText)
        {
            //We don't start declarations like Turtle does so don't do anything here
            return;
        }

        /// <summary>
        /// Start alternate literal completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartAlternateLiteralCompletion(string newText)
        {
            if (TemporaryState == AutoCompleteState.AlternateLiteral || TemporaryState == AutoCompleteState.AlternateLongLiteral)
            {
                TemporaryState = AutoCompleteState.None;
                State = AutoCompleteState.None;
            }
            else
            {
                State = AutoCompleteState.AlternateLiteral;
            }
        }

        /// <summary>
        /// Start variable completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void StartVariableCompletion(string newText)
        {
            State = AutoCompleteState.Variable;
            _editor.Suggest(_vars);
        }

        /// <summary>
        /// Try to auto-complete
        /// </summary>
        /// <param name="newText">New Text</param>
        public override void TryAutoComplete(string newText)
        {
            base.TryAutoComplete(newText);

            //Don't do anything if auto-complete not currently active
            if (State == AutoCompleteState.Disabled || State == AutoCompleteState.Inserted) return;

            if (State == AutoCompleteState.None)
            {
                if (newText.Length == 1)
                {
                    char c = newText[0];
                    if (char.IsLetter(c))
                    {
                        StartKeywordOrQNameCompletion(newText);
                    }
                    else
                    {
                        switch (c)
                        {
                            case '_':
                                StartBNodeCompletion(newText);
                                break;
                            case ':':
                                StartQNameCompletion(newText);
                                break;
                            case '<':
                                StartUriCompletion(newText);
                                break;
                            case '#':
                                StartCommentCompletion(newText);
                                break;
                            case '"':
                                StartLiteralCompletion(newText);
                                break;
                            case '\'':
                                StartAlternateLiteralCompletion(newText);
                                break;
                            case '?':
                            case '$':
                                StartVariableCompletion(newText);
                                break;
                            case '.':
                            case ',':
                            case ';':
                                State = AutoCompleteState.None;
                                break;
                        }
                    }
                }

                if (State == AutoCompleteState.None || State == AutoCompleteState.Disabled) return;
            } 

            //If not currently auto-completing then do nothing - if the call to base.TryAutoComplete() couldn't
            //do anything and our start code didn't do anything we aren't doing auto-completion
            if (State == AutoCompleteState.None) return;

            try
            {

                //If Length is less than zero then user has moved the caret so we'll abort our completion and start a new one
                if (Length < 0)
                {
                    _editor.EndSuggestion();
                    TryAutoComplete(newText);
                    return;
                }

                if (newText.Length > 0)
                {
                    switch (State)
                    {
                        case AutoCompleteState.AlternateLiteral:
                            TryAlternateLiteralCompletion(newText);
                            break;

                        case AutoCompleteState.AlternateLongLiteral:
                            TryAlternateLongLiteralCompletion(newText);
                            break;

                        case AutoCompleteState.Variable:
                            TryVariableCompletion(newText);
                            break;

                        default:
                            //No other auto-completion supported
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

        /// <summary>
        /// Try alternate long literal completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryAlternateLongLiteralCompletion(string newText)
        {
            if (newText == "'")
            {
                //Is this an escaped '?
                if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\'"))
                {
                    //Not escaped so terminate the literal if the buffer ends in 3 ' and the length is >= 6
                    if (CurrentText.Length >= 6 && CurrentText.Substring(CurrentText.Length - 3, 3).Equals("'''"))
                    {
                        LastCompletion = AutoCompleteState.LongLiteral;
                        _editor.EndSuggestion();
                    }
                }
            }
        }

        /// <summary>
        /// Try alternate literal completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryAlternateLiteralCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }

            if (newText == "'")
            {
                if (CurrentText.Length == 2)
                {
                    //Might be a long literal so have to wait and see
                }
                else if (CurrentText.Length == 3)
                {
                    char last = CurrentText[CurrentText.Length - 1];
                    if (CurrentText.ToString().Equals("'''"))
                    {
                        //Switch to long literal mode
                        State = AutoCompleteState.AlternateLongLiteral;
                    }
                    else if (char.IsWhiteSpace(last) || char.IsPunctuation(last))
                    {
                        //White Space/Punctuation means we've left the empty literal
                        LastCompletion = AutoCompleteState.AlternateLiteral;
                        _editor.EndSuggestion();
                    }
                    else if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\'"))
                    {
                        //Not an escape so ends the literal
                        LastCompletion = AutoCompleteState.AlternateLiteral;
                        _editor.EndSuggestion();
                    }
                }
                else
                {
                    //Is this an escaped '?
                    if (!CurrentText.Substring(CurrentText.Length - 2, 2).Equals("\\'"))
                    {
                        //Not escaped so terminates the literal
                        LastCompletion = AutoCompleteState.AlternateLiteral;
                        _editor.EndSuggestion();
                    }
                }
            }
        }

        /// <summary>
        /// Try variable completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected virtual void TryVariableCompletion(string newText)
        {
            if (IsNewLine(newText))
            {
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
            }

            char c = newText[0];
            if (Length > 1)
            {
                if (char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c != '_' && c != '-'))
                {
                    LastCompletion = AutoCompleteState.Variable;
                    _editor.EndSuggestion();
                    DetectVariables();
                    return;
                }
            }

            if (!IsValidPartialVariableName(CurrentText.ToString()))
            {
                //Not a Variable so close the window
                State = AutoCompleteState.None;
                _editor.EndSuggestion();
                DetectVariables();
            }
        }

        /// <summary>
        /// Try declaration completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected override void TryDeclarationCompletion(string newText)
        {
            //We don't do declarations like Turtle does so don't do anything here
            return;
        }

        /// <summary>
        /// Try prefix completion
        /// </summary>
        /// <param name="newText">New Text</param>
        protected override void TryPrefixCompletion(string newText)
        {
            //We don't do Prefix declarations like Turtle does so don't do anything here
            return;
        }

        /// <summary>
        /// Is something a valid partial QName?
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        protected override bool IsValidPartialQName(string value)
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

                if (XmlSpecsHelper.IsNameStartChar(lchars[0]) || char.IsNumber(lchars[0]))
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
        /// Is something a valid partial variable name?
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns></returns>
        protected virtual bool IsValidPartialVariableName(string value)
        {
            if (value.Length == 0) return false;

            if (value[0] != '$' && value[0] != '?') return false;

            if (value.Length == 1) return true;

            char[] cs = value.ToCharArray(1, value.Length - 1);

            //First Character must be from PN_CHARS_U or a digit
            char first = cs[0];
            if (char.IsDigit(first) || SparqlSpecsHelper.IsPNCharsU(first))
            {
                if (cs.Length > 1)
                {
                    for (int i = 1; i < cs.Length; i++)
                    {
                        //Middle Chars must be from PN_CHARS or a '.'
                        if (!(cs[i] == '.' || SparqlSpecsHelper.IsPNChars(cs[i])))
                        {
                            return false;
                        }
                        //Can't do the last character specific test because this is only a partial test
                    }

                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Auto-completer implementation for SPARQL 1.1
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public class Sparql11AutoCompleter<T>
        : SparqlAutoCompleter<T>
    {
        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public Sparql11AutoCompleter(ITextEditorAdaptor<T> editor)
            : base(editor, SparqlQuerySyntax.Sparql_1_1) { }
    }

    /// <summary>
    /// Auto-completer implementation for SPARQL 1.0
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public class Sparql10AutoCompleter<T>
        : SparqlAutoCompleter<T>
    {
        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public Sparql10AutoCompleter(ITextEditorAdaptor<T> editor)
            : base(editor, SparqlQuerySyntax.Sparql_1_0) { }
    }


}
