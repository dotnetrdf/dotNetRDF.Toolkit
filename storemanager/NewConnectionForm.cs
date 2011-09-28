﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using VDS.RDF.Storage;
using VDS.RDF.Utilities.StoreManager.Connections;

namespace VDS.RDF.Utilities.StoreManager
{
    public partial class NewConnectionForm 
        : Form
    {
        private List<IConnectionDefinition> _definitions = new List<IConnectionDefinition>();
        private IGenericIOManager _connection;

        public NewConnectionForm()
        {
            InitializeComponent();

            this._definitions.AddRange(ConnectionDefinitionManager.GetDefinitions().OrderBy(d => d.StoreName));
            this.lstStoreTypes.DataSource = this._definitions;
            this.lstStoreTypes.DisplayMember = "StoreName";
        }

        public IGenericIOManager Connection
        {
            get
            {
                return this._connection;
            }
        }

        private void lstStoreTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            IConnectionDefinition def = this.lstStoreTypes.SelectedItem as IConnectionDefinition;
            if (def != null)
            {
                this.lblDescrip.Text = def.StoreDescription;

                int i = 0;
                tblSettings.Controls.Clear();
                foreach (KeyValuePair<PropertyInfo, ConnectionAttribute> setting in def.OrderBy(s => s.Value.DisplayOrder))
                {
                    if (setting.Value.Type != ConnectionSettingType.Boolean)
                    {
                        Label label = new Label();
                        label.Text = setting.Value.DisplayName + ":";
                        label.TextAlign = ContentAlignment.MiddleLeft;
                        tblSettings.Controls.Add(label, 0, i);
                    }

                    switch (setting.Value.Type)
                    {
                        case ConnectionSettingType.String:
                        case ConnectionSettingType.Password:
                            //String/Password so show a Textbox
                            TextBox box = new TextBox();
                            String s = (String)setting.Key.GetValue(def, null);
                            box.Text = (s != null) ? s : String.Empty;
                            box.Width = 225;
                            box.Tag = setting.Key;
                            if (setting.Value.Type == ConnectionSettingType.Password) box.PasswordChar = '*';

                            //Add the Event Handler which updates the Definition as the user types
                            box.TextChanged += new EventHandler((_sender, args) =>
                                {
                                    (box.Tag as PropertyInfo).SetValue(def, box.Text, null);
                                });

                            //Show DisplaySuffix if relevant
                            if (!String.IsNullOrEmpty(setting.Value.DisplaySuffix))
                            {
                                FlowLayoutPanel flow = new FlowLayoutPanel();
                                flow.Margin = new Padding(0);
                                flow.Controls.Add(box);
                                Label suffix = new Label();
                                suffix.Text = setting.Value.DisplaySuffix;
                                suffix.AutoSize = true;
                                suffix.TextAlign = ContentAlignment.MiddleLeft;
                                flow.Controls.Add(suffix);
                                flow.WrapContents = false;
                                flow.AutoSize = true;
                                flow.AutoScroll = false;
                                tblSettings.Controls.Add(flow, 1, i);
                            }
                            else
                            {
                                tblSettings.Controls.Add(box, 1, i);
                            }
                            break;

                        case ConnectionSettingType.Boolean:
                            //Boolean so show a Checkbox
                            CheckBox check = new CheckBox();
                            check.AutoSize = true;
                            check.TextAlign = ContentAlignment.MiddleLeft;
                            check.CheckAlign = ContentAlignment.MiddleLeft;
                            check.Checked = (bool)setting.Key.GetValue(def, null);
                            check.Text = setting.Value.DisplayName;
                            check.Tag = setting.Key;

                            //Add the Event Handler which updates the Definition when the Checkbox changes
                            check.CheckedChanged += new EventHandler((_sender, args) =>
                                {
                                    (check.Tag as PropertyInfo).SetValue(def, check.Checked, null);
                                });

                            this.tblSettings.SetColumnSpan(check, 2);
                            this.tblSettings.Controls.Add(check, 0, i);
                            break;

                        case ConnectionSettingType.Integer:
                            //Integer so show a Numeric Up/Down control
                            NumericUpDown num = new NumericUpDown();
                            num.ThousandsSeparator = true;
                            num.DecimalPlaces = 0;
                            int val = (int)setting.Key.GetValue(def, null);
                            if (setting.Value.IsValueRestricted)
                            {
                                num.Minimum = setting.Value.MinValue;
                                num.Maximum = setting.Value.MaxValue;
                            }
                            else
                            {
                                num.Minimum = Int32.MinValue;
                                num.Maximum = Int32.MaxValue;
                            }
                            num.Value = val;
                            num.Tag = setting.Key;

                            //Add the Event Handler which updates the Definition as the number changes
                            num.ValueChanged += new EventHandler((_sender, args) =>
                                {
                                    (num.Tag as PropertyInfo).SetValue(def, (int)num.Value, null);
                                });

                            tblSettings.Controls.Add(num, 1, i);
                            break;
                    }

                    i++;
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            IConnectionDefinition def = this.lstStoreTypes.SelectedItem as IConnectionDefinition;
            if (def == null) return;
            try
            {
                this._connection = def.OpenConnection();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connection to " + def.StoreName + " Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
