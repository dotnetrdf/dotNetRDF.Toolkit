/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

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
using VDS.RDF.Configuration;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Connections
{
    /// <summary>
    /// Container class that holds all the necessary information about a connection
    /// </summary>
    public class Connection
        : IEquatable<Connection>, INotifyPropertyChanged
    {
        private DateTimeOffset _created, _modified;
        private bool _readOnly;

        /// <summary>
        /// Namespace URI for the Store Manager namespace
        /// </summary>
        public const string StoreManagerNamespace = "http://www.dotnetrdf.org/StoreManager#";

        private string _name;

        /// <summary>
        /// Creates a new connection as a copy of the existing connection
        /// </summary>
        /// <param name="connection">Connection</param>
        protected Connection(Connection connection)
        {
            Definition = connection.Definition.Copy();
            RootUri = CreateRootUri();
            Created = DateTimeOffset.UtcNow;
            LastModified = Created;
            LastOpened = null;
            IsReadOnly = connection.IsReadOnly;
            Name = "Copy of " + connection.Name;
        }

        /// <summary>
        /// Creates a new connection which will initially be in the closed state
        /// </summary>
        /// <param name="g">Configuration Graph</param>
        /// <param name="rootUri">Root URI for the connections information</param>
        public Connection(IGraph g, Uri rootUri)
        {
            if (ReferenceEquals(rootUri, null)) throw new ArgumentNullException("rootUri");
            RootUri = rootUri;
            LoadConfiguration(g);
        }

        /// <summary>
        /// Creates a new connection which will initially be in the closed state
        /// </summary>
        /// <param name="definition">Definition</param>
        public Connection(IConnectionDefinition definition)
            : this(definition, CreateRootUri())
        {
        }

        /// <summary>
        /// Creates a new connection which will initially be in the closed state
        /// </summary>
        /// <param name="definition">Definition</param>
        /// <param name="rootUri">Root URI</param>
        public Connection(IConnectionDefinition definition, Uri rootUri)
        {
            if (ReferenceEquals(definition, null)) throw new ArgumentNullException("definition");
            if (ReferenceEquals(rootUri, null)) throw new ArgumentNullException("rootUri");
            Definition = definition;
            RootUri = rootUri;

            Created = DateTimeOffset.UtcNow;
            LastModified = Created;
            LastOpened = null;
            IsReadOnly = false;
        }

        /// <summary>
        /// Creates a connection which is in the open state
        /// </summary>
        /// <param name="definition">Definition</param>
        /// <param name="provider">Storage Provider</param>
        public Connection(IConnectionDefinition definition, IStorageProvider provider)
            : this(definition, provider, CreateRootUri()) { }

        /// <summary>
        /// Creates a connection which is in the open state
        /// </summary>
        /// <param name="definition">Definition</param>
        /// <param name="provider">Storage Provider</param>
        /// <param name="rootUri">Root URI</param>
        public Connection(IConnectionDefinition definition, IStorageProvider provider, Uri rootUri)
        {
            if (ReferenceEquals(definition, null)) throw new ArgumentNullException("definition");
            if (ReferenceEquals(rootUri, null)) throw new ArgumentNullException("rootUri");
            if (ReferenceEquals(provider, null)) throw new ArgumentNullException("provider");
            Definition = definition;
            RootUri = rootUri;
            StorageProvider = provider;

            Created = DateTimeOffset.UtcNow;
            LastModified = Created;
            LastOpened = Created;
            IsReadOnly = false;

            // Because when created this way the definition may pertain not to this specific connection
            // we need to serialize and re-populate the definition to ensure we have the correct information
            IGraph g = new Graph();
            SaveConfiguration(g);
            INode rootNode = g.CreateUriNode(RootUri);
            Definition.PopulateFrom(g, rootNode);
        }

        /// <summary>
        /// Creates a fresh root URI as a UUID based URI
        /// </summary>
        /// <returns>Fresh root URI</returns>
        public static Uri CreateRootUri()
        {
            Guid uuid;
            do
            {
                uuid = Guid.NewGuid();
            } while (uuid.Equals(Guid.Empty));
            return new Uri("urn:uuid:" + uuid.ToString());
        }

        /// <summary>
        /// Gets the connection definition used to create the connection
        /// </summary>
        public IConnectionDefinition Definition { get; set; }

        /// <summary>
        /// Gets the storage provider
        /// </summary>
        /// <remarks>
        /// Will typically only be available if the connection is currently open
        /// </remarks>
        public IStorageProvider StorageProvider { get; private set; }

        /// <summary>
        /// Gets the connection information
        /// </summary>
        /// <remarks>
        /// Only applicable for open connections, otherwise a <see cref="NotSupportedException"/> is thrown
        /// </remarks>
        public ConnectionInfo Information
        {
            get
            {
                if (!ReferenceEquals(StorageProvider, null)) return new ConnectionInfo(StorageProvider);
                throw new InvalidOperationException("Cannot access connection information for a closed connection");
            }
        }

        /// <summary>
        /// Root URI used for serializing the connection information
        /// </summary>
        public Uri RootUri { get; private set; }

        /// <summary>
        /// Gets/Sets the friendly name associated with this connection
        /// </summary>
        public string Name
        {
            get
            {
                if (!ReferenceEquals(_name, null)) return _name;
                return !ReferenceEquals(StorageProvider, null) ? StorageProvider.ToString() : Definition.ToString();
            }
            set
            {
                if (!ReferenceEquals(value, null) && !value.Equals(Name)) LastModified = DateTimeOffset.UtcNow;
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        /// <summary>
        /// Gets/Sets the created date
        /// </summary>
        public DateTimeOffset Created
        {
            get { return _created; }
            private set
            {
                _created = value;
                RaisePropertyChanged("Created");
            }
        }

        /// <summary>
        /// Gets/Sets the last modified date
        /// </summary>
        public DateTimeOffset LastModified
        {
            get { return _modified; }
            set
            {
                _modified = value;
                RaisePropertyChanged("LastModified");
            }
        }

        /// <summary>
        /// Gets/Sets the last opened date
        /// </summary>
        public DateTimeOffset? LastOpened { get; private set; }

        /// <summary>
        /// Gets whether the connection is currently open
        /// </summary>
        public bool IsOpen
        {
            get { return !ReferenceEquals(StorageProvider, null); }
        }

        /// <summary>
        /// Gets how many active users of this connection there are i.e. how many times has <see cref="Open()"/> been called on this connection
        /// </summary>
        public int ActiveUsers { get; private set; }

        /// <summary>
        /// Gets whether this should be a read-only connection
        /// </summary>
        /// <remarks>
        /// This is used as the default setting when calling the no-argument <see cref="Open()"/> method
        /// </remarks>
        public bool IsReadOnly
        {
            get { return _readOnly; }
            private set
            {
                if (value == _readOnly) return;
                _readOnly = value;
                RaisePropertyChanged("IsReadOnly");
            }
        }

        /// <summary>
        /// Opens the connection if it is not already open
        /// </summary>
        public void Open()
        {
            Open(IsReadOnly);
        }

        /// <summary>
        /// Opens the connection if it is not already open
        /// </summary>
        /// <param name="readOnly">Whether to open the connection in read-only mode</param>
        /// <remarks>
        /// If the connection is already open then this merely increments the <see cref="ActiveUsers"/> property and updates the <see cref="LastOpened"/> date and returns.
        /// </remarks>
        public void Open(bool readOnly)
        {
            if (!ReferenceEquals(StorageProvider, null))
            {
                // Already open so increment active users
                ActiveUsers++;
                LastOpened = DateTimeOffset.UtcNow;
                RaisePropertyChanged("ActiveUsers");
                RaisePropertyChanged("LastOpened");
                return;
            }

            // Need to open the connection
            ActiveUsers++;
            IsReadOnly = readOnly;
            StorageProvider = Definition.OpenConnection();

            // Make read-only if necessary
            if (IsReadOnly && !StorageProvider.IsReadOnly)
            {
                if (StorageProvider is IQueryableStorage)
                {
                    StorageProvider = new QueryableReadOnlyConnector((IQueryableStorage) StorageProvider);
                }
                else
                {
                    StorageProvider = new ReadOnlyConnector(StorageProvider);
                }
            }
            LastOpened = DateTimeOffset.UtcNow;
            RaisePropertyChanged("ActiveUsers");
            RaisePropertyChanged("LastOpened");
            RaisePropertyChanged("IsOpen");
        }

        /// <summary>
        /// Request to close the connection, if there are multiple active users then the actual connection will remain open
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        /// <summary>
        /// Request to close the connection if it is not already closed, if there are multiple active users then the connection will remain open unless <paramref name="forceClose"/> is set to true
        /// </summary>
        /// <param name="forceClose">Whether to force close even if there are multiple active users</param>
        public void Close(bool forceClose)
        {
            // If not open do nothing
            if (ReferenceEquals(StorageProvider, null)) return;

            // Decrement ative users
            ActiveUsers--;
            RaisePropertyChanged("ActiveUsers");

            // Still in use so leave open
            if (ActiveUsers != 0 && !forceClose) return;

            // Close the connection for real
            StorageProvider.Dispose();
            StorageProvider = null;
            RaisePropertyChanged("IsOpen");
        }

        /// <summary>
        /// Saves the configuration for this connection to the given graph
        /// </summary>
        /// <param name="g">Graph</param>
        public void SaveConfiguration(IGraph g)
        {
            ConfigurationSerializationContext context = new ConfigurationSerializationContext(g);
            INode rootNode = context.Graph.CreateUriNode(RootUri);

            // Save the specific configuration for the connection only if it is currently open
            if (!ReferenceEquals(StorageProvider, null))
            {
                IConfigurationSerializable serializable = StorageProvider as IConfigurationSerializable;
                if (ReferenceEquals(serializable, null)) throw new InvalidOperationException("The underlying connection does not support serializing its configuration");

                // Remove any previous saved configuration
                // Note that this may leave some orphaned configuration information for complex configurations but since that would be linked by blank nodes it won't matter
                context.Graph.Retract(context.Graph.GetTriplesWithSubject(rootNode).ToList());

                // Serialize the new configuration state
                context.NextSubject = rootNode;
                serializable.SerializeConfiguration(context);
            }

            // Always save the additional Store Manager information

            // Friendly Name
            context.Graph.NamespaceMap.AddNamespace("rdfs", UriFactory.Create(NamespaceMapper.RDFS));
            INode rdfsLabel = context.Graph.CreateUriNode("rdfs:label");
            context.Graph.Retract(context.Graph.GetTriplesWithSubjectPredicate(rootNode, rdfsLabel).ToList());
            context.Graph.Assert(rootNode, rdfsLabel, context.Graph.CreateLiteralNode(Name));

            // Store Manager tracked information
            context.Graph.NamespaceMap.AddNamespace("store", UriFactory.Create(StoreManagerNamespace));
            INode created = context.Graph.CreateUriNode("store:created");
            context.Graph.Retract(context.Graph.GetTriplesWithSubjectPredicate(rootNode, created).ToList());
            context.Graph.Assert(rootNode, created, Created.ToLiteral(context.Graph));
            INode lastModified = context.Graph.CreateUriNode("store:lastModified");
            context.Graph.Retract(context.Graph.GetTriplesWithSubjectPredicate(rootNode, lastModified).ToList());
            context.Graph.Assert(rootNode, lastModified, LastModified.ToLiteral(context.Graph));
            INode lastOpened = context.Graph.CreateUriNode("store:lastOpened");
            context.Graph.Retract(context.Graph.GetTriplesWithSubjectPredicate(rootNode, lastOpened).ToList());
            if (LastOpened.HasValue) context.Graph.Assert(rootNode, lastOpened, LastOpened.Value.ToLiteral(context.Graph));
            INode defType = context.Graph.CreateUriNode("store:definitionType");
            context.Graph.Retract(context.Graph.GetTriplesWithSubjectPredicate(rootNode, defType).ToList());
            context.Graph.Assert(rootNode, defType, Definition.GetType().AssemblyQualifiedName.ToLiteral(context.Graph));
            INode readOnly = context.Graph.CreateUriNode("store:readOnly");
            context.Graph.Assert(rootNode, readOnly, IsReadOnly.ToLiteral(context.Graph));
        }

        /// <summary>
        /// Loads the connection definition for the connection from the given graph
        /// </summary>
        /// <param name="g">Graph</param>
        public void LoadConfiguration(IGraph g)
        {
            g.NamespaceMap.AddNamespace("store", UriFactory.Create(StoreManagerNamespace));
            g.NamespaceMap.AddNamespace("dnr", UriFactory.Create(ConfigurationLoader.ConfigurationNamespace));
            INode rootNode = g.CreateUriNode(RootUri);

            // First off need to find the definition type (if any)
            Triple t = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("store:definitionType")).FirstOrDefault();
            if (t != null && t.Object.NodeType == NodeType.Literal)
            {
                string typeString = ((ILiteralNode) t.Object).Value;
                Type defType = TryGetType(typeString, new string[] {"StoreManager.Core", "dotNetRDF", "dotNetRDF.Data.Virtuoso"});
                if (defType != null)
                {
                    Definition = (IConnectionDefinition) Activator.CreateInstance(defType);
                } 
                
            }
            if (ReferenceEquals(Definition, null))
            {
                // Have to figure out the definition type another way
                t = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("dnr:type")).FirstOrDefault();
                if (t != null && t.Object.NodeType == NodeType.Literal)
                {
                    string typeString = ((ILiteralNode)t.Object).Value;
                    Type providerType = TryGetType(typeString, new string[] { "StoreManager.Core", "dotNetRDF", "dotNetRDF.Data.Virtuoso" });
                    if (providerType != null)
                    {
                        IConnectionDefinition temp = ConnectionDefinitionManager.GetDefinitionByTargetType(providerType);
                        if (temp != null)
                        {
                            Definition = (IConnectionDefinition) Activator.CreateInstance(temp.GetType());
                        }
                    }
                }
            }
            if (ReferenceEquals(Definition, null)) throw new ArgumentException("Unable to locate the necessary configuration information to load this connection from the given Graph");

            // Populate information
            Definition.PopulateFrom(g, rootNode);
            LoadInformation(g);
        }

        /// <summary>
        /// Tries to load a type using the given type name
        /// </summary>
        /// <remarks>
        /// <para>
        /// First tries to load the type using the precise type name given, if this succeeds then the type is returned.
        /// </para>
        /// <para>
        /// If this fails and the given type name does not contain any assembly information it then attempts to load the type adding the assembly name to the type name.  If the type is found in any of the assemblies it is returned, assemblies will be searched in the order specified so if a type name is ambigious between assemblies then you need to either specify the assembly explicitly in the type name or specify the assemblies to search in the preferred search order.
        /// </para>
        /// <para>
        /// If the type cannot be found then <strong>null</strong> will be returned.
        /// </para>
        /// </remarks>
        /// <param name="typeString">Type String</param>
        /// <param name="assemblies">Assemblies to search</param>
        /// <returns>Type if found, null otherwise</returns>
        private static Type TryGetType(string typeString, IEnumerable<string> assemblies)
        {
            // Try type name as given
            try
            {
                Type type = Type.GetType(typeString);
                if (type != null) return type;
            }
            catch
            {
                // Ignore and try searching assemblies
            }
            // If type name contained assembly information searching assemblies will not help
            if (typeString.Contains(",")) return null;

            // Search assemblies
            foreach (string assembly in assemblies)
            {
                try
                {
                    Type type = Type.GetType(typeString + ", " + assembly);
                    if (type != null) return type;
                }
                catch
                {
                    // Ignore and try next assembly
                }
            }

            // Failed to find anything
            return null;
        }

        /// <summary>
        /// Loads ancillary information such as name and modified dates from the given graph
        /// </summary>
        /// <param name="g">Graph</param>
        public void LoadInformation(IGraph g)
        {
            g.NamespaceMap.AddNamespace("store", UriFactory.Create(StoreManagerNamespace));
            g.NamespaceMap.AddNamespace("dnr", UriFactory.Create(ConfigurationLoader.ConfigurationNamespace));
            g.NamespaceMap.AddNamespace("rdfs", UriFactory.Create(NamespaceMapper.RDFS));
            INode rootNode = g.CreateUriNode(RootUri);

            // Created, Last Modified and Last Opened
            Triple created = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("store:created")).FirstOrDefault();
            Triple lastModified = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("store:lastModified")).FirstOrDefault();
            Triple lastOpened = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("store:lastOpened")).FirstOrDefault();
// ReSharper disable PossibleInvalidOperationException
            // Resharper warnings are incorrect since logic of the GetDate() method guarantees we'll always produce a value
            Created = GetDate(created, DateTimeOffset.UtcNow).Value;
            LastModified = GetDate(lastModified, Created).Value;
// ReSharper restore PossibleInvalidOperationException
            LastOpened = GetDate(lastOpened, null);

            // Read-Only?
            IsReadOnly = ConfigurationLoader.GetConfigurationBoolean(g, rootNode, g.CreateUriNode("store:readOnly"), false);

            // Friendly Name
            Triple nameTriple = g.GetTriplesWithSubjectPredicate(rootNode, g.CreateUriNode("rdfs:label")).FirstOrDefault();
            if (nameTriple == null) return;
            if (nameTriple.Object.NodeType != NodeType.Literal) return;
            Name = ((ILiteralNode) nameTriple.Object).Value;
        }

        /// <summary>
        /// Makes a copy of this connection which will be in the closed state
        /// </summary>
        /// <returns>Copy</returns>
        public Connection Copy()
        {
            return new Connection(this);
        }

        /// <summary>
        /// Gets the date based on the stored value using the default value if there was no stored value
        /// </summary>
        /// <param name="t">Triple whose object is the stored value</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Date</returns>
        private static DateTimeOffset? GetDate(Triple t, DateTimeOffset? defaultValue)
        {
            if (t == null) return defaultValue;
            INode n = t.Object;
            if (n.NodeType == NodeType.Literal)
            {
                IValuedNode value = n.AsValuedNode();
                try
                {
                    return value.AsDateTimeOffset();
                }
                catch (RdfQueryException)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Determines whether the connection is equal to another object
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>True if equals, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return obj is Connection && Equals((Connection) obj);
        }

        /// <summary>
        /// Gets the hash code for a connection
        /// </summary>
        /// <returns>Hash Code</returns>
        public override int GetHashCode()
        {
            return RootUri.GetEnhancedHashCode();
        }

        /// <summary>
        /// Gets the string representation of this connection
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Determines whether the connection is equal to another connection
        /// </summary>
        /// <param name="other">Connection</param>
        /// <returns>True if equals, false otherwise</returns>
        public bool Equals(Connection other)
        {
            return EqualityHelper.AreUrisEqual(RootUri, other.RootUri);
        }

        /// <summary>
        /// Event which is raised when a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method for raising the property changed event
        /// </summary>
        /// <param name="propertyName">Property Name</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}