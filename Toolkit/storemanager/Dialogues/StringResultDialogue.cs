/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-develop@lists.sf.net)

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
using System.Windows.Forms;

namespace VDS.RDF.Utilities.StoreManager.Dialogues
{
    public partial class StringResultDialogue : Form
    {
        private readonly Control _targetControl;

        public StringResultDialogue(string title, string result, Control control, string controlDescription)
        {
            InitializeComponent();
            Text = string.Format(Text, title);
            txtResult.Text = result;
            _targetControl = control;
            btnCopyToControl.Text = string.Format(btnCopyToControl.Text, controlDescription);
            btnCopyToControl.Enabled = _targetControl != null;
        }

        public StringResultDialogue(string title, string result)
            : this(title, result, null, "Editor") { }

        private void btnCopyToControl_Click(object sender, EventArgs e)
        {
            if (_targetControl == null) return;
            _targetControl.Text = txtResult.Text;
            Close();
        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtResult.Text, TextDataFormat.Text);
            Close();
        }
    }
}
