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
using System.Net;
using System.Net.Http;
using System.Text;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Update;

namespace VDS.RDF.Utilities.StoreManager.Connections.BuiltIn
{
    /// <summary>
    /// Definition for read-only connection to SPARQL endpoints
    /// </summary>
    public class ReadWriteSparqlConnectionDefinition
        : BaseHttpConnectionDefinition
    {
        /// <summary>
        /// Creates a new definition
        /// </summary>
        public ReadWriteSparqlConnectionDefinition()
            : base("SPARQL Query & Update", "Connect to any SPARQL protocol compliant store that provides both Query and Update endpoints", typeof(ReadWriteSparqlConnector)) { }

        /// <summary>
        /// Gets/Sets the Query Endpoint URI
        /// </summary>
        [Connection(DisplayName = "Query Endpoint URI", DisplayOrder = 1, IsRequired = true, AllowEmptyString = false, PopulateVia = ConfigurationLoader.PropertyQueryEndpoint, PopulateFrom = ConfigurationLoader.PropertyQueryEndpointUri),
         DefaultValue("http://example.org/query")]
        public string QueryEndpointUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the Query Endpoint URI
        /// </summary>
        [Connection(DisplayName = "Update Endpoint URI", DisplayOrder = 2, IsRequired = true, AllowEmptyString = false, PopulateVia = ConfigurationLoader.PropertyUpdateEndpoint, PopulateFrom = ConfigurationLoader.PropertyUpdateEndpointUri),
         DefaultValue("http://example.org/update")]
        public string UpdateEndpointUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the Default Graph URI
        /// </summary>
        [Connection(DisplayName = "Query Default Graph", DisplayOrder = 3, IsRequired = false, AllowEmptyString = true, PopulateVia = ConfigurationLoader.PropertyQueryEndpoint, PopulateFrom = ConfigurationLoader.PropertyDefaultGraphUri)]
        public string QueryDefaultGraphUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the Load Method
        /// </summary>
        [Connection(DisplayName = "Load Method", DisplayOrder = 3, Type = ConnectionSettingType.Enum, PopulateVia = ConfigurationLoader.PropertyQueryEndpoint, PopulateFrom = ConfigurationLoader.PropertyLoadMode),
         DefaultValue(SparqlConnectorLoadMethod.Construct)]
        public SparqlConnectorLoadMethod LoadMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets whether local parsing of queries should be skipped
        /// </summary>
        [Connection(DisplayName = "Skip Local Query and Update Parsing?", DisplayOrder = 4, Type = ConnectionSettingType.Boolean, PopulateFrom = ConfigurationLoader.PropertySkipParsing),
         DefaultValue(false)]
        public bool SkipLocalParsing { get; set; }

        /// <summary>
        /// Opens the connection
        /// </summary>
        /// <returns></returns>
        protected override IStorageProvider OpenConnectionInternal()
        {
            var httpClient = UseProxy ? new HttpClient(new HttpClientHandler { Proxy = GetProxy() }) : new HttpClient();
            var client = new SparqlQueryClient(httpClient, new Uri(QueryEndpointUri));
            if (!string.IsNullOrEmpty(QueryDefaultGraphUri)) client.DefaultGraphs.Add(QueryDefaultGraphUri);
            var updateClient = new SparqlUpdateClient(httpClient, new Uri(UpdateEndpointUri));
            ReadWriteSparqlConnector connector = new ReadWriteSparqlConnector(client, updateClient, LoadMode);
            return connector;
        }

        /// <summary>
        /// Makes a copy of the current connection definition
        /// </summary>
        /// <returns>Copy of the connection definition</returns>
        public override IConnectionDefinition Copy()
        {
            ReadWriteSparqlConnectionDefinition definition = new ReadWriteSparqlConnectionDefinition();
            definition.QueryEndpointUri = QueryEndpointUri;
            definition.QueryDefaultGraphUri = QueryDefaultGraphUri;
            definition.UpdateEndpointUri = UpdateEndpointUri;
            definition.LoadMode = LoadMode;
            definition.SkipLocalParsing = SkipLocalParsing;
            definition.ProxyPassword = ProxyPassword;
            definition.ProxyUsername = ProxyUsername;
            definition.ProxyServer = ProxyServer;
            return definition;
        }

        public override string ToString()
        {
            return "[SPARQL Query & Update] Query: " + QueryEndpointUri.ToSafeString() + " Update: " + UpdateEndpointUri.ToSafeString();
        }
    }
}
