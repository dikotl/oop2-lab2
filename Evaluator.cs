using System.Collections;
using System.Diagnostics;

namespace Calculator;

/// <summary>
/// MathFunc stores amount of arguments and the function itself.
/// </summary>
internal record MathFunc(int ArgsCount, Func<double[], double> F);

internal class Evaluator
{
    private Dictionary<string, double> variables = new()
    {
        ["Pi"] = double.Pi,
        ["E"] = double.E,
    };

    private Dictionary<string, MathFunc> functions = new()
    {
        ["sqrt"] = new MathFunc(1, (args) => Math.Sqrt(args[0])),
        ["pow"] = new MathFunc(2, (args) => Math.Pow(args[0], args[1])),
        ["ln"] = new MathFunc(1, (args) => Math.Log(args[0])),
        ["log"] = new MathFunc(2, (args) => Math.Log(args[0], args[1])),
        ["abs"] = new MathFunc(1, (args) => Math.Abs(args[0])),
        ["sin"] = new MathFunc(1, (args) => Math.Sin(args[0])),
        ["cos"] = new MathFunc(1, (args) => Math.Cos(args[0])),
        ["tan"] = new MathFunc(1, (args) => Math.Tan(args[0])),
        ["cot"] = new MathFunc(1, (args) => 1 / Math.Tan(args[0])),
        ["sec"] = new MathFunc(1, (args) => 1 / Math.Cos(args[0])),
        ["csc"] = new MathFunc(1, (args) => 1 / Math.Sin(args[0])),
        ["sinh"] = new MathFunc(1, (args) => Math.Sinh(args[0])),
        ["cosh"] = new MathFunc(1, (args) => Math.Cosh(args[0])),
        ["tanh"] = new MathFunc(1, (args) => Math.Tanh(args[0])),
        ["coth"] = new MathFunc(1, (args) => (Math.Exp(args[0]) + Math.Exp(-args[0])) / (Math.Exp(args[0]) - Math.Exp(-args[0]))),
        ["sech"] = new MathFunc(1, (args) => 2 / (Math.Exp(args[0]) + Math.Exp(-args[0]))),
        ["csch"] = new MathFunc(1, (args) => 2 / (Math.Exp(args[0]) - Math.Exp(-args[0]))),
    };

    public double Eval(string input)
    {
        var ast = new Parser(input).Parse();
        var stack = new Stack<double>();
        var commands = new List<ICommand>();

        FlattenAST(ast, commands);

        foreach (ICommand command in commands)
        {
            command.Execute(stack, variables);
        }

        if (stack.Count != 1)
        {
            throw new UnreachableException("AST flattening implementation bug");
        }

        return stack.Pop();
    }

    private void FlattenAST(Ast node, List<ICommand> commands)
    {
        if (node is Ast.Number number)
        {
            commands.Add(new PushCommand(number.Value));
        }
        else if (node is Ast.DefineVariable def)
        {
            FlattenAST(def.Value, commands);
            commands.Add(new StoreCommand(def.Name));
        }
        else if (node is Ast.Variable variable)
        {
            if (!variables.TryGetValue(variable.Name, out double value))
            {
                throw new ExecutionException($"Variable '{variable.Name}' is not defined");
            }
            commands.Add(new PushCommand(value));
        }
        else if (node is Ast.Function function)
        {
            if (!functions.TryGetValue(function.Name, out MathFunc? f))
            {
                throw new ExecutionException($"Unknown function '{function.Name}'");
            }
            if (function.Args.Length != f.ArgsCount)
            {
                throw new ExecutionException($"Expected {f.ArgsCount} arguments for '{function.Name}'");
            }
            foreach (var arg in function.Args)
            {
                FlattenAST(arg, commands);
            }
            commands.Add(new FunctionCommand(f));
        }
        else if (node is Ast.BinaryOperation binary)
        {
            FlattenAST(binary.Y, commands);
            FlattenAST(binary.X, commands);
            commands.Add(binary.Kind switch
            {
                BinaryOp.Add => new AddCommand(),
                BinaryOp.Subtract => new SubtractCommand(),
                BinaryOp.Multiply => new MultiplyCommand(),
                BinaryOp.Divide => new DivideCommand(),
                BinaryOp.Remainder => new RemainderCommand(),
                BinaryOp.Power => new PowerCommand(),
                _ => throw new UnreachableException($"Unhandled binary operation: {binary.Kind}"),
            });
        }
        else if (node is Ast.UnaryOperation unary)
        {
            FlattenAST(unary.X, commands);
            commands.Add(new NegateCommand());
        }
        else
        {
            throw new UnreachableException($"Unhandled AST node: {node}");
        }
    }
}
