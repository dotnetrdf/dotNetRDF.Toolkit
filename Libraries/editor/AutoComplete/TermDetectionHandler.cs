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
using VDS.RDF.Parsing.Handlers;

namespace VDS.RDF.Utilities.Editor.AutoComplete
{
    /// <summary>
    /// A RDF Handler designed to detect terms defined in the data
    /// </summary>
    public class TermDetectionHandler
        : BaseRdfHandler
    {
        private INode _rdfType, _rdfsClass, _rdfProperty, _rdfsDatatype, _rdfsLabel, _rdfsComment;
        private HashSet<INode> _terms;
        private Dictionary<INode, string> _termLabels, _termComments;
        private NamespaceMapper _nsmap;

        /// <summary>
        /// Creates a new handler
        /// </summary>
        public TermDetectionHandler()
        {
            _rdfType = CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            _rdfsClass = CreateUriNode(new Uri(NamespaceMapper.RDFS + "Class"));
            _rdfProperty = CreateUriNode(new Uri(NamespaceMapper.RDF + "Property"));
            _rdfsDatatype = CreateUriNode(new Uri(NamespaceMapper.RDFS + "Datatype"));
            _rdfsLabel = CreateUriNode(new Uri(NamespaceMapper.RDFS + "label"));
            _rdfsComment = CreateUriNode(new Uri(NamespaceMapper.RDFS + "comment"));
        }

        /// <summary>
        /// Gets the detected terms
        /// </summary>
        public IEnumerable<NamespaceTerm> DetectedTerms
        {
            get
            {
                if (_terms != null)
                {
                    List<NamespaceTerm> results = new List<NamespaceTerm>();
                    foreach (INode term in _terms)
                    {
                        //Must be reduceable to a QName
                        string qname;
                        if (_nsmap.ReduceToQName(term.ToString(), out qname))
                        {
                            string prefix = qname.StartsWith(":") ? string.Empty : qname.Substring(0, qname.IndexOf(':'));
                            string label = _termLabels.ContainsKey(term) ? _termLabels[term] : (_termComments.ContainsKey(term) ? _termComments[term] : string.Empty);
                            results.Add(new NamespaceTerm(_nsmap.GetNamespaceUri(prefix).AbsoluteUri, qname, label));
                        }
                    }

                    return results;
                }
                else
                {
                    return Enumerable.Empty<NamespaceTerm>();
                }
            }
        }

        /// <summary>
        /// Starts RDF handling
        /// </summary>
        protected override void StartRdfInternal()
        {
            _terms = new HashSet<INode>();
            _termLabels = new Dictionary<INode, string>();
            _termComments = new Dictionary<INode, string>();
            _nsmap = new NamespaceMapper(true);
        }

        /// <summary>
        /// Handles namespace declarations
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="namespaceUri">URI</param>
        /// <returns></returns>
        protected override bool HandleNamespaceInternal(string prefix, Uri namespaceUri)
        {
            _nsmap.AddNamespace(prefix, namespaceUri);
            return true;
        }

        /// <summary>
        /// Handles triple declarations
        /// </summary>
        /// <param name="t">Triple</param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            if (t.Subject.NodeType == NodeType.Uri)
            {
                if (t.Predicate.Equals(_rdfType))
                {
                    if (t.Object.Equals(_rdfsClass) || t.Object.Equals(_rdfsDatatype) || t.Object.Equals(_rdfProperty))
                    {
                        if (!_terms.Contains(t.Subject))
                        {
                            _terms.Add(t.Subject);
                        }
                    }
                }
                else if (t.Predicate.Equals(_rdfsLabel) && t.Object.NodeType == NodeType.Literal)
                {
                    if (!_termLabels.ContainsKey(t.Subject))
                    {
                        _termLabels.Add(t.Subject, ((ILiteralNode)t.Object).Value);
                    }
                }
                else if (t.Predicate.Equals(_rdfsComment) && t.Object.NodeType == NodeType.Literal)
                {
                    if (!_termComments.ContainsKey(t.Subject))
                    {
                        _termLabels.Add(t.Subject, ((ILiteralNode)t.Object).Value);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Handles quad declarations
        /// </summary>
        /// <param name="t"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        protected override bool HandleQuadInternal(Triple t, IRefNode graph)
        {
            return HandleTriple(t);
        }

        /// <summary>
        /// Gets that this handler accepts all inputs
        /// </summary>
        public override bool AcceptsAll
        {
            get 
            {
                return true; 
            }
        }
    }
}
