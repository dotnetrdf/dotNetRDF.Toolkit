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

namespace VDS.RDF.Utilities.Editor.AutoComplete.Data
{
    /// <summary>
    /// Abstract base implementation of auto-complete data
    /// </summary>
    public abstract class BaseCompletionData
        : ICompletionData
    {
        private const double DefaultPriority = 1.0d;

        private string _display, _insert, _descrip;
        private double _priority = DefaultPriority;

        /// <summary>
        /// Creates new completion data
        /// </summary>
        /// <param name="displayText">Display Text</param>
        /// <param name="insertText">Insertion Text</param>
        public BaseCompletionData(string displayText, string insertText)
            : this(displayText, insertText, string.Empty, DefaultPriority) { }

        /// <summary>
        /// Creates new completion data
        /// </summary>
        /// <param name="displayText">Display Text</param>
        /// <param name="insertText">Insertion Text</param>
        /// <param name="description">Description</param>
        public BaseCompletionData(string displayText, string insertText, string description)
            : this(displayText, insertText, description, DefaultPriority) { }

        /// <summary>
        /// Creates new completion data
        /// </summary>
        /// <param name="displayText">Display Text</param>
        /// <param name="insertText">Insertion Text</param>
        /// <param name="description">Description</param>
        /// <param name="priority">Priority</param>
        public BaseCompletionData(string displayText, string insertText, string description, double priority)
        {
            _display = displayText;
            _insert = insertText;
            _descrip = description;
            _priority = priority;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description
        {
            get
            {
                return _descrip;
            }
        }

        /// <summary>
        /// Gets the priority
        /// </summary>
        public double Priority
        {
            get
            {
                return _priority;
            }
        }

        /// <summary>
        /// Gets the display text
        /// </summary>
        public string DisplayText
        {
            get
            {
                return _display;
            }
        }

        /// <summary>
        /// Gets the insertion text
        /// </summary>
        public string InsertionText
        {
            get
            {
                return _insert;
            }
        }

        /// <summary>
        /// Sort relative to other completion data
        /// </summary>
        /// <param name="other">Other data</param>
        /// <returns></returns>
        public int CompareTo(ICompletionData other)
        {
            int c = Priority.CompareTo(other.Priority);
            if (c == 0)
            {
                return InsertionText.CompareTo(other.InsertionText);
            }
            else
            {
                return c;
            }
        }

        /// <summary>
        /// Sort relative to other object
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (obj is ICompletionData)
            {
                return CompareTo((ICompletionData)obj);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Equality to other object
        /// </summary>
        /// <param name="obj">Other Object</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is ICompletionData)
            {
                return Equals((ICompletionData)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Equality to other completion data
        /// </summary>
        /// <param name="other">Other Data</param>
        /// <returns></returns>
        public bool Equals(ICompletionData other)
        {
            return GetHashCode().Equals(other.GetHashCode());
        }

        /// <summary>
        /// Hash Code of the data
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// String representation of the data
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetType().Name + ": " + InsertionText;
        }
    }
}
