using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Calculator;

internal record HistoryEntry(string Input, string Output);

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Evaluator _evaluator = new();
    private Stack<HistoryEntry> _undoStack = [];
    private Stack<HistoryEntry> _redoStack = [];
    private bool _isScientificVisible = false;

    public MainWindow()
    {
        InitializeComponent();
        input.Focus();
    }

    private void SaveState()
    {
        _undoStack.Push(new HistoryEntry(input.Text, output.Text));
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
        }
        catch (ParseException ex)
        {
            output.Text = $"{ex.Message}";
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
            SaveState();
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        input.Text = "";
        output.Text = "";
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_undoStack.TryPop(out HistoryEntry? entry))
        {
            // Store the current state so we can redo.
            _redoStack.Push(entry);

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

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_redoStack.TryPop(out HistoryEntry? entry))
        {
            // Save the current state to undo before jumping forward.
            _undoStack.Push(entry);

            // Restore the entry.
            input.Text = entry.Input;
            output.Text = entry.Output;
        }
    }

    private void EraseButton_Click(object sender, RoutedEventArgs e)
    {
        if (input.Text.Length > 0)
        {
            input.Text = input.Text[..^1];
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EvalButton_Click(this, new());
        }
    }

    private void ToggleScientific_Click(object sender, RoutedEventArgs e)
    {
        // TODO: fix window resizing may break the animation start.

        var widthAnimation = new DoubleAnimation
        {
            Duration = TimeSpan.FromSeconds(0.3),
            EasingFunction = new ExponentialEase(),
        };

        if (!_isScientificVisible)
        {
            // Show the column.
            ScientificColumn.Width = new GridLength(1, GridUnitType.Star);
            widthAnimation.To = ActualWidth + 100;
            Width += 100;
        }
        else
        {
            // Hide the column.
            ScientificColumn.Width = new GridLength(0);
            widthAnimation.To = ActualWidth - 100;
            Width -= 100;
            // We use a completed event to set column width to 0 only after
            // the window has finished shrinking.
            // widthAnimation.Completed += (s, ev) =>
            // {
            //     ScientificColumn.Width = new GridLength(0);
            // };
        }
        _isScientificVisible = !_isScientificVisible;
        BeginAnimation(WidthProperty, widthAnimation);
    }

    private void MathFunc_Click(object sender, RoutedEventArgs e)
    {
        switch (((Button)sender).Content)
        {
        case "Pi":
            input.Text += "Pi";
            break;

        case "E":
            input.Text += "E";
            break;

        case "abs":
            input.Text += "abs(";
            break;

        case "sqrt":
            input.Text += "sqrt(";
            break;

        case "pow":
            input.Text += "^";
            break;

        case "log":
            input.Text += "log(";
            break;
        }
    }
}
