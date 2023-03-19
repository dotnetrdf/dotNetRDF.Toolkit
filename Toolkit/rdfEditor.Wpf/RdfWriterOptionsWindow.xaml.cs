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
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VDS.RDF.Writing;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for RdfWriterOptionsWindow.xaml
    /// </summary>
    public partial class RdfWriterOptionsWindow : Window
    {
        private IRdfWriter _writer;

        public RdfWriterOptionsWindow(IRdfWriter writer)
        {
            InitializeComponent();

            //Show Compression Levels
            Type clevels = typeof(WriterCompressionLevel);
            foreach (FieldInfo field in clevels.GetFields())
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = field.Name;
                item.Tag = field.GetValue(null);
                cboCompressionLevel.Items.Add(item);
                if (field.Name.Equals("Default"))
                {
                    cboCompressionLevel.SelectedItem = item;
                }
            }
            if (cboCompressionLevel.SelectedItem == null && cboCompressionLevel.Items.Count > 0)
            {
                cboCompressionLevel.SelectedItem = cboCompressionLevel.Items[0];
            }

            //Enable/Disable relevant controls
            cboCompressionLevel.IsEnabled = (writer is ICompressingWriter);
            chkHighSpeed.IsEnabled = (writer is IHighSpeedWriter);
            chkPrettyPrint.IsEnabled = (writer is IPrettyPrintingWriter);
            chkUseAttributes.IsEnabled = (writer is IAttributeWriter);
            chkUseDtds.IsEnabled = (writer is IDtdWriter);
            stkHtmlWriter.IsEnabled = (writer is IHtmlWriter);
            stkXmlWriter.IsEnabled = (writer is IDtdWriter || writer is IAttributeWriter);

            _writer = writer;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //Apply the selected Options
            if (_writer is ICompressingWriter)
            {
                try
                {
                    int? cLevel = ((ComboBoxItem)cboCompressionLevel.SelectedItem).Tag as int?;
                    if (cLevel != null) ((ICompressingWriter)_writer).CompressionLevel = cLevel.Value;
                }
                catch
                {
                    //Can't set Compression Level so skip
                }
            }
            if (_writer is IHighSpeedWriter)
            {
                ((IHighSpeedWriter)_writer).HighSpeedModePermitted = chkHighSpeed.IsChecked.Value;
            }
            if (_writer is IPrettyPrintingWriter)
            {
                ((IPrettyPrintingWriter)_writer).PrettyPrintMode = chkPrettyPrint.IsChecked.Value;
            }
            if (_writer is IAttributeWriter)
            {
                ((IAttributeWriter)_writer).UseAttributes = chkUseAttributes.IsChecked.Value;
            }
            if (_writer is IDtdWriter)
            {
                ((IDtdWriter)_writer).UseDtd = chkUseDtds.IsChecked.Value;
            }
            if (_writer is IHtmlWriter)
            {
                IHtmlWriter htmlWriter = (IHtmlWriter)_writer;
                htmlWriter.Stylesheet = txtStylesheet.Text;
                htmlWriter.CssClassBlankNode = ToUnsafeString(txtCssClassBNodes.Text);
                htmlWriter.CssClassDatatype = ToUnsafeString(txtCssClassDatatypes.Text);
                htmlWriter.CssClassLangSpec = ToUnsafeString(txtCssClassLangSpec.Text);
                htmlWriter.CssClassLiteral = ToUnsafeString(txtCssClassLiterals.Text);
                htmlWriter.CssClassUri = ToUnsafeString(txtCssClassUri.Text);
                htmlWriter.UriPrefix = txtPrefixUris.Text;
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string ToUnsafeString(string value)
        {
            return (value.Equals(string.Empty) ? null : value);
        }
    }
}
