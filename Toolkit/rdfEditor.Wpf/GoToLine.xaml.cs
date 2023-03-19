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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for GoToLine.xaml
    /// </summary>
    public partial class GoToLine : Window
    {
        private int _line, _maxLine;

        public GoToLine(ITextEditorAdaptor<TextEditor> editor)
        {
            InitializeComponent();

            _line = editor.Control.Document.GetLineByOffset(editor.CaretOffset).LineNumber;
            _maxLine = editor.Control.Document.LineCount;
            txtLineNumber.Text = _line.ToString();
            lblLineNumber.Content = string.Format((string)lblLineNumber.Content, _maxLine);

            txtLineNumber.SelectAll();
            txtLineNumber.Focus();
        }

        public int Line
        {
            get
            {
                return _line;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtLineNumber.Text, out _line))
            {
                if (_line > 0 && _line <= _maxLine)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Line Number is not in the range 1-" + _maxLine, "Invalid Line Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Not a valid Line Number!", "Invalid Line Number", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
