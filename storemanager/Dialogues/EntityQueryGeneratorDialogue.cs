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
using VDS.RDF.Utilities.StoreManager.Properties;

namespace VDS.RDF.Utilities.StoreManager.Dialogues
{
    public partial class EntityQueryGeneratorDialogue : Form
    {
        public EntityQueryGeneratorDialogue(String query)
        {
            InitializeComponent();
            this.txtCurrentQuery.Text = query.Replace("\n", "\r\n");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.MinPredicateUsageLimit = (int) this.numValuesPerPredicateLimit.Value;
            this.QueryString = this.txtCurrentQuery.Text;
            this.Close();
        }

        /// <summary>
        /// Gets the minimum number of times a predicate must be used to be included in the generated query
        /// </summary>
        public int MinPredicateUsageLimit { get; private set; }

        /// <summary>
        /// Gets the Query String the user entered
        /// </summary>
        public String QueryString { get; private set; }
    }
}
