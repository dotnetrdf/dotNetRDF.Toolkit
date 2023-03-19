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
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Storage.Management.Provisioning;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    /// <summary>
    /// Creates a task for deleting a Store
    /// </summary>
    public class DeleteStoreTask
        : NonCancellableTask<TaskResult>
    {
        private readonly IStorageServer _server;
        private readonly string _id;

        /// <summary>
        /// Gets a Store
        /// </summary>
        /// <param name="server">Server</param>
        /// <param name="id"></param>
        public DeleteStoreTask(IStorageServer server, string id)
            : base("Delete Store")
        {
            _server = server;
            _id = id;
        }

        /// <summary>
        /// Runs the task
        /// </summary>
        /// <returns></returns>
        protected override TaskResult RunTaskInternal()
        {
            Information = "Deleting Store " + _id + "...";
            if (_server != null)
            {
                _server.DeleteStore(_id);
                return new TaskResult(true);
            }
            else
            {
                throw new RdfStorageException("Deleting a store is unsupported");
            }
        }
    }
}
