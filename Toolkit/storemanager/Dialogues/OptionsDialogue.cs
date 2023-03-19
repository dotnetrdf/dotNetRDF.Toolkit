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
    public partial class OptionsDialogue : Form
    {
        public OptionsDialogue()
        {
            InitializeComponent();
            LoadOptions();
        }

        private void LoadOptions()
        {
            chkAlwaysEdit.Checked = Settings.Default.AlwaysEdit;
            chkAlwaysRestoreActiveConnections.Checked = Settings.Default.AlwaysRestoreActiveConnections;
            chkEditorDetectUrls.Checked = Settings.Default.EditorDetectUrls;
            chkEditorHighlighting.Checked = Settings.Default.EditorHighlighting;
            chkEditorWordWrap.Checked = Settings.Default.EditorWordWrap;
            chkPromptRestoreActiveConnections.Checked = Settings.Default.PromptRestoreActiveConnections;
            chkShowStartPage.Checked = Settings.Default.ShowStartPage;
            chkUtf8Bom.Checked = Settings.Default.UseUtf8Bom;

            numMaxRecentConnections.Value = Settings.Default.MaxRecentConnections;
            numPreviewSize.Value = Settings.Default.PreviewSize;
        }

        private void SaveOptions()
        {
            Settings.Default.AlwaysEdit = chkAlwaysEdit.Checked;
            Settings.Default.AlwaysRestoreActiveConnections = chkAlwaysRestoreActiveConnections.Checked;
            Settings.Default.EditorDetectUrls = chkEditorDetectUrls.Checked;
            Settings.Default.EditorHighlighting = chkEditorHighlighting.Checked;
            Settings.Default.EditorWordWrap = chkEditorWordWrap.Checked;
            Settings.Default.PromptRestoreActiveConnections = chkPromptRestoreActiveConnections.Checked;
            Settings.Default.ShowStartPage = chkShowStartPage.Checked;
            Settings.Default.UseUtf8Bom = chkUtf8Bom.Checked;

            Settings.Default.MaxRecentConnections = (int) numMaxRecentConnections.Value;
            Settings.Default.PreviewSize = (int) numPreviewSize.Value;

            Settings.Default.Save();
        }

        private void ResetOptions(bool toDefault)
        {
            if (toDefault)
            {
                Settings.Default.Reset();
                Settings.Default.Save();
            }
            LoadOptions();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveOptions();
            Close();
        }

        private void btnResetDefault_Click(object sender, EventArgs e)
        {
            ResetOptions(true);
        }

        private void btnResetCurrent_Click(object sender, EventArgs e)
        {
            ResetOptions(false);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
