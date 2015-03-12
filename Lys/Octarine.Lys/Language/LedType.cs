/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class LedType : TypeBase
    {
        public LedType()
        {
        }

        public override string Identifier
        {
            get { return "led"; }
        }

        public override IType OperationProperty(string name)
        {
            switch (name)
            {
                case "r": return new FloatType(32);
                case "g": return new FloatType(32);
                case "b": return new FloatType(32);
                default: return base.OperationProperty(name);
            }
        }

    }
}