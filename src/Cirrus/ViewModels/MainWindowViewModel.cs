using Windows.ApplicationModel;
using Cirrus.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

namespace Cirrus.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
#if CIRRUS_PREVIEW
    // Unpackaged apps do not have Windows.ApplicationModel.Package.Current.
    public string ApplicationName => "Cirrus";
#else
    public string ApplicationName => Package.Current.DisplayName;
#endif

    [ObservableProperty]
    public partial InfoBarSeverity NotificationSeverity { get; set; } = InfoBarSeverity.Informational;

    [RelayCommand]
    public static void NavigateBack()
    {
        if (MainWindow.Current is not { } window) return;
        window.NavigationStack.Pop();
        if (window.NavigationStack.Peek() is not { } view) return;
        window.Navigate(view.ViewType, view.ViewModel, Enums.NavigationDirection.Backward);
    }

    [RelayCommand]
    private static void SwitchFullScreen()
    {
        if (MainWindow.Current is not { } window) return;
        var currentPresenterKind = window.AppWindow.Presenter.Kind;
        var targetPresenterKind = currentPresenterKind is AppWindowPresenterKind.FullScreen
            ? AppWindowPresenterKind.Default
            : AppWindowPresenterKind.FullScreen;
        window.AppWindow.SetPresenter(targetPresenterKind);
    }
}