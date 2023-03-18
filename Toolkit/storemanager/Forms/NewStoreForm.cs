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
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using VDS.RDF.Storage.Management;
using VDS.RDF.Storage.Management.Provisioning;

namespace VDS.RDF.Utilities.StoreManager.Forms
{
    public partial class NewStoreForm : Form
    {
        private readonly IStorageServer _server;
        private readonly IStoreTemplate _defaultTemplate;
        private readonly BindingList<IStoreTemplate> _templates = new BindingList<IStoreTemplate>();

        public NewStoreForm(IStorageServer server)
        {
            _server = server;
            InitializeComponent();

            //Generate Templates
            _defaultTemplate = _server.GetDefaultTemplate(string.Empty);
            foreach (IStoreTemplate template in _server.GetAvailableTemplates(string.Empty))
            {
                _templates.Add(template);
            }
            cboTemplates.DataSource = _templates;
            radTemplateDefault.Text = string.Format(radTemplateDefault.Text, _defaultTemplate);

            //Wire up property grid
            propConfig.SelectedObjectsChanged += propConfig_SelectedObjectsChanged;
            propConfig.PropertyValueChanged += propConfig_PropertyValueChanged;

        }

        /// <summary>
        /// Gets/Sets the template
        /// </summary>
        public IStoreTemplate Template
        {
            get;
            set;
        }

        private void txtStoreID_TextChanged(object sender, EventArgs e)
        {
            bool enabled = !txtStoreID.Text.Equals(string.Empty);
            grpTemplates.Enabled = enabled;
            grpConfig.Enabled = enabled;

            _defaultTemplate.ID = txtStoreID.Text;
            foreach (IStoreTemplate t in _templates)
            {
                t.ID = txtStoreID.Text;
            }

            if (enabled)
            {
                //Update Property Grid
                propConfig.SelectedObject = radTemplateDefault.Checked ? _defaultTemplate : cboTemplates.SelectedItem;
                propConfig.Refresh();
            }
            else
            {
                //Clear Templates
                _templates.Clear();
                propConfig.SelectedObject = null;
            }
        }

        private void radTemplateDefault_CheckedChanged(object sender, EventArgs e)
        {
            if (radTemplateDefault.Checked)
            {
                propConfig.SelectedObject = _defaultTemplate;
            }
        }

        private void radTemplateSelected_CheckedChanged(object sender, EventArgs e)
        {
            cboTemplates.Enabled = radTemplateSelected.Checked;
            if (radTemplateSelected.Checked)
            {
                propConfig.SelectedObject = cboTemplates.SelectedItem;
            }
        }

        private void cboTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (radTemplateSelected.Checked)
            {
                propConfig.SelectedObject = cboTemplates.SelectedItem;
            }
        }


        void propConfig_SelectedObjectsChanged(object sender, EventArgs e)
        {
            btnCreate.Enabled = propConfig.SelectedObject != null;
        }

        void propConfig_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (e.ChangedItem.Label == null || !e.ChangedItem.Label.Equals("ID")) return;
            string id = e.ChangedItem.Value.ToString();
            if (!id.Equals(txtStoreID.Text))
            {
                txtStoreID.Text = id;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            Template = propConfig.SelectedObject as IStoreTemplate;

            List<string> errors = Template != null ? Template.Validate().ToList() : new List<string> { "No template selected"};
            if (errors.Count > 0)
            {
                InvalidTemplateForm invalid = new InvalidTemplateForm(errors);
                invalid.ShowDialog();
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
