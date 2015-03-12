/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Text;

namespace Octarine.Lys
{
    public static class Helper
    {
        public static string PrependNamespace(string symbol, params string[] namespaceParts)
        {
            if (symbol == null) throw new ArgumentNullException("symbol");
            if (namespaceParts == null) throw new ArgumentNullException("namespaceParts");

            StringBuilder sb = new StringBuilder();
            foreach (string ns in namespaceParts)
            {
                sb.Append(ns);
                sb.Append("::");
            }
            sb.Append(symbol);
            return sb.ToString();
        }
    }
}