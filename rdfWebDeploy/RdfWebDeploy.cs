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
using System.Reflection;
using VDS.RDF.Configuration;

namespace VDS.RDF.Utilities.Web.Deploy
{
    public enum WebDeployMode
    {
        Deploy,
        Extract,
        Help
    }

    public static class RdfWebDeployHelper
    {
        public const String NamespacePrefixes = "PREFIX rdf: <" + NamespaceMapper.RDF + "> PREFIX rdfs: <" + NamespaceMapper.RDFS + "> PREFIX xsd: <" + NamespaceMapper.XMLSCHEMA + "> PREFIX fn: <" + Query.Expressions.XPathFunctionFactory.XPathFunctionsNamespace + "> PREFIX dnr: <" + ConfigurationLoader.ConfigurationNamespace + ">";

        private static readonly List<String> _requiredDLLs = new List<string>()
        {
            "dotNetRDF.dll",
            "HtmlAgilityPack.dll",
            "Newtonsoft.Json.dll"
        };

        private static readonly List<String> _virtuosoDLLs = new List<string>()
        {
            "dotNetRDF.Data.Virtuoso.dll",
            "OpenLink.Data.Virtuoso.dll"
        };

        private static readonly List<String> _fulltextDLLs = new List<string>()
        {
            "dotNetRDF.Query.FullText.dll",
            "Lucene.Net.dll"
        };

        public static IEnumerable<String> RequiredDLLs
        {
            get
            {
                return _requiredDLLs;
            }
        }

        public static IEnumerable<String> RequiredVirtuosoDLLs
        {
            get
            {
                return _virtuosoDLLs;
            }
        }

        public static IEnumerable<String> RequiredFullTextDLLs
        {
            get
            {
                return _fulltextDLLs;
            }
        }

        public static String ExecutablePath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }
    }

    public class RdfWebDeploy
    {
        public void RunDeployment(String[] args)
        {
            if (args.Length == 0)
            {
                this.ShowUsage();
            }
            else
            {
                switch (args[0])
                {
                    case "-deploy":
                        Deploy d = new Deploy();
                        d.RunDeploy(args);
                        break;
                    //case "-extract":
                    //    Extract e = new Extract();
                    //    e.RunExtract(args);
                    //    break;
                    case "-dllupdate":
                        DllUpdate du = new DllUpdate();
                        du.RunDllUpdate(args);
                        break;
                    case "-dllverify":
                        DllVerify dv = new DllVerify();
                        dv.Verify(args);
                        break;
                    case "-test":
                        Test t = new Test();
                        t.RunTest(args);
                        break;
                    case "-list":
                        List l = new List();
                        l.RunList(args);
                        break;
                    case "-vocab":
                        Vocab v = new Vocab();
                        v.RunVocab(args);
                        break;
                    case "-help":
                        this.ShowUsage();
                        break;
                    case "-xmldeploy":
                        XmlDeploy x = new XmlDeploy();
                        x.RunXmlDeploy(args);
                        break;
                    default:
                        this.ShowUsage();
                        break;
                }
            }
        }

        private void ShowUsage()
        {
            Console.WriteLine("rdfWebDeploy Utility for dotNetRDF");
            Console.WriteLine("--------------------------------");
            Console.WriteLine();
            Console.WriteLine("Command usage is as follows:");
            Console.WriteLine("rdfWebDeploy mode [options]");
            Console.WriteLine();
            Console.WriteLine("e.g. rdfWebDeploy -deploy /demos config.ttl");
            //Console.WriteLine("e.g. rdfWebDeploy -extract /demos config.ttl");
            Console.WriteLine("e.g. rdfWebDeploy -dllverify /demos");
            Console.WriteLine();
            Console.WriteLine("Notes");
            Console.WriteLine("-----");
            Console.WriteLine();
            Console.WriteLine("All modes which support the webapp parameter specify it as the virtual path for the parameter on your local IIS instance, if you don't have a local IIS instance specify a path to the root directory of your web application and specify the -noiis option as an additional command line argument");
            Console.WriteLine();
            Console.WriteLine("Supported Modes");
            Console.WriteLine("-----------------");
            Console.WriteLine();
            Console.WriteLine("-deploy webapp config.ttl [options]");
            Console.WriteLine("Automatically deploys the given configuration file to the given web applications by setting up it's Web.Config file appropriately and deploying necessary DLLs.");
            Console.WriteLine();
            Console.WriteLine("-dllupdate webapp [options]");
            Console.WriteLine("Updates all the required DLLs in the applications bin directory to the versions in the toolkits directory.");
            Console.WriteLine();
            Console.WriteLine("-dllverify webapp [options]");
            Console.WriteLine("Verifies whether the required DLLs are present in the applications bin directory");
            Console.WriteLine();
            //Console.WriteLine("-extract webapp config.ttl [options]");
            //Console.WriteLine(" Generates an outline configuration file based on the given web applications Web.Config file.  Extraction will not overwrite any existing files");
            //Console.WriteLine();
            Console.WriteLine("-help");
            Console.WriteLine("Shows this usage guide");
            Console.WriteLine();
            Console.WriteLine("-list config.ttl");
            Console.WriteLine("Lists the Handlers in the given configuration file");
            Console.WriteLine();
            Console.WriteLine("-test config.ttl");
            Console.WriteLine("Tests whether a configuration file parses and makes various tests for validity");
            Console.WriteLine();
            Console.WriteLine("-vocab file.ttl");
            Console.WriteLine("Outputs the Configuration Vocabulary to the given file for use as a reference");
            Console.WriteLine();
            Console.WriteLine("-xmldeploy web.config config.ttl [options]");
            Console.WriteLine("Automatically deploys the given configuration file to the given web applications by setting up it's Web.Config file appropriately and deploying necessary DLLs");
            Console.WriteLine();
            Console.WriteLine("Supported Options");
            Console.WriteLine("-----------------");
            Console.WriteLine();
            Console.WriteLine("-fulltext");
            Console.WriteLine("Includes Query.FullText related DLLs.  Supported by any more that deploys DLLs");
            Console.WriteLine();
            Console.WriteLine("-negotiate");
            Console.WriteLine("If specificied then the Negotiate by File Extension Module will be registered. Used by -deploy and -xmldeploy");
            Console.WriteLine();
            Console.WriteLine("-nointreg");
            Console.WriteLine("If specified then Handlers and Modules will not be registered for IIS Integrated Mode.  Used by -deploy and -xmldeploy");
            Console.WriteLine();
            Console.WriteLine("-noclassicreg");
            Console.WriteLine("If specified then Handlers and Modules will not be registered for IIS Classic Mode.  Used by -deploy and -xmldeploy");
            Console.WriteLine();
            Console.WriteLine("-noiis");
            Console.WriteLine("If specified indicates that there is not a local IIS instance available or you wish to deploy to a web application which is not associated with your local IIS instance.  Essentially forces -deploy mode to switch to -xmldeploy mode.  Supported by all modes that take the webapp parameter");
            Console.WriteLine();
            Console.WriteLine("-site \"Site Name\"");
            Console.WriteLine("Specifies the IIS site in which the web application resides.  Supported by all modes that take the webapp parameter");
            Console.WriteLine();
            Console.WriteLine("-virtuoso");
            Console.WriteLine("Includes Data.Virtuoso related DLLs.  Supported by any mode that deploys DLLs");
        }
    }
}
