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
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager.Tasks
{
    /// <summary>
    /// Task for running queries
    /// </summary>
    public class QueryTask
        : NonCancellableTask<object>
    {
        private readonly IQueryableStorage _storage;
        protected readonly SparqlQueryParser _parser = new SparqlQueryParser();
        private readonly GenericQueryProcessor _processor;
        private readonly bool _usePaging = false;
        private readonly int _pageSize = 1000;

        /// <summary>
        /// Creates a new Query Task
        /// </summary>
        /// <param name="storage">Storage Provider</param>
        /// <param name="query">Query</param>
        public QueryTask(IQueryableStorage storage, string query)
            : base("SPARQL Query")
        {
            _storage = storage;
            _processor = new GenericQueryProcessor(storage);
            QueryString = query;
        }

        /// <summary>
        /// Creates a new Query Task
        /// </summary>
        /// <param name="storage">Storage Provider</param>
        /// <param name="query">Query</param>
        /// <param name="pageSize">Page Size</param>
        public QueryTask(IQueryableStorage storage, string query, int pageSize)
            : this(storage, query)
        {
            _usePaging = true;
            _pageSize = pageSize;
        }

        /// <summary>
        /// Runs the task
        /// </summary>
        /// <returns></returns>
        protected override object RunTaskInternal()
        {
            try
            {
                //Firstly try and parse the Query
                Query = _parser.ParseFromString(QueryString);
            }
            catch
            {
                Information = "Query is not valid SPARQL 1.0/1.1 - will attempt to evaluate it but underlying store may reject the originalQuery...";
            }

            // Successfuly parsed originalQuery
            if (Query != null)
            {
                //Then apply it to the Manager using the GenericQueryProcessor
                try
                {
                    //Check that paging can be used if it was enabled
                    if (_usePaging)
                    {
                        if (Query.Limit >= 0 || Query.Offset > 0)
                        {
                            throw new RdfQueryException("Cannot apply originalQuery paging when the SPARQL Query already contains an explicit LIMIT and/or OFFSET clause");
                        }
                        else if (Query.QueryType == SparqlQueryType.Ask)
                        {
                            throw new RdfQueryException("Cannot apply originalQuery paging to an ASK Query");
                        }
                    }

                    int offset = 0;
                    TimeSpan totalTime = TimeSpan.Zero;

                    switch (Query.QueryType)
                    {
                        case SparqlQueryType.Ask:
                            SparqlResultSet blnResult = _processor.ProcessQuery(Query) as SparqlResultSet;
                            if (blnResult == null) throw new RdfQueryException("Store did not return a SPARQL Result Set for the ASK originalQuery as was expected");
                            return blnResult;

                        case SparqlQueryType.Construct:
                        case SparqlQueryType.Describe:
                        case SparqlQueryType.DescribeAll:
                            Graph g = new Graph();
                            g.NamespaceMap.Import(Query.NamespaceMap);

                            do
                            {
                                if (_usePaging)
                                {
                                    Query.Limit = _pageSize;
                                    Query.Offset = offset;
                                }
                                object result = _processor.ProcessQuery(Query);
                                totalTime += Query.QueryExecutionTime.HasValue ? Query.QueryExecutionTime.Value : TimeSpan.Zero;

                                if (!(result is IGraph)) throw new RdfQueryException("SPARQL Query did not return a RDF Graph as expected");
                                IGraph temp = (IGraph) result;

                                //If no further results can halt
                                if (temp.Triples.Count == 0) break;
                                offset += _pageSize;

                                //Merge the partial result into the final result
                                g.Merge(temp);
                            } while (_usePaging);

                            Information = "Query Completed OK (Took " + totalTime.ToString() + ")";
                            return g;

                        case SparqlQueryType.Select:
                        case SparqlQueryType.SelectAll:
                        case SparqlQueryType.SelectAllDistinct:
                        case SparqlQueryType.SelectAllReduced:
                        case SparqlQueryType.SelectDistinct:
                        case SparqlQueryType.SelectReduced:
                            SparqlResultSet results = new SparqlResultSet();
                            ResultSetHandler handler = new ResultSetHandler(results);
                            try
                            {
                                handler.StartResults();

                                do
                                {
                                    if (_usePaging)
                                    {
                                        Query.Limit = _pageSize;
                                        Query.Offset = offset;
                                    }
                                    object result = _processor.ProcessQuery(Query);
                                    totalTime += Query.QueryExecutionTime.HasValue ? Query.QueryExecutionTime.Value : TimeSpan.Zero;

                                    if (!(result is SparqlResultSet)) throw new RdfQueryException("SPARQL Query did not return a SPARQL Result Set as expected");
                                    SparqlResultSet rset = (SparqlResultSet) result;
                                    foreach (string var in rset.Variables)
                                    {
                                        handler.HandleVariable(var);
                                    }

                                    //If no further results can halt
                                    if (rset.Count == 0) break;
                                    offset += _pageSize;

                                    //Merge the partial result into the final result
                                    foreach (SparqlResult r in rset)
                                    {
                                        handler.HandleResult(r);
                                    }
                                } while (_usePaging);

                                handler.EndResults(true);
                            }
                            catch
                            {
                                handler.EndResults(false);
                                throw;
                            }
                            Information = "Query Completed OK (Took " + totalTime.ToString() + ")";
                            return results;

                        default:
                            throw new RdfQueryException("Cannot evaluate an unknown originalQuery type");
                    }
                }
                catch
                {
                    //Try and show the execution time if possible
                    if (Query.QueryExecutionTime.HasValue)
                    {
                        Information = "Query Failed (Took " + Query.QueryExecutionTime.Value + ")";
                    }
                    else
                    {
                        Information = "Query Failed";
                    }
                    throw;
                }
            }

            // Unsuccessfully parsed originalQuery - may be using syntax extensions specific to the target store
            DateTime start = DateTime.Now;
            try
            {
                if (_usePaging)
                {
                    throw new RdfQueryException("Cannot apply paging to a Query that we cannot parse as a valid SPARQL 1.0/1.1 originalQuery");
                }
                object results = _storage.Query(QueryString);
                Information = "Query Completed OK (Took " + (DateTime.Now - start) + ")";
                return results;
            }
            catch
            {
                //Try and show the execution time if possible
                try
                {
                    Information = "Query Failed (Took " + (DateTime.Now - start) + ")";
                }
                catch
                {
                    Information = "Query Failed";
                }
                throw;
            }
        }

        /// <summary>
        /// Returns that the task cannot be cancelled
        /// </summary>
        public override bool IsCancellable
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the originalQuery (assuming it is valid standard SPARQL)
        /// </summary>
        public SparqlQuery Query { get; protected set; }

        /// <summary>
        /// Gets the originalQuery string that is being used
        /// </summary>
        public string QueryString { get; protected set; }
    }
}