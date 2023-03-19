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

namespace VDS.RDF.Utilities.Editor
{
    /// <summary>
    /// Visual options for text editors
    /// </summary>
    /// <typeparam name="TFont">Font Type</typeparam>
    /// <typeparam name="TColor">Colour Type</typeparam>
    public abstract class VisualOptions<TFont, TColor>
        where TFont : class
        where TColor: struct
    {
        private bool _clickableUris = false,
                     _showLineNumbers = true,
                     _showSpaces = false,
                     _showTabs = false,
                     _showEndOfLine = false,
                     _wordWrap = false;
        private TFont _fontFace = null,
                      _errorFontFace = null;
        private double _fontSize = 13.0d;
        private TColor? _foreground = null,
                        _background = null,
                        _errorForeground = null,
                        _errorBackground = null;
        private string _errorDecoration = null;

        /// <summary>
        /// Gets/Sets whether clickable URIs are enabled
        /// </summary>
        public bool EnableClickableUris
        {
            get
            {
                return _clickableUris;
            }
            set
            {
                if (value != _clickableUris)
                {
                    _clickableUris = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether to show line numbers
        /// </summary>
        public bool ShowLineNumbers
        {
            get
            {
                return _showLineNumbers;
            }
            set
            {
                if (value != _showLineNumbers)
                {
                    _showLineNumbers = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether to show spaces
        /// </summary>
        public bool ShowSpaces
        {
            get
            {
                return _showSpaces;
            }
            set
            {
                if (value != _showSpaces)
                {
                    _showSpaces = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether to show tabs
        /// </summary>
        public bool ShowTabs
        {
            get
            {
                return _showTabs;
            }
            set
            {
                if (value != _showTabs)
                {
                    _showTabs = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets whether to show new line characters
        /// </summary>
        public bool ShowEndOfLine
        {
            get
            {
                return _showEndOfLine;
            }
            set
            {
                if (value != _showEndOfLine)
                {
                    _showEndOfLine = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets word wrap
        /// </summary>
        public bool WordWrap
        {
            get
            {
                return _wordWrap;
            }
            set
            {
                if (value != _wordWrap)
                {
                    _wordWrap = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets the font face
        /// </summary>
        public TFont FontFace
        {
            get
            {
                return _fontFace;
            }
            set
            {
                if (value == null)
                {
                    if (_fontFace != null)
                    {
                        _fontFace = null;
                        RaiseChanged();
                    }
                }
                else
                {
                    if (_fontFace == null)
                    {
                        _fontFace = value;
                        RaiseChanged();
                    }
                    else if (!_fontFace.Equals(value))
                    {
                        _fontFace = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the font face for error highlights
        /// </summary>
        public TFont ErrorFontFace
        {
            get
            {
                return _errorFontFace;
            }
            set
            {
                if (value == null)
                {
                    if (_errorFontFace != null)
                    {
                        _errorFontFace = null;
                        RaiseChanged();
                    }
                }
                else
                {
                    if (_errorFontFace == null)
                    {
                        _errorFontFace = value;
                        RaiseChanged();
                    }
                    else if (!_errorFontFace.Equals(value))
                    {
                        _errorFontFace = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the font size
        /// </summary>
        public double FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                if (value != _fontSize)
                {
                    _fontSize = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Gets/Sets the foreground colour
        /// </summary>
        public TColor Foreground
        {
            get
            {
                if (_foreground != null)
                {
                    return _foreground.Value;
                }
                else
                {
                    return default(TColor);
                }
            }
            set
            {
                if (_foreground != null)
                {
                    _foreground = value;
                    RaiseChanged();
                }
                else
                {
                    if (!_foreground.Equals(value))
                    {
                        _foreground = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the background colour
        /// </summary>
        public TColor Background
        {
            get
            {
                if (_background != null)
                {
                    return _background.Value;
                }
                else
                {
                    return default(TColor);
                }
            }
            set
            {
                if (_background != null)
                {
                    _background = value;
                    RaiseChanged();
                }
                else
                {
                    if (!_background.Equals(value))
                    {
                        _background = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the foreground colour for error highlights
        /// </summary>
        public TColor ErrorForeground
        {
            get
            {
                if (_errorForeground != null)
                {
                    return _errorForeground.Value;
                }
                else
                {
                    return default(TColor);
                }
            }
            set
            {
                if (_errorForeground != null)
                {
                    _errorForeground = value;
                    RaiseChanged();
                }
                else
                {
                    if (!_errorForeground.Equals(value))
                    {
                        _errorForeground = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the background colour for error highlights
        /// </summary>
        public TColor ErrorBackground
        {
            get
            {
                if (_errorBackground != null)
                {
                    return _errorBackground.Value;
                }
                else
                {
                    return default(TColor);
                }
            }
            set
            {
                if (_errorBackground != null)
                {
                    _errorBackground = value;
                    RaiseChanged();
                }
                else
                {
                    if (!_errorBackground.Equals(value))
                    {
                        _errorBackground = value;
                        RaiseChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets/Sets the decoration for error highlights
        /// </summary>
        public string ErrorDecoration
        {
            get
            {
                return _errorDecoration;
            }
            set
            {
                if (value != _errorDecoration)
                {
                    _errorDecoration = value;
                    RaiseChanged();
                }
            }
        }

        /// <summary>
        /// Event which is raised whenever options change
        /// </summary>
        public event OptionsChanged Changed;

        /// <summary>
        /// Helper method for raising the options changed event
        /// </summary>
        protected void RaiseChanged()
        {
            OptionsChanged d = Changed;
            if (d != null) d();
        }
    }

    /// <summary>
    /// Delegate for option changed events
    /// </summary>
    public delegate void OptionsChanged();
}
