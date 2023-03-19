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
using System.Threading;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    /// <summary>
    /// Task for listing stores
    /// </summary>
    public class ListStoresTask
        : NonCancellableTask<IEnumerable<string>>
    {
        private readonly IStorageServer _manager;

        /// <summary>
        /// Creates a new list stores task
        /// </summary>
        /// <param name="manager">Storage Provider</param>
        public ListStoresTask(IStorageServer manager)
            : base("List Stores")
        {
            _manager = manager;
        }

        /// <summary>
        /// Runs the task
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<string> RunTaskInternal()
        {
            if (_manager == null) throw new RdfStorageException("Store does not support listing of available Stores");
            // TODO IStorageServer does not require IStorageCapabilies to be implemented, add IServerCapabilities interface?
            //IStorageCapabilities caps = this._manager as IStorageCapabilities;

            //if (caps != null)
            //{
            //    if (!caps.IsReady)
            //    {
            //        this.Information = "Waiting for Store to become ready...";
            //        this.RaiseStateChanged();
            //        while (!caps.IsReady)
            //        {
            //            Thread.Sleep(250);
            //        }
            //    }
            //}

            
            return _manager.ListStores();
        }
    }
}
