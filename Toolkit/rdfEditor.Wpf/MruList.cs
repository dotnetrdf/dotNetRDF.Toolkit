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
using System.Text;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    public class MruList
    {
        private List<string> _files = new List<string>();
        private string _file;
        private int _size;

        public const int DefaultSize = 9;
        public const int DefaultShortFilenameLength = 50;

        public MruList(string file, int size)
        {
            if (file == null) throw new ArgumentNullException("file");
            _file = file;
            _size = Math.Max(1, size);
            Load();
        }

        public MruList(string file)
            : this(file, DefaultSize) { }

        public IEnumerable<string> Files
        {
            get
            {
                return (from i in Enumerable.Range(0, _files.Count).Reverse()
                        select _files[i]);
            }
        }

        public void Load()
        {
            if (File.Exists(_file))
            {
                using (StreamReader reader = new StreamReader(_file))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.Equals(string.Empty)) continue;
                        if (File.Exists(line))
                        {
                            _files.Add(line);
                        }
                    }
                    reader.Close();
                }

                while (_files.Count > _size)
                {
                    _files.RemoveAt(0);
                }
            }
        }

        public void Add(string file)
        {
            _files.Remove(file);
            _files.Add(file);
            Save();
        }

        public void Remove(string file)
        {
            _files.Remove(file);
            Save();
        }

        public void Save()
        {
            while (_files.Count > _size)
            {
                _files.RemoveAt(0);
            }

            using (StreamWriter writer = new StreamWriter(_file))
            {
                foreach (string file in _files)
                {
                    writer.WriteLine(file);
                }
                writer.Close();
            }
        }

        public void Clear()
        {
            _files.Clear();
            Save();
        }

        public static string ShortenFilename(string file)
        {
            return ShortenFilename(file, DefaultShortFilenameLength);
        }

        public static string ShortenFilename(string file, int maxLength)
        {
            if (file.Length <= maxLength)
            {
                return file;
            }
            else
            {
                StringBuilder shortFile = new StringBuilder();
                string filename = Path.GetFileName(file);

                string sepChar = new string(new char[] { Path.DirectorySeparatorChar });
                string path = file.Substring(0, file.Length - filename.Length);
                if (path.Length == 0) return filename;
                string[] dirs = path.Split(new char[] { Path.DirectorySeparatorChar });

                int i = -1;
                while (i < dirs.Length && shortFile.Length + filename.Length < maxLength)
                {
                    if (shortFile.Length + filename.Length + dirs[i+1].Length >= maxLength) break;
                    i++;
                    shortFile.Append(dirs[i]);
                    shortFile.Append(sepChar);
                }
                if (i < dirs.Length - 1)
                {
                    shortFile.Append("...");
                    shortFile.Append(sepChar);
                }
                shortFile.Append(filename);
                return shortFile.ToString();
            }
        }
    }
}
