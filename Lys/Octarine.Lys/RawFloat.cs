/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys
{
    /// <summary>
    /// A floating point number in terms of strings.
    /// </summary>
    public struct RawFloat
    {
        public string IntegerPart;
        public string FractionalPart;
        public string ExponentialPart;
        public int Bits;

        public string String
        {
            get
            {
                string s = string.Format("{0}.{1}", IntegerPart, FractionalPart);
                if (ExponentialPart.Length > 0)
                    s += "E" + ExponentialPart;
                return s;
            }
        }

        public override string ToString()
        {
            string s = string.Format("{0}.{1}", IntegerPart, FractionalPart);
            if (ExponentialPart.Length > 0)
                s += "E" + ExponentialPart;
            if (Bits > 0)
                s += "@" + Bits;
            return s;
        }
    }
}