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
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    /// <summary>
    /// Abstract Base Class for import tasks
    /// </summary>
    public abstract class BaseImportTask 
        : CancellableTask<TaskResult>
    {
        private readonly IStorageProvider _manager;
        private readonly Uri _targetUri;
        private CancellableHandler _canceller;
        private readonly StoreCountHandler _counter = new StoreCountHandler();
        private readonly ImportProgressHandler _progress;
        private readonly int _batchSize;

        /// <summary>
        /// Creates a new Import Task
        /// </summary>
        /// <param name="name">Task Name</param>
        /// <param name="manager">Storage Provider to import to</param>
        /// <param name="targetGraph">Target Graph to import to</param>
        /// <param name="batchSize">Batch Size for imports</param>
        public BaseImportTask(string name, IStorageProvider manager, Uri targetGraph, int batchSize)
            : base(name)
        {
            _manager = manager;
            _targetUri = targetGraph;
            _batchSize = batchSize;
            if (_batchSize <= 0) _batchSize = 100;

            _progress = new ImportProgressHandler(_counter);
            _progress.Progress += new ImportProgressEventHandler(ReportProgress);
        }

        /// <summary>
        /// Progress reporter event handler
        /// </summary>
        void ReportProgress()
        {
            int readTriples = _counter.TripleCount;
            int importedTriples = readTriples / _batchSize * _batchSize;
            Information = $"Read {readTriples} Triple(s), Imported {importedTriples} Triple(s) in {_counter.GraphCount} Graph(s) so far...";
        }
        
        /// <summary>
        /// Implementation of the task
        /// </summary>
        /// <returns></returns>
        protected sealed override TaskResult RunTaskInternal()
        {
            if (_manager.UpdateSupported)
            {
                //Use a WriteToStoreHandler for direct writing
                _canceller = new CancellableHandler(new WriteToStoreHandler(_manager, GetTargetUri(), _batchSize));
                if (HasBeenCancelled) _canceller.Cancel();

                //Wrap in a ChainedHandler to ensure we permit cancellation but also count imported triples
                ChainedHandler m = new ChainedHandler(new IRdfHandler[] { _canceller, _progress });
                ImportUsingHandler(m);
            }
            else
            {
                //Use a StoreHandler to load into memory and will do a SaveGraph() at the end
                TripleStore store = new TripleStore();
                _canceller = new CancellableHandler(new StoreHandler(store));
                if (HasBeenCancelled) _canceller.Cancel();

                //Wrap in a ChainedHandler to ensure we permit cancellation but also count imported triples
                ChainedHandler m = new ChainedHandler(new IRdfHandler[] { _canceller, _progress });
                ImportUsingHandler(m);

                //Finally Save to the underlying Store
                foreach (IGraph g in store.Graphs)
                {
                    if (g.BaseUri == null)
                    {
                        g.BaseUri = GetTargetUri();
                    }
                    _manager.SaveGraph(g);
                }
            }
            Information = _counter.TripleCount + " Triple(s) in " + _counter.GraphCount + " Graph(s) Imported";

            return new TaskResult(true);
        }

        /// <summary>
        /// Abstract method to be implemented by derived classes to perform the actual importing
        /// </summary>
        /// <param name="handler"></param>
        protected abstract void ImportUsingHandler(IRdfHandler handler);

        /// <summary>
        /// Gets the Target URI
        /// </summary>
        /// <returns></returns>
        private Uri GetTargetUri()
        {
            if (_targetUri != null)
            {
                return _targetUri;
            }
            else
            {
                return GetDefaultTargetUri();
            }
        }

        /// <summary>
        /// Gets the Default Target URI
        /// </summary>
        /// <returns></returns>
        protected virtual Uri GetDefaultTargetUri()
        {
            return null;
        }

        /// <summary>
        /// Cancels the Task
        /// </summary>
        protected override void CancelInternal()
        {
            if (_canceller != null)
            {
                _canceller.Cancel();
            }
        }
    }

    /// <summary>
    /// Delegate for Import Progress Events
    /// </summary>
    public delegate void ImportProgressEventHandler();

    /// <summary>
    /// RDF Handler which can raise Progress Events
    /// </summary>
    class ImportProgressHandler
        : BaseRdfHandler, IWrappingRdfHandler
    {
        private StoreCountHandler _handler;
        private int _progressCount = 0;

        /// <summary>
        /// Creates a new Handler
        /// </summary>
        /// <param name="handler">Handler</param>
        public ImportProgressHandler(StoreCountHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            _handler = handler;
        }

        /// <summary>
        /// Gets the Inner Handler
        /// </summary>
        public IEnumerable<IRdfHandler> InnerHandlers
        {
            get
            {
                return _handler.AsEnumerable().OfType<IRdfHandler>();
            }
        }

        /// <summary>
        /// Starts the Handling of RDF
        /// </summary>
        protected override void StartRdfInternal()
        {
            _progressCount = 0;
            _handler.StartRdf();
        }

        /// <summary>
        /// Ends the Handling of RDF
        /// </summary>
        /// <param name="ok">Whether parsing finished OK</param>
        protected override void EndRdfInternal(bool ok)
        {
            _handler.EndRdf(ok);
        }

        /// <summary>
        /// Handles Triples
        /// </summary>
        /// <param name="t">Triple</param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            bool temp = _handler.HandleTriple(t);
            _progressCount++;
            if (_progressCount == 1000)
            {
                _progressCount = 0;
                RaiseImportProgress();
            }
            return temp;
        }

        protected override bool HandleQuadInternal(Triple t, IRefNode graph)
        {
            bool temp = _handler.HandleQuad(t, graph);
            _progressCount++;
            if (_progressCount == 1000)
            {
                _progressCount = 0;
                RaiseImportProgress();
            }
            return temp;
        }

        /// <summary>
        /// Returns that this handler does accept everything
        /// </summary>
        public override bool AcceptsAll
        {
            get 
            {
                return true; 
            }
        }

        /// <summary>
        /// Progress Event
        /// </summary>
        public event ImportProgressEventHandler Progress;

        /// <summary>
        /// Raises the Progress Event
        /// </summary>
        private void RaiseImportProgress()
        {
            ImportProgressEventHandler d = Progress;
            if (d != null)
            {
                d();
            }
        }
    }
}
