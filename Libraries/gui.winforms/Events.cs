/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VDS.RDF.GUI.WinForms.Controls;

namespace VDS.RDF.GUI.WinForms
{
    /// <summary>
    /// Event that occurs when a URI is clicked
    /// </summary>
    /// <param name="sender">Originator of the event</param>
    /// <param name="u">URI that was clicked</param>
    public delegate void UriClickedEventHandler(object sender, Uri u);

    /// <summary>
    /// Event that occurs when the formatter is changed
    /// </summary>
    /// <param name="sender">Originator of the event</param>
    /// <param name="formatter">Formatter that is now selected</param>
    public delegate void FormatterChanged(object sender, Formatter formatter);

    /// <summary>
    /// Event that occurs when result are requested to be closed
    /// </summary>
    /// <param name="sender">Originator of the event</param>
    public delegate void ResultCloseRequested(object sender);

    /// <summary>
    /// Event that occurs when results are requested to be detached
    /// </summary>
    /// <param name="sender">Originator of the event</param>
    public delegate void ResultDetachRequested(object sender);
}
