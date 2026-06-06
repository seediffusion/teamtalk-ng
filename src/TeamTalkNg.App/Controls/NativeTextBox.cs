using System.Windows;
using System.Windows.Forms.Integration;
using Drawing = System.Drawing;
using Media = System.Windows.Media;
using Forms = System.Windows.Forms;

namespace TeamTalkNg.App.Controls;

public sealed class NativeTextBox : WindowsFormsHost
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(NativeTextBox),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextPropertyChanged));

    public static readonly DependencyProperty TextBackgroundProperty = DependencyProperty.Register(
        nameof(TextBackground),
        typeof(Media.Brush),
        typeof(NativeTextBox),
        new PropertyMetadata(System.Windows.SystemColors.WindowBrush, OnColorPropertyChanged));

    public static readonly DependencyProperty TextForegroundProperty = DependencyProperty.Register(
        nameof(TextForeground),
        typeof(Media.Brush),
        typeof(NativeTextBox),
        new PropertyMetadata(System.Windows.SystemColors.ControlTextBrush, OnColorPropertyChanged));

    private readonly Forms.TextBox textBox = new();
    private bool syncingText;

    public NativeTextBox()
    {
        textBox.BorderStyle = Forms.BorderStyle.FixedSingle;
        textBox.AcceptsReturn = false;
        textBox.Multiline = false;
        textBox.Font = new Drawing.Font("Segoe UI", 9F);
        textBox.TextChanged += TextBox_OnTextChanged;
        textBox.KeyDown += TextBox_OnKeyDown;
        Child = textBox;
        Loaded += (_, _) => ApplyColors();
    }

    public event EventHandler? SendRequested;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Media.Brush TextBackground
    {
        get => (Media.Brush)GetValue(TextBackgroundProperty);
        set => SetValue(TextBackgroundProperty, value);
    }

    public Media.Brush TextForeground
    {
        get => (Media.Brush)GetValue(TextForegroundProperty);
        set => SetValue(TextForegroundProperty, value);
    }

    public bool FocusNativeEdit()
    {
        return textBox.Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            textBox.TextChanged -= TextBox_OnTextChanged;
            textBox.KeyDown -= TextBox_OnKeyDown;
            textBox.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void OnTextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (NativeTextBox)dependencyObject;
        string newText = e.NewValue as string ?? string.Empty;
        if (string.Equals(control.textBox.Text, newText, StringComparison.Ordinal))
        {
            return;
        }

        control.syncingText = true;
        try
        {
            control.textBox.Text = newText;
            control.textBox.SelectionStart = control.textBox.TextLength;
        }
        finally
        {
            control.syncingText = false;
        }
    }

    private static void OnColorPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((NativeTextBox)dependencyObject).ApplyColors();
    }

    private void TextBox_OnTextChanged(object? sender, EventArgs e)
    {
        if (syncingText)
        {
            return;
        }

        SetCurrentValue(TextProperty, textBox.Text);
    }

    private void TextBox_OnKeyDown(object? sender, Forms.KeyEventArgs e)
    {
        if (e.KeyCode != Forms.Keys.Enter || e.Shift || e.Control || e.Alt)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        SendRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyColors()
    {
        if (TryGetColor(TextBackground, out Drawing.Color background))
        {
            textBox.BackColor = background;
        }

        if (TryGetColor(TextForeground, out Drawing.Color foreground))
        {
            textBox.ForeColor = foreground;
        }
    }

    private static bool TryGetColor(Media.Brush brush, out Drawing.Color color)
    {
        if (brush is Media.SolidColorBrush solidColorBrush)
        {
            System.Windows.Media.Color mediaColor = solidColorBrush.Color;
            color = Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            return true;
        }

        color = Drawing.Color.Empty;
        return false;
    }
}
