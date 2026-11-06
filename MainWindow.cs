using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace EfmiConflictAuditor
{
    public static class Theme
    {
        public static readonly SolidColorBrush BgDeep = BrushFromHex("#0E1015");
        public static readonly SolidColorBrush BgPanel = BrushFromHex("#161922");
        public static readonly SolidColorBrush BgCard = BrushFromHex("#1D222E");
        public static readonly SolidColorBrush BgCardHover = BrushFromHex("#252B3B");
        public static readonly SolidColorBrush BgCardSelected = BrushFromHex("#2A3246");
        public static readonly SolidColorBrush BgCode = BrushFromHex("#0A0C10");

        public static readonly SolidColorBrush BorderSubtle = BrushFromHex("#2A3040");
        public static readonly SolidColorBrush BorderFocus = BrushFromHex("#F5C518");

        public static readonly SolidColorBrush AccentAmber = BrushFromHex("#F5C518");
        public static readonly SolidColorBrush AccentAmberHover = BrushFromHex("#FFD438");
        public static readonly SolidColorBrush AccentCyan = BrushFromHex("#00E5FF");
        public static readonly SolidColorBrush AccentGreen = BrushFromHex("#2ECC71");
        public static readonly SolidColorBrush AccentGreenDark = BrushFromHex("#1B7A43");
        public static readonly SolidColorBrush AccentPurple = BrushFromHex("#B37FEB");

        public static readonly SolidColorBrush TextPrimary = BrushFromHex("#F0F4F8");
        public static readonly SolidColorBrush TextSecondary = BrushFromHex("#9AA5B8");
        public static readonly SolidColorBrush TextMuted = BrushFromHex("#636E82");
        public static readonly SolidColorBrush TextDark = BrushFromHex("#0E1015");

        public static SolidColorBrush BrushFromHex(string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static (SolidColorBrush bg, SolidColorBrush border, SolidColorBrush fg) GetSeverityColors(string severity)
        {
            return (severity?.ToUpperInvariant()) switch
            {
                "CRITICAL" => (BrushFromHex("#3B151B"), BrushFromHex("#FF4D4F"), BrushFromHex("#FF7875")),
                "HIGH" => (BrushFromHex("#3B2212"), BrushFromHex("#FF8800"), BrushFromHex("#FFA940")),
                "MEDIUM" => (BrushFromHex("#382D12"), BrushFromHex("#FAAD14"), BrushFromHex("#FFC53D")),
                "WARNING" => (BrushFromHex("#382D12"), BrushFromHex("#FAAD14"), BrushFromHex("#FFC53D")),
                "INFO" => (BrushFromHex("#112A3C"), BrushFromHex("#1890FF"), BrushFromHex("#69C0FF")),
                _ => (BrushFromHex("#252A36"), BrushFromHex("#525C72"), BrushFromHex("#B0B8C8"))
            };
        }

        public static DrawingImage CreateAppIcon()
        {
            var group = new DrawingGroup();

            // Outer hexagon dark shield with amber border
            var hexGeo = Geometry.Parse("M 32,4 L 56,18 L 56,46 L 32,60 L 8,46 L 8,18 Z");
            var shieldDrawing = new GeometryDrawing(
                BrushFromHex("#161922"),
                new Pen(AccentAmber, 4.0) { LineJoin = PenLineJoin.Round },
                hexGeo);
            group.Children.Add(shieldDrawing);

            // Lightning bolt / scanner pulse in center
            var boltGeo = Geometry.Parse("M 35,14 L 20,34 L 31,34 L 28,50 L 44,29 L 33,29 Z");
            var boltDrawing = new GeometryDrawing(
                AccentAmber,
                new Pen(AccentCyan, 1.2) { LineJoin = PenLineJoin.Round },
                boltGeo);
            group.Children.Add(boltDrawing);

            var img = new DrawingImage(group);
            img.Freeze();
            return img;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableDarkTitleBar(Window window)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
                int useDark = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (or 19 on older Win10)
                if (DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, 19, ref useDark, sizeof(int));
                }
            }
            catch
            {
                // Ignore on older OS
            }
        }

        public static Button CreateStyledButton(
            string text,
            SolidColorBrush bg,
            SolidColorBrush fg,
            SolidColorBrush borderBrush,
            Thickness padding,
            bool bold = true)
        {
            var btn = new Button
            {
                Content = text,
                Background = bg,
                Foreground = fg,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Padding = padding,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                FontSize = 13,
                Cursor = Cursors.Hand,
                FocusVisualStyle = null
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "PART_Border";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88));
            template.Triggers.Add(hoverTrigger);

            var pressedTrigger = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.72));
            template.Triggers.Add(pressedTrigger);

            var disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.38));
            template.Triggers.Add(disabledTrigger);

            btn.Template = template;
            return btn;
        }

        public static Border CreatePillBadge(string text, SolidColorBrush bg, SolidColorBrush border, SolidColorBrush fg, double fontSize = 11)
        {
            return new Border
            {
                Background = bg,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = fg,
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }
    }

    public class MainWindow : Window
    {
        private string _currentTarget;
        private AuditResult _currentAudit;
        private string _activeModTypeFilter = "ALL"; // "ALL", "MergedSkeleton", "CrossIB", "Simple"
        private string _activeCategoryFilter = "ALL";
        private ConflictItem _selectedConflict;
        private List<ConflictItem> _filteredConflicts = new List<ConflictItem>();
        private int _renderLimit = 100;

        // UI Controls for dynamic bilingual update
        private TextBlock _lblAppTitle;
        private Button _btnLangToggle;

        private TextBlock _lblTarget;
        private TextBox _txtTarget;
        private Button _btnBrowse;
        private Button _btnOpenFolder;

        private Button _btnScanNow;
        private CheckBox _chkIncludeDisabled;
        private TextBlock _lblIncludeDisabled;
        private CheckBox _chkAutoSaveReport;
        private TextBlock _lblAutoSaveReport;
        private Button _btnExportReport;

        // Section Headers
        private TextBlock _lblWarningSectionHeader;
        private TextBlock _lblConflictSectionHeader;

        // Warning Card 1: ShaderFixes Status Indicator
        private Border _sfIndicatorBorder;
        private TextBlock _lblSfTitle;
        private TextBlock _lblSfSub;
        private Border _sfOpenBadge;
        private TextBlock _lblSfOpenBadge;

        // Warning Card 2: Global Classifier Bundle Warning (Category W)
        private Border _warnClassifierBorder;
        private TextBlock _lblWarnClassifierTitle;
        private TextBlock _lblWarnClassifierSub;
        private TextBlock _lblWarnClassifierCount;

        // Conflict Detection Row 1: 5 Mod Type Cards
        private readonly Dictionary<string, (Border border, TextBlock title, TextBlock count)> _modTypeCards =
            new Dictionary<string, (Border, TextBlock, TextBlock)>();

        // Conflict Detection Row 2: 4 Category Filter Cards (A, B, C, D)
        private UniformGrid _cardsGrid;
        private readonly Dictionary<string, (Border border, TextBlock title, TextBlock count)> _filterCards =
            new Dictionary<string, (Border, TextBlock, TextBlock)>();

        // Left Pane
        private TextBlock _lblListHeader;
        private TextBox _txtSearch;
        private TextBlock _lblSearchPlaceholder;
        private Button _btnClearSearch;
        private StackPanel _listStack;
        private ScrollViewer _listScroll;

        // Right Pane
        private TextBlock _lblDetailHeader;
        private ScrollViewer _detailScroll;
        private StackPanel _detailContainer;

        // Status Bar
        private TextBlock _lblStatusSummary;
        private TextBlock _lblStatusAction;

        public MainWindow(string initialTarget, bool initialIncludeDisabled)
        {
            _currentTarget = initialTarget;
            Icon = Theme.CreateAppIcon();
            Title = I18n.AppTitle;
            Width = 1280;
            Height = 890;
            MinWidth = 980;
            MinHeight = 680;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Theme.BgDeep;
            Foreground = Theme.TextPrimary;
            FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI, sans-serif");
            AllowDrop = true;

            SourceInitialized += (s, e) => Theme.EnableDarkTitleBar(this);
            DragOver += OnWindowDragOver;
            Drop += OnWindowDrop;

            BuildUserInterface(initialIncludeDisabled);
            RefreshLanguageText();
        }

        private void BuildUserInterface(bool initialIncludeDisabled)
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Control Panel
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Two-Row Filter Matrix
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Split Workspace
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Status Bar

            // =====================================================================
            // ROW 0: Header Bar
            // =====================================================================
            var headerBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 12, 18, 12)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Accent bar + Icon
            var logoStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            logoStack.Children.Add(new Border
            {
                Width = 5,
                Height = 36,
                CornerRadius = new CornerRadius(2.5),
                Background = Theme.AccentAmber,
                Margin = new Thickness(0, 0, 12, 0)
            });
            logoStack.Children.Add(new Image
            {
                Source = Theme.CreateAppIcon(),
                Width = 34,
                Height = 34,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _lblAppTitle = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(_lblAppTitle);

            _btnLangToggle = Theme.CreateStyledButton(
                "",
                Theme.BgCard,
                Theme.AccentAmber,
                Theme.AccentAmber,
                new Thickness(14, 7, 14, 7));
            _btnLangToggle.Click += (s, e) =>
            {
                I18n.Current = I18n.IsZh ? Lang.EN : Lang.ZH;
                RefreshLanguageText();
            };

            Grid.SetColumn(logoStack, 0);
            Grid.SetColumn(titleStack, 1);
            Grid.SetColumn(_btnLangToggle, 2);
            headerGrid.Children.Add(logoStack);
            headerGrid.Children.Add(titleStack);
            headerGrid.Children.Add(_btnLangToggle);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            rootGrid.Children.Add(headerBorder);

            // =====================================================================
            // ROW 1: Target & Actions Control Bar
            // =====================================================================
            var ctrlBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 10, 18, 12)
            };
            var ctrlStack = new StackPanel();

            // Row 1A: Target Directory
            var targetGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _lblTarget = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 13
            };

            _txtTarget = new TextBox
            {
                Text = _currentTarget,
                Background = Theme.BgDeep,
                Foreground = Theme.TextPrimary,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = Theme.AccentAmber
            };
            _txtTarget.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    _currentTarget = _txtTarget.Text.Trim();
                    RunScan(null);
                }
            };

            _btnBrowse = Theme.CreateStyledButton("", Theme.BgCard, Theme.TextPrimary, Theme.BorderSubtle, new Thickness(12, 6, 12, 6), false);
            _btnBrowse.Margin = new Thickness(8, 0, 0, 0);
            _btnBrowse.Click += OnBrowseTargetClicked;

            _btnOpenFolder = Theme.CreateStyledButton("", Theme.BgCard, Theme.TextPrimary, Theme.BorderSubtle, new Thickness(12, 6, 12, 6), false);
            _btnOpenFolder.Margin = new Thickness(8, 0, 0, 0);
            _btnOpenFolder.Click += OnOpenTargetFolderClicked;

            Grid.SetColumn(_lblTarget, 0);
            Grid.SetColumn(_txtTarget, 1);
            Grid.SetColumn(_btnBrowse, 2);
            Grid.SetColumn(_btnOpenFolder, 3);
            targetGrid.Children.Add(_lblTarget);
            targetGrid.Children.Add(_txtTarget);
            targetGrid.Children.Add(_btnBrowse);
            targetGrid.Children.Add(_btnOpenFolder);
            ctrlStack.Children.Add(targetGrid);

            // Row 1B: Action Buttons & Checkboxes
            var actionDock = new DockPanel { LastChildFill = false };

            _btnScanNow = Theme.CreateStyledButton("", Theme.AccentAmber, Theme.TextDark, Theme.AccentAmber, new Thickness(18, 7, 18, 7), true);
            _btnScanNow.Margin = new Thickness(0, 0, 14, 0);
            _btnScanNow.VerticalAlignment = VerticalAlignment.Center;
            _btnScanNow.Click += (s, e) =>
            {
                _currentTarget = _txtTarget.Text.Trim();
                RunScan(null);
            };
            DockPanel.SetDock(_btnScanNow, Dock.Left);
            actionDock.Children.Add(_btnScanNow);

            // Include DISABLED_ checkbox wrapped in a sleek pill border
            var disabledToggleBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            _lblIncludeDisabled = new TextBlock
            {
                Foreground = Theme.TextPrimary,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            _chkIncludeDisabled = new CheckBox
            {
                IsChecked = initialIncludeDisabled,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = _lblIncludeDisabled,
                Cursor = Cursors.Hand
            };
            _chkIncludeDisabled.Checked += (s, e) =>
            {
                disabledToggleBorder.BorderBrush = Theme.AccentAmber;
                UpdateCategoryDCardVisibility(true);
                if (IsLoaded && _lblStatusAction != null)
                {
                    _lblStatusAction.Text = I18n.T(
                        "已开启 DISABLED_ 扫描选项，请点击「🔍 开始扫描」",
                        "DISABLED_ option enabled — click '🔍 Scan Now'");
                }
            };
            _chkIncludeDisabled.Unchecked += (s, e) =>
            {
                disabledToggleBorder.BorderBrush = Theme.BorderSubtle;
                UpdateCategoryDCardVisibility(false);
                if (IsLoaded && _lblStatusAction != null)
                {
                    _lblStatusAction.Text = I18n.T(
                        "已关闭 DISABLED_ 扫描选项，请点击「🔍 开始扫描」",
                        "DISABLED_ option disabled — click '🔍 Scan Now'");
                }
            };
            disabledToggleBorder.MouseLeftButtonUp += (s, e) =>
            {
                if (!ReferenceEquals(e.OriginalSource, _chkIncludeDisabled) && !IsInsideCheckBox(e.OriginalSource as DependencyObject))
                {
                    _chkIncludeDisabled.IsChecked = !(_chkIncludeDisabled.IsChecked == true);
                }
            };
            if (initialIncludeDisabled) disabledToggleBorder.BorderBrush = Theme.AccentAmber;
            disabledToggleBorder.Child = _chkIncludeDisabled;
            DockPanel.SetDock(disabledToggleBorder, Dock.Left);
            actionDock.Children.Add(disabledToggleBorder);

            // Auto-save TXT report checkbox
            var autoSaveBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 12, 6),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            _lblAutoSaveReport = new TextBlock
            {
                Foreground = Theme.TextSecondary,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            _chkAutoSaveReport = new CheckBox
            {
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = _lblAutoSaveReport,
                Cursor = Cursors.Hand
            };
            autoSaveBorder.Child = _chkAutoSaveReport;
            autoSaveBorder.MouseLeftButtonUp += (s, e) =>
            {
                if (!ReferenceEquals(e.OriginalSource, _chkAutoSaveReport) && !IsInsideCheckBox(e.OriginalSource as DependencyObject))
                {
                    _chkAutoSaveReport.IsChecked = !(_chkAutoSaveReport.IsChecked == true);
                }
            };
            DockPanel.SetDock(autoSaveBorder, Dock.Left);
            actionDock.Children.Add(autoSaveBorder);

            // Export Report button moved right next to Auto-save TXT Report
            _btnExportReport = Theme.CreateStyledButton("", Theme.BgCard, Theme.TextPrimary, Theme.BorderSubtle, new Thickness(12, 7, 12, 7), false);
            _btnExportReport.Margin = new Thickness(0, 0, 14, 0);
            _btnExportReport.VerticalAlignment = VerticalAlignment.Center;
            _btnExportReport.Click += OnExportReportClicked;
            DockPanel.SetDock(_btnExportReport, Dock.Left);
            actionDock.Children.Add(_btnExportReport);

            ctrlStack.Children.Add(actionDock);
            ctrlBorder.Child = ctrlStack;
            Grid.SetRow(ctrlBorder, 1);
            rootGrid.Children.Add(ctrlBorder);

            // =====================================================================
            // ROW 2: Two Titled Panels
            //   1) ⚠️ 风险预警 (Risk Warnings): ShaderFixes + Global Classifier Warning
            //   2) 🔥 冲突检测 (Conflict Detection): 5 Mod Types + 4 Conflict Categories (A/B/C/D)
            // =====================================================================
            var filterMatrixStack = new StackPanel
            {
                Margin = new Thickness(18, 6, 18, 2)
            };

            // ---------------------------------------------------------------------
            // Panel 1: ⚠️ 风险预警 (Risk Warnings)
            // ---------------------------------------------------------------------
            var warningPanelBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var warningPanelStack = new StackPanel();

            var warningHeaderBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(14, 6, 14, 6)
            };
            _lblWarningSectionHeader = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary
            };
            warningHeaderBorder.Child = _lblWarningSectionHeader;
            warningPanelStack.Children.Add(warningHeaderBorder);

            var warningCardsGrid = new UniformGrid
            {
                Rows = 1,
                Columns = 2,
                Margin = new Thickness(6, 6, 6, 6)
            };

            // Warning Card 1 (Left): ShaderFixes Status Indicator
            _sfIndicatorBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1, 3, 1, 1),
                CornerRadius = new CornerRadius(7),
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };

            var sfDock = new DockPanel();
            _sfOpenBadge = new Border
            {
                Background = Theme.BgDeep,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _lblSfOpenBadge = new TextBlock
            {
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            _sfOpenBadge.Child = _lblSfOpenBadge;
            DockPanel.SetDock(_sfOpenBadge, Dock.Right);
            sfDock.Children.Add(_sfOpenBadge);

            var sfTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _lblSfTitle = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _lblSfSub = new TextBlock
            {
                FontSize = 11.5,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 1, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            sfTextStack.Children.Add(_lblSfTitle);
            sfTextStack.Children.Add(_lblSfSub);
            sfDock.Children.Add(sfTextStack);

            _sfIndicatorBorder.Child = sfDock;
            _sfIndicatorBorder.MouseLeftButtonUp += OnShaderFixesIndicatorClicked;
            warningCardsGrid.Children.Add(_sfIndicatorBorder);

            // Warning Card 2 (Right): Global Classifier Bundle Warning (Category W)
            _warnClassifierBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1, 3, 1, 1),
                CornerRadius = new CornerRadius(7),
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };

            var wcDock = new DockPanel();
            _lblWarnClassifierCount = new TextBlock
            {
                Text = "0",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.BrushFromHex("#FAAD14"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 0, 0)
            };
            DockPanel.SetDock(_lblWarnClassifierCount, Dock.Right);
            wcDock.Children.Add(_lblWarnClassifierCount);

            var wcTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _lblWarnClassifierTitle = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _lblWarnClassifierSub = new TextBlock
            {
                FontSize = 11.5,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 1, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            wcTextStack.Children.Add(_lblWarnClassifierTitle);
            wcTextStack.Children.Add(_lblWarnClassifierSub);
            wcDock.Children.Add(wcTextStack);

            _warnClassifierBorder.Child = wcDock;
            _warnClassifierBorder.MouseLeftButtonUp += (s, e) =>
            {
                _activeCategoryFilter = (_activeCategoryFilter == "W") ? "ALL" : "W";
                _renderLimit = 100;
                UpdateCardFilterHighlights();
                RebuildConflictListUI();
                if (_filteredConflicts.Count > 0 && !_filteredConflicts.Contains(_selectedConflict))
                {
                    SelectConflictItem(_filteredConflicts[0]);
                }
                else if (_filteredConflicts.Count == 0)
                {
                    _selectedConflict = null;
                    RebuildDetailInspectorUI();
                }
            };
            warningCardsGrid.Children.Add(_warnClassifierBorder);

            warningPanelStack.Children.Add(warningCardsGrid);
            warningPanelBorder.Child = warningPanelStack;
            filterMatrixStack.Children.Add(warningPanelBorder);

            // ---------------------------------------------------------------------
            // Panel 2: 🔥 冲突检测 (Conflict Detection)
            // ---------------------------------------------------------------------
            var conflictPanelBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            var conflictPanelStack = new StackPanel();

            var conflictHeaderBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(14, 6, 14, 6)
            };
            _lblConflictSectionHeader = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary
            };
            conflictHeaderBorder.Child = _lblConflictSectionHeader;
            conflictPanelStack.Children.Add(conflictHeaderBorder);

            var conflictCardsStack = new StackPanel
            {
                Margin = new Thickness(6, 6, 6, 6)
            };

            // Top Row: 5 Mod Type Cards
            var typeCardsGrid = new UniformGrid
            {
                Rows = 1,
                Columns = 5,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var typeDefs = new[]
            {
                ("ALL", Theme.AccentAmber),
                ("MergedSkeleton", Theme.AccentGreen),
                ("CrossIB", Theme.BrushFromHex("#FF8800")),
                ("Simple", Theme.AccentCyan),
                ("ShaderPatch", Theme.BrushFromHex("#FF6B9D"))
            };

            foreach (var (tKey, tAccent) in typeDefs)
            {
                var tBorder = new Border
                {
                    Background = Theme.BgCard,
                    BorderBrush = tKey == _activeModTypeFilter ? tAccent : Theme.BorderSubtle,
                    BorderThickness = new Thickness(1, 3, 1, 1),
                    CornerRadius = new CornerRadius(7),
                    Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(12, 6, 12, 6),
                    Cursor = Cursors.Hand
                };

                var tDock = new DockPanel();
                var lblTypeTitle = new TextBlock
                {
                    FontSize = 12.5,
                    Foreground = Theme.TextPrimary,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var lblTypeCount = new TextBlock
                {
                    Text = "0",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = tAccent,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                DockPanel.SetDock(lblTypeTitle, Dock.Left);
                DockPanel.SetDock(lblTypeCount, Dock.Right);
                tDock.Children.Add(lblTypeTitle);
                tDock.Children.Add(lblTypeCount);
                tBorder.Child = tDock;

                string capturedTypeKey = tKey;
                tBorder.MouseLeftButtonUp += (s, e) =>
                {
                    _activeModTypeFilter = capturedTypeKey;
                    _activeCategoryFilter = "ALL";
                    _renderLimit = 100;
                    UpdateCardFilterHighlights();
                    UpdateCardCountsForCurrentFilter();
                    RebuildConflictListUI();
                    if (_filteredConflicts.Count > 0 && !_filteredConflicts.Contains(_selectedConflict))
                    {
                        SelectConflictItem(_filteredConflicts[0]);
                    }
                    else if (_filteredConflicts.Count == 0)
                    {
                        _selectedConflict = null;
                        RebuildDetailInspectorUI();
                    }
                };

                _modTypeCards[tKey] = (tBorder, lblTypeTitle, lblTypeCount);
                typeCardsGrid.Children.Add(tBorder);
            }
            conflictCardsStack.Children.Add(typeCardsGrid);

            // Bottom Row: 4 Conflict Category Filter Cards (A, B, C, D)
            _cardsGrid = new UniformGrid
            {
                Rows = 1,
                Columns = initialIncludeDisabled ? 3 : 4
            };

            var cardDefs = new[]
            {
                ("A", Theme.BrushFromHex("#FF4D4F")),
                ("B", Theme.BrushFromHex("#FF8800")),
                ("C", Theme.AccentPurple),
                ("D", Theme.BrushFromHex("#40A9FF"))
            };

            foreach (var (key, accent) in cardDefs)
            {
                var cardBorder = new Border
                {
                    Background = Theme.BgCard,
                    BorderBrush = key == _activeCategoryFilter ? accent : Theme.BorderSubtle,
                    BorderThickness = new Thickness(1, 3, 1, 1),
                    CornerRadius = new CornerRadius(7),
                    Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(12, 6, 12, 6),
                    Cursor = Cursors.Hand
                };

                var cDock = new DockPanel();
                var lblTitle = new TextBlock
                {
                    FontSize = 12.5,
                    Foreground = Theme.TextPrimary,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var lblCount = new TextBlock
                {
                    Text = "0",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = accent,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(lblTitle, Dock.Left);
                DockPanel.SetDock(lblCount, Dock.Right);
                cDock.Children.Add(lblTitle);
                cDock.Children.Add(lblCount);
                cardBorder.Child = cDock;

                string capturedKey = key;
                cardBorder.MouseLeftButtonUp += (s, e) =>
                {
                    _activeCategoryFilter = (_activeCategoryFilter == capturedKey) ? "ALL" : capturedKey;
                    _renderLimit = 100;
                    UpdateCardFilterHighlights();
                    RebuildConflictListUI();
                    if (_filteredConflicts.Count > 0 && !_filteredConflicts.Contains(_selectedConflict))
                    {
                        SelectConflictItem(_filteredConflicts[0]);
                    }
                    else if (_filteredConflicts.Count == 0)
                    {
                        _selectedConflict = null;
                        RebuildDetailInspectorUI();
                    }
                };

                _filterCards[key] = (cardBorder, lblTitle, lblCount);
                _cardsGrid.Children.Add(cardBorder);
            }

            UpdateCategoryDCardVisibility(initialIncludeDisabled);

            conflictCardsStack.Children.Add(_cardsGrid);
            conflictPanelStack.Children.Add(conflictCardsStack);
            conflictPanelBorder.Child = conflictPanelStack;
            filterMatrixStack.Children.Add(conflictPanelBorder);

            Grid.SetRow(filterMatrixStack, 2);
            rootGrid.Children.Add(filterMatrixStack);

            // =====================================================================
            // ROW 3: Split Workspace (Left: Conflict List | Right: Detail Inspector)
            // =====================================================================
            var splitGrid = new Grid { Margin = new Thickness(18, 4, 18, 10) };
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4.6, GridUnitType.Star), MinWidth = 340 });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5.4, GridUnitType.Star), MinWidth = 380 });

            // Left Panel
            var leftBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var leftHeaderBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(14, 9, 14, 9)
            };
            _lblListHeader = new TextBlock
            {
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary
            };
            leftHeaderBorder.Child = _lblListHeader;
            Grid.SetRow(leftHeaderBorder, 0);
            leftGrid.Children.Add(leftHeaderBorder);

            // Search box row
            var searchGrid = new Grid { Margin = new Thickness(10, 8, 10, 8) };
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtSearch = new TextBox
            {
                Background = Theme.BgDeep,
                Foreground = Theme.TextPrimary,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 12.5,
                CaretBrush = Theme.AccentAmber
            };
            _lblSearchPlaceholder = new TextBlock
            {
                Foreground = Theme.TextMuted,
                FontSize = 12,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                _lblSearchPlaceholder.Visibility = string.IsNullOrEmpty(_txtSearch.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                RebuildConflictListUI();
            };

            _btnClearSearch = Theme.CreateStyledButton("", Theme.BgCard, Theme.TextSecondary, Theme.BorderSubtle, new Thickness(10, 4, 10, 4), false);
            _btnClearSearch.Margin = new Thickness(6, 0, 0, 0);
            _btnClearSearch.Click += (s, e) => _txtSearch.Text = string.Empty;

            Grid.SetColumn(_txtSearch, 0);
            Grid.SetColumn(_lblSearchPlaceholder, 0);
            Grid.SetColumn(_btnClearSearch, 1);
            searchGrid.Children.Add(_txtSearch);
            searchGrid.Children.Add(_lblSearchPlaceholder);
            searchGrid.Children.Add(_btnClearSearch);
            Grid.SetRow(searchGrid, 1);
            leftGrid.Children.Add(searchGrid);

            _listStack = new StackPanel { Margin = new Thickness(8, 0, 8, 8) };
            _listScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _listStack
            };
            Grid.SetRow(_listScroll, 2);
            leftGrid.Children.Add(_listScroll);
            leftBorder.Child = leftGrid;

            // GridSplitter
            var splitter = new GridSplitter
            {
                Width = 10,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeWE
            };

            // Right Panel (Inspector)
            var rightBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var rightHeaderBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(14, 9, 14, 9)
            };
            _lblDetailHeader = new TextBlock
            {
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary
            };
            rightHeaderBorder.Child = _lblDetailHeader;
            Grid.SetRow(rightHeaderBorder, 0);
            rightGrid.Children.Add(rightHeaderBorder);

            _detailContainer = new StackPanel { Margin = new Thickness(14, 12, 14, 14) };
            _detailScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _detailContainer
            };
            Grid.SetRow(_detailScroll, 1);
            rightGrid.Children.Add(_detailScroll);
            rightBorder.Child = rightGrid;

            Grid.SetColumn(leftBorder, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(rightBorder, 2);
            splitGrid.Children.Add(leftBorder);
            splitGrid.Children.Add(splitter);
            splitGrid.Children.Add(rightBorder);
            Grid.SetRow(splitGrid, 3);
            rootGrid.Children.Add(splitGrid);

            // =====================================================================
            // ROW 4: Status Bar
            // =====================================================================
            var statusBorder = new Border
            {
                Background = Theme.BgPanel,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 7, 18, 7)
            };
            var statusDock = new DockPanel();
            _lblStatusSummary = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            _lblStatusAction = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.AccentAmber,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(_lblStatusSummary, Dock.Left);
            DockPanel.SetDock(_lblStatusAction, Dock.Right);
            statusDock.Children.Add(_lblStatusSummary);
            statusDock.Children.Add(_lblStatusAction);
            statusBorder.Child = statusDock;
            Grid.SetRow(statusBorder, 4);
            rootGrid.Children.Add(statusBorder);

            Content = rootGrid;
        }

        private void RefreshLanguageText()
        {
            Title = I18n.AppTitle;
            _lblAppTitle.Text = I18n.AppTitle;
            _btnLangToggle.Content = I18n.LangToggleText;

            _lblTarget.Text = I18n.TargetLabel;
            _btnBrowse.Content = I18n.BrowseBtn;
            _btnOpenFolder.Content = I18n.OpenFolderBtn;

            _btnScanNow.Content = I18n.ScanNowBtn;
            _lblIncludeDisabled.Text = I18n.IncludeDisabledLabel;
            _chkIncludeDisabled.ToolTip = I18n.IncludeDisabledTooltip;
            _lblAutoSaveReport.Text = I18n.AutoSaveReportLabel;
            _btnExportReport.Content = I18n.ExportReportBtn;

            _lblWarningSectionHeader.Text = I18n.WarningSectionHeader;
            _lblConflictSectionHeader.Text = I18n.ConflictSectionHeader;
            _lblWarnClassifierTitle.Text = I18n.WarnClassifierTitle;
            _lblWarnClassifierSub.Text = I18n.WarnClassifierSub;

            if (_modTypeCards.ContainsKey("ALL")) _modTypeCards["ALL"].title.Text = I18n.TypeAllTitle;
            if (_modTypeCards.ContainsKey("MergedSkeleton")) _modTypeCards["MergedSkeleton"].title.Text = I18n.TypeMergedTitle;
            if (_modTypeCards.ContainsKey("CrossIB")) _modTypeCards["CrossIB"].title.Text = I18n.TypeCrossIBTitle;
            if (_modTypeCards.ContainsKey("Simple")) _modTypeCards["Simple"].title.Text = I18n.TypeSimpleTitle;
            if (_modTypeCards.ContainsKey("ShaderPatch")) _modTypeCards["ShaderPatch"].title.Text = I18n.TypeShaderPatchTitle;

            if (_filterCards.ContainsKey("A")) _filterCards["A"].title.Text = I18n.CardATitle;
            if (_filterCards.ContainsKey("B")) _filterCards["B"].title.Text = I18n.CardBTitle;
            if (_filterCards.ContainsKey("C")) _filterCards["C"].title.Text = I18n.CardCTitle;
            if (_filterCards.ContainsKey("D")) _filterCards["D"].title.Text = I18n.CardDTitle;

            _lblListHeader.Text = I18n.ListHeaderTitle;
            _lblSearchPlaceholder.Text = I18n.SearchPlaceholder;
            _btnClearSearch.Content = I18n.ClearSearchBtn;
            _lblDetailHeader.Text = I18n.DetailHeaderTitle;

            UpdateShaderFixesIndicatorUI();
            UpdateCardCountsForCurrentFilter();
            UpdateCardFilterHighlights();
            UpdateActionButtonsState();
            UpdateStatusBar();
            RebuildConflictListUI();
            RebuildDetailInspectorUI();
        }

        private void UpdateShaderFixesIndicatorUI()
        {
            if (_sfIndicatorBorder == null) return;

            if (_currentAudit == null)
            {
                _sfIndicatorBorder.Background = Theme.BgCard;
                _sfIndicatorBorder.BorderBrush = Theme.BorderSubtle;
                _lblSfTitle.Text = I18n.SfIdleTitle;
                _lblSfTitle.Foreground = Theme.TextPrimary;
                _lblSfSub.Text = I18n.SfIdleSub;
                _lblSfSub.Foreground = Theme.TextSecondary;
                _lblSfOpenBadge.Text = I18n.OpenFolderBtn;
                _sfOpenBadge.Visibility = Visibility.Collapsed;
                return;
            }

            if (_currentAudit.ShaderFixesFileCount == 0)
            {
                _sfIndicatorBorder.Background = Theme.BrushFromHex("#10281E");
                _sfIndicatorBorder.BorderBrush = Theme.AccentGreen;
                _lblSfTitle.Text = I18n.SfSafeTitle;
                _lblSfTitle.Foreground = Theme.BrushFromHex("#58D68D");
                _lblSfSub.Text = I18n.SfSafeSub;
                _lblSfSub.Foreground = Theme.TextSecondary;
                _lblSfOpenBadge.Text = I18n.OpenFolderBtn;
                _sfOpenBadge.Background = Theme.BrushFromHex("#163829");
                _sfOpenBadge.BorderBrush = Theme.AccentGreen;
                _lblSfOpenBadge.Foreground = Theme.BrushFromHex("#58D68D");
                _sfOpenBadge.Visibility = _currentAudit.HasShaderFixesFolder ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                _sfIndicatorBorder.Background = Theme.BrushFromHex("#3B1812");
                _sfIndicatorBorder.BorderBrush = Theme.BrushFromHex("#FF6B35");
                _lblSfTitle.Text = I18n.SfWarnTitle(_currentAudit.ShaderFixesFileCount);
                _lblSfTitle.Foreground = Theme.BrushFromHex("#FF9F43");
                _lblSfSub.Text = I18n.SfWarnSub;
                _lblSfSub.Foreground = Theme.TextPrimary;
                _lblSfOpenBadge.Text = I18n.OpenFolderBtn;
                _sfOpenBadge.Background = Theme.BrushFromHex("#FF6B35");
                _sfOpenBadge.BorderBrush = Theme.BrushFromHex("#FF9F43");
                _lblSfOpenBadge.Foreground = Theme.TextDark;
                _sfOpenBadge.Visibility = Visibility.Visible;
            }
        }

        private static (SolidColorBrush bg, SolidColorBrush border, SolidColorBrush fg) GetModTypeBadgeColors(ModArchType mt)
        {
            return mt switch
            {
                ModArchType.MergedSkeleton => (Theme.BrushFromHex("#123324"), Theme.AccentGreen, Theme.BrushFromHex("#58D68D")),
                ModArchType.CrossIB => (Theme.BrushFromHex("#3A2312"), Theme.BrushFromHex("#FF8800"), Theme.BrushFromHex("#FFA940")),
                ModArchType.ShaderPatch => (Theme.BrushFromHex("#3A1528"), Theme.BrushFromHex("#FF6B9D"), Theme.BrushFromHex("#FF94B8")),
                _ => (Theme.BrushFromHex("#132A3A"), Theme.AccentCyan, Theme.BrushFromHex("#69C0FF"))
            };
        }

        private bool MatchesModTypeFilter(ConflictItem item, string modTypeFilter)
        {
            if (string.IsNullOrEmpty(modTypeFilter) || modTypeFilter == "ALL")
                return true;

            return modTypeFilter switch
            {
                "MergedSkeleton" => item.InvolvedModTypes.Contains(ModArchType.MergedSkeleton),
                "CrossIB" => item.InvolvedModTypes.Contains(ModArchType.CrossIB),
                "Simple" => item.InvolvedModTypes.Contains(ModArchType.Simple),
                "ShaderPatch" => item.InvolvedModTypes.Contains(ModArchType.ShaderPatch),
                _ => true
            };
        }

        private void UpdateCardCountsForCurrentFilter()
        {
            if (_currentAudit == null)
            {
                return;
            }

            // Warning Card 2: Global Classifier Warning (Category W)
            int warnClassifierCount = _currentAudit.AllFindings.Count(x => x.Category == "W");
            _lblWarnClassifierCount.Text = warnClassifierCount.ToString();
            _lblWarnClassifierCount.Foreground = warnClassifierCount > 0
                ? Theme.BrushFromHex("#FAAD14")
                : Theme.AccentGreen;

            // Conflict Detection Row 1: Mod Type conflict counts (strictly true conflicts A/B/C/D, excluding W)
            var trueConflicts = _currentAudit.AllFindings.Where(x => x.Category != "W").ToList();

            _modTypeCards["ALL"].count.Text = trueConflicts.Count.ToString();
            _modTypeCards["MergedSkeleton"].count.Text = trueConflicts.Count(x => x.InvolvedModTypes.Contains(ModArchType.MergedSkeleton)).ToString();
            _modTypeCards["CrossIB"].count.Text = trueConflicts.Count(x => x.InvolvedModTypes.Contains(ModArchType.CrossIB)).ToString();
            _modTypeCards["Simple"].count.Text = trueConflicts.Count(x => x.InvolvedModTypes.Contains(ModArchType.Simple)).ToString();
            _modTypeCards["ShaderPatch"].count.Text = trueConflicts.Count(x => x.InvolvedModTypes.Contains(ModArchType.ShaderPatch)).ToString();

            // Conflict Detection Row 2: Category counts (A/B/C/D) filtered by active Mod Type
            var typeFiltered = trueConflicts
                .Where(x => MatchesModTypeFilter(x, _activeModTypeFilter))
                .ToList();

            _filterCards["A"].count.Text = typeFiltered.Count(x => x.Category == "A").ToString();
            _filterCards["B"].count.Text = typeFiltered.Count(x => x.Category == "B").ToString();
            _filterCards["C"].count.Text = typeFiltered.Count(x => x.Category == "C").ToString();
            _filterCards["D"].count.Text = typeFiltered.Count(x => x.Category == "D").ToString();
        }

        private void UpdateCardFilterHighlights()
        {
            bool isWarnSelected = _activeCategoryFilter == "W";

            if (_warnClassifierBorder != null)
            {
                _warnClassifierBorder.BorderBrush = isWarnSelected ? Theme.BrushFromHex("#FAAD14") : Theme.BorderSubtle;
                _warnClassifierBorder.Background = isWarnSelected ? Theme.BgCardSelected : Theme.BgCard;
            }

            var typeAccents = new Dictionary<string, SolidColorBrush>
            {
                ["ALL"] = Theme.AccentAmber,
                ["MergedSkeleton"] = Theme.AccentGreen,
                ["CrossIB"] = Theme.BrushFromHex("#FF8800"),
                ["Simple"] = Theme.AccentCyan,
                ["ShaderPatch"] = Theme.BrushFromHex("#FF6B9D")
            };

            foreach (var kv in _modTypeCards)
            {
                bool active = !isWarnSelected && kv.Key == _activeModTypeFilter;
                kv.Value.border.BorderBrush = active ? typeAccents[kv.Key] : Theme.BorderSubtle;
                kv.Value.border.Background = active ? Theme.BgCardSelected : Theme.BgCard;
            }

            var accents = new Dictionary<string, SolidColorBrush>
            {
                ["A"] = Theme.BrushFromHex("#FF4D4F"),
                ["B"] = Theme.BrushFromHex("#FF8800"),
                ["C"] = Theme.AccentPurple,
                ["D"] = Theme.BrushFromHex("#40A9FF")
            };

            foreach (var kv in _filterCards)
            {
                bool active = kv.Key == _activeCategoryFilter;
                kv.Value.border.BorderBrush = active ? accents[kv.Key] : Theme.BorderSubtle;
                kv.Value.border.Background = active ? Theme.BgCardSelected : Theme.BgCard;
            }
        }

        private void UpdateCategoryDCardVisibility(bool hideD)
        {
            if (!_filterCards.ContainsKey("D") || _cardsGrid == null)
                return;

            if (hideD)
            {
                _filterCards["D"].border.Visibility = Visibility.Collapsed;
                _cardsGrid.Columns = 3;
                if (_activeCategoryFilter == "D")
                {
                    _activeCategoryFilter = "ALL";
                    UpdateCardFilterHighlights();
                    if (_currentAudit != null)
                    {
                        RebuildConflictListUI();
                    }
                }
            }
            else
            {
                _filterCards["D"].border.Visibility = Visibility.Visible;
                _cardsGrid.Columns = 4;
            }
        }

        private void UpdateActionButtonsState()
        {
            _btnExportReport.IsEnabled = _currentAudit != null;
        }

        private void UpdateStatusBar()
        {
            if (_currentAudit == null)
            {
                _lblStatusSummary.Text = I18n.EmptyListPrompt;
                return;
            }

            _lblStatusSummary.Text = I18n.StatusBarSummary(
                _currentAudit.TotalMods,
                _currentAudit.ActiveMods,
                _currentAudit.DisabledMods,
                _currentAudit.IniCount,
                _currentAudit.ShaderOverrideCount,
                _currentAudit.ShaderRegexCount,
                _currentAudit.AllFindings.Count(x => x.Category != "W"));
        }

        private void RunScan(bool? saveReportOverride)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _currentTarget = _txtTarget.Text.Trim();
                bool includeDis = _chkIncludeDisabled.IsChecked == true;

                _currentAudit = AuditorEngine.RunAudit(_currentTarget, includeDis);

                UpdateShaderFixesIndicatorUI();
                UpdateCategoryDCardVisibility(includeDis);
                UpdateCardCountsForCurrentFilter();
                UpdateCardFilterHighlights();
                UpdateActionButtonsState();
                UpdateStatusBar();

                _selectedConflict = null;
                _renderLimit = 100;
                RebuildConflictListUI();
                if (_filteredConflicts.Count > 0)
                {
                    SelectConflictItem(_filteredConflicts[0]);
                }
                else
                {
                    RebuildDetailInspectorUI();
                }

                bool shouldSaveReport = saveReportOverride ?? (_chkAutoSaveReport.IsChecked == true);
                if (shouldSaveReport)
                {
                    SaveReportToDisk();
                }
                else
                {
                    _lblStatusAction.Text = I18n.T(
                        $"✔ 扫描完成 ({DateTime.Now:HH:mm:ss})",
                        $"✔ Scan completed ({DateTime.Now:HH:mm:ss})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    I18n.T("扫描出错", "Scan Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveReportToDisk()
        {
            if (_currentAudit == null) return;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string reportPath = Path.Combine(_currentAudit.ScanRoot, $"EFMI_Conflict_Report_{stamp}.txt");
            string reportContent = AuditorEngine.GenerateReportText(_currentAudit);
            File.WriteAllText(reportPath, reportContent, new UTF8Encoding(true));

            _lblStatusAction.Text = I18n.T(
                $"📄 报告已保存: {Path.GetFileName(reportPath)}",
                $"📄 Report saved: {Path.GetFileName(reportPath)}");
        }

        private void RebuildConflictListUI()
        {
            _listStack.Children.Clear();

            if (_currentAudit == null)
            {
                _listStack.Children.Add(new TextBlock
                {
                    Text = I18n.EmptyListPrompt,
                    Foreground = Theme.TextSecondary,
                    FontSize = 13,
                    Margin = new Thickness(12, 24, 12, 12),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            string query = (_txtSearch.Text ?? string.Empty).Trim();

            _filteredConflicts = _currentAudit.AllFindings
                .Where(item =>
                {
                    if (_activeCategoryFilter == "W")
                    {
                        if (item.Category != "W") return false;
                    }
                    else
                    {
                        if (item.Category == "W") return false;
                        if (!MatchesModTypeFilter(item, _activeModTypeFilter))
                            return false;
                        if (_activeCategoryFilter != "ALL" && item.Category != _activeCategoryFilter)
                            return false;
                    }

                    if (!string.IsNullOrEmpty(query))
                    {
                        bool match =
                            item.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.Subtitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.Reason.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.Sections.Any(s =>
                                (s.Hash != null && s.Hash.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (s.FilterRaw != null && s.FilterRaw.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                s.RelativeFile.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!match) return false;
                    }
                    return true;
                })
                .ToList();

            _lblListHeader.Text = $"{I18n.ListHeaderTitle} ({_filteredConflicts.Count})";

            if (_filteredConflicts.Count == 0)
            {
                _listStack.Children.Add(new Border
                {
                    Background = Theme.BgCard,
                    BorderBrush = Theme.BorderSubtle,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(16),
                    Margin = new Thickness(4, 12, 4, 4),
                    Child = new TextBlock
                    {
                        Text = I18n.EmptyListGood,
                        Foreground = Theme.AccentGreen,
                        FontSize = 13.5,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    }
                });
                return;
            }

            int visibleCount = Math.Min(_filteredConflicts.Count, _renderLimit);
            for (int idx = 0; idx < visibleCount; idx++)
            {
                var item = _filteredConflicts[idx];
                bool isSelected = ReferenceEquals(item, _selectedConflict);
                var (sevBg, sevBorder, sevFg) = Theme.GetSeverityColors(item.Severity);

                var itemBorder = new Border
                {
                    Tag = item,
                    Background = isSelected ? Theme.BgCardSelected : Theme.BgCard,
                    BorderBrush = isSelected ? Theme.AccentAmber : Theme.BorderSubtle,
                    BorderThickness = new Thickness(isSelected ? 1.5 : 1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(2, 4, 2, 4),
                    Cursor = Cursors.Hand
                };

                var infoStack = new StackPanel();

                // Badges Row
                var badgesWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
                badgesWrap.Children.Add(Theme.CreatePillBadge(item.Severity, sevBg, sevBorder, sevFg, 10.5));
                badgesWrap.Children.Add(Theme.CreatePillBadge(
                    item.CategoryBadge,
                    Theme.BgDeep,
                    Theme.BorderSubtle,
                    Theme.TextSecondary,
                    10.5));

                foreach (var mt in item.InvolvedModTypes.OrderByDescending(x => x))
                {
                    var (mtBg, mtBorder, mtFg) = GetModTypeBadgeColors(mt);
                    badgesWrap.Children.Add(Theme.CreatePillBadge(
                        I18n.ModTypeBadge(mt),
                        mtBg,
                        mtBorder,
                        mtFg,
                        10.5));
                }

                if (item.InvolvesDisabledMod)
                {
                    badgesWrap.Children.Add(Theme.CreatePillBadge(
                        $"⏸ {I18n.DisabledBadge}",
                        Theme.BrushFromHex("#282C37"),
                        Theme.BrushFromHex("#58627A"),
                        Theme.BrushFromHex("#C0C8D8"),
                        10.5));
                }

                infoStack.Children.Add(badgesWrap);

                // Title
                infoStack.Children.Add(new TextBlock
                {
                    Text = item.Title,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = Theme.TextPrimary,
                    TextWrapping = TextWrapping.Wrap
                });

                // Subtitle
                infoStack.Children.Add(new TextBlock
                {
                    Text = item.Subtitle,
                    FontSize = 11.5,
                    Foreground = Theme.TextSecondary,
                    Margin = new Thickness(0, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });

                itemBorder.Child = infoStack;

                itemBorder.MouseLeftButtonUp += (s, e) =>
                {
                    SelectConflictItem(item);
                };

                _listStack.Children.Add(itemBorder);
            }

            if (_filteredConflicts.Count > visibleCount)
            {
                int remaining = _filteredConflicts.Count - visibleCount;
                var btnMore = Theme.CreateStyledButton(
                    I18n.T($"⏬ 显示剩余 {remaining} 条结果 (点击展开全部)", $"⏬ Show remaining {remaining} items (Click to expand all)"),
                    Theme.BgCard,
                    Theme.AccentAmber,
                    Theme.AccentAmber,
                    new Thickness(14, 9, 14, 9),
                    true);
                btnMore.Margin = new Thickness(4, 8, 4, 8);
                btnMore.Click += (s, e) =>
                {
                    _renderLimit = int.MaxValue;
                    RebuildConflictListUI();
                };
                _listStack.Children.Add(btnMore);
            }
        }

        private static bool IsInsideCheckBox(DependencyObject dep)
        {
            while (dep != null)
            {
                if (dep is CheckBox) return true;
                dep = VisualTreeHelper.GetParent(dep);
            }
            return false;
        }

        private void SelectConflictItem(ConflictItem item)
        {
            _selectedConflict = item;
            foreach (var child in _listStack.Children.OfType<Border>())
            {
                if (child.Tag is ConflictItem ci)
                {
                    bool sel = ReferenceEquals(ci, _selectedConflict);
                    child.Background = sel ? Theme.BgCardSelected : Theme.BgCard;
                    child.BorderBrush = sel ? Theme.AccentAmber : Theme.BorderSubtle;
                    child.BorderThickness = new Thickness(sel ? 1.5 : 1);
                }
            }
            RebuildDetailInspectorUI();
        }

        private void RebuildDetailInspectorUI()
        {
            _detailContainer.Children.Clear();

            if (_selectedConflict == null)
            {
                _detailContainer.Children.Add(new TextBlock
                {
                    Text = I18n.SelectItemPrompt,
                    Foreground = Theme.TextSecondary,
                    FontSize = 13,
                    Margin = new Thickness(6, 18, 6, 6),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            var item = _selectedConflict;
            var (sevBg, sevBorder, sevFg) = Theme.GetSeverityColors(item.Severity);

            // 1. Overview & Reason Card
            var overviewBorder = new Border
            {
                Background = Theme.BgCard,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var ovStack = new StackPanel();

            var topBadges = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            topBadges.Children.Add(Theme.CreatePillBadge(item.Severity, sevBg, sevBorder, sevFg, 11));
            topBadges.Children.Add(Theme.CreatePillBadge(item.CategoryBadge, Theme.BgDeep, Theme.BorderSubtle, Theme.TextSecondary, 11));
            foreach (var mt in item.InvolvedModTypes.OrderByDescending(x => x))
            {
                var (mtBg, mtBorder, mtFg) = GetModTypeBadgeColors(mt);
                topBadges.Children.Add(Theme.CreatePillBadge(I18n.ModTypeBadge(mt), mtBg, mtBorder, mtFg, 11));
            }
            if (item.InvolvesDisabledMod)
            {
                topBadges.Children.Add(Theme.CreatePillBadge(
                    $"⏸ {I18n.DisabledBadge}",
                    Theme.BrushFromHex("#282C37"),
                    Theme.BrushFromHex("#58627A"),
                    Theme.BrushFromHex("#C0C8D8"),
                    11));
            }
            ovStack.Children.Add(topBadges);

            ovStack.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                TextWrapping = TextWrapping.Wrap
            });
            ovStack.Children.Add(new TextBlock
            {
                Text = item.Subtitle,
                FontSize = 12,
                Foreground = Theme.AccentAmber,
                Margin = new Thickness(0, 3, 0, 10),
                TextWrapping = TextWrapping.Wrap
            });

            // Reason Box
            ovStack.Children.Add(new TextBlock
            {
                Text = I18n.T("📌 冲突原因分析:", "📌 Conflict Analysis:"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 3)
            });
            ovStack.Children.Add(new TextBlock
            {
                Text = item.Reason,
                FontSize = 12.5,
                Foreground = Theme.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Recommendation Box
            ovStack.Children.Add(new TextBlock
            {
                Text = I18n.T("💡 处理与排查建议:", "💡 Recommendation:"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 3)
            });
            ovStack.Children.Add(new TextBlock
            {
                Text = item.Recommendation,
                FontSize = 12.5,
                Foreground = Theme.AccentCyan,
                TextWrapping = TextWrapping.Wrap
            });

            overviewBorder.Child = ovStack;
            _detailContainer.Children.Add(overviewBorder);

            // 2. Involved INI Files & One-Click Open/Locate Actions
            _detailContainer.Children.Add(new TextBlock
            {
                Text = $"{I18n.InvolvedFilesTitle} ({item.FileEntries.Count})",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 6)
            });

            foreach (var fe in item.FileEntries)
            {
                var fileCard = new Border
                {
                    Background = Theme.BgCard,
                    BorderBrush = Theme.BorderSubtle,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var fileGrid = new Grid();
                fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var fInfoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var modRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
                var (mtBg, mtBorder, mtFg) = GetModTypeBadgeColors(fe.ModType);
                modRow.Children.Add(Theme.CreatePillBadge(
                    I18n.ModTypeBadge(fe.ModType),
                    mtBg,
                    mtBorder,
                    mtFg,
                    10));
                if (fe.IsDisabled)
                {
                    modRow.Children.Add(Theme.CreatePillBadge(
                        I18n.DisabledBadge,
                        Theme.BrushFromHex("#282C37"),
                        Theme.BrushFromHex("#58627A"),
                        Theme.BrushFromHex("#C0C8D8"),
                        10));
                }
                modRow.Children.Add(new TextBlock
                {
                    Text = fe.ModId,
                    FontSize = 12.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Theme.AccentAmber,
                    VerticalAlignment = VerticalAlignment.Center
                });
                fInfoStack.Children.Add(modRow);

                fInfoStack.Children.Add(new TextBlock
                {
                    Text = $"{fe.RelativePath}   ({fe.LinesSummary})",
                    FontSize = 12,
                    Foreground = Theme.TextPrimary,
                    TextWrapping = TextWrapping.Wrap
                });

                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };

                string targetIniPath = fe.FilePath;
                var btnOpenIni = Theme.CreateStyledButton(
                    I18n.OpenIniBtn,
                    Theme.BgDeep,
                    Theme.TextPrimary,
                    Theme.BorderSubtle,
                    new Thickness(10, 5, 10, 5),
                    false);
                btnOpenIni.FontSize = 11.5;
                btnOpenIni.Margin = new Thickness(0, 0, 6, 0);
                btnOpenIni.ToolTip = targetIniPath;
                btnOpenIni.Click += (s, e) => OpenFileInEditor(targetIniPath);

                var btnReveal = Theme.CreateStyledButton(
                    I18n.RevealFileBtn,
                    Theme.BgDeep,
                    Theme.AccentCyan,
                    Theme.BorderSubtle,
                    new Thickness(10, 5, 10, 5),
                    false);
                btnReveal.FontSize = 11.5;
                btnReveal.ToolTip = targetIniPath;
                btnReveal.Click += (s, e) => RevealFileInExplorer(targetIniPath);

                btnPanel.Children.Add(btnOpenIni);
                btnPanel.Children.Add(btnReveal);

                Grid.SetColumn(fInfoStack, 0);
                Grid.SetColumn(btnPanel, 1);
                fileGrid.Children.Add(fInfoStack);
                fileGrid.Children.Add(btnPanel);
                fileCard.Child = fileGrid;

                _detailContainer.Children.Add(fileCard);
            }

            // 3. Section Code Preview
            _detailContainer.Children.Add(new TextBlock
            {
                Text = I18n.CodePreviewTitle,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Theme.TextPrimary,
                Margin = new Thickness(0, 8, 0, 6)
            });

            var sbCode = new StringBuilder();
            int maxPreview = Math.Min(item.Sections.Count, 25);
            for (int i = 0; i < maxPreview; i++)
            {
                var sec = item.Sections[i];
                string disMark = sec.ModIsDisabled ? $"[{I18n.DisabledBadge}] " : "";
                sbCode.AppendLine($"; --- {disMark}{sec.Mod} :: {sec.RelativeFile} (Line {sec.Line}) ---");
                sbCode.AppendLine(sec.Text.Trim());
                if (!string.IsNullOrEmpty(sec.PatternText))
                {
                    sbCode.AppendLine($"[{sec.Name}.Pattern]");
                    sbCode.AppendLine(sec.PatternText.Trim());
                }
                sbCode.AppendLine();
            }
            if (item.Sections.Count > maxPreview)
            {
                sbCode.AppendLine(I18n.T(
                    $"; ... 其余 {item.Sections.Count - maxPreview} 个节已省略 ...",
                    $"; ... {item.Sections.Count - maxPreview} more sections omitted ..."));
            }

            var codeBox = new TextBox
            {
                Text = sbCode.ToString().TrimEnd(),
                IsReadOnly = true,
                Background = Theme.BgCode,
                Foreground = Theme.BrushFromHex("#D8E2F0"),
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _detailContainer.Children.Add(codeBox);
        }

        private void OpenFileInEditor(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show(this, I18n.T($"文件不存在: {filePath}", $"File not found: {filePath}"));
                return;
            }
            try
            {
                // Use Windows Shell default file association for .ini (VS Code, Notepad++, Sublime, etc.)
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    // Fallback to notepad.exe only if the system has no .ini association configured
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }
        }

        private void RevealFileInExplorer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show(this, I18n.T($"文件不存在: {filePath}", $"File not found: {filePath}"));
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void OnBrowseTargetClicked(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = I18n.T("选择 EFMI Mods 文件夹或单个 Mod 文件夹", "Select EFMI Mods folder or a specific Mod folder"),
                SelectedPath = Directory.Exists(_currentTarget) ? _currentTarget : AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = false
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
            {
                _currentTarget = dlg.SelectedPath;
                _txtTarget.Text = _currentTarget;
                RunScan(null);
            }
        }

        private void OnOpenTargetFolderClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string root = AuditorEngine.GetScanRoot(_txtTarget.Text.Trim());
                if (Directory.Exists(root))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{root}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void OnExportReportClicked(object sender, RoutedEventArgs e)
        {
            if (_currentAudit == null) return;
            SaveReportToDisk();
            MessageBox.Show(
                this,
                I18n.T(
                    $"报告已导出到扫描根目录:\n{_currentAudit.ScanRoot}",
                    $"Report exported to scan root:\n{_currentAudit.ScanRoot}"),
                I18n.T("导出成功", "Export Complete"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OnShaderFixesIndicatorClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string sfDir = _currentAudit != null && !string.IsNullOrEmpty(_currentAudit.ShaderFixesPath)
                    ? _currentAudit.ShaderFixesPath
                    : Path.Combine(AuditorEngine.GetScanRoot(_txtTarget.Text.Trim()), "ShaderFixes");

                if (Directory.Exists(sfDir))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{sfDir}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        this,
                        I18n.T(
                            $"当前目录下未找到 ShaderFixes 文件夹:\n{sfDir}",
                            $"ShaderFixes folder not found under current directory:\n{sfDir}"),
                        "ShaderFixes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void OnWindowDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnWindowDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0)
                {
                    _currentTarget = paths[0];
                    _txtTarget.Text = _currentTarget;
                    RunScan(null);
                }
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string target = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            bool includeDisabled = false;

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i].Trim().Trim('"');
                    if (a.Equals("-Target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        target = args[++i].Trim().Trim('"').TrimEnd('\\', '/');
                    }
                    else if (a.Equals("-IncludeDisabled", StringComparison.OrdinalIgnoreCase))
                    {
                        includeDisabled = true;
                    }
                    else if (a.Equals("-Lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        string l = args[++i].Trim().Trim('"');
                        I18n.Current = l.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? Lang.EN : Lang.ZH;
                    }
                    else if (!a.StartsWith("-") && (Directory.Exists(a) || File.Exists(a)))
                    {
                        target = a.TrimEnd('\\', '/');
                    }
                }
            }

            var app = new Application();
            var win = new MainWindow(target, includeDisabled);
            app.Run(win);
        }
    }
}
