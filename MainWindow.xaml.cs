using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Calculator;

internal record Function(int ArgsCount, Func<double[], double> F);

internal record HistoryEntry(string Input, string Output, ICommand[] Commands);

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Dictionary<string, double> variables = new()
    {
        ["Pi"] = double.Pi,
        ["E"] = double.E,
    };

    private Dictionary<string, Function> functions = new()
    {
        ["sqrt"] = new Function(1, (args) => Math.Sqrt(args[0])),
        ["pow"] = new Function(2, (args) => Math.Pow(args[0], args[1])),
        ["abs"] = new Function(1, (args) => Math.Abs(args[0])),
        ["sin"] = new Function(1, (args) => Math.Sin(args[0])),
        ["cos"] = new Function(1, (args) => Math.Cos(args[0])),
        ["tan"] = new Function(1, (args) => Math.Tan(args[0])),
        ["cot"] = new Function(1, (args) => 1 / Math.Tan(args[0])),
        ["sec"] = new Function(1, (args) => 1 / Math.Cos(args[0])),
        ["csc"] = new Function(1, (args) => 1 / Math.Sin(args[0])),
        ["sinh"] = new Function(1, (args) => Math.Sinh(args[0])),
        ["cosh"] = new Function(1, (args) => Math.Cosh(args[0])),
        ["tanh"] = new Function(1, (args) => Math.Tanh(args[0])),
        ["coth"] = new Function(1, (args) => (Math.Exp(args[0]) + Math.Exp(-args[0])) / (Math.Exp(args[0]) - Math.Exp(-args[0]))),
        ["sech"] = new Function(1, (args) => 2 / (Math.Exp(args[0]) + Math.Exp(-args[0]))),
        ["csch"] = new Function(1, (args) => 2 / (Math.Exp(args[0]) - Math.Exp(-args[0]))),
    };

    private Stack<HistoryEntry> _history = [];

    public MainWindow()
    {
        InitializeComponent();
        input.Focus();
    }

    private void NumberButton_Click(object sender, RoutedEventArgs e)
    {
        input.Text += ((Button)sender).Content;
    }

    private void OperatorButton_Click(object sender, RoutedEventArgs e)
    {
        input.Text += ((Button)sender).Content;
    }

    private void EqualsButton_Click(object sender, RoutedEventArgs e)
    {
        Ast ast;
        try
        {
            ast = new Parser(input.Text).Parse();
        }
        catch (ParseException ex)
        {
            output.Text = $"Error! {ex.Message}";
            return;
        }

        Stack<double> stack = new();
        ICommand[] commands;

        try
        {
            commands = FlattenAST(ast);
            foreach (ICommand command in commands)
            {
                command.Execute(stack);
            }
        }
        catch (ExecutionException ex)
        {
            output.Text = $"Error! {ex.Message}";
            return;
        }

        if (stack.Count != 1)
        {
            throw new UnreachableException("invalid commands");
        }

        output.Text = $"{stack.Pop():g}";
        _history.Push(new HistoryEntry(input.Text, output.Text, commands));
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        input.Text = "";
        output.Text = "";
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_history.TryPop(out HistoryEntry? entry))
        {
            // Restore previous entry.
            input.Text = entry.Input;
            output.Text = entry.Output;
        }
        else
        {
            // Nothing was evaluated yet, same as clear.
            ClearButton_Click(sender, new());
        }
    }

    private void EraseButton_Click(object sender, RoutedEventArgs e)
    {
        if (input.Text.Length > 0)
        {
            input.Text = input.Text[..^1];
        }
    }

    private void input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EqualsButton_Click(this, new());
        }
    }

    private ICommand[] FlattenAST(Ast node) => node switch
    {
        Ast.Number number => [new PushCommand(number.Value)],

        Ast.Variable variable => variables.TryGetValue(variable.Name, out double value)
            ? [new PushCommand(value)]
            : throw new ExecutionException($"Variable '{variable.Name}' is not defined"),

        Ast.Function function => functions.TryGetValue(function.Name, out Function? f)
            ? function.Args.Length == f.ArgsCount
                ? [.. function.Args.SelectMany(FlattenAST), new FunctionCommand(f)]
                : throw new ExecutionException($"Expected {f.ArgsCount} arguments for '{function.Name}'")
            : throw new ExecutionException($"Unknown function '{function.Name}'"),

        Ast.BinaryOperation op => [
            .. FlattenAST(op.Y),
            .. FlattenAST(op.X),
            op.Kind switch
            {
                BinaryOp.Add => new AddCommand(),
                BinaryOp.Subtract => new SubtractCommand(),
                BinaryOp.Multiply => new MultiplyCommand(),
                BinaryOp.Divide => new DivideCommand(),
                BinaryOp.Remainder => new RemainderCommand(),
                BinaryOp.Power => new PowerCommand(),
                _ => throw new UnreachableException($"Unhandled binary operation: {op.Kind}"),
            },
        ],

        Ast.UnaryOperation { Kind: UnaryOp.Negate, X: var x } => [
            .. FlattenAST(x),
            new NegateCommand(),
        ],

        _ => throw new UnreachableException($"Unhandled AST node: {node}"),
    };
}
