/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Enumerates all operation types supported by the compiler/virtual machine.
    /// </summary>
    public enum OperationType : ushort
    {
        // OpCode structure
        NoOperation = 0,
        Pop,
        Swap,
        BeginOpBlock,
        EndOpBlock,

        // Operators
        Plus,
        Minus,
        Times,
        Divide,
        Modulo,
        LogicalNot,
        LogicalAnd,
        LogicalOr,
        UnaryPlus,
        UnaryMinus,
        BitwiseNot,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        LeftShift,
        RightShift,
        Equal,
        NotEqual,
        LessThan,
        LessThanEqual,
        MoreThan,
        MoreThanEqual,
        Assign,
        AssignBitwiseAnd,
        AssignBitwiseOr,
        AssignBitwiseXor,
        AssignLeftShift,
        AssignRightShift,
        AssignPlus,
        AssignMinus,
        AssignTimes,
        AssignDivide,
        AssignModulo,
        SuffixIncrement,
        SuffixDecrement,
        PrefixIncrement,
        PrefixDecrement,
        PropertyAccess,
        Index,
        ColonIndex, // parametrized (numArgs: int)
        Call, // parametrized (numArgs: int)

        // Loops
        BeginScope,
        EndScope,
        If,
        For,
        While,
        DoWhile,

        // Object creation
        LoadSymbol, // parametrized (variableName: string)
        LoadConstant, // parametrized (constant: string|RawInt|RawFloat|bool)
        LoadUndefined,
        LoadFunction, // parametrized (functionPath: string[])
        MakeList, // parametrized (length: int)
        MakeVector, // parametrized (dimension: int)

        // Program interaction
        CreateVariable, // parametrized (variableName: string)
        Return,
        ReturnValue,
        Import, // parametrized (namespace: string[])
        Async,
        AsyncWait,
        SyncAbort,
        SyncEnd,
    }
}