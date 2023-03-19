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
using VDS.RDF;

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// A namespace map implementation where namespaces may be scoped by file offset
    /// </summary>
    public class OffsetScopedNamespaceMapper 
        : INamespaceMapper
    {
        private Dictionary<string, List<OffsetMapping>> _uris = new Dictionary<string, List<OffsetMapping>>();
        private Dictionary<int, List<OffsetMapping>> _prefixes = new Dictionary<int, List<OffsetMapping>>();
        private int _offset = 0;

        /// <summary>
        /// Constructs a new Namespace Map
        /// </summary>
        /// <remarks>The Prefixes rdf, rdfs and xsd are automatically defined</remarks>
        public OffsetScopedNamespaceMapper()
            : this(false) { }

        /// <summary>
        /// Constructs a new Namespace Map which is optionally empty
        /// </summary>
        /// <param name="empty">Whether the Namespace Map should be empty, if set to false the Prefixes rdf, rdfs and xsd are automatically defined</param>
        public OffsetScopedNamespaceMapper(bool empty)
        {
            if (!empty)
            {
                //Add Standard Namespaces
                AddNamespace("rdf", new Uri(NamespaceMapper.RDF));
                AddNamespace("rdfs", new Uri(NamespaceMapper.RDFS));
                AddNamespace("xsd", new Uri(NamespaceMapper.XMLSCHEMA));
            }
        }

        /// <summary>
        /// Adds a namespace
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="uri">URI</param>
        public void AddNamespace(string prefix, Uri uri)
        {
            OffsetMapping mapping = new OffsetMapping(prefix, uri, _offset);
            if (!_prefixes.ContainsKey(uri.GetEnhancedHashCode())) _prefixes.Add(uri.GetEnhancedHashCode(), new List<OffsetMapping>());

            if (_uris.ContainsKey(prefix))
            {
                //Is it defined at the current offset level?
                if (_uris[prefix].Any(m => m.Offset == _offset))
                {
                    //If it is then we override it
                    _uris[prefix].RemoveAll(m => m.Offset == _offset);
                    _prefixes[uri.GetEnhancedHashCode()].RemoveAll(m => m.Offset == _offset);

                    _uris[prefix].Add(mapping);
                    _prefixes[uri.GetEnhancedHashCode()].Add(mapping);
                    OnNamespaceModified(prefix, uri);
                }
                else
                {
                    //If not we simply add it
                    _uris[prefix].Add(mapping);
                    _prefixes[uri.GetEnhancedHashCode()].Add(mapping);
                    OnNamespaceAdded(prefix, uri);
                }
            }
            else
            {
                //Not yet defined so add it
                _uris.Add(prefix, new List<OffsetMapping>());
                _uris[prefix].Add(mapping);
                _prefixes[uri.GetEnhancedHashCode()].Add(mapping);
                OnNamespaceAdded(prefix, uri);
            }
        }

        /// <summary>
        /// Clears namespaces
        /// </summary>
        public void Clear()
        {
            _uris.Clear();
            _prefixes.Clear();
        }

        /// <summary>
        /// Gets a namespace
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <returns></returns>
        public Uri GetNamespaceUri(string prefix)
        {
            if (_uris.ContainsKey(prefix))
            {
                if (_uris[prefix].Any(m => m.Offset < _offset))
                {
                    return _uris[prefix].Last(m => m.Offset < _offset).Uri;
                }
                else
                {
                    throw new RdfException("The Namespace URI for the given Prefix '" + prefix + "' is not in-scope at the current Offset");
                }
            }
            else
            {
                throw new RdfException("The Namespace URI for the given Prefix '" + prefix + "' is not known by the in-scope NamespaceMapper");
            }
        }

        /// <summary>
        /// Gets a prefix
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns></returns>
        public string GetPrefix(Uri uri)
        {
            int hash = uri.GetEnhancedHashCode();
            if (_prefixes.ContainsKey(hash))
            {
                if (_prefixes[hash].Any(m => m.Offset < _offset))
                {
                    return _prefixes[hash].Last(m => m.Offset < _offset).Prefix;
                }
                else
                {
                    throw new RdfException("The Prefix for the given URI '" + uri.ToString() + "' is not in-scope at the current Offset");
                }
            }
            else
            {
                throw new RdfException("The Prefix for the given URI '" + uri.ToString() + "' is not known by the in-scope NamespaceMapper");
            }
        }

        /// <summary>
        /// Gets whether a given namespace prefix is declared
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <returns></returns>
        public bool HasNamespace(string prefix)
        {
            if (_uris.ContainsKey(prefix))
            {
                return _uris[prefix].Any(m => m.Offset < _offset);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Imports another namespace map
        /// </summary>
        /// <param name="nsmap"></param>
        public void Import(INamespaceMapper nsmap)
        {
            string tempPrefix = "ns0";
            int tempPrefixID = 0;
            foreach (string prefix in nsmap.Prefixes)
            {
                if (!_uris.ContainsKey(prefix))
                {
                    //Non-colliding Namespaces get copied across
                    AddNamespace(prefix, nsmap.GetNamespaceUri(prefix));
                }
                else
                {
                    //Colliding Namespaces get remapped to new prefixes
                    //Assuming the prefixes aren't already used for the same Uri
                    if (!GetNamespaceUri(prefix).AbsoluteUri.Equals(nsmap.GetNamespaceUri(prefix).AbsoluteUri, StringComparison.Ordinal))
                    {
                        while (_uris.ContainsKey(tempPrefix))
                        {
                            tempPrefixID++;
                            tempPrefix = "ns" + tempPrefixID;
                        }
                        AddNamespace(tempPrefix, nsmap.GetNamespaceUri(prefix));
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the Current Offset
        /// </summary>
        public int CurrentOffset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }

        /// <summary>
        /// Event which is raised when a namespace is added
        /// </summary>
        public event NamespaceChanged NamespaceAdded;

        /// <summary>
        /// Event which is raised when a namespace is changed
        /// </summary>
        public event NamespaceChanged NamespaceModified;

        /// <summary>
        /// Event which is raised when a namespace is removed
        /// </summary>
        public event NamespaceChanged NamespaceRemoved;

        /// <summary>
        /// Internal Helper for the NamespaceAdded Event which raises it only when a Handler is registered
        /// </summary>
        /// <param name="prefix">Namespace Prefix</param>
        /// <param name="uri">Namespace Uri</param>
        protected virtual void OnNamespaceAdded(string prefix, Uri uri)
        {
            NamespaceChanged handler = NamespaceAdded;
            if (handler != null)
            {
                handler(prefix, uri);
            }
        }

        /// <summary>
        /// Internal Helper for the NamespaceModified Event which raises it only when a Handler is registered
        /// </summary>
        /// <param name="prefix">Namespace Prefix</param>
        /// <param name="uri">Namespace Uri</param>
        protected virtual void OnNamespaceModified(string prefix, Uri uri)
        {
            NamespaceChanged handler = NamespaceModified;
            if (handler != null)
            {
                handler(prefix, uri);
            }
        }

        /// <summary>
        /// Internal Helper for the NamespaceRemoved Event which raises it only when a Handler is registered
        /// </summary>
        /// <param name="prefix">Namespace Prefix</param>
        /// <param name="uri">Namespace Uri</param>
        protected virtual void OnNamespaceRemoved(string prefix, Uri uri)
        {
            NamespaceChanged handler = NamespaceRemoved;
            if (handler != null)
            {
                handler(prefix, uri);
            }
        }

        /// <summary>
        /// Gets the available prefixes
        /// </summary>
        public IEnumerable<string> Prefixes
        {
            get 
            {
                return (from prefix in _uris.Keys
                        where HasNamespace(prefix)
                        select prefix);
            }
        }

        /// <summary>
        /// Tries to reduce a URI to a QName
        /// </summary>
        /// <param name="uri">URI</param>
        /// <param name="qname">Resulting QName</param>
        /// <param name="validationFunction">An optional validation function that returns true if the provided QName string is acceptable.</param>
        /// <returns>True if reduction succeeds, false otherwise</returns>
        public bool ReduceToQName(string uri, out string qname, Func<string, bool> validationFunction = null)
        {
            foreach (Uri u in _uris.Values.Select(l => l.Last(m => m.Offset < _offset).Uri))
            {
                string baseuri = u.AbsoluteUri;

                //Does the Uri start with the Base Uri
                if (uri.StartsWith(baseuri))
                {
                    //Remove the Base Uri from the front of the Uri
                    qname = uri.Substring(baseuri.Length);
                    //Add the Prefix back onto the front plus the colon to give a QName
                    qname = GetPrefix(u) + ":" + qname;
                    if (qname.Equals(":")) continue;
                    if (qname.Contains("/") || qname.Contains("#")) continue;
                    if (validationFunction != null && !validationFunction(qname)) continue;
                    return true;
                }
            }

            //Failed to find a Reduction
            qname = string.Empty;
            return false;
        }

        /// <summary>
        /// Removes a namespace
        /// </summary>
        /// <param name="prefix">Prefix</param>
        public void RemoveNamespace(string prefix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disposes of the namespace map
        /// </summary>
        public void Dispose()
        {
            _prefixes.Clear();
            _uris.Clear();
        }
    }

    /// <summary>
    /// Represents a namespace mapping with an associated offset
    /// </summary>
    class OffsetMapping
    {
        private int _offset;
        private string _prefix;
        private Uri _uri;

        /// <summary>
        /// Creates a new mapping
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="uri">URI</param>
        /// <param name="offset">Offset</param>
        public OffsetMapping(string prefix, Uri uri, int offset)
        {
            _prefix = prefix;
            _uri = uri;
            _offset = offset;
        }

        /// <summary>
        /// Creates a new mapping
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="uri">URI</param>
        public OffsetMapping(string prefix, Uri uri)
            : this(prefix, uri, 0) { }

        /// <summary>
        /// Gets the offset
        /// </summary>
        public int Offset
        {
            get 
            {
                return _offset;
            }
        }

        /// <summary>
        /// Gets the prefix
        /// </summary>
        public string Prefix
        {
            get 
            {
                return _prefix;
            }
        }

        /// <summary>
        /// Gets the URI
        /// </summary>
        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }
    }
}
