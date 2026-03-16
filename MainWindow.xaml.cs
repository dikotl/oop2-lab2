using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Calculator;

internal record HistoryEntry(string Input, string Output);

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Evaluator _evaluator = new();
    private Stack<HistoryEntry> _history = [];

    public MainWindow()
    {
        InitializeComponent();
        input.Focus();
    }

    private void InputButton_Click(object sender, RoutedEventArgs e)
    {
        input.Text += ((Button)sender).Content;
    }

    private void EvalButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            output.Text = $"{_evaluator.Eval(input.Text):g}";
            _history.Push(new HistoryEntry(input.Text, output.Text));
        }
        catch (ParseException ex)
        {
            output.Text = $"Error! {ex.Message}";
            return;
        }
        catch (ExecutionException ex)
        {
            output.Text = $"Error! {ex.Message}";
            return;
        }
        catch (Exception)
        {
            output.Text = "Internal error!";
            return;
        }
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
            EvalButton_Click(this, new());
        }
    }
}
