﻿/*

Copyright Robert Vesse 2009-12
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

namespace VDS.RDF.Utilities.StoreManager.Connections
{
    
    public enum ConnectionSettingType
    {
        String = 0,
        Password = 1,
        Integer = 2,
        Boolean = 3,
        Enum = 4,
        File = 5
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public class ConnectionAttribute
        : Attribute
    {
        public String DisplayName
        {
            get;
            set;
        }

        public String DisplaySuffix
        {
            get;
            set;
        }

        public int DisplayOrder
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool IsRequired
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool AllowEmptyString
        {
            get;
            set;
        }

        public ConnectionSettingType Type
        {
            get;
            set;
        }

        public String NotRequiredIf
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool IsValueRestricted
        {
            get;
            set;
        }

        public int MinValue
        {
            get;
            set;
        }

        public int MaxValue
        {
            get;
            set;
        }

        public String FileFilter
        {
            get;
            set;
        }
    }
}
