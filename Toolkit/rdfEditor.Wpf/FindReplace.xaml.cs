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
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for FindReplace.xaml
    /// </summary>
    public partial class FindReplace : Window
    {
        private FindReplaceMode _mode = FindReplaceMode.Find;
        private readonly WpfFindAndReplace _engine = new WpfFindAndReplace();

        public FindReplace()
        {
            InitializeComponent();
            GotFocus += new RoutedEventHandler(FindReplace_GotFocus);
        }

        void FindReplace_GotFocus(object sender, RoutedEventArgs e)
        {
            Window_GotFocus(sender, e);
        }

        public FindReplaceMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
                if (_mode == FindReplaceMode.Find)
                {
                    ToggleReplaceVisibility(Visibility.Collapsed);
                }
                else
                {
                    ToggleReplaceVisibility(Visibility.Visible);
                }
            }
        }

        public ITextEditorAdaptor<TextEditor> Editor
        {
            get;
            set;
        }

        public void FindNext()
        {
            _engine.Find(Editor);
        }

        private void ToggleReplaceVisibility(Visibility v)
        {
            lblReplace.Visibility = v;
            cboReplace.Visibility = v;
            btnReplace.Visibility = v;
            btnReplaceAll.Visibility = v;

            switch (v)
            {
                case Visibility.Collapsed:
                case Visibility.Hidden:
                    Title = "Find";
                    break;
                default:
                    Title = "Find and Replace";
                    break;
            }
            stkDialog.UpdateLayout();
        }

        private void btnFindNext_Click(object sender, RoutedEventArgs e)
        {
            _engine.FindText = cboFind.Text;
            _engine.Find(Editor);
        }

        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == FindReplaceMode.Find) return;
            _engine.FindText = cboFind.Text;
            _engine.ReplaceText = cboReplace.Text;
            _engine.Replace(Editor);
        }

        private void btnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == FindReplaceMode.Find) return;
            _engine.FindText = cboFind.Text;
            _engine.ReplaceText = cboReplace.Text;
            _engine.ReplaceAll(Editor);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            e.Cancel = true;
        }

        private void btnReplace_GotFocus(object sender, RoutedEventArgs e)
        {
            if (btnReplace.Visibility != Visibility.Visible)
            {
                cboFind.Focus();
            }
        }

        private void btnReplaceAll_GotFocus(object sender, RoutedEventArgs e)
        {
            if (btnReplaceAll.Visibility != Visibility.Visible)
            {
                cboFind.Focus();
            }
        }

        private void cboReplace_GotFocus(object sender, RoutedEventArgs e)
        {
            if (cboReplace.Visibility != Visibility.Visible)
            {
                chkMatchCase.Focus();
            }
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is EditorWindow)
            {
                cboFind.Focus();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cboFind.Focus();
        }

        private void cboLookIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tag = ((ComboBoxItem)cboLookIn.SelectedItem).Tag as string;
            if (tag == null)
            {
                _engine.Scope = FindAndReplaceScope.CurrentDocument;
            }
            else
            {
                switch (tag)
                {
                    case "Selection":
                        _engine.Scope = FindAndReplaceScope.Selection;
                        break;

                    case "Current Document":
                    default:
                        _engine.Scope = FindAndReplaceScope.CurrentDocument;
                        break;
                }
            }
        }

        private void chkMatchCase_Click(object sender, RoutedEventArgs e)
        {
            _engine.MatchCase = (chkMatchCase.IsChecked == true);
        }

        private void chkMatchWholeWord_Click(object sender, RoutedEventArgs e)
        {
            _engine.MatchWholeWord = (chkMatchWholeWord.IsChecked == true);
        }

        private void chkSearchUp_Click(object sender, RoutedEventArgs e)
        {
            _engine.SearchUp = (chkSearchUp.IsChecked == true);
        }

        private void chkRegex_Click(object sender, RoutedEventArgs e)
        {
            _engine.UseRegex = (chkRegex.IsChecked == true);
        }
    }
}
