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
using System.ComponentModel;
using System.Linq;
using System.Text;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Connections
{
    /// <summary>
    /// Wrapper class that can be placed around a <see cref="IStorageProvider"/> instance and used to display its information in a property grid
    /// </summary>
    public class ConnectionInfo
    {
        private IStorageProvider _manager;

        /// <summary>
        /// Creates a new Connection Information wrapper
        /// </summary>
        /// <param name="manager">Storage Provider</param>
        public ConnectionInfo(IStorageProvider manager)
        {
            this._manager = manager;
        }

        /// <summary>
        /// Gets the name of the connection
        /// </summary>
        [Category("Basic"),Description("The name of the connection")]
        public String Name
        {
            get
            {
                return this._manager.ToString();
            }
        }

        /// <summary>
        /// Gets the .Net Type of the connection
        /// </summary>
        [Category("Basic"), Description("The .Net type of the connection")]
        public String Type
        {
            get
            {
                return this._manager.GetType().AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets whether the connection is read only
        /// </summary>
        [Category("Basic"), Description("Is the connection read-only?")]
        public bool IsReadOnly
        {
            get
            {
                return this._manager.IsReadOnly;
            }
        }

        /// <summary>
        /// Gets whether the connection is ready
        /// </summary>
        [Category("Basic"), Description("Is the connection ready for use?")]
        public bool IsReady
        {
            get
            {
                return this._manager.IsReady;
            }
        }

        /// <summary>
        /// Gets the connection
        /// </summary>
        [Category("Basic"), Description("The actual connection object, may be expanded to see implementation specific additional information"),
         TypeConverter(typeof(ExpandableObjectConverter))]
        public IStorageProvider Connection
        {
            get
            {
                return this._manager;
            }
        }

        /// <summary>
        /// Gets wether the connection is queryable
        /// </summary>
        [Category("Features"),Description("Is SPARQL Query supported by this connection?")]
        public bool IsQueryable
        {
            get
            {
                return (this._manager is IQueryableStorage);
            }
        }

        /// <summary>
        /// Gets the SPARQL Update mode used for the connection
        /// </summary>
        [Category("Features"),Description("Is SPARQL Update supported by this connection and if so how?  Possible values are Native, Approximated and Not Supported")]
        public String UpdateMode
        {
            get
            {
                if (this._manager is IUpdateableStorage)
                {
                    return "Native";
                }
                else if (!this._manager.IsReadOnly)
                {
                    return "Approximated";
                }
                else
                {
                    return "Not Supported";
                }
            }
        }

        /// <summary>
        /// Gets whether the connection can list graphs
        /// </summary>
        [Category("Features"),Description("Is the connection capable of listing graphs?")]
        public bool CanListGraphs
        {
            get
            {
                return this._manager.ListGraphsSupported;
            }
        }

        /// <summary>
        /// Gets whether the connection can delete graphs
        /// </summary>
        [Category("Features"),Description("Is the connection capable of deleting graphs?")]
        public bool CanDeleteGraphs
        {
            get
            {
                return this._manager.DeleteSupported;
            }
        }

        /// <summary>
        /// Gets whethe data imported by a <see cref="IStorageProvider.SaveGraph"/> call will append to existing data or overwrite it, see <see cref="ConnectionInfo.SaveToDefaultBehaviour"/> and <see cref="ConnectionInfo.SaveToNamedBehaviour"/> for more detailed information
        /// </summary>
        [Category("Features"),Description("When a data import is performed with Store Manager will the imported data be appended to any existing data in the target graph(s) or will it overwrite it?  If this property is false exact import behaviour can be determined by checking the SaveToDefaultBehaviour and SaveToNamedBehaviour properties.")]
        public bool WillImportsAppend
        {
            get
            {
                return !this._manager.IsReadOnly && this._manager.UpdateSupported;
            }
        }

        /// <summary>
        /// Gets whether the connection is a Triple/Quad store
        /// </summary>
        [Category("IO Capabilities"),Description("Shows if the connection works in Triples or Quads")]
        public String StorageType
        {
            get
            {
                if ((this._manager.IOBehaviour & IOBehaviour.IsTripleStore) != 0)
                {
                    return "Triples";
                }
                else if ((this._manager.IOBehaviour & IOBehaviour.IsQuadStore) != 0)
                {
                    return "Quads";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets whether the connection supports the notion of an explicit default graph
        /// </summary>
        [Category("IO Capabilities"),Description("Does the connection support an explicit default graph?")]
        public bool IsDefaultGraphSupported
        {
            get
            {
                return ((this._manager.IOBehaviour & IOBehaviour.HasDefaultGraph) != 0);
            }
        }

        /// <summary>
        /// Gets whether the connection supports named graphs
        /// </summary>
        [Category("IO Capabilities"),Description("Does the connection support named graphs?")]
        public bool AreNamedGraphsSupported
        {
            get
            {
                return ((this._manager.IOBehaviour & IOBehaviour.HasNamedGraphs) != 0);
            }
        }

        /// <summary>
        /// Gets whether data imported by a <see cref="IStorageProvider.SaveGraph"/> call will append to existing data or overwrite it when saving to the Default Graph
        /// </summary>
        [Category("IO Capabilities"),Description("How does the connection handle saving graph data to the default graph?  Possible values are Overwrite, Append, N/A (Read Only) and Unknown.  Please note that when using the Import feature Store Manager will always attempt to append data unless the connection does not allow this, please see the WillImportsAppend property to see what the behaviour for this connection will be when used within Store Manager.  Note - If that property is false then this behaviour applies for triples in the default graph.")]
        public String SaveToDefaultBehaviour
        {
            get
            {
                if (this._manager.IsReadOnly)
                {
                    return "N/A (Read Only)";
                }
                else if ((this._manager.IOBehaviour & IOBehaviour.OverwriteDefault) != 0)
                {
                    return "Overwrite";
                }
                else if ((this._manager.IOBehaviour & IOBehaviour.AppendToDefault) != 0)
                {
                    return "Append";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets whethe data imported by a <see cref="IStorageProvider.SaveGraph"/> call will append to existing data or overwrite it when saving to a Named Graph
        /// </summary>
        [Category("IO Capabilities"), Description("How does the connection handle saving graph data to named graphs?  Possible values are Overwrite, Append, N/A (Read Only) and Unknown.  Please note that when using the Import feature Store Manager will always attempt to append data unless the connection does not allow this, please see the WillImportsAppend property to see what the behaviour for this connection will be when used within Store Manager.  Note - If that property is false then this behaviour applies for triples in named graphs.")]
        public String SaveToNamedBehaviour
        {
            get
            {
                if (this._manager.IsReadOnly)
                {
                    return "N/A (Read Only)";
                }
                else if ((this._manager.IOBehaviour & IOBehaviour.OverwriteNamed) != 0)
                {
                    return "Overwrite";
                }
                else if ((this._manager.IOBehaviour & IOBehaviour.AppendToNamed) != 0)
                {
                    return "Append";
                }
                else
                {
                    return "Unknown";
                }
            }
        }
    }
}
