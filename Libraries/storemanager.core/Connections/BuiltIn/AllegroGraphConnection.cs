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
using System.ComponentModel;
using VDS.RDF.Configuration;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Connections.BuiltIn
{
    /// <summary>
    /// Definition for connections to AllegroGraph
    /// </summary>
    public class AllegroGraphConnectionDefinition
        : BaseHttpCredentialsOptionalServerConnectionDefinition
    {
        /// <summary>
        /// Creates a new definition
        /// </summary>
        public AllegroGraphConnectionDefinition()
            : base("Allegro Graph", "Connect to Franz AllegroGraph, Version 3.x and 4.x are supported", typeof(AllegroGraphConnector)) { }

        /// <summary>
        /// Gets/Sets the Server URI
        /// </summary>
        [Connection(DisplayName = "Server URI", IsRequired = true, DisplayOrder = -1, PopulateFrom = ConfigurationLoader.PropertyServer),
         DefaultValue("http://localhost:9875/")]
        public override string Server
        {
            get
            {
                return base.Server;
            }
            set
            {
                base.Server = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Catalog ID
        /// </summary>
        [Connection(DisplayName = "Catalog ID", DisplayOrder = 1, AllowEmptyString = true, IsRequired = true, Type = ConnectionSettingType.String, NotRequiredIf = "UseRootCatalog", PopulateFrom = ConfigurationLoader.PropertyCatalog)]
        public string CatalogID
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets whether to use the Root Catalog
        /// </summary>
        [Connection(DisplayName = "Use Root Catalog? (4.x and Higher)", DisplayOrder = 2, Type = ConnectionSettingType.Boolean, PopulateFrom = ConfigurationLoader.PropertyUser)]
        public bool UseRootCatalog
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the Store ID
        /// </summary>
        [Connection(DisplayName = "Store ID", DisplayOrder = 3, AllowEmptyString = false, IsRequired = true, Type = ConnectionSettingType.String, PopulateFrom = ConfigurationLoader.PropertyPassword)]
        public string StoreID
        {
            get;
            set;
        }

        /// <summary>
        /// Opens the connection to AllegroGraph
        /// </summary>
        /// <returns></returns>
        protected override IStorageProvider OpenConnectionInternal()
        {
            if (UseRootCatalog)
            {
                if (UseProxy)
                {
                    return new AllegroGraphConnector(Server, StoreID, Username, Password, GetProxy());
                }
                else
                {
                    return new AllegroGraphConnector(Server, StoreID, Username, Password);
                }
            }
            else
            {
                if (UseProxy)
                {
                    return new AllegroGraphConnector(Server, CatalogID, StoreID, GetProxy());
                }
                else
                {
                    return new AllegroGraphConnector(Server, CatalogID, StoreID, Username, Password);
                }
                
            }
        }

        /// <summary>
        /// Makes a copy of the current connection definition
        /// </summary>
        /// <returns>Copy of the connection definition</returns>
        public override IConnectionDefinition Copy()
        {
            AllegroGraphConnectionDefinition definition = new AllegroGraphConnectionDefinition();
            definition.Server = Server;
            definition.CatalogID = CatalogID;
            definition.UseRootCatalog = UseRootCatalog;
            definition.StoreID = StoreID;
            definition.ProxyPassword = ProxyPassword;
            definition.ProxyUsername = ProxyUsername;
            definition.ProxyServer = ProxyServer;
            definition.Username = Username;
            definition.Password = Password;
            return definition;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "[AllegroGraph] Store '" + StoreID.ToSafeString() + "' in Catalog '" + (UseRootCatalog ? "Root Catalog" : CatalogID.ToSafeString()) + "' on Server '" + Server.ToSafeString() + "'";
        }
    }
}
