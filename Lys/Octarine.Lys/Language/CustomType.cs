/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;

namespace Octarine.Lys.Language
{
    public class CustomType : TypeBase
    {
        public CustomType(string name, Dictionary<string, IType> fields)
        {
            _name = name;
            _fields = fields;
        }

        private string _name;
        private Dictionary<string, IType> _fields;

        public override string Identifier
        {
            get { return _name; }
        }

        public Dictionary<string, IType> Fields
        {
            get { return _fields; }
        }

        public override IType OperationProperty(string name)
        {
            if (_fields.ContainsKey(name))
                return _fields[name];
            else
                return base.OperationProperty(name);
        }

    }
}