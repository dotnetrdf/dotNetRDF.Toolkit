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
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace VDS.RDF.Utilities.Editor.Wpf.Syntax
{
    /// <summary>
    /// Fake text run context needed for some operations
    /// </summary>
    public class FakeTextRunContext
        : ITextRunConstructionContext
    {
        /// <summary>
        /// Creates a new fake context
        /// </summary>
        public FakeTextRunContext() { }

        /// <summary>
        /// Gets the document
        /// </summary>
        public ICSharpCode.AvalonEdit.Document.TextDocument Document
        {
            get 
            { 
                return null; 
            }
        }

        /// <summary>
        /// Gets the properties
        /// </summary>
        public TextRunProperties GlobalTextRunProperties
        {
            get 
            { 
                return null; 
            }
        }

        /// <summary>
        /// Gets the text view
        /// </summary>
        public TextView TextView
        {
            get 
            { 
                return null;
            }
        }

        /// <summary>
        /// Gets the visual line
        /// </summary>
        public VisualLine VisualLine
        {
            get 
            { 
                return null;
            }
        }

        /// <summary>
        /// Gets the text
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <returns>Text</returns>
        public StringSegment GetText(int offset, int length)
        {
            return new StringSegment();
        }
    }
}
