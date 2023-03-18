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
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// Abstract base implementation of an auto-completer
    /// </summary>
    /// <typeparam name="T">Control Type</typeparam>
    public abstract class BaseAutoCompleter<T> 
        : IAutoCompleter<T>
    {
        private AutoCompleteState _state = AutoCompleteState.None;
        private AutoCompleteState _lastCompletion = AutoCompleteState.None;
        private AutoCompleteState _temp = AutoCompleteState.None;

        /// <summary>
        /// Text Editor
        /// </summary>
        protected ITextEditorAdaptor<T> _editor;
        private int _startOffset = 0;

        /// <summary>
        /// Creates a new auto-completer
        /// </summary>
        /// <param name="editor">Text Editor</param>
        public BaseAutoCompleter(ITextEditorAdaptor<T> editor)
        {
            _editor = editor;
        }

        #region Current Text Management

        /// <summary>
        /// Gets/Sets the start offset
        /// </summary>
        public int StartOffset
        {
            get
            {
                return _startOffset;
            }
            set
            {
                _startOffset = value;
            }
        }

        /// <summary>
        /// Gets/Sets the length
        /// </summary>
        public int Length
        {
            get
            {
                return _editor.CaretOffset - StartOffset;
            }
        }

        /// <summary>
        /// Gets the current text
        /// </summary>
        public string CurrentText
        {
            get
            {
                if (_editor == null)
                {
                    return string.Empty;
                }
                else
                {
                    return _editor.GetText(_startOffset, Length);
                }
            }
        }

        #endregion

        /// <summary>
        /// Detect the state of the auto-completer
        /// </summary>
        public void DetectState()
        {
            //Don't do anything if currently disabled
            if (State == AutoCompleteState.Disabled) return;

            //Then call the derived classes internal state detection (if any)
            DetectStateInternal();

            //TODO: Want to call some method in ITextEditorAdaptor which tells it what syntax element we are currently in
        }

        /// <summary>
        /// Method which derived classes should override to add their state detection logic
        /// </summary>
        protected virtual void DetectStateInternal()
        { }

        /// <summary>
        /// Try to auto-complete the given text
        /// </summary>
        /// <param name="newText">Text</param>
        public abstract void TryAutoComplete(string newText);

        /// <summary>
        /// Gets/Sets the auto-complete state
        /// </summary>
        public AutoCompleteState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        /// <summary>
        /// Gets/Sets the last completion state
        /// </summary>
        public AutoCompleteState LastCompletion
        {
            get
            {
                return _lastCompletion;
            }
            set
            {
                _lastCompletion = value;
            }
        }

        /// <summary>
        /// Gets/Sets the temporary state
        /// </summary>
        protected AutoCompleteState TemporaryState
        {
            get
            {
                return _temp;
            }
            set
            {
                _temp = value;
            }
        }
    }
}
