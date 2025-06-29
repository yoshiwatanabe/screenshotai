using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotCapture.UI;

public partial class AreaSelectionOverlay : Form
{
    private Point _startPoint;
    private Point _endPoint;
    private bool _isSelecting;
    private Rectangle _selectionRectangle;
    private TaskCompletionSource<Rectangle>? _selectionTask;

    public AreaSelectionOverlay()
    {
        InitializeComponent();
        SetupOverlay();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form properties
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(800, 600);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Name = "AreaSelectionOverlay";
        this.Text = "Select Area";
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.KeyPreview = true;
        
        this.ResumeLayout(false);
    }

    private void SetupOverlay()
    {
        // Make form cover all screens
        var bounds = GetVirtualScreenBounds();
        this.Bounds = bounds;
        
        // Semi-transparent background
        this.BackColor = Color.Black;
        this.Opacity = 0.3;
        
        // Enable double buffering for smooth drawing
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | 
                     ControlStyles.ResizeRedraw, true);
        
        // Set cursor to crosshair
        this.Cursor = Cursors.Cross;
        
        // Wire up events
        this.MouseDown += OnMouseDown;
        this.MouseMove += OnMouseMove;
        this.MouseUp += OnMouseUp;
        this.KeyDown += OnKeyDown;
        this.Paint += OnPaint;
    }

    public Task<Rectangle> ShowSelectionAsync()
    {
        _selectionTask = new TaskCompletionSource<Rectangle>();
        this.Show();
        this.Focus();
        return _selectionTask.Task;
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _startPoint = e.Location;
            _endPoint = e.Location;
            _isSelecting = true;
            _selectionRectangle = Rectangle.Empty;
            this.Invalidate();
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            _endPoint = e.Location;
            UpdateSelectionRectangle();
            this.Invalidate();
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isSelecting)
        {
            _isSelecting = false;
            _endPoint = e.Location;
            UpdateSelectionRectangle();
            
            // Complete the selection
            CompleteSelection(_selectionRectangle);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            // Cancel selection
            CompleteSelection(Rectangle.Empty);
        }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        if (_isSelecting && !_selectionRectangle.IsEmpty)
        {
            // Draw selection rectangle
            using var pen = new Pen(Color.Red, 2);
            e.Graphics.DrawRectangle(pen, _selectionRectangle);
            
            // Draw selection info
            var info = $"{_selectionRectangle.Width} x {_selectionRectangle.Height}";
            using var brush = new SolidBrush(Color.White);
            using var font = new Font("Arial", 12, FontStyle.Bold);
            
            var textSize = e.Graphics.MeasureString(info, font);
            var textLocation = new PointF(
                _selectionRectangle.X + (_selectionRectangle.Width - textSize.Width) / 2,
                _selectionRectangle.Y - textSize.Height - 5);
            
            // Draw background for text
            using var backgroundBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
            e.Graphics.FillRectangle(backgroundBrush, 
                textLocation.X - 5, textLocation.Y - 2, 
                textSize.Width + 10, textSize.Height + 4);
            
            e.Graphics.DrawString(info, font, brush, textLocation);
        }
    }

    private void UpdateSelectionRectangle()
    {
        var x = Math.Min(_startPoint.X, _endPoint.X);
        var y = Math.Min(_startPoint.Y, _endPoint.Y);
        var width = Math.Abs(_endPoint.X - _startPoint.X);
        var height = Math.Abs(_endPoint.Y - _startPoint.Y);
        
        _selectionRectangle = new Rectangle(x, y, width, height);
    }

    private void CompleteSelection(Rectangle selection)
    {
        this.Hide();
        _selectionTask?.SetResult(selection);
        _selectionTask = null;
    }

    private Rectangle GetVirtualScreenBounds()
    {
        var left = SystemInformation.VirtualScreen.Left;
        var top = SystemInformation.VirtualScreen.Top;
        var width = SystemInformation.VirtualScreen.Width;
        var height = SystemInformation.VirtualScreen.Height;
        
        return new Rectangle(left, top, width, height);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // If form is closing without completion, cancel the task
        _selectionTask?.TrySetResult(Rectangle.Empty);
        base.OnFormClosing(e);
    }
}