<<<<<<< HEAD
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace SwishLauncher.App.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private ElementTheme _appTheme = ElementTheme.Default;

    [ObservableProperty]
    private bool _useHighContrast;

    public SettingsViewModel()
    {
        Title = "Settings";
    }

    /// <summary>
    /// Called by SettingsPage.xaml.cs after the command fires
    /// to propagate the theme to the root FrameworkElement.
    /// </summary>
    [RelayCommand]
    private void SetTheme(ElementTheme theme) => AppTheme = theme;
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SwishLauncher.App.ViewModels;

public partial class SettingsViewModel : ObservableObject { }
>>>>>>> 9706f627a483bf1c8f3594c82126f8c90ca9edc6
