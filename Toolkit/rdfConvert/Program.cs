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
using System.Linq;
using System.Net;
using System.Text;

//REQ: Add command line arguments to import custom parsers and serializers

namespace VDS.RDF.Utilities.Convert
{
    class Program
    {
        static void Main(string[] args)
        {
            //Disable URI Interning as this will otherwise cause us to use way too much
            //memory when doing large streaming conversions
            UriFactory.InternUris = false;

            //Set Console Output Encoding to UTF-8 with no BOM
            Console.OutputEncoding = new UTF8Encoding(false);

            // Set use of TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (args.Length >= 1 && args[0].Equals("-rapper"))
            {
                RapperConvert rapper = new RapperConvert();
                rapper.RunConvert(args.Skip(1).ToArray());
            }
            else
            {
                RdfConvert converter = new RdfConvert();
                converter.RunConvert(args);
            }
        }
    }
}
