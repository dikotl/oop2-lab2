using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Calculator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ExpressionEvaluator _evaluator = new();
    private List<string> _inputHistory = [""];
    private int _historyPointer = -1;
    private bool _isScientificVisible = false;
    private bool _inHistory = false;

    private Expects Expected
    {
        get;
        set
        {
            Debug.WriteLine($"MAIN: {value}");
            field = value;
        }
    } = ExpressionParser.EXPECTED_OPERAND;

    public MainWindow()
    {
        InitializeComponent();
        input.Focus();
    }

    private void QuitHistory()
    {
        _historyPointer = -1;
        _inHistory = false;
    }

    private void AppendInputText(string text)
    {
        input.Text += text;
        Parse();
        QuitHistory();
    }

    private void DigitButton_Click(object sender, RoutedEventArgs _)
    {
        if ((Expected & Expects.Digit) != 0 || (Expected & Expects.DigitAfterDot) != 0)
        {
            AppendInputText((string)((Button)sender).Content);
        }
    }

    private void ZeroesButton_Click(object sender, RoutedEventArgs _)
    {
        if ((Expected & Expects.DigitAfterDot) != 0)
        {
            AppendInputText("00");
        }
    }

    private void CommaButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Dot) != 0)
        {
            AppendInputText(".");
        }
        if ((Expected & Expects.Comma) != 0)
        {
            AppendInputText(", ");
        }
    }

    private void OperatorButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Binary) != 0)
        {
            AppendInputText((string)((Button)sender).Content);
        }
    }

    private void MinusButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Binary) != 0 || (Expected & Expects.Unary) != 0)
        {
            AppendInputText("-");
        }
    }

    private void VariableButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Binary) != 0)
        {
            input.Text += "*";
        }
        else if ((Expected & Expects.Name) == 0)
        {
            return;
        }
        AppendInputText((string)((Button)sender).Content);
    }

    private void OpenParenButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Binary) != 0)
        {
            AppendInputText("*");
        }
        else if ((Expected & Expects.LeParen) == 0)
        {
            return;
        }
        AppendInputText("(");
    }

    private void CloseParenButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.RiParen) != 0)
        {
            AppendInputText((string)((Button)sender).Content);
        }
    }

    private void MathFuncButton_Click(object sender, RoutedEventArgs e)
    {
        if ((Expected & Expects.Binary) != 0)
        {
            AppendInputText("*");
        }
        else if ((Expected & Expects.Name) == 0)
        {
            return;
        }
        QuitHistory();
        AppendInputText((string)((Button)sender).Content + "(");
    }

    private void EvalButton_Click(object sender, RoutedEventArgs e)
    {
        Eval();
    }

    private void Parse()
    {
        Expected = _evaluator.ExpectsForText(input.Text);
    }

    private void Eval()
    {
        try
        {
            double result;
            (result, Expected) = _evaluator.Eval(input.Text);
            output.Text = $"{result:g}";
        }
        catch (ParseException ex)
        {
            output.Text = $"{ex.Message}";
            Expected = ex.Expected;
        }
        catch (ExecutionException ex)
        {
            output.Text = $"Error! {ex.Message}";
        }
        catch (Exception)
        {
            output.Text = "Internal error!";
        }
        finally
        {
            if (!_inHistory) _inputHistory.Add(input.Text);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();
        input.Text = "";
        output.Text = "";
        Expected = ExpressionParser.EXPECTED_OPERAND;
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        _historyPointer = _historyPointer switch
        {
            0 => 0,
            < 0 when _inputHistory.Count > 1 => _inputHistory.Count - (input.Text == _inputHistory[^1] ? 2 : 1),
            < 0 => _inputHistory.Count - 1,
            > 0 => _historyPointer - 1,
        };
        input.Text = _inputHistory[_historyPointer];
        _inHistory = true;
        Eval();
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_historyPointer >= 0 && _historyPointer < _inputHistory.Count - 1)
        {
            input.Text = _inputHistory[++_historyPointer];
            _inHistory = true;
            Eval();
        }
    }

    private void EraseButton_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();

        if (input.Text.Length > 0)
        {
            input.Text = input.Text[..^1];
            Parse();
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        QuitHistory();

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Eval();
        }
        else
        {
            Parse();
        }
    }

    private void ToggleScientific_Click(object sender, RoutedEventArgs e)
    {
        if (!_isScientificVisible)
        {
            ScientificColumn.Width = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            ScientificColumn.Width = new GridLength(0);
        }
        _isScientificVisible = !_isScientificVisible;
    }
}
