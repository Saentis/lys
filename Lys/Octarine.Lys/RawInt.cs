/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys
{
    public enum RawIntBase
    {
        Binary,
        Decimal,
        Hexadecimal
    }

    /// <summary>
    /// An integer number in terms of strings.
    /// </summary>
    public struct RawInt
    {
        public string Integer;
        public RawIntBase Base;
        public int Bits;
        public bool Unsigned;

        public string String
        {
            get
            {
                switch (Base)
                {
                    case RawIntBase.Decimal:
                        return Integer;
                    case RawIntBase.Binary:
                    case RawIntBase.Hexadecimal:
                        // TODO: string representation of bin/hex
                        throw new System.NotImplementedException();
                    default:
                        throw new System.InvalidOperationException("Invalid base: " + this.Base);
                }
            }
        }

        public override string ToString()
        {
            string s;
            switch (Base)
            {
                case RawIntBase.Binary: s = "0b" + Integer; break;
                case RawIntBase.Decimal: s = Integer; break;
                case RawIntBase.Hexadecimal: s = "0x" + Integer; break;
                default: throw new System.InvalidOperationException("Invalid base: " + this.Base);
            }
            if (!Unsigned)
                s += "u";
            if (Bits > 0)
                s += "@" + Bits;
            return s;
        }
    }
}