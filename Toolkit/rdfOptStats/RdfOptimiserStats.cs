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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing;

namespace VDS.RDF.Utilities.OptimiserStats
{
    public class RdfOptimiserStats
    {
        private string[] _args;
        private bool _subjects = false, _predicates = false, _objects = false, _nodes = false;
        private string _file = "stats.ttl";
        private List<string> _inputs = new List<string>();
        private bool _literals = false;

        public RdfOptimiserStats(string[] args)
        {
            _args = args;
        }

        public void Run()
        {
            if (_args.Length == 0)
            {
                ShowUsage();
            }
            else
            {
                if (!ParseOptions())
                {
                    Console.Error.WriteLine("rdfOptStats: Error: One/More options were invalid");
                    return;
                }

                if (_inputs.Count == 0)
                {
                    Console.Error.WriteLine("rdfOptStats: Error: No Inputs Specified");
                    return;
                }

                List<BaseStatsHandler> handlers = new List<BaseStatsHandler>();
                if (_subjects && _predicates && _objects)
                {
                    handlers.Add(new SPOStatsHandler(_literals));
                }
                else if (_subjects && _predicates)
                {
                    handlers.Add(new SPStatsHandler(_literals));
                }
                else
                {
                    if (_subjects) handlers.Add(new SubjectStatsHandler(_literals));
                    if (_predicates) handlers.Add(new PredicateStatsHandler(_literals));
                    if (_objects) handlers.Add(new ObjectStatsHandler(_literals));
                }
                if (_nodes)
                {
                    handlers.Add(new NodeStatsHandler());
                }

                bool ok = true;
                IRdfHandler handler;
                if (handlers.Count == 1)
                {
                    handler = handlers[0];
                }
                else
                {
                    handler = new MultiHandler(handlers.OfType<IRdfHandler>());
                }

                Stopwatch timer = new Stopwatch();
                timer.Start();
                for (int i = 0; i < _inputs.Count; i++)
                {
                    Console.WriteLine("rdfOptStats: Processing Input " + (i + 1) + " of " + _inputs.Count + " - '" + _inputs[i] + "'");

                    try
                    {
                        FileLoader.Load(handler, _inputs[i]);
                    }
                    catch (RdfParserSelectionException)
                    {
                        ok = false;
                        Console.Error.WriteLine("rdfOptStats: Error: Unable to select a Parser to read input");
                        break;
                    }
                    catch (RdfParseException)
                    {
                        ok = false;
                        Console.Error.WriteLine("rdfOptStats: Error: Parsing Error while reading input");
                        break;
                    }
                    catch (RdfException)
                    {
                        ok = false;
                        Console.Error.WriteLine("rdfOptStats: Error: RDF Error while reading input");
                        break;
                    }
                    catch (Exception)
                    {
                        ok = false;
                        Console.Error.WriteLine("rdfOptStats: Error: Unexpected Error while reading input");
                        break;
                    }
                }
                Console.WriteLine("rdfOptStats: Finished Processing Inputs");
                timer.Stop();
                Console.WriteLine("rdfOptStats: Took " + timer.Elapsed + " to process inputs");
                timer.Reset();

                if (ok)
                {
                    //Output the Stats
                    timer.Start();
                    Graph g = new Graph();
                    g.NamespaceMap.Import(handlers.First().Namespaces);
                    try
                    {
                        foreach (BaseStatsHandler h in handlers)
                        {
                            h.GetStats(g);
                        }
                        IRdfWriter writer = MimeTypesHelper.GetWriterByFileExtension(MimeTypesHelper.GetTrueFileExtension(_file));
                        if (writer is ICompressingWriter compressingWriter)
                        {
                            compressingWriter.CompressionLevel = WriterCompressionLevel.High;
                        }
                        if (writer is IHighSpeedWriter highSpeedWriter)
                        {
                            highSpeedWriter.HighSpeedModePermitted = false;
                        }
                        writer.Save(g, _file);

                        Console.WriteLine("rdfOptStats: Statistics output to " + _file);
                        timer.Stop();
                        Console.WriteLine("rdfOptStats: Took " + timer.Elapsed + " to output statistics");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("rdfOptStats: Error: Unexpected error outputting statistics to " + _file);
                        Console.Error.WriteLine(ex.Message);
                        Console.Error.WriteLine(ex.StackTrace);
                    }
                }
                else
                {
                    Console.Error.WriteLine("rdfOptStats: Error: Unable to output statistics due to errors during input processing");
                }
            }
        }

        private bool ParseOptions()
        {
            bool ok = true;

            for (int i = 0; i < _args.Length; i++)
            {
                string arg = _args[i];
                switch (arg)
                {
                    case "-all":
                        _subjects = true;
                        _predicates = true;
                        _objects = true;
                        break;
                    case "-s":
                        _subjects = true;
                        break;
                    case "-p":
                        _predicates = true;
                        break;
                    case "-o":
                        _objects = true;
                        break;
                    case "-nodes":
                        _nodes = true;
                        break;

                    case "-literals":
                        _literals = true;
                        break;

                    case "-output":
                        if (i < _args.Length - 1)
                        {
                            _file = _args[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine("rdfOptStats: Error: -output option should be followed by a filename");
                            ok = false;
                        }
                        break;

                    default:
                        ok = ParseInputs(arg);
                        break;
                }

                if (!ok) break;
            }

            return ok;
        }

        private bool ParseInputs(string arg)
        {
            if (arg.Contains("*"))
            {
                int count = arg.ToCharArray().Count(c => c == '*');
                if (count > 2 || (count == 2 && !arg.EndsWith("*.*")))
                {
                    Console.Error.WriteLine("rdfOptStats: Error: Input Wildcard '" + arg + "' does not appear to be a valid wildcard");
                    return false;
                }

                string sep = new string(new char[] { Path.DirectorySeparatorChar });
                if (arg.Contains(sep))
                {
                    //Wildcard has a Directory in it
                    string dir = arg.Substring(0, arg.LastIndexOf(sep) + 1);
                    if (!Directory.Exists(dir))
                    {
                        Console.Error.WriteLine("rdfOptStats: Error: Input Wildcard '" + arg + "' uses a Directory '" + dir + "' which does not exist");
                        return false;
                    }

                    string wildcard = arg.Substring(dir.Length);
                    return ParseInputs(wildcard, dir);
                }
                else
                {
                    return ParseInputs(arg, Environment.CurrentDirectory);
                }
            }
            else
            {
                if (!File.Exists(arg))
                {
                    Console.Error.WriteLine("rdfOptStats: Error: Input File '" + arg + "' does not exist");
                    return false;
                }
                else
                {
                    _inputs.Add(arg);
                    return true;
                }
            }
        }

        private bool ParseInputs(string arg, string dir)
        {
            if (arg.Contains("*"))
            {
                //Wildcard is a File wildcard only
                if (arg.Equals("*") || arg.Equals("*.*"))
                {
                    //All Files
                    _inputs.AddRange(Directory.GetFiles(dir));
                    return true;
                }
                else if (arg.Contains("*."))
                {
                    //All Files with a given Extension
                    string ext = arg.Substring(arg.LastIndexOf("*.") + 1);
                    _inputs.AddRange(Directory.GetFiles(dir).Where(f => ext.Equals(MimeTypesHelper.GetTrueFileExtension(f))));
                    return true;
                }
                else
                {
                    //Invalid File Wildcard
                    Console.Error.WriteLine("rdfOptStats: Error: Input Wildcard '" + arg + "' does not appear to be a valid wildcard - only simple wildcards like *.* or *.rdf are currently supported");
                    return false;
                }
            } 
            else
            {
                if (!File.Exists(arg))
                {
                    Console.Error.WriteLine("rdfOptStats: Error: Input File '" + arg + "' does not exist");
                    return false;
                }
                else
                {
                    _inputs.Add(arg);
                    return true;
                }
            }
        }

        private void ShowUsage()
        {
            Console.WriteLine("rdfOptStats");
            Console.WriteLine("-----------");
            Console.WriteLine();
            Console.WriteLine("Command usage is as follows:");
            Console.WriteLine("rdfOptStats [options] input1 [input2 [input3 ...]]");
            Console.WriteLine();
            Console.WriteLine("e.g. rdfOptStats -all -output stats.ttl data1.rdf data2.rdf");
            Console.WriteLine("e.g. rdfOptStats -all -output stats.nt data\\*");
            Console.WriteLine();
            Console.WriteLine("Notes");
            Console.WriteLine("-----");
            Console.WriteLine("Only simple wildcard patterns are supported as inputs e.g.");
            Console.WriteLine("data\\*");
            Console.WriteLine("some\\path\\*.rdf");
            Console.WriteLine("*.*");
            Console.WriteLine("*.ttl");
            Console.WriteLine();
            Console.WriteLine("Any other wildcard pattern will be rejected");
            Console.WriteLine();
            Console.WriteLine("Supported Options");
            Console.WriteLine("-----------------");
            Console.WriteLine();
            Console.WriteLine(" -all");
            Console.WriteLine("  Specifies that counts of Subjects, Predicates and Objects should be generated");
            Console.WriteLine();
            Console.WriteLine(" -literals");
            Console.WriteLine("  Specifies that counts should include Literals (default is URIs only) - this requires an output format that supports Literal Subjects e.g. N3");
            Console.WriteLine();
            Console.WriteLine(" -nodes");
            Console.WriteLine("  Specifies that aggregated for Nodes should be generated i.e. counts that don't specify which position the URI/Literal occurs in");
            Console.WriteLine();
            Console.WriteLine(" -p");
            Console.WriteLine("  Specifies that counts for Predicates should be generated");
            Console.WriteLine();
            Console.WriteLine(" -o");
            Console.WriteLine("  Specifies that counts for Objects should be generated");
            Console.WriteLine();
            Console.WriteLine(" -output file");
            Console.WriteLine("  Specifies the file to output the statistics to");
            Console.WriteLine();
            Console.WriteLine(" -s");
            Console.WriteLine("  Specifies that counts for Subjects should be generated");
            Console.WriteLine();
        }
    }
}
