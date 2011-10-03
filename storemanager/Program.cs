﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

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

If this license is not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VDS.RDF.Storage;

namespace VDS.RDF.Utilities.StoreManager
{
    static class Program
    {
        private static ManagerForm _main;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _main = new ManagerForm();
            Application.Run(_main);
        }

        public static ManagerForm MainForm
        {
            get
            {
                return _main;
            }
        }

        public static IEnumerable<IGenericIOManager> ActiveConnections
        {
            get
            {
                if (_main != null)
                {
                    return (from managerForm in _main.MdiChildren.OfType<StoreManagerForm>()
                            select managerForm.Manager);
                }
                else
                {
                    return Enumerable.Empty<IGenericIOManager>();
                }
            }
        }
    }
}
