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
using VDS.RDF.Parsing.Handlers;

namespace VDS.RDF.Utilities.Convert
{
    class WriteToFileHandler : BaseRdfHandler
    {
        private string _file;
        private Encoding _encoding;
        private Type _formatterType;
        private WriteThroughHandler _handler;

        public WriteToFileHandler(string file, Encoding enc, Type formatterType)
        {
            _file = file;
            _encoding = enc;
            _formatterType = formatterType;
        }

        protected override void StartRdfInternal()
        {
            _handler = new WriteThroughHandler(_formatterType, new StreamWriter(_file, false, _encoding));
            _handler.StartRdf();
        }

        protected override void EndRdfInternal(bool ok)
        {
            _handler.EndRdf(ok);
        }

        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            return _handler.HandleBaseUri(baseUri);
        }

        protected override bool HandleNamespaceInternal(string prefix, Uri namespaceUri)
        {
            return _handler.HandleNamespace(prefix, namespaceUri);
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            return _handler.HandleTriple(t);
        }

        protected override bool HandleQuadInternal(Triple t, IRefNode graph)
        {
            return _handler.HandleQuad(t, graph);
        }

        public override bool AcceptsAll
        {
            get 
            {
                return true; 
            }
        }
    }
}
