/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Language
{
    /// <summary>
    /// A variable used in combination with function definitions.
    /// </summary>
    public struct Variable
    {
        /// <summary>
        /// Initializes a new variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="type">The variable data type.</param>
        public Variable(string name, IType type)
        {
            this.Name = name;
            this.Type = type;
        }

        /// <summary>
        /// The variable name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The variable data type.
        /// </summary>
        public IType Type;

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Variable)
                return ((Variable)obj).Name == this.Name;
            else
                return base.Equals(obj);
        }
    }
}