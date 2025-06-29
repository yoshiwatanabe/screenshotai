using GalleryViewer.Models;
using GalleryViewer.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GalleryViewer.Views;

public partial class GalleryWindow : Window
{
    public GalleryViewModel ViewModel { get; }

    public GalleryWindow(GalleryViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        Loaded += GalleryWindow_Loaded;
        KeyDown += GalleryWindow_KeyDown;
    }

    private async void GalleryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void ScreenshotTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) // Double-click
        {
            var border = sender as FrameworkElement;
            var screenshot = border?.DataContext as ScreenshotViewModel;
            
            if (screenshot != null)
            {
                ViewModel.SelectedScreenshot = screenshot;
                screenshot.OpenImageCommand.Execute(null);
            }
        }
        else // Single-click
        {
            var border = sender as FrameworkElement;
            var screenshot = border?.DataContext as ScreenshotViewModel;
            
            if (screenshot != null)
            {
                ViewModel.SelectedScreenshot = screenshot;
            }
        }
    }

    private void GalleryWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle keyboard shortcuts
        switch (e.Key)
        {
            case Key.F5:
                ViewModel.RefreshCommand.Execute(null);
                e.Handled = true;
                break;
                
            case Key.Delete:
                if (ViewModel.SelectedScreenshot != null)
                {
                    ViewModel.DeleteSelectedCommand.Execute(null);
                    e.Handled = true;
                }
                break;
                
            case Key.Enter:
                if (ViewModel.SelectedScreenshot != null)
                {
                    ViewModel.OpenSelectedCommand.Execute(null);
                    e.Handled = true;
                }
                break;
                
            case Key.Escape:
                ViewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
                break;
                
            case Key.F when e.KeyboardDevice.Modifiers == ModifierKeys.Control:
                // Focus search box when Ctrl+F is pressed
                // TODO: Implement focus search box
                e.Handled = true;
                break;
        }
    }
}

// Helper converters for XAML bindings
public static class Converters
{
    public static readonly BooleanToVisibilityConverter BoolToVisibilityConverter = new();
    
    public static readonly IValueConverter InvertBooleanConverter = new InvertBooleanConverter();
    
    public static readonly IValueConverter IsNotNullConverter = new IsNotNullConverter();
    
    public static readonly IValueConverter StringToVisibilityConverter = new StringToVisibilityConverter();
    
    public static readonly IValueConverter GreaterThanZeroToVisibilityConverter = new GreaterThanZeroToVisibilityConverter();
}

public class InvertBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : false;
    }
}

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is int intValue && intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}