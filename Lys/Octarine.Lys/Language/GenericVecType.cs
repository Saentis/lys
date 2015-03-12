/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class GenericVecType : TypeBase
    {
        public GenericVecType()
        {
        }

        public override string Identifier
        {
            get { return "vec?"; }
        }

    }
}