namespace LAC3;

public partial class MainWindow : Window
{
    internal Grid activePanel;
    internal bool isListening;
    internal int _nextProfileIndex = 0;
    private string _currentlyEditing = string.Empty;

    internal static ClickBind Temp_ActivationBind;
    internal static ClickBind Temp_ActionBind;
    private const string SettingsFileName = "State.json";

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        timeBeginPeriod(1);
        activePanel = MainPanel;

        InnerBorder.SizeChanged += (s, e) =>
        {
            var rect = new Rect(0, 0, InnerBorder.ActualWidth, InnerBorder.ActualHeight);
            InnerBorder.Clip = new RectangleGeometry(rect, 8, 8);
        };
        Bind.PreviewKeyDown += SetKeyBinding;
        BindClick.PreviewKeyDown += SetKeyPressBinding;
        Bind.PreviewMouseDown += SetMouseBinding;
        BindClick.PreviewMouseDown += SetMouseClickBinding;

        foreach (var kliker in GetAllKlikers())
            AddProfileButtonBoot(kliker.ClickerName);

        Loaded += MainWindow_Loaded;
    }
    
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await GetTitleAsync();
    }



    internal void SaveSettings()
    {
        var snapshot = new
        {
            PerfMode,
            PerfModeTime,
            BIPM,
            Klikers = ClickerManager
                .GetAllKlikers()
                .ToDictionary(
                    k => k.ClickerName,
                    k => new
                    {
                        ActivationKey = k.ActivationBind.Key.HasValue ? k.ActivationBind.Key.Value.ToString() : null,
                        ActivationMouse = k.ActivationBind.Mouse.HasValue ? k.ActivationBind.Mouse.Value.ToString() : null,
                        ActionKey = k.ActionBind.Key.HasValue ? k.ActionBind.Key.Value.ToString() : null,
                        ActionMouse = k.ActionBind.Mouse.HasValue ? k.ActionBind.Mouse.Value.ToString() : null,
                        k.HoldDuration,
                        k.Delay,
                        k.MaxDelay,
                        k.BurstCount,
                        k.HoldMode,
                        k.ToggleMode,
                        k.BurstMode,
                        ShouldSpam = k.BurstMode || k.HoldMode
                    }
                )
        };

        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(snapshot, opts);
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName), json);
        SendLogMessage($"State saved to {SettingsFileName}");
    }






    internal void LoadSettings()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
        if (!File.Exists(path))
        {
            SendLogMessage("No state file found; using defaults.");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            // 1) Restore global flags + update UI
            PerfMode = root.GetProperty("PerfMode").GetBoolean();
            PerfModeTime = root.GetProperty("PerfModeTime").GetInt32();
            BIPM = root.GetProperty("BIPM").GetBoolean();

            PerfCB.IsChecked = PerfMode;
            PerfCheck.Visibility = PerfMode ? Visibility.Visible : Visibility.Hidden;
            PMT.Text = PerfModeTime.ToString();

            BIPMCB.IsChecked = BIPM;
            BIPMCheck.Visibility = BIPM ? Visibility.Visible : Visibility.Hidden;

            // 2) Clear out existing profiles
            ClickerManager.ClearAll();
            ProfilesPanel.Children.Clear();
            _nextProfileIndex = 0;

            // 3) Recreate from JSON
            foreach (var prop in root.GetProperty("Klikers").EnumerateObject())
            {
                var name = prop.Name;
                var data = prop.Value;

                try
                {
                    // read the four raw strings
                    string? actKeyStr = data.GetProperty("ActivationKey").GetString();
                    string? actMouseStr = data.GetProperty("ActivationMouse").GetString();
                    string? act2KeyStr = data.GetProperty("ActionKey").GetString();
                    string? act2MouseStr = data.GetProperty("ActionMouse").GetString();

                    // build Activation bind
                    ClickBind activation = actKeyStr is not null
                        ? new ClickBind { Key = (Key)Enum.Parse(typeof(Key), actKeyStr) }
                        : actMouseStr is not null
                        ? new ClickBind { Mouse = (MouseButton)Enum.Parse(typeof(MouseButton), actMouseStr) }
                        : throw new InvalidOperationException("Neither ActivationKey nor ActivationMouse present.");

                    // build Action bind
                    ClickBind action = act2KeyStr is not null
                        ? new ClickBind { Key = (Key)Enum.Parse(typeof(Key), act2KeyStr) }
                        : act2MouseStr is not null
                        ? new ClickBind { Mouse = (MouseButton)Enum.Parse(typeof(MouseButton), act2MouseStr) }
                        : throw new InvalidOperationException("Neither ActionKey nor ActionMouse present.");

                    // primitives
                    var hold = (ushort)data.GetProperty("HoldDuration").GetUInt32();
                    var delay = (ushort)data.GetProperty("Delay").GetUInt32();
                    var maxDelay = (ushort)data.GetProperty("MaxDelay").GetUInt32();
                    var burstCount = (ushort)data.GetProperty("BurstCount").GetUInt32();
                    var holdMode = data.GetProperty("HoldMode").GetBoolean();
                    var toggleMode = data.GetProperty("ToggleMode").GetBoolean();
                    var burstMode = data.GetProperty("BurstMode").GetBoolean();
                    var shouldSpam = data.GetProperty("ShouldSpam").GetBoolean();

                    // recreate profile & UI
                    ClickerManager.CreateKliker(
                        name,
                        activation,
                        action,
                        hold,
                        delay,
                        maxDelay,
                        burstCount,
                        holdMode,
                        toggleMode,
                        burstMode,
                        shouldSpam
                    );
                }
                catch (Exception ex)
                {
                    SendLogMessage($"Skipping '{name}': {ex.Message}");
                }
            }

            SendLogMessage($"Loaded state from {SettingsFileName}");
        }
        catch (Exception ex)
        {
            SendLogMessage($"Unable to load state: {ex.Message}");
        }
    }
    private void PerformanceMode(object sender, RoutedEventArgs e)
    {
        var cb = (CheckBox)sender;
        if (cb.IsChecked == true)
        {
            PerfMode = true;
            PerfModeTime = 15;
            if (PerfCheck != null)
                PerfCheck.Visibility = Visibility.Visible;
            SendLogMessage($"Performance Mode is now {PerfMode}, minimum delay will be {PerfModeTime}ms");
        }
        else
        {
            PerfMode = false;
            if (PerfCheck != null)
                PerfCheck.Visibility = Visibility.Hidden;
            SendLogMessage($"Performance Mode is now {PerfMode}, minimum delay will be 0ms");
        }
    }

    private void BurstIgnorePerfMode(object sender, RoutedEventArgs e)
    {
        var cb = (CheckBox)sender;
        if (cb.IsChecked == true)
        {
            BIPM = true;
            if (BIPMCheck != null)
                BIPMCheck.Visibility = Visibility.Visible;
            SendLogMessage($"BIPM is now {BIPM}, minimum delay for Burst-Mode will be 0ms");
        }
        else
        {
            BIPM = false;
            if (BIPMCheck != null)
                BIPMCheck.Visibility = Visibility.Hidden;
            SendLogMessage($"BIPM is now {BIPM}, minimum delay for Burst-Mode will be {PerfModeTime}ms");
        }
    }
    private void PMT_LostFocus(object sender, RoutedEventArgs e) => ParsePerformanceTime();

    private void PMT_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ParsePerformanceTime();
            Keyboard.ClearFocus();
        }
    }

    private void ParsePerformanceTime()
    {
        if (int.TryParse(PMT.Text, out int result))
        {
            PerfModeTime = result;
            SendLogMessage($"Performance time set to {PerfModeTime}");
        }
        else
        {
            SendLogMessage("Invalid input for performance time.");
            PMT.Text = PerfModeTime.ToString();
        }
    }

    private void ResetSettings(object sender, RoutedEventArgs e)
    {
        PerfModeTime = 10;
        PMT.Text = $"{PerfModeTime}";
        PerfMode = true;
        PerfCheck.Visibility = Visibility.Visible;
        PerfCB.IsChecked = true;
        BIPM = false;
        BIPMCheck.Visibility = Visibility.Hidden;
        BIPMCB.IsChecked = false;
        SendLogMessage("Settings have been reset to default values.");
    }

    private void ToSettingsPanel(object sender, RoutedEventArgs e)
    {
        if (activePanel == SettingsPanel) return;
        SlidePanelY(activePanel, SettingsPanel, -1);
    }

    private void ToProfilePanel(object sender, RoutedEventArgs e)
    {
        if (activePanel == ProfilePanel) return;
        SlidePanelY(activePanel, ProfilePanel, 1);
    }

    private void ToKlikerPanelY(object sender, RoutedEventArgs e)
    {
        if (activePanel == KlikerPanel) return;
        SlidePanelY(activePanel, KlikerPanel, -1);
    }

    private void ToLogPanelY(object sender, RoutedEventArgs e)
    {
        if (activePanel == LogPanel) return;
        SlidePanelY(activePanel, LogPanel, 1);
    }

    private void ToKlikerPanel(object sender, RoutedEventArgs e)
    {
        if (activePanel == KlikerPanel) return;
        SlidePanelX(activePanel, KlikerPanel, 1);
    }

    private void ToLogPanel(object sender, RoutedEventArgs e)
    {
        if (activePanel == LogPanel) return;
        SlidePanelX(activePanel, LogPanel, -1);
    }

    private void ToMainPanel(object sender, RoutedEventArgs e)
    {
        if (activePanel == MainPanel) return;
        var dir = activePanel == KlikerPanel ? -1 : 1;
        SlidePanelX(activePanel, MainPanel, dir);
    }

    private void SlidePanelX(Grid from, Grid to, double direction)
    {
        double offset = LayoutRoot.ActualWidth;
        to.Visibility = Visibility.Visible;
        var ttTo = (TranslateTransform)to.RenderTransform;
        ttTo.X = direction * offset;
        var duration = new Duration(TimeSpan.FromMilliseconds(500));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        var animOut = new DoubleAnimation(0, -direction * offset, duration)
        { EasingFunction = easing };
        Storyboard.SetTarget(animOut, from);
        Storyboard.SetTargetProperty(animOut,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        sb.Children.Add(animOut);
        var animIn = new DoubleAnimation(direction * offset, 0, duration)
        { EasingFunction = easing };
        Storyboard.SetTarget(animIn, to);
        Storyboard.SetTargetProperty(animIn,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        sb.Children.Add(animIn);
        sb.Completed += (s, ev) =>
        {
            from.Visibility = Visibility.Collapsed;
            ((TranslateTransform)from.RenderTransform).X = 0;
            ((TranslateTransform)to.RenderTransform).X = 0;
            activePanel = to;
        };
        sb.Begin();
    }

    private void SlidePanelY(Grid from, Grid to, double direction)
    {
        double offset = LayoutRoot.ActualHeight;
        to.Visibility = Visibility.Visible;
        var ttTo = (TranslateTransform)to.RenderTransform;
        ttTo.Y = direction * offset;
        var duration = new Duration(TimeSpan.FromMilliseconds(500));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        var animOut = new DoubleAnimation(0, -direction * offset, duration)
        { EasingFunction = easing };
        Storyboard.SetTarget(animOut, from);
        Storyboard.SetTargetProperty(animOut,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        sb.Children.Add(animOut);
        var animIn = new DoubleAnimation(direction * offset, 0, duration)
        { EasingFunction = easing };
        Storyboard.SetTarget(animIn, to);
        Storyboard.SetTargetProperty(animIn,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        sb.Children.Add(animIn);
        sb.Completed += (s, ev) =>
        {
            from.Visibility = Visibility.Collapsed;
            ((TranslateTransform)from.RenderTransform).Y = 0;
            ((TranslateTransform)to.RenderTransform).Y = 0;
            activePanel = to;
        };
        sb.Begin();
    }

    private void LogOutput(object sender, TextChangedEventArgs e)
    {
    }

    private void SetMouseBinding(object sender, MouseButtonEventArgs e)
    {
        if (!isListening)
        {
            isListening = true;
            Bind.Text = "Press any key or mouse button…";
            e.Handled = true;
            Keyboard.Focus(Bind);
        }
        else
        {
            var bind = new ClickBind { Mouse = e.ChangedButton };
            Temp_ActivationBind = bind;

            Bind.Text = e.ChangedButton.ToString();
            isListening = false;
            e.Handled = true;
        }
    }

    private void SetKeyBinding(object sender, KeyEventArgs e)
    {
        if (!isListening) return;

        var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        var bind = new ClickBind { Key = key };
        Temp_ActivationBind = bind;

        Bind.Text = key.ToString();
        isListening = false;
        e.Handled = true;
    }

    private void SetMouseClickBinding(object sender, MouseButtonEventArgs e)
    {
        if (!isListening)
        {
            isListening = true;
            BindClick.Text = "Press any key or mouse button…";
            e.Handled = true;
            Keyboard.Focus(BindClick);
        }
        else
        {
            var bind = new ClickBind { Mouse = e.ChangedButton };
            Temp_ActionBind = bind;

            BindClick.Text = e.ChangedButton.ToString();
            isListening = false;
            e.Handled = true;
        }
    }

    private void SetKeyPressBinding(object sender, KeyEventArgs e)
    {
        if (!isListening) return;

        var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        var bind = new ClickBind { Key = key };
        Temp_ActionBind = bind;

        BindClick.Text = key.ToString();
        isListening = false;
        e.Handled = true;
    }
    private void MaxDelayTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void DelayTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void HoldDurTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);
    private void BurstCountTextBox(object sender, TextCompositionEventArgs e) => e.Handled = !int.TryParse(e.Text, out _);

    private void ProfileNameTextBox(object sender, TextCompositionEventArgs e)
    {
    }


    private void SaveProfileChanges(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentlyEditing))
            return;
        var newName = NProfileNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }
        bool nameClash = ProfilesPanel.Children.OfType<Button>().Select(b => b.Content?.ToString()).Any(content => content != null && content.Equals(newName, StringComparison.OrdinalIgnoreCase) && !content.Equals(_currentlyEditing, StringComparison.OrdinalIgnoreCase));

        if (nameClash)
        {
            return;
        }

        ClickBind activation = Temp_ActivationBind;
        ClickBind action = Temp_ActionBind;

        if (!(ushort.TryParse(NHoldDurTextBox.Text, out var holdDur) &&
              ushort.TryParse(NdelayTextBox.Text, out var delay) &&
              ushort.TryParse(NMaxDelayTextBox.Text, out var maxDelay)))
        {
            return;
        }

        if (!ushort.TryParse(NBurstCountTextBox.Text, out var burstCount))
        {
            return;
        }

        var holdMode = HTC.IsChecked == true;
        var toggleMode = TTC.IsChecked == true;
        var burstMode = PTB.IsChecked == true;

        if (!newName.Equals(_currentlyEditing, StringComparison.OrdinalIgnoreCase))
        {
            DeleteKliker(_currentlyEditing);
            CreateKliker(
                newName,
                activation,
                action,
                holdDur,
                delay,
                maxDelay,
                burstCount,
                holdMode,
                toggleMode,
                burstMode,
                shouldSpam: false
            );
            var btn = ProfilesPanel.Children.OfType<Button>().First(b => b.Content.ToString() == _currentlyEditing);
            btn.Content = newName;
            _currentlyEditing = newName;
        }
        else
        {
            bool ok = UpdateKliker(
                newName,
                activation,
                action,
                holdDur,
                delay,
                maxDelay,
                burstCount,
                holdMode,
                toggleMode,
                burstMode
            );
            if (!ok)
            {
                SendLogMessage($"Failed to update profile '{newName}'.");
                return;
            }
        }
        var existing = GetKliker(_currentlyEditing);
        DebugNameLabel.Content = $"Name: {newName}";
        if (existing != null)
        {
            DebugActivationLabel.Content = existing.ActivationBind.IsKey ? existing.ActivationBind.Key.ToString() : existing.ActivationBind.Mouse.ToString();
            DebugActionLabel.Content = existing.ActionBind.IsKey ? existing.ActionBind.Key.ToString() : existing.ActionBind.Mouse.ToString();
        }
        DebugHoldDurLabel.Content = $"HoldDuration: {holdDur}";
        DebugDelayLabel.Content = $"Delay: {delay}";
        DebugMaxDelayLabel.Content = $"MaxDelay: {maxDelay}";
        DebugBurstCountLabel.Content = $"BurstCount: {burstCount}";
        DebugHoldModeLabel.Content = $"HoldMode: {holdMode}";
        DebugToggleModeLabel.Content = $"ToggleMode: {toggleMode}";
        DebugBurstModeLabel.Content = $"BurstMode: {burstMode}";
        DebugThreadLabel.Content = $"Profile is on CPU Thread #{existing?.ThreadId.ToString()}";
    }

    private void PTBChecked(object sender, RoutedEventArgs e)
    {
        TTC.IsChecked = false;
        HTC.IsChecked = false;
    }

    private void TTCChecked(object sender, RoutedEventArgs e)
    {
        PTB.IsChecked = false;
        HTC.IsChecked = false;
    }

    private void HTCChecked(object sender, RoutedEventArgs e)
    {
        PTB.IsChecked = false;
        TTC.IsChecked = false;
    }

    private void ProfileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var name = ProfileNameTextBox2.Text.Trim();

        bool hasText = !string.IsNullOrWhiteSpace(name);
        bool alreadyExists = ProfilesPanel.Children.OfType<Button>().Any(b => b.Content is string content && content.Equals(name, StringComparison.OrdinalIgnoreCase));

        AddProfileButton.IsEnabled = hasText && !alreadyExists;

        TextNull = !hasText;
        NameExists = alreadyExists;

        ProfileNameTextBox2.ClearValue(ToolTipProperty);
    }

    private void AddProfileButtonBoot(string profileName)
    {
        var btn = new Button
        {
            Content = profileName,
            Cursor = Cursors.Hand,
            Width = double.NaN,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Opacity = 0.75,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(84, 84, 84)),
            Foreground = Brushes.White,
        };
        btn.Click += ProfileButton;
        ProfilesPanel.Children.Add(btn);
    }

    private void DeleteProfile(object sender, RoutedEventArgs e)
    {
        var name = ProfileNameTextBox2.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var btn = ProfilesPanel.Children
                               .OfType<Button>()
                               .FirstOrDefault(b =>
                                   string.Equals((string)b.Content, name, StringComparison.OrdinalIgnoreCase));
        if (btn == null)
            return;

        DeleteKliker(name);
        ProfilesPanel.Children.Remove(btn);

        ProfileNameTextBox2.Text = "";
    }


    private void ClearAllProfiles(object sender, RoutedEventArgs e)
    {
        ClearAll();

        ProfilesPanel.Children.Clear();

        _currentlyEditing = string.Empty;
        EditingLabel.Content = string.Empty;
        ProfileNameTextBox2.Text = "";
        Bind.Text = "";
        BindClick.Text = "";
        NHoldDurTextBox.Text = "";
        NdelayTextBox.Text = "";
        NMaxDelayTextBox.Text = "";
        NBurstCountTextBox.Text = "";
        HTC.IsChecked = false;
        TTC.IsChecked = false;
        PTB.IsChecked = false;
    }

    private void AddProfile(object sender, RoutedEventArgs e)
    {
        var name = ProfileNameTextBox2.Text.Trim();
        bool alreadyExists = ProfilesPanel.Children.OfType<Button>().Any(b => string.Equals(b.Content?.ToString(), name, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(name) || alreadyExists)
            return;
        var btn = new Button
        {
            Content = name,
            Cursor = Cursors.Hand,
            Width = double.NaN,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Opacity = 0.75,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(84, 84, 84)),
            Foreground = Brushes.White,
        };
        btn.Click += ProfileButton;
        ProfilesPanel.Children.Add(btn);
        var defaultActivation = new ClickBind { Key = Key.F1 };
        var defaultAction = new ClickBind { Mouse = MouseButton.Left };

        CreateKliker(
            name,
            defaultActivation,
            defaultAction,
            holdDuration: 0,
            delay: 0,
            maxDelay: 0,
            burstCount: 0,
            holdMode: true,
            toggleMode: false,
            burstMode: false,
            shouldSpam: true
        );
        ProfileNameTextBox2.Text = "";
    }

    private void ProfileButton(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Content is not string profileName) return;
        var clicker = GetKliker(profileName);
        if (clicker == null) return;

        _currentlyEditing = profileName;
        EditingLabel.Content = $"Editing {profileName}";
        NProfileNameTextBox.Text = clicker.ClickerName;
        var act = clicker.ActivationBind;
        Bind.Text = act.IsKey ? act.Key.ToString() : act.Mouse.ToString();
        Temp_ActivationBind = act;
        var act2 = clicker.ActionBind;
        BindClick.Text = act2.IsKey ? act2.Key.ToString() : act2.Mouse.ToString();
        Temp_ActionBind = act2;
        NHoldDurTextBox.Text = clicker.HoldDuration.ToString();
        NdelayTextBox.Text = clicker.Delay.ToString();
        NMaxDelayTextBox.Text = clicker.MaxDelay.ToString();
        NBurstCountTextBox.Text = clicker.BurstCount.ToString();
        HTC.IsChecked = clicker.HoldMode;
        TTC.IsChecked = clicker.ToggleMode;
        PTB.IsChecked = clicker.BurstMode;
        DebugNameLabel.Content = $"Name: {clicker.ClickerName}";
        DebugActivationLabel.Content = clicker.ActivationBind.IsKey ? clicker.ActivationBind.Key.ToString() : clicker.ActivationBind.Mouse.ToString();
        DebugActionLabel.Content = clicker.ActionBind.IsKey ? clicker.ActionBind.Key.ToString() : clicker.ActionBind.Mouse.ToString();
        DebugHoldDurLabel.Content = $"HoldDuration: {clicker.HoldDuration.ToString()}";
        DebugDelayLabel.Content = $"Delay: {clicker.Delay.ToString()}";
        DebugMaxDelayLabel.Content = $"MaxDelay: {clicker.MaxDelay.ToString()}";
        DebugBurstCountLabel.Content = $"BurstCount: {clicker.BurstCount.ToString()}";
        DebugHoldModeLabel.Content = $"HoldMode: {clicker.HoldMode.ToString()}";
        DebugToggleModeLabel.Content = $"ToggleMode: {clicker.ToggleMode.ToString()}";
        DebugBurstModeLabel.Content = $"BurstMode: {clicker.BurstMode.ToString()}";
        DebugThreadLabel.Content = $"Profile is on CPU Thread #{clicker.ThreadId.ToString()}";

        ToProfilePanel(sender, e);
    }

    private void LoadSettingsbtn(object sender, RoutedEventArgs e)
    {
        SendLogMessage("Sent load state request...");
        LoadSettings();
    }

    private void SaveSettingsbtn(object sender, RoutedEventArgs e)
    {
        SendLogMessage("Sent save state request...");
        SaveSettings();
    }

    private async void CoinFlip(object sender, RoutedEventArgs e)
    {
        CoinLabel.Content = "...";
        await Task.Delay(500);
        var result = RandomGenerator.Next(2) == 0 ? "Heads" : "Tails";
        CoinLabel.Content = result;
    }
}
