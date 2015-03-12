/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;
using Octarine.Lys.Process;

namespace Octarine.Lys.Language
{
    public struct Namespace
    {
        public string[] Path;
        public UserFunction[] Functions;
        public Dictionary<string, IType> TypeDefinitions;

        public static readonly Namespace Null = new Namespace { Path = new string[0], Functions = new UserFunction[0] };
    }
}