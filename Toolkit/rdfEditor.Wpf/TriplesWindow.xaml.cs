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
using VDS.RDF;
using VDS.RDF.Writing.Formatting;
using VDS.RDF.Utilities.Editor.Wpf.Controls;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for TriplesWindow.xaml
    /// </summary>
    public partial class TriplesWindow : Window
    {
        private INodeFormatter _formatter;
        private IGraph _g;
        private Grid _grid;

        public TriplesWindow(IGraph g, INodeFormatter formatter)
        {
            InitializeComponent();
            _formatter = formatter;
            _g = g;
            _grid = gridTriples;

            RenderTriples();
        }

        public TriplesWindow(IGraph g)
            : this(g, new NTriplesFormatter()) { }


        private void RenderTriples()
        {
            if (_grid == null) return;
            if (_grid.RowDefinitions.Count > 1)
            {
                _grid.RowDefinitions.RemoveRange(1, _grid.RowDefinitions.Count - 1);
                _grid.Children.RemoveRange(5, _grid.Children.Count - 5);
            }

            int row = 1;
            foreach (Triple t in _g.Triples)
            {
                RowDefinition def = new RowDefinition();
                def.Height = GridLength.Auto;
                _grid.RowDefinitions.Add(def);

                Control s = RenderNode(t.Subject);
                _grid.Children.Add(s);
                Grid.SetColumn(s, 0);
                Grid.SetRow(s, row);

                Control p = RenderNode(t.Predicate);
                _grid.Children.Add(p);
                Grid.SetColumn(p, 2);
                Grid.SetRow(p, row);

                Control o = RenderNode(t.Object);
                _grid.Children.Add(o);
                Grid.SetColumn(o, 4);
                Grid.SetRow(o, row);
                row++;

                _grid.RowDefinitions.Add(new RowDefinition());
                GridSplitter rowSplitter = new GridSplitter();
                rowSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                rowSplitter.Height = 1;
                rowSplitter.ResizeDirection = GridResizeDirection.Rows;
                rowSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                rowSplitter.Background = Brushes.Black;
                rowSplitter.Foreground = Brushes.Black;
                Grid.SetColumn(rowSplitter, 0);
                Grid.SetRow(rowSplitter, row);
                Grid.SetColumnSpan(rowSplitter, 5);
                _grid.Children.Add(rowSplitter);
                row++;
            }

            Grid.SetRowSpan(split1, row + 1);
            Grid.SetRowSpan(split2, row + 1);

            _grid.InvalidateVisual();
            ((ScrollViewer)FindName("scroll")).InvalidateScrollInfo();
        }

        private Control RenderNode(INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Blank:
                    Label bnode = new Label();
                    bnode.Content = _formatter.Format(n);
                    bnode.Padding = new Thickness(2);
                    return bnode;

                case NodeType.GraphLiteral:
                    Label glit = new Label();
                    glit.Content = "{Graph Literals cannot be shown in this Viewer}";
                    glit.Padding = new Thickness(2);
                    return glit;

                case NodeType.Literal:
                    return new LiteralNodeControl((ILiteralNode)n, _formatter);

                case NodeType.Uri:
                    return new UriNodeControl((IUriNode)n, _formatter);

                default:
                    Label unknown = new Label();
                    unknown.Content = "{Unknown Node Types cannot be shown in this Viewer}";
                    unknown.Padding = new Thickness(2);
                    return unknown;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboFormatter.SelectedIndex > -1)
            {
                switch (cboFormatter.SelectedIndex)
                {
                    case 0:
                        _formatter = new NTriplesFormatter();
                        break;
                    case 1:
                        _formatter = new TurtleFormatter(_g);
                        break;
                    case 2:
                        _formatter = new UncompressedTurtleFormatter();
                        break;
                    case 3:
                        _formatter = new Notation3Formatter(_g);
                        break;
                    case 4:
                        _formatter = new UncompressedNotation3Formatter();
                        break;
                    case 5:
                        _formatter = new CsvFormatter();
                        break;
                    case 6:
                        _formatter = new TsvFormatter();
                        break;
                }

                RenderTriples();
            }
        }
    }
}
