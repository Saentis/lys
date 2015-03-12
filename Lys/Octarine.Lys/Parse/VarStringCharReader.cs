/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Text;

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for reading characters from a variable string.
    /// </summary>
    public class VarStringCharReader : StringCharReader
    {
        /// <summary>
        /// Initializes a new string character reader.
        /// </summary>
        public VarStringCharReader() : base(string.Empty) { }

        /// <summary>
        /// Overwrites the internal string and resets the pointer.
        /// </summary>
        /// <param name="str"></param>
        public new void SetString(string str)
        {
            base.SetString(str);
        }

    }
}
