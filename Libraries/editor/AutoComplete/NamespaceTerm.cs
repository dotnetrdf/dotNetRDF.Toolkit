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

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// Represents information about a term defined in some namespace
    /// </summary>
    public class NamespaceTerm
    {
        /// <summary>
        /// Creates a new namespace term
        /// </summary>
        /// <param name="namespaceUri">Namespace URI</param>
        /// <param name="term">Term</param>
        public NamespaceTerm(string namespaceUri, string term)
            : this(namespaceUri, term, string.Empty) { }

        /// <summary>
        /// Creates a new namespace term
        /// </summary>
        /// <param name="namespaceUri">Namespace URI</param>
        /// <param name="term">Term</param>
        /// <param name="label">Label</param>
        public NamespaceTerm(string namespaceUri, string term, string label)
        {
            NamespaceUri = namespaceUri;
            Term = term;
            Label = label;
        }

        /// <summary>
        /// Gets the Namespace URI
        /// </summary>
        public string NamespaceUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the term
        /// </summary>
        public string Term
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets/Sets the label
        /// </summary>
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the URI string for the term
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return NamespaceUri + Term;
        }

        /// <summary>
        /// Gets the hash code for the term
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Determines whether a term is equal to some other object
        /// </summary>
        /// <param name="obj">Other Object</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is NamespaceTerm)
            {
                NamespaceTerm other = (NamespaceTerm)obj;
                return ToString().Equals(other.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }
    }
}
