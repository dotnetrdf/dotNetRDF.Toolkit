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
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using VDS.RDF.Utilities.Editor.Syntax;
using VDS.RDF.Utilities.Editor.Wpf.Syntax;

namespace VDS.RDF.Utilities.Editor.Wpf
{
    /// <summary>
    /// Interaction logic for AppearanceSettings.xaml
    /// </summary>
    public partial class AppearanceSettings
        : Window
    {
        private List<string> _colours = new List<string>();
        private VisualOptions<FontFamily, Color> _options;

        private List<string> _decorations;

        private static ITextRunConstructionContext _colourContext = new FakeTextRunContext();

        public AppearanceSettings(VisualOptions<FontFamily, Color> options)
        {
            _options = options;

            _decorations = new List<string>()
            {
                "None",
                "Baseline",
                "OverLine",
                "Strikethrough",
                "Underline"
            };
            InitializeComponent();

            //Prepare a list of colours as we need this to select the relevant colours later
            Type t = typeof(Colors);
            Type cType = typeof(Color);
            foreach (PropertyInfo p in t.GetProperties())
            {
                if (p.PropertyType.Equals(cType))
                {
                    try
                    {
                        Color c = (Color)ColorConverter.ConvertFromString(p.Name);
                        _colours.Add(c.ToString());
                    }
                    catch
                    {
                        //Ignore errors here
                    }
                }
            }

            //Show current settings
            ShowSettings();
        }

        #region Display Settings

        public IEnumerable<string> Decorations
        {
            get
            {
                return _decorations;
            }
        }

        private void ShowSettings()
        {
            //Show Editor Settings
            FontFamily font = (Properties.Settings.Default.EditorFontFace == null) ? _options.FontFace : Properties.Settings.Default.EditorFontFace;
            cboFont.SelectedItem = font;
            fontSizeSlider.Value = Math.Round(Properties.Settings.Default.EditorFontSize);
            ShowColour(cboEditorForeground, Properties.Settings.Default.EditorForeground);
            ShowColour(cboEditorBackground, Properties.Settings.Default.EditorBackground);

            //Show Syntax Highlighting Settings
            ShowColour(cboColourXmlAttrName, Properties.Settings.Default.SyntaxColourXmlAttrName);
            ShowColour(cboColourXmlAttrValue, Properties.Settings.Default.SyntaxColourXmlAttrValue);
            ShowColour(cboColourXmlBrokenEntities, Properties.Settings.Default.SyntaxColourXmlBrokenEntity);
            ShowColour(cboColourXmlCData, Properties.Settings.Default.SyntaxColourXmlCData);
            ShowColour(cboColourXmlComments, Properties.Settings.Default.SyntaxColourXmlComments);
            ShowColour(cboColourXmlDocType, Properties.Settings.Default.SyntaxColourXmlDocType);
            ShowColour(cboColourXmlEntities, Properties.Settings.Default.SyntaxColourXmlEntity);
            ShowColour(cboColourXmlTags, Properties.Settings.Default.SyntaxColourXmlTag);

            ShowColour(cboColourBNode, Properties.Settings.Default.SyntaxColourBNode);
            ShowColour(cboColourComments, Properties.Settings.Default.SyntaxColourComment);
            ShowColour(cboColourEscapedChars, Properties.Settings.Default.SyntaxColourEscapedChar);
            ShowColour(cboColourKeywords, Properties.Settings.Default.SyntaxColourKeyword);
            ShowColour(cboColourLangSpec, Properties.Settings.Default.SyntaxColourLangSpec);
            ShowColour(cboColourNumbers, Properties.Settings.Default.SyntaxColourNumbers);
            ShowColour(cboColourPunctuation, Properties.Settings.Default.SyntaxColourPunctuation);
            ShowColour(cboColourQNames, Properties.Settings.Default.SyntaxColourQName);
            ShowColour(cboColourStrings, Properties.Settings.Default.SyntaxColourString);
            ShowColour(cboColourURIs, Properties.Settings.Default.SyntaxColourURI);
            ShowColour(cboColourVariable, Properties.Settings.Default.SyntaxColourVariables);

            //Show Error Highlighting Settings
            font = (Properties.Settings.Default.EditorFontFace == null) ? _options.FontFace : Properties.Settings.Default.ErrorHighlightFontFamily;
            cboErrorFont.SelectedItem = font;
            ShowDecoration(cboErrorDecoration, Properties.Settings.Default.ErrorHighlightDecoration);
            ShowColour(cboColourErrorFont, Properties.Settings.Default.ErrorHighlightForeground);
            ShowColour(cboColourErrorBackground, Properties.Settings.Default.ErrorHighlightBackground);
        }

        private void ShowColour(ComboBox combo, Color c)
        {
            string s = c.ToString();
            for (int i = 0; i < _colours.Count; i++)
            {
                if (s.Equals(_colours[i]))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private Color GetColour(Color def, ComboBox combo)
        {
            try
            {
                if (combo.SelectedIndex < 0 || combo.SelectedIndex >= _colours.Count)
                {
                    return def;
                }
                else
                {
                    return (Color)ColorConverter.ConvertFromString(_colours[combo.SelectedIndex]);
                }
            }
            catch
            {
                return def;
            }
        }

        private void ShowDecoration(ComboBox combo, string decoration)
        {
            if (decoration == null || decoration.Equals(string.Empty))
            {
                decoration = "None";
            }
            for (int i = 0; i < _decorations.Count; i++)
            {
                if (decoration.Equals(_decorations[i]))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private string GetDecoration(ComboBox combo)
        {
            if (combo.SelectedItem == null || combo.SelectedIndex < 0)
            {
                return null;
            }
            else
            {
                return _decorations[combo.SelectedIndex];
            }
        }

        #endregion

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //First save general Editor Appearance Settings
                Properties.Settings.Default.EditorFontFace = (FontFamily)cboFont.SelectedItem;
                Properties.Settings.Default.EditorFontSize = fontSizeSlider.Value;
                Properties.Settings.Default.EditorForeground = GetColour(Properties.Settings.Default.EditorForeground, cboEditorForeground);
                Properties.Settings.Default.EditorBackground = GetColour(Properties.Settings.Default.EditorBackground, cboEditorBackground);
                Properties.Settings.Default.Save();

                //Then save Syntax Highlighting Settings
                Properties.Settings.Default.SyntaxColourXmlAttrName = GetColour(Properties.Settings.Default.SyntaxColourXmlAttrName, cboColourXmlAttrName);
                Properties.Settings.Default.SyntaxColourXmlAttrValue = GetColour(Properties.Settings.Default.SyntaxColourXmlAttrValue, cboColourXmlAttrValue);
                Properties.Settings.Default.SyntaxColourXmlBrokenEntity = GetColour(Properties.Settings.Default.SyntaxColourXmlBrokenEntity, cboColourXmlBrokenEntities);
                Properties.Settings.Default.SyntaxColourXmlCData = GetColour(Properties.Settings.Default.SyntaxColourXmlCData, cboColourXmlCData);
                Properties.Settings.Default.SyntaxColourXmlComments = GetColour(Properties.Settings.Default.SyntaxColourXmlComments, cboColourXmlComments);
                Properties.Settings.Default.SyntaxColourXmlDocType = GetColour(Properties.Settings.Default.SyntaxColourXmlDocType, cboColourXmlDocType);
                Properties.Settings.Default.SyntaxColourXmlEntity = GetColour(Properties.Settings.Default.SyntaxColourXmlEntity, cboColourXmlEntities);
                Properties.Settings.Default.SyntaxColourXmlTag = GetColour(Properties.Settings.Default.SyntaxColourXmlTag, cboColourXmlTags);

                Properties.Settings.Default.SyntaxColourBNode = GetColour(Properties.Settings.Default.SyntaxColourBNode, cboColourBNode);
                Properties.Settings.Default.SyntaxColourComment = GetColour(Properties.Settings.Default.SyntaxColourComment, cboColourComments);
                Properties.Settings.Default.SyntaxColourEscapedChar = GetColour(Properties.Settings.Default.SyntaxColourEscapedChar, cboColourEscapedChars);
                Properties.Settings.Default.SyntaxColourKeyword = GetColour(Properties.Settings.Default.SyntaxColourKeyword, cboColourKeywords);
                Properties.Settings.Default.SyntaxColourLangSpec = GetColour(Properties.Settings.Default.SyntaxColourLangSpec, cboColourLangSpec);
                Properties.Settings.Default.SyntaxColourNumbers = GetColour(Properties.Settings.Default.SyntaxColourNumbers, cboColourNumbers);
                Properties.Settings.Default.SyntaxColourPunctuation = GetColour(Properties.Settings.Default.SyntaxColourPunctuation, cboColourPunctuation);
                Properties.Settings.Default.SyntaxColourQName = GetColour(Properties.Settings.Default.SyntaxColourQName, cboColourQNames);
                Properties.Settings.Default.SyntaxColourString = GetColour(Properties.Settings.Default.SyntaxColourString, cboColourStrings);
                Properties.Settings.Default.SyntaxColourURI = GetColour(Properties.Settings.Default.SyntaxColourURI, cboColourURIs);
                Properties.Settings.Default.SyntaxColourVariables = GetColour(Properties.Settings.Default.SyntaxColourVariables, cboColourVariable);

                //Then save the Error Highlighting Settings
                Properties.Settings.Default.ErrorHighlightBackground = GetColour(Properties.Settings.Default.ErrorHighlightBackground, cboColourErrorBackground);
                Properties.Settings.Default.ErrorHighlightDecoration = GetDecoration(cboErrorDecoration);
                Properties.Settings.Default.ErrorHighlightFontFamily = (FontFamily)cboErrorFont.SelectedItem;
                Properties.Settings.Default.ErrorHighlightForeground = GetColour(Properties.Settings.Default.ErrorHighlightForeground, cboColourErrorFont);

                //Finally save the updated settings
                Properties.Settings.Default.Save();

                //Force the Syntax Manager to update colours appropriately
                AppearanceSettings.UpdateHighlightingColours();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred trying to save your settings: " + ex.Message, "Save Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FontFamilyConverter converter = new FontFamilyConverter();
                FontFamily consolas = (FontFamily)converter.ConvertFromString("Consolas");
                Properties.Settings.Default.EditorFontFace = consolas;
                fontSizeSlider.Value = 13d;
                Properties.Settings.Default.EditorForeground = Colors.Black;
                Properties.Settings.Default.EditorBackground = Colors.White;
                Properties.Settings.Default.Save();
            }
            catch
            {
                //Can't reset settings
            }
            //Show settings
            ShowSettings();
        }

        private void lnkAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(lnkAdvancedSettings.NavigateUri.AbsoluteUri);
            }
            catch
            {
                //Ignore errors launching the URI
            }
        }

        private void btnResetSyntaxChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.SyntaxColourBNode = Colors.SteelBlue;
                Properties.Settings.Default.SyntaxColourComment = Colors.Green;
                Properties.Settings.Default.SyntaxColourEscapedChar = Colors.Teal;
                Properties.Settings.Default.SyntaxColourKeyword = Colors.Red;
                Properties.Settings.Default.SyntaxColourLangSpec = Colors.DarkGreen;
                Properties.Settings.Default.SyntaxColourNumbers = Colors.DarkBlue;
                Properties.Settings.Default.SyntaxColourPunctuation = Colors.DarkGreen;
                Properties.Settings.Default.SyntaxColourQName = Colors.DarkMagenta;
                Properties.Settings.Default.SyntaxColourString = Colors.Blue;
                Properties.Settings.Default.SyntaxColourURI = Colors.DarkMagenta;
                Properties.Settings.Default.SyntaxColourVariables = Colors.DarkOrange;
                Properties.Settings.Default.SyntaxColourXmlAttrName = Colors.Red;
                Properties.Settings.Default.SyntaxColourXmlAttrValue = Colors.Blue;
                Properties.Settings.Default.SyntaxColourXmlBrokenEntity = Colors.Olive;
                Properties.Settings.Default.SyntaxColourXmlCData = Colors.Blue;
                Properties.Settings.Default.SyntaxColourXmlComments = Colors.Green;
                Properties.Settings.Default.SyntaxColourXmlDocType = Colors.Blue;
                Properties.Settings.Default.SyntaxColourXmlEntity = Colors.Teal;
                Properties.Settings.Default.SyntaxColourXmlTag = Colors.DarkMagenta;
                Properties.Settings.Default.Save();
            }
            catch
            {
                //Can't reset settings
            }
            ShowSettings();
            //SyntaxManager.UpdateHighlightingColours();
        }

        private void btnAbandon_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnErrorReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.ErrorHighlightBackground = Colors.DarkRed;
                Properties.Settings.Default.ErrorHighlightForeground = Colors.White;
                Properties.Settings.Default.ErrorHighlightDecoration = null;
                Properties.Settings.Default.ErrorHighlightFontFamily = null;
            }
            catch
            {
                //Can't reset settings
            }
            ShowSettings();
        }

        private void btnResetAll_Click(object sender, RoutedEventArgs e)
        {
            btnReset_Click(sender, e);
            btnResetSyntaxChanges_Click(sender, e);
            btnErrorReset_Click(sender, e);
        }

        public static void UpdateHighlightingColours()
        {
            //Only applicable if not using customised XSHD Files
            if (!Properties.Settings.Default.UseCustomisedXshdFiles)
            {
                IHighlightingDefinition h;

                //Apply XML Format Colours
                h = HighlightingManager.Instance.GetDefinition("XML");
                if (h != null)
                {
                    foreach (HighlightingColor c in h.NamedHighlightingColors)
                    {
                        switch (c.Name)
                        {
                            case "Comment":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlComments);
                                break;
                            case "CData":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlCData);
                                break;
                            case "DocType":
                            case "XmlDeclaration":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlDocType);
                                break;
                            case "XmlTag":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlTag);
                                break;
                            case "AttributeName":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlAttrName);
                                break;
                            case "AttributeValue":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlAttrValue);
                                break;
                            case "Entity":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlEntity);
                                break;
                            case "BrokenEntity":
                                AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourXmlBrokenEntity);
                                break;

                        }
                    }
                }

                //Apply non-XML format colours
                foreach (SyntaxDefinition def in SyntaxManager.Definitions)
                {
                    if (!def.IsXmlFormat)
                    {
                        h = HighlightingManager.Instance.GetDefinition(def.Name);
                        if (h != null)
                        {
                            foreach (HighlightingColor c in h.NamedHighlightingColors)
                            {
                                switch (c.Name)
                                {
                                    case "BNode":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourBNode);
                                        break;
                                    case "Comment":
                                    case "Comments":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourComment);
                                        break;
                                    case "EscapedChar":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourEscapedChar);
                                        break;
                                    case "Keyword":
                                    case "Keywords":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourKeyword);
                                        break;
                                    case "LangSpec":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourLangSpec);
                                        break;
                                    case "Numbers":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourNumbers);
                                        break;
                                    case "Punctuation":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourPunctuation);
                                        break;
                                    case "QName":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourQName);
                                        break;
                                    case "String":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourString);
                                        break;
                                    case "URI":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourURI);
                                        break;
                                    case "Variable":
                                        AdjustHighlightingColour(c, Properties.Settings.Default.SyntaxColourVariables);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void AdjustHighlightingColour(HighlightingColor current, Color desired)
        {
            if (!desired.Equals(current.Foreground.GetColor(_colourContext)))
            {
                current.Foreground = new CustomHighlightingBrush(desired);
            }
        }
    }
}
