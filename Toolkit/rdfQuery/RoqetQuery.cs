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
using System.Reflection;
using System.Net;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace VDS.RDF.Utilities.Query
{
    public class RoqetQuery
    {
        private bool _count = false;
        private bool _dump = false;
        private string _dumpFormat = string.Empty;
        private bool _dryrun = false;
        private bool _quiet = false;
        private bool _walk = false;
        private ISparqlResultsWriter _resultsWriter = new SparqlXmlWriter();
        private IRdfWriter _graphWriter = new NTriplesWriter();
        private string _inputUri = string.Empty;
        private string _queryString = string.Empty;
        private SparqlQueryParser _parser = new SparqlQueryParser();
        private WebDemandTripleStore _store = new WebDemandTripleStore();
        private List<string> _namedGraphs = new List<string>();
        
        public void RunQuery(string[] args)
        {
            if (!SetOptions(args))
            {
                //Abort if we can't set options properly
                return;
            }

            //If no input URI/Query specified exit
            if (_inputUri.Equals(string.Empty) && _queryString.Equals(string.Empty))
            {
                Console.Error.WriteLine("rdfQuery: No Query Input URI of -e QUERY option was specified so nothing to do");
                return;
            }
            else if (!_inputUri.Equals(string.Empty))
            {
                //Try and load the query from the File/URI
                try
                {
                    Uri u = new Uri(_inputUri);
                    if (u.IsAbsoluteUri)
                    {
                        using (StreamReader reader = new StreamReader(HttpWebRequest.Create(_inputUri).GetResponse().GetResponseStream()))
                        {
                            _queryString = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        using (StreamReader reader = new StreamReader(_inputUri))
                        {
                            _queryString = reader.ReadToEnd();
                        }
                    }
                }
                catch (UriFormatException)
                {
                    using (StreamReader reader = new StreamReader(_inputUri))
                    {
                        _queryString = reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("rdfQuery: Error: Unable to read the query from the URI '" + _inputUri + "' due to the following error:");
                    Console.Error.WriteLine("rdfQuery: Error: " + ex.Message);
                    return;
                }
            }

            //Try to parse the query
            SparqlQuery q;
            try
            {
                q = _parser.ParseFromString(_queryString);
            }
            catch (RdfParseException parseEx)
            {
                Console.Error.WriteLine("rdfQuery: Parser Error: Unable to parse the query due to the following error:");
                Console.Error.WriteLine("rdfQuery: Parser Error: " + parseEx.Message);
                return;
            }
            catch (RdfQueryException queryEx)
            {
                Console.Error.WriteLine("rdfQuery: Query Error: Unable to read the query due to the following error:");
                Console.Error.WriteLine("rdfQuery: Query Error: " + queryEx.Message);
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("rdfQuery: Error: Unable to read the query due to the following error:");
                Console.Error.WriteLine("rdfQuery: Error: " + ex.Message);
                return;
            }

            //If dumping/dry-running/walking then just print the appropriate debug stuff and exit
            if (_dryrun || _walk || _dump)
            {
                if (_dump || _walk)
                {
                    switch (_dumpFormat)
                    {
                        case "debug":
                            Console.WriteLine("rdfQuery: Parsed and Optimised Query with explicit nesting where appropriate:");
                            Console.WriteLine();
                            Console.WriteLine(q.ToString());
                            Console.WriteLine();
                            Console.WriteLine("rdfQuery: Algebra Form of Query");
                            Console.WriteLine();
                            Console.WriteLine(q.ToAlgebra().ToString());
                            break;
                        case "sparql":
                            Console.WriteLine(q.ToString());
                            break;
                        case "structure":
                            Console.WriteLine(q.ToAlgebra().ToString());
                            break;
                        default:
                            Console.Error.WriteLine("rdfQuery: Unknown dump format");
                            break;
                    }
                }
                else
                {
                    Console.Error.WriteLine("rdfQuery: Dry run complete - Query OK");
                }
                return;
            }

            //Show number of Graphs and Triples we're querying against
            if (!_quiet) Console.Error.WriteLine("rdfQuery: Making query against " + _store.Graphs.Count + " Graphs with " + _store.Triples.Count() + " Triples (plus " + _namedGraphs.Count + " named graphs which will be loaded as required)");

            //Now execute the actual query against the store
            //Add additional names graphs to the query
            foreach (string uri in _namedGraphs)
            {
                try
                {
                    q.AddNamedGraph(new Uri(uri));
                }
                catch (UriFormatException)
                {
                    Console.Error.WriteLine("rdfQuery: Ignoring Named Graph URI '" + uri + "' since it does not appear to be a valid URI");
                }
            }
            try
            {
                //Object results = this._store.ExecuteQuery(q);
                var processor = new LeviathanQueryProcessor(_store);
                var results = processor.ProcessQuery(q);
                if (results is SparqlResultSet)
                {
                    _resultsWriter.Save((SparqlResultSet)results, Console.Out);
                }
                else if (results is Graph)
                {
                    _graphWriter.Save((Graph)results, Console.Out);
                }
                else
                {
                    Console.Error.WriteLine("rdfQuery: Unexpected result from query - unable to output result");
                }
            }
            catch (RdfQueryException queryEx)
            {
                Console.Error.WriteLine("rdfQuery: Query Error: Unable to execute query due to the following error:");
                Console.Error.WriteLine("rdfQuery: Query Error: " + queryEx.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("rdfQuery: Error: Unable to execute query due to the following error:");
                Console.Error.WriteLine("rdfQuery: Error: " + ex.Message);
            }
        }

        private bool SetOptions(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0].Equals("-h") || args[0].Equals("--help"))))
            {
                ShowUsage();
                return false;
            }

            int i = 0;
            string arg;
            while (i < args.Length)
            {
                arg = args[i];

                if (arg.Equals("-e") || arg.Equals("--exec"))
                {
                    i++;
                    if (i >= args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a query string to be executed");
                        return false;
                    }
                    arg = args[i];
                    _queryString = arg;
                }
                else if (arg.Equals("-i") || arg.Equals("--input"))
                {
                    i++;
                    if (i > args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a query language");
                        return false;
                    }
                    arg = args[i];
                    if (!SetLanguage(arg)) return false;
                }
                else if (arg.Equals("-r") || arg.Equals("--results"))
                {
                    i++;
                    if (i > args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a query results format");
                        return false;
                    }
                    arg = args[i];
                    if (!SetResultsFormat(arg)) return false;
                }
                else if (arg.Equals("-c") || arg.Equals("--count"))
                {
                    _count = true;
                }
                else if (arg.Equals("-D") || arg.Equals("--data"))
                {
                    i++;
                    if (i > args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a data file URI");
                        return false;
                    }
                    arg = args[i];
                    if (!SetDataUri(arg)) return false;
                }
                else if (arg.Equals("-d") || arg.Equals("--dump-query"))
                {
                    i++;
                    if (i > args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a dump format");
                        return false;
                    }
                    arg = args[i];
                    if (!SetDumpFormat(arg)) return false;
                }
                else if (arg.Equals("-f") || arg.Equals("--feature"))
                {
                    i++;
                    if (i >= args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a feature setting");
                        return false;
                    }
                    Console.Error.WriteLine("rdfQuery: rdfQuery does not support features - this option will be ignored");
                }
                else if (arg.Equals("-G") || arg.Equals("--named") || arg.Equals("-s") || arg.Equals("--source"))
                {
                    i++;
                    if (i >= args.Length)
                    {
                        Console.Error.WriteLine("rdfQuery: Unexpected end of arguments - expected a named graph URI");
                        return false;
                    }
                    arg = args[i];
                    if (!SetNamedUri(arg)) return false;
                }
                else if (arg.Equals("-help") || arg.Equals("--help"))
                {
                    //Ignore when other arguments are specified
                }
                else if (arg.Equals("-n") || arg.Equals("--dryrun"))
                {
                    _dryrun = true;
                }
                else if (arg.Equals("-q") || arg.Equals("--quiet"))
                {
                    _quiet = true;
                }
                else if (arg.Equals("-v") || arg.Equals("--version"))
                {
                    Console.WriteLine("dotNetRDF Version " + Assembly.GetAssembly(typeof(Triple)).GetName().Version.ToString());
                    Console.WriteLine("rdfQuery Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    Console.WriteLine("http://www.dotnetrdf.org");
                    return false;
                }
                else if (arg.Equals("-w") || arg.Equals("--walk-query"))
                {
                    _walk = true;
                    if (_dumpFormat.Equals(string.Empty))
                    {
                        _dumpFormat = "debug";
                    }
                }
                else if (arg.StartsWith("-") && !arg.Equals("-"))
                {
                    Console.Error.WriteLine("rdfQuery: Unexpected argument '" + arg + "' encountered - this does not appear to be a valid roqet mode option");
                    return false;
                }
                else
                {
                    //Assume this is the Input URI
                    _inputUri = arg;

                    i++;
                    if (i < args.Length)
                    {
                        //Assume the next thing is the Input Base URI if we haven't had a -I or --input-uri option
                        arg = args[i];
                        if (!SetBaseUri(arg)) return false;

                        if (i < args.Length - 1)
                        {
                            Console.Error.WriteLine("rdfConvert: Additional arguments were specified after the Input URI (and Input Base URI if specified) which is not permitted - these arguments have been ignored");
                        }
                    }

                    return true;
                }

                i++;
            }

            return true;
        }

        private bool SetResultsFormat(string format)
        {
            switch (format)
            {
                case "xml":
                    _resultsWriter = new SparqlXmlWriter();
                    _graphWriter = new RdfXmlWriter();
                    break;
                case "json":
                    _resultsWriter = new SparqlJsonWriter();
                    _graphWriter = new RdfJsonWriter();
                    break;
                case "ntriples":
                    _graphWriter = new NTriplesWriter();
                    break;
                case "rdfxml":
                    _graphWriter = new RdfXmlWriter();
                    break;
                case "turtle":
                    _graphWriter = new CompressingTurtleWriter(WriterCompressionLevel.High);
                    break;
                case "n3":
                    _graphWriter = new Notation3Writer(WriterCompressionLevel.High);
                    break;
                case "html":
                case "rdfa":
                    _resultsWriter = new SparqlHtmlWriter();
                    _graphWriter = new HtmlWriter();
                    break;
                case "csv":
                    _resultsWriter = new SparqlCsvWriter();
                    _graphWriter = new CsvWriter();
                    break;
                case "tsv":
                    _resultsWriter = new SparqlTsvWriter();
                    _graphWriter = new TsvWriter();
                    break;
                default:
                    Console.Error.WriteLine("rdfQuery: The value '" + format + "' is not a valid Results Format");
                    return false;
            }

            return true;
        }

        private bool SetLanguage(string lang)
        {
            switch (lang)
            {
                case "sparql":
                    //OK
                    return true;
                case "rdql":
                    Console.Error.WriteLine("rdfQuery: dotNetRDF does not support RDQL");
                    return false;
                default:
                    Console.Error.WriteLine("rdfQuery: The value '" + lang + "' is not a valid query language");
                    return false;
            }
        }

        private bool SetDataUri(string uri)
        {
            //No need to load the data if we aren't bothering to execute the query
            if (_dryrun || _walk || _dump) return true;

            IGraph g = LoadGraph(uri, false);
            if (g == null)
            {
                return false;
            }
            else
            {
                _store.Add(g, true);
                return true;
            }
        }

        private bool SetNamedUri(string uri)
        {
            _namedGraphs.Add(uri);
            return true;
        }

        private bool SetBaseUri(string uri)
        {
            try 
            {
                _parser.DefaultBaseUri = new Uri(uri);
                return true;
            } 
            catch (UriFormatException) 
            {
                Console.Error.WriteLine("Unable to use the Base URI '" + uri + "' as it is not a valid URI");
                return false;
            }
        }

        private bool SetDumpFormat(string format)
        {
            switch (format)
            {
                case "debug":
                case "structure":
                case "sparql":
                    _dump = true;
                    _dumpFormat = format;
                    return true;
                default:
                    Console.Error.WriteLine("rdfQuery: The value '" + format + "' is not a valid dump format");
                    return false;
            }
        }

        private IGraph LoadGraph(string uri, bool fromFile)
        {
            Graph g = new Graph();
            try
            {
                if (fromFile)
                {
                    FileLoader.Load(g, uri);
                }
                else
                {
                    Uri u = new Uri(uri);
                    if (u.IsAbsoluteUri)
                    {
                        UriLoader.Load(g, u);
                    }
                    else
                    {
                        FileLoader.Load(g, uri);
                    }
                }
                return g;
            }
            catch (UriFormatException)
            {
                //Try loading as a file as it's not a valid URI
                return LoadGraph(uri, true);
            }
            catch (RdfParseException parseEx)
            {
                Console.Error.WriteLine("rdfQuery: Parser Error: Unable to parse data from URI '" + uri + "' due to the following error:");
                Console.Error.WriteLine("rdfQuery: Parser Error: " + parseEx.Message);
            }
            catch (RdfException rdfEx)
            {
                Console.Error.WriteLine("rdfQuery: RDF Error: Unable to read data from URI '" + uri + "' due to the following error:");
                Console.Error.WriteLine("rdfQuery: RDF Error: " + rdfEx.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("rdfQuery: Error: Unable to read data from URI '" + uri + "' due to the following error:");
                Console.Error.WriteLine("rdfQuery: Error: " + ex.Message);
            }
            return null;
        }

        private void ShowUsage()
        {
            Console.WriteLine("rdfQuery Utility for dotNetRDF");
            Console.WriteLine("--------------------------------");
            Console.WriteLine();
            Console.WriteLine("Running in roqet compatibility mode (first argument was -roqet)");
            Console.WriteLine("Usage is rdfQuery -roqet ROQET_ARGS");
            Console.WriteLine("In roqet compatibility mode ROQET_ARGS must be replaced with arguments in the roqet command line format as described at http://librdf.org/rasqal/roqet.html");
            Console.WriteLine();
            Console.WriteLine("Supported Output Formats");
            Console.WriteLine("------------------------");
            Console.WriteLine();
            Console.WriteLine("The first set of formats provide output for either Result Sets/Graphs and the second set only provide Graph output");
            Console.WriteLine();
            Console.WriteLine("xml          SPARQL XML Results or RDF/XML");
            Console.WriteLine("json         SPARQL JSON Results or RDF/JSON Resource-Centric");
            Console.WriteLine("html         HTML Table of Results or HTML containing RDFa");
            Console.WriteLine("rdfa         HTML Table of Results or HTML containing RDFa");
            Console.WriteLine("csv          Comma Separated Values");
            Console.WriteLine("tsv          Tab Separated Values");
            Console.WriteLine();
            Console.WriteLine("ntriples     NTriples");
            Console.WriteLine("turtle       Turtle");
            Console.WriteLine("n3           Notation 3");
            Console.WriteLine("rdfxml       RDF/XML");
            Console.WriteLine();
            Console.WriteLine("Notes");
            Console.WriteLine("-----");
            Console.WriteLine();
            Console.WriteLine("1. The -f roqet option is currently ignored");
            Console.WriteLine("2. roqet compatibility mode copies the syntax (not the logic) of the Redland roqet tool so it does not make any guarantee that it behaves in the same way as roqet would given a particular input.");

        }
    }
}
