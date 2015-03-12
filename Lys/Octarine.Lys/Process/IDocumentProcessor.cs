/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Octarine.Lys.Language;
using Octarine.Lys.Parse;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Interface for processing the document structure (such as namespaces, functions).
    /// </summary>
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Reads the document.
        /// </summary>
        Namespace[] Read();

    }
}