using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using settingsViews = MAAUnified.App.Features.Settings;
using MAAUnified.App.ViewModels.Settings;

namespace MAAUnified.App.Features.Root;

public partial class SettingsView : UserControl
{
    private static readonly string[] SectionOrder =
    [
        "ConfigurationManager",
        "Timer",
        "Performance",
        "Game",
        "Connect",
        "Start",
        "RemoteControl",
        "GUI",
        "Background",
        "ExternalNotification",
        "HotKey",
        "Achievement",
        "VersionUpdate",
        "IssueReport",
        "About",
    ];

    private readonly Dictionary<string, Border> _sectionAnchors = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _materializedSections = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _sectionTopCache = new(StringComparer.OrdinalIgnoreCase);
    private ScrollViewer? _sectionScrollViewer;
    private StackPanel? _sectionContentPanel;
    private bool _suppressSectionSelectionChanged;
    private bool _suppressSectionScrollChanged;
    private bool _sectionTopCacheDirty = true;
    private double _lastKnownExtentHeight = -1d;
    private double _lastKnownViewportHeight = -1d;
    private DispatcherTimer? _progressiveMaterializationTimer;

    public SettingsView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            _sectionScrollViewer = this.FindControl<ScrollViewer>("SectionScrollViewer");
            _sectionContentPanel = this.FindControl<StackPanel>("SectionContentPanel");
            if (_sectionContentPanel is not null)
            {
                _sectionContentPanel.SizeChanged += OnSectionContentPanelSizeChanged;
            }

            RebuildSectionAnchors();
            ResetSectionMaterialization();
            EnsureCurrentSectionMaterialized();
            Dispatcher.UIThread.Post(ScrollToSelectedSection, DispatcherPriority.Loaded);
            StartProgressiveSectionMaterialization();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            CancelProgressiveSectionMaterialization();
        };
        DataContextChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                RebuildSectionAnchors();
                ResetSectionMaterialization();
                EnsureCurrentSectionMaterialized();
                ScrollToSelectedSection();
                StartProgressiveSectionMaterialization();
            }, DispatcherPriority.Loaded);
        };
    }

    private SettingsPageViewModel? VM => DataContext as SettingsPageViewModel;

    private void OnSectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionSelectionChanged)
        {
            return;
        }

        Dispatcher.UIThread.Post(ScrollToSelectedSection, DispatcherPriority.Background);
    }

    private void OnSectionScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_suppressSectionScrollChanged)
        {
            return;
        }

        InvalidateSectionTopCacheIfLayoutChanged();
        TryMaterializeNextSectionForScroll();
        UpdateSelectedSectionFromScroll();
    }

    private void OnSectionContentPanelSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        InvalidateSectionTopCache();
        RefreshSectionTopCacheIfNeeded();
    }

    private void RebuildSectionAnchors()
    {
        _sectionAnchors.Clear();
        RegisterSectionAnchor("ConfigurationManager", "SectionConfigurationManager");
        RegisterSectionAnchor("Timer", "SectionTimer");
        RegisterSectionAnchor("Performance", "SectionPerformance");
        RegisterSectionAnchor("Game", "SectionGame");
        RegisterSectionAnchor("Connect", "SectionConnect");
        RegisterSectionAnchor("Start", "SectionStart");
        RegisterSectionAnchor("RemoteControl", "SectionRemoteControl");
        RegisterSectionAnchor("GUI", "SectionGui");
        RegisterSectionAnchor("Background", "SectionBackground");
        RegisterSectionAnchor("ExternalNotification", "SectionExternalNotification");
        RegisterSectionAnchor("HotKey", "SectionHotKey");
        RegisterSectionAnchor("Achievement", "SectionAchievement");
        RegisterSectionAnchor("VersionUpdate", "SectionVersionUpdate");
        RegisterSectionAnchor("IssueReport", "SectionIssueReport");
        RegisterSectionAnchor("About", "SectionAbout");
        InvalidateSectionTopCache();
    }

    private void RegisterSectionAnchor(string key, string controlName)
    {
        if (this.FindControl<Border>(controlName) is { } anchor)
        {
            _sectionAnchors[key] = anchor;
        }
    }

    private void ResetSectionMaterialization()
    {
        foreach (var anchor in _sectionAnchors.Values)
        {
            anchor.Child = null;
        }

        _materializedSections.Clear();
        InvalidateSectionTopCache();
    }

    private void EnsureCurrentSectionMaterialized()
    {
        var selectedKey = VM?.SelectedSection?.Key;
        if (string.IsNullOrWhiteSpace(selectedKey))
        {
            return;
        }

        EnsureSectionMaterialized(selectedKey);
    }

    private bool EnsureSectionsThrough(string key)
    {
        var anyMaterialized = false;
        foreach (var sectionKey in SectionOrder)
        {
            if (EnsureSectionMaterialized(sectionKey))
            {
                anyMaterialized = true;
            }

            if (string.Equals(sectionKey, key, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return anyMaterialized;
    }

    private bool EnsureSectionMaterialized(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || _materializedSections.Contains(key)
            || !_sectionAnchors.TryGetValue(key, out var anchor))
        {
            return false;
        }

        var content = CreateSectionContent(key);
        if (content is null)
        {
            return false;
        }

        anchor.Child = content;
        _materializedSections.Add(key);
        InvalidateSectionTopCache();
        if (VM is { } vm)
        {
            _ = vm.EnsureSectionDataLoadedAsync(key);
        }

        return true;
    }

    private Control? CreateSectionContent(string key)
    {
        return key switch
        {
            "ConfigurationManager" => new settingsViews.ConfigurationManagerView(),
            "Timer" => new settingsViews.TimerSettingsView(),
            "Performance" => new settingsViews.PerformanceSettingsView(),
            "Game" => new settingsViews.GameSettingsView(),
            "Connect" => BuildConnectSettingsView(),
            "Start" => new settingsViews.StartSettingsView(),
            "RemoteControl" => new settingsViews.RemoteControlSettingsView(),
            "GUI" => new settingsViews.GuiSettingsView(),
            "Background" => new settingsViews.BackgroundSettingsView(),
            "ExternalNotification" => new settingsViews.ExternalNotificationSettingsView(),
            "HotKey" => new settingsViews.HotKeySettingsView(),
            "Achievement" => new settingsViews.AchievementSettingsView(),
            "VersionUpdate" => new settingsViews.VersionUpdateSettingsView(),
            "IssueReport" => new settingsViews.IssueReportView(),
            "About" => new settingsViews.AboutSettingsView(),
            _ => null,
        };
    }

    private Control BuildConnectSettingsView()
    {
        var view = new settingsViews.ConnectSettingsView();
        if (VM is { } vm)
        {
            view.DataContext = vm.ConnectionGameSharedState;
        }

        return view;
    }

    private void InvalidateSectionTopCache()
    {
        _sectionTopCacheDirty = true;
    }

    private void InvalidateSectionTopCacheIfLayoutChanged()
    {
        if (_sectionScrollViewer is null)
        {
            return;
        }

        var extentHeight = _sectionScrollViewer.Extent.Height;
        var viewportHeight = _sectionScrollViewer.Viewport.Height;
        if (Math.Abs(extentHeight - _lastKnownExtentHeight) > 0.5d
            || Math.Abs(viewportHeight - _lastKnownViewportHeight) > 0.5d)
        {
            _lastKnownExtentHeight = extentHeight;
            _lastKnownViewportHeight = viewportHeight;
            InvalidateSectionTopCache();
        }
    }

    private void RefreshSectionTopCacheIfNeeded()
    {
        if (!_sectionTopCacheDirty || _sectionContentPanel is null || VM is null)
        {
            return;
        }

        _sectionTopCache.Clear();
        foreach (var section in VM.Sections)
        {
            if (!_materializedSections.Contains(section.Key)
                || !_sectionAnchors.TryGetValue(section.Key, out var anchor))
            {
                continue;
            }

            var point = anchor.TranslatePoint(default, _sectionContentPanel);
            if (point.HasValue)
            {
                _sectionTopCache[section.Key] = point.Value.Y;
            }
        }

        _sectionTopCacheDirty = false;
    }

    private void TryMaterializeNextSectionForScroll()
    {
        var scrollViewer = _sectionScrollViewer;
        if (scrollViewer is null)
        {
            return;
        }

        var threshold = Math.Max(180d, scrollViewer.Viewport.Height * 0.35d);
        var viewportBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height;
        if (viewportBottom < scrollViewer.Extent.Height - threshold)
        {
            return;
        }

        TryMaterializeNextSectionInOrder();
    }

    private bool TryMaterializeNextSectionInOrder()
    {
        foreach (var sectionKey in SectionOrder)
        {
            if (_materializedSections.Contains(sectionKey))
            {
                continue;
            }

            return EnsureSectionMaterialized(sectionKey);
        }

        return false;
    }

    private void StartProgressiveSectionMaterialization()
    {
        CancelProgressiveSectionMaterialization();
        if (!TryMaterializeNextSectionInOrder())
        {
            return;
        }

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(45),
        };
        timer.Tick += OnProgressiveMaterializationTick;
        _progressiveMaterializationTimer = timer;
        timer.Start();
    }

    private void CancelProgressiveSectionMaterialization()
    {
        var timer = _progressiveMaterializationTimer;
        _progressiveMaterializationTimer = null;
        if (timer is null)
        {
            return;
        }

        timer.Stop();
        timer.Tick -= OnProgressiveMaterializationTick;
    }

    private void OnProgressiveMaterializationTick(object? sender, EventArgs e)
    {
        if (TryMaterializeNextSectionInOrder())
        {
            return;
        }

        CancelProgressiveSectionMaterialization();
    }

    private void ScrollToSelectedSection()
    {
        var vm = VM;
        var scrollViewer = _sectionScrollViewer;
        var contentPanel = _sectionContentPanel;
        if (vm?.SelectedSection is null || scrollViewer is null || contentPanel is null)
        {
            return;
        }

        EnsureSectionsThrough(vm.SelectedSection.Key);
        RefreshSectionTopCacheIfNeeded();

        if (!_sectionTopCache.TryGetValue(vm.SelectedSection.Key, out var top)
            && (_sectionAnchors.TryGetValue(vm.SelectedSection.Key, out var anchor)
                && anchor.TranslatePoint(default, contentPanel) is { } point))
        {
            top = point.Y;
            _sectionTopCache[vm.SelectedSection.Key] = top;
        }

        if (!_sectionTopCache.TryGetValue(vm.SelectedSection.Key, out top))
        {
            return;
        }

        SuppressSectionScrollChangedOnce();
        scrollViewer.Offset = new Vector(
            scrollViewer.Offset.X,
            Math.Max(top, 0d));
    }

    private void UpdateSelectedSectionFromScroll()
    {
        var vm = VM;
        var scrollViewer = _sectionScrollViewer;
        if (vm is null || scrollViewer is null || _sectionAnchors.Count == 0)
        {
            return;
        }

        RefreshSectionTopCacheIfNeeded();
        if (_sectionTopCache.Count == 0)
        {
            return;
        }

        var threshold = scrollViewer.Offset.Y + Math.Max(24d, scrollViewer.Viewport.Height * 0.25d);
        SettingsSectionViewModel? candidate = null;
        var candidateTop = double.MinValue;

        foreach (var section in vm.Sections)
        {
            if (!_sectionTopCache.TryGetValue(section.Key, out var top))
            {
                continue;
            }

            if (top <= threshold && top >= candidateTop)
            {
                candidate = section;
                candidateTop = top;
            }
        }

        candidate ??= vm.Sections.Count > 0 ? vm.Sections[0] : null;
        if (candidate is null || ReferenceEquals(candidate, vm.SelectedSection))
        {
            return;
        }

        SuppressSectionSelectionChangedOnce();
        vm.SelectedSection = candidate;
    }

    private void SuppressSectionSelectionChangedOnce()
    {
        _suppressSectionSelectionChanged = true;
        Dispatcher.UIThread.Post(
            () => _suppressSectionSelectionChanged = false,
            DispatcherPriority.Background);
    }

    private void SuppressSectionScrollChangedOnce()
    {
        _suppressSectionScrollChanged = true;
        Dispatcher.UIThread.Post(
            () => _suppressSectionScrollChanged = false,
            DispatcherPriority.Background);
    }
}
