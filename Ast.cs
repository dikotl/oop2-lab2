namespace Calculator;

internal abstract record Ast
{
    internal sealed record Number(double Value) : Ast;
    internal sealed record Variable(string Name) : Ast;
    internal sealed record Function(string Name, Ast[] Args) : Ast;
    internal sealed record BinaryOperation(Ast X, Ast Y, BinaryOp Kind) : Ast;
    internal sealed record UnaryOperation(Ast X, UnaryOp Kind) : Ast;
}

internal enum BinaryOp
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Remainder,
    Power,
}

internal enum UnaryOp
{
    Negate,
}
