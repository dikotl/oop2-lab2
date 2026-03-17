using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Calculator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Evaluator _evaluator = new();
    private List<string> _inputHistory = [""];
    private int _historyPointer = -1;
    private bool _isScientificVisible = false;
    private bool _inHistory = false;

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

    private void InputButton_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();
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
            if (!_inHistory) _inputHistory.Add(input.Text);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();
        input.Text = "";
        output.Text = "";
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
        EvalButton_Click(this, new());
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_historyPointer >= 0 && _historyPointer < _inputHistory.Count - 1)
        {
            input.Text = _inputHistory[++_historyPointer];
            _inHistory = true;
            EvalButton_Click(this, new());
        }
    }

    private void EraseButton_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();

        if (input.Text.Length > 0)
        {
            input.Text = input.Text[..^1];
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        QuitHistory();

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EvalButton_Click(this, new());
        }
    }

    private void ToggleScientific_Click(object sender, RoutedEventArgs e)
    {
        QuitHistory();

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
        QuitHistory();

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
