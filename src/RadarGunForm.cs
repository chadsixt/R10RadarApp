using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LaunchMonitor.Proto;
using Timer = System.Windows.Forms.Timer;

namespace gspro_r10
{
  public class RadarGunForm : Form
  {
    private readonly Label mphLabel;
    private readonly Label shotLabel;
    private readonly Label statusLabel;
    private readonly ListBox velocityHistory;
    private readonly TextBox sessionNameTextBox;
    private readonly Button startSessionButton;
    private readonly Button stopSessionButton;
    private readonly Button recentToggleButton;
    private readonly Label sessionStatusLabel;
    private readonly Button consoleButton;
    private readonly Label pitchCountLabel;
    private readonly Label averageSpeedLabel;
    private readonly Label maxSpeedLabel;
    private readonly Label consistencyLabel;
    private readonly Label liveSpeedCaptionLabel;
    private readonly Label totalDistanceLabel;
    private readonly HeatmapPanel pitchLocationPanel;
    private readonly Label pitchLocationTitleLabel;
    private readonly TableLayoutPanel topCards;
    private readonly TableLayoutPanel bottom;
    private readonly Control chartPanel;
    private readonly Control recentPitchesPanel;
    private readonly Label recentPitchesTitleLabel;
    private readonly Button toggleRecentPitchesButton;
    private readonly Timer recentPitchesAnimationTimer;
    private readonly TableLayoutPanel rootLayout;
    private readonly TableLayoutPanel contentPanel;
    private readonly TableLayoutPanel metricsPanel;
    private readonly TableLayoutPanel headerPanel;
    private readonly GlassPanel navigationPanel;
    private readonly GlassPanel liveCardPanel;
    private readonly GlassPanel deviceCardPanel;
    private readonly Control sessionHeaderPanel;
    private readonly Label dashboardNavItem;
    private readonly Label analyticsNavItem;
    private readonly GlassPanel analyticsPanel;
    private readonly Panel bottomRightPanel;
    private readonly Image? fullScreenFieldBackground;
    private readonly Timer resetTimer;
    private readonly List<VelocityReading> sessionReadings = new();
    private readonly List<VelocityReading> dashboardReadings = new();
    private bool sessionActive;
    private bool sessionSaved = true;
    private bool recentPitchesExpanded = true;
    private int recentPitchesTargetWidth = 285;
    private string sessionName = string.Empty;
    private string sessionType = string.Empty;
    private string connectionStatus = "STARTING";
    private DateTime? sessionStartedAt;
    private const double InchesToMeters = 0.0254;
    private const double MonitorBelowBallInches = 8.0;
    private const double BattingLaunchHeightInches = 24.0;
    private const double PitchingReleaseHeightInches = 68.0;

    private sealed record VelocityReading(DateTime Timestamp, int ShotId, double BallSpeedMph);

    public RadarGunForm()
    {
      Text = "R10 Radar App - Version 1.5";
      StartPosition = FormStartPosition.CenterScreen;
      BackColor = Color.FromArgb(7, 13, 21);
      ForeColor = Color.FromArgb(224, 231, 240);
      ClientSize = new Size(1460, 780);
      MinimumSize = new Size(1160, 650);
      Font = new Font("Segoe UI", 10F);

      Color surface = Color.FromArgb(15, 25, 37);
      Color surfaceRaised = Color.FromArgb(19, 31, 45);
      Color blue = Color.FromArgb(0, 151, 255);
      Color muted = Color.FromArgb(151, 164, 180);

      string fieldImagePath = Path.Combine(AppContext.BaseDirectory, "assets", "batting-practice-field.png");
      if (File.Exists(fieldImagePath))
      {
        using var source = Image.FromFile(fieldImagePath);
        fullScreenFieldBackground = new Bitmap(source);
      }

      rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = BackColor, BackgroundImageLayout = ImageLayout.Stretch };
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
      rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));

      navigationPanel = new GlassPanel(Color.FromArgb(220, 12, 21, 32)) { Dock = DockStyle.Fill };
      var brand = new Label { Location = new Point(0, 8), Size = new Size(190, 78), Text = "R10\nRADAR APP", ForeColor = Color.White, Font = new Font("Segoe UI", 16F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
      navigationPanel.Controls.Add(brand);
      var navItems = new FlowLayoutPanel { Location = new Point(17, 94), Size = new Size(170, 270), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };
      dashboardNavItem = CreateNavItem("[ ]   Dashboard", true);
      analyticsNavItem = CreateNavItem("[~]   Analytics", false);
      navItems.Controls.Add(dashboardNavItem);
      navItems.Controls.Add(analyticsNavItem);
      navItems.Controls.Add(CreateNavItem("[o]   Session", false));
      navItems.Controls.Add(CreateNavItem("[o]   History", false));
      navItems.Controls.Add(CreateNavItem("[o]   Players", false));
      navItems.Controls.Add(CreateNavItem("[o]   Settings", false));
      dashboardNavItem.Click += (_, _) => ShowDashboardView();
      analyticsNavItem.Click += (_, _) => ShowAnalyticsView();
      navigationPanel.Controls.Add(navItems);
      navigationPanel.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 55, Text = ".  App v1.5.0\n    Up to date", ForeColor = Color.FromArgb(91, 213, 126), Font = new Font("Segoe UI", 9F), Padding = new Padding(0, 10, 0, 0) });
      rootLayout.Controls.Add(navigationPanel, 0, 0);

      contentPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = BackColor, Padding = new Padding(24, 20, 24, 20) };
      contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 9)); contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 18)); contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 43)); rootLayout.Controls.Add(contentPanel, 1, 0);

      headerPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
      headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54)); headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
      sessionHeaderPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
      sessionHeaderPanel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 22, Text = "SESSION NAME", ForeColor = muted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) });
      sessionNameTextBox = new TextBox { Dock = DockStyle.Bottom, Height = 32, Text = "Bullpen - May 20, 2025", BackColor = surfaceRaised, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 13F) }; sessionHeaderPanel.Controls.Add(sessionNameTextBox); headerPanel.Controls.Add(sessionHeaderPanel, 0, 0);
      var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
      startSessionButton = CreateButton("[>]  Start Session", 136, Color.FromArgb(17, 177, 71)); stopSessionButton = CreateButton("[ ]  Stop", 104, surfaceRaised); stopSessionButton.Enabled = false;
      actions.Controls.Add(stopSessionButton); actions.Controls.Add(startSessionButton); headerPanel.Controls.Add(actions, 1, 0); contentPanel.Controls.Add(headerPanel, 0, 0);
      startSessionButton.Click += (_, _) => StartSession(); stopSessionButton.Click += (_, _) => StopSession();
      recentToggleButton = CreateButton("Hide Hits", 92, surfaceRaised); recentToggleButton.Height = 28; recentToggleButton.Click += (_, _) => ToggleRecentHits(); actions.Controls.Add(recentToggleButton);

      topCards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
      topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34)); topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
      liveCardPanel = (GlassPanel)CreateCard(surface); liveCardPanel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 24, Text = "LIVE SPEED                              . LIVE", ForeColor = Color.FromArgb(255, 166, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
      mphLabel = new Label { Dock = DockStyle.Fill, Text = "0.0", ForeColor = Color.FromArgb(255, 159, 10), Font = new Font("Segoe UI", 68F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; liveCardPanel.Controls.Add(mphLabel);
      liveSpeedCaptionLabel = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "MPH", ForeColor = muted, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; liveCardPanel.Controls.Add(liveSpeedCaptionLabel); topCards.Controls.Add(liveCardPanel, 0, 0);
      totalDistanceLabel = new Label { Dock = DockStyle.Bottom, Height = 20, Text = "TOTAL DISTANCE  0 FT", ForeColor = muted, Font = new Font("Segoe UI", 8F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; liveCardPanel.Controls.Add(totalDistanceLabel);
      deviceCardPanel = (GlassPanel)CreateCard(surface); deviceCardPanel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "DEVICE STATUS", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
      shotLabel = new Label { Dock = DockStyle.Top, Height = 42, Text = ".  R10 Connected", ForeColor = Color.FromArgb(43, 226, 112), Font = new Font("Segoe UI", 15F, FontStyle.Bold), Padding = new Padding(0, 8, 0, 0) }; deviceCardPanel.Controls.Add(shotLabel);
      statusLabel = new Label { Dock = DockStyle.Top, Height = 36, Text = "Bluetooth   Connected\nBattery        87%", ForeColor = muted, Font = new Font("Segoe UI", 10F), Padding = new Padding(8, 2, 0, 0) }; deviceCardPanel.Controls.Add(statusLabel); topCards.Controls.Add(deviceCardPanel, 1, 0);
      pitchLocationPanel = new HeatmapPanel { Dock = DockStyle.Fill, BackColor = surface }; pitchLocationTitleLabel = new Label { Dock = DockStyle.Top, Height = 28, Text = "PREDICTED PLATE LOCATION", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(10, 8, 0, 0) }; pitchLocationPanel.Controls.Add(pitchLocationTitleLabel); topCards.Controls.Add(pitchLocationPanel, 2, 0); contentPanel.Controls.Add(topCards, 0, 1);

      metricsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.Transparent }; for (int i = 0; i < 4; i++) metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      maxSpeedLabel = AddMetric(metricsPanel, 0, "MAX SPEED", "0.0", "MPH", Color.White, surfaceRaised); averageSpeedLabel = AddMetric(metricsPanel, 1, "AVERAGE SPEED", "0.0", "MPH", Color.White, surfaceRaised); pitchCountLabel = AddMetric(metricsPanel, 2, "PITCH COUNT", "0", "PITCHES", Color.White, surfaceRaised); consistencyLabel = AddMetric(metricsPanel, 3, "CONSISTENCY", "--", "+/- 0.0 MPH", Color.FromArgb(42, 222, 111), surfaceRaised); contentPanel.Controls.Add(metricsPanel, 0, 2);

      bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent }; bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
      var chart = new VelocityChartPanel { Dock = DockStyle.Fill, BackColor = surface }; chart.Controls.Add(new Label { Dock = DockStyle.Top, Height = 34, Text = "VELOCITY OVER TIME", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(14, 12, 0, 0) }); chartPanel = chart;
      analyticsPanel = new GlassPanel(Color.FromArgb(218, 12, 21, 32)) { Dock = DockStyle.Fill, Padding = new Padding(12) };
      analyticsPanel.Controls.Add(chartPanel);
      rootLayout.Controls.Add(analyticsPanel, 1, 0);
      var recent = new GlassPanel(Color.FromArgb(220, 12, 21, 32)) { Dock = DockStyle.Fill, Padding = new Padding(14, 20, 12, 20) };
      recentPitchesTitleLabel = new Label { Dock = DockStyle.Top, Height = 45, Text = "RECENT PITCHES", ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Padding = new Padding(4, 8, 0, 0) };
      toggleRecentPitchesButton = new Button { Dock = DockStyle.Top, Height = 36, Text = "Hide  \u276f", ForeColor = Color.FromArgb(190, 210, 230), BackColor = Color.FromArgb(24, 39, 55), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
      toggleRecentPitchesButton.FlatAppearance.BorderColor = Color.FromArgb(45, 67, 88);
      toggleRecentPitchesButton.Click += (_, _) => SetRecentPitchesExpanded(!recentPitchesExpanded);
      velocityHistory = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9F), ForeColor = Color.FromArgb(228, 235, 243), BackColor = Color.FromArgb(12, 21, 32), BorderStyle = BorderStyle.None, HorizontalScrollbar = true, ItemHeight = 34 };
      velocityHistory.Items.Add("PITCH    SPEED         TIME");
      recent.Controls.Add(velocityHistory);
      recent.Controls.Add(recentPitchesTitleLabel);
      recent.Controls.Add(toggleRecentPitchesButton);
      recentPitchesPanel = recent;
      rootLayout.Controls.Add(recentPitchesPanel, 2, 0);
      sessionStatusLabel = new Label { Dock = DockStyle.Fill, Text = "No session started", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(2, 8, 0, 0) }; bottom.Controls.Add(sessionStatusLabel, 0, 1);
      consoleButton = CreateButton("Show Console", 120, surfaceRaised); consoleButton.Dock = DockStyle.Bottom; consoleButton.Height = 28; consoleButton.Click += (_, _) => ToggleConsole();
      bottomRightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }; bottomRightPanel.Controls.Add(consoleButton); bottom.Controls.Add(bottomRightPanel, 1, 1); contentPanel.Controls.Add(bottom, 0, 3);
      Controls.Add(rootLayout);
      ShowDashboardView();
      Resize += (_, _) => UpdateResponsiveLayout();
      pitchLocationPanel.Resize += (_, _) => pitchLocationPanel.Invalidate();
      UpdateResponsiveLayout();

      recentPitchesAnimationTimer = new Timer { Interval = 15 };
      recentPitchesAnimationTimer.Tick += (_, _) => AnimateRecentPitchesPanel();

      resetTimer = new Timer
      {
        Interval = 2200
      };
      resetTimer.Tick += (_, _) =>
      {
        resetTimer.Stop();
        mphLabel.Text = "0.0";
        shotLabel.Text = connectionStatus;
        shotLabel.ForeColor = GetConnectionStatusColor(connectionStatus);
        statusLabel.Text = "Bluetooth   Connected\nBattery        87%";
      };
      pitchLocationPanel.DisplayMode = HeatmapPanel.PanelMode.Heatmap;
      SetBattingPracticeLayout(false);
    }

    private static Button CreateButton(string text, int width, Color backColor) => new Button
    {
      Text = text,
      Width = width,
      Height = 46,
      BackColor = backColor,
      ForeColor = Color.White,
      Font = new Font("Segoe UI", 9, FontStyle.Bold),
      FlatStyle = FlatStyle.Flat,
      FlatAppearance = { BorderColor = Color.FromArgb(38, 71, 102), BorderSize = 1 }
    };

    private static Panel CreateCard(Color backColor) => new GlassPanel(Color.FromArgb(218, backColor)) { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(5) };

    private static Label CreateNavItem(string text, bool active) => new Label { Width = 156, Height = 48, Text = text, ForeColor = active ? Color.White : Color.FromArgb(169, 181, 195), BackColor = active ? Color.FromArgb(24, 46, 69) : Color.Transparent, Font = new Font("Segoe UI", 10F, active ? FontStyle.Bold : FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Margin = new Padding(0, 2, 0, 2) };

    private static Label AddMetric(TableLayoutPanel parent, int column, string title, string value, string suffix, Color valueColor, Color backColor)
    {
      var card = CreateCard(backColor); card.Controls.Add(new Label { Dock = DockStyle.Top, Height = 24, Text = title, ForeColor = Color.FromArgb(165, 177, 191), Font = new Font("Segoe UI", 8F, FontStyle.Bold) });
      var valueLabel = new Label { Dock = DockStyle.Fill, Text = value, ForeColor = valueColor, Font = new Font("Segoe UI", 27F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }; card.Controls.Add(valueLabel); card.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 18, Text = suffix, ForeColor = Color.FromArgb(165, 177, 191), Font = new Font("Segoe UI", 8F) }); parent.Controls.Add(card, column, 0); return valueLabel;
    }

    private void SetRecentPitchesExpanded(bool expanded)
    {
      recentPitchesExpanded = expanded;
      recentPitchesTargetWidth = expanded ? 285 : 44;
      recentPitchesAnimationTimer.Stop();
      if (expanded)
      {
        recentPitchesPanel.Padding = new Padding(14, 20, 12, 20);
        toggleRecentPitchesButton.Text = "\u276e";
      }
      else
      {
        recentPitchesTitleLabel.Visible = false;
        velocityHistory.Visible = false;
        recentPitchesPanel.Padding = new Padding(4, 20, 4, 20);
        toggleRecentPitchesButton.Text = "\u276e";
      }
      recentPitchesAnimationTimer.Start();
    }

    private void AnimateRecentPitchesPanel()
    {
      ColumnStyle rail = rootLayout.ColumnStyles[2];
      float difference = recentPitchesTargetWidth - rail.Width;
      if (Math.Abs(difference) <= 2)
      {
        rail.Width = recentPitchesTargetWidth;
        recentPitchesAnimationTimer.Stop();
        if (recentPitchesExpanded)
        {
          recentPitchesTitleLabel.Visible = true;
          velocityHistory.Visible = true;
          toggleRecentPitchesButton.Text = "Hide  \u276f";
        }
        else
        {
          toggleRecentPitchesButton.Text = "\u276e";
        }
        rootLayout.PerformLayout();
        return;
      }

      rail.Width += Math.Sign(difference) * Math.Max(6, Math.Abs(difference) / 4);
      rootLayout.PerformLayout();
    }

    private sealed class GlassPanel : Panel
    {
      private Color overlayColor;

      public Color OverlayColor
      {
        get => overlayColor;
        set { overlayColor = value; Invalidate(); }
      }

      public GlassPanel(Color overlay)
      {
        overlayColor = overlay;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
      }

      protected override void OnPaintBackground(PaintEventArgs e)
      {
        base.OnPaintBackground(e);
        using var brush = new SolidBrush(overlayColor);
        e.Graphics.FillRectangle(brush, ClientRectangle);
      }
    }

    private void ToggleConsole()
    {
      bool consoleVisible = Program.ToggleConsoleWindow();
      consoleButton.Text = consoleVisible ? "Hide Console" : "Show Console";
    }

    private void ToggleRecentHits()
    {
      bool showRecentHits = !recentPitchesPanel.Visible || rootLayout.ColumnStyles[2].Width == 0;
      recentPitchesPanel.Visible = showRecentHits;
      rootLayout.ColumnStyles[2].Width = showRecentHits ? 285 : 0;
      recentToggleButton.Text = showRecentHits ? "Hide Hits" : "Show Hits";
      rootLayout.PerformLayout();
    }

    private void ShowDashboardView()
    {
      contentPanel.Visible = true;
      analyticsPanel.Visible = false;
      recentPitchesPanel.Visible = true;
      rootLayout.ColumnStyles[2].Width = 285;
      recentToggleButton.Text = "Hide Hits";
      SetNavigationState(dashboardNavItem);
    }

    private void ShowAnalyticsView()
    {
      if (sessionType == "Batting Practice") return;
      contentPanel.Visible = false;
      analyticsPanel.Visible = true;
      recentPitchesPanel.Visible = false;
      rootLayout.ColumnStyles[2].Width = 0;
      recentToggleButton.Text = "Show Hits";
      analyticsPanel.BringToFront();
      SetNavigationState(analyticsNavItem);
    }

    private static void SetNavigationState(Label activeItem)
    {
      if (activeItem.Parent == null) return;
      foreach (Control sibling in activeItem.Parent.Controls)
      {
        if (sibling is Label label)
        {
          label.BackColor = label == activeItem ? Color.FromArgb(24, 46, 69) : Color.Transparent;
          label.ForeColor = label == activeItem ? Color.White : Color.FromArgb(169, 181, 195);
        }
      }
    }

    private void UpdateResponsiveLayout()
    {
      int availableHeight = Math.Max(540, ClientSize.Height);
      float scale = Math.Clamp(availableHeight / 780F, 0.82F, 1.35F);
      mphLabel.Font = new Font("Segoe UI", 68F * scale, FontStyle.Bold);
      liveSpeedCaptionLabel.Font = new Font("Segoe UI", 14F * scale, FontStyle.Bold);
      maxSpeedLabel.Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold);
      totalDistanceLabel.Font = new Font("Segoe UI", 8F * scale, FontStyle.Bold);
      averageSpeedLabel.Font = new Font("Segoe UI", (sessionType == "Batting Practice" ? 22F : 27F) * scale, FontStyle.Bold);
      pitchCountLabel.Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold);
      consistencyLabel.Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold);
      statusLabel.Font = new Font("Segoe UI", 10F * scale, FontStyle.Regular);
      int buttonHeight = (int)Math.Clamp(46F * scale, 38F, 56F);
      startSessionButton.Height = buttonHeight;
      stopSessionButton.Height = buttonHeight;
    }

    private sealed class VelocityChartPanel : Panel
    {
      protected override void OnPaint(PaintEventArgs e)
      {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var gridPen = new Pen(Color.FromArgb(35, 52, 70));
        using var linePen = new Pen(Color.FromArgb(0, 151, 255), 2F);
        int left = 38, top = 48, right = Width - 22, bottom = Height - 28;
        for (int index = 0; index < 5; index++)
        {
          int y = top + (bottom - top) * index / 4;
          e.Graphics.DrawLine(gridPen, left, y, right, y);
        }
        e.Graphics.DrawLine(gridPen, left, top, left, bottom);
        e.Graphics.DrawLine(gridPen, left, bottom, right, bottom);
        Point[] points = Enumerable.Range(0, 18).Select(index => new Point(left + (right - left) * index / 17, top + 65 - (int)(Math.Sin(index * 1.6) * 18) - (index % 4) * 2)).ToArray();
        e.Graphics.DrawLines(linePen, points);
        using var dotBrush = new SolidBrush(Color.FromArgb(0, 151, 255));
        foreach (Point point in points) e.Graphics.FillEllipse(dotBrush, point.X - 4, point.Y - 4, 8, 8);
      }
    }

    private sealed class HeatmapPanel : Panel
    {
      public enum PanelMode { Heatmap, HitterDiamond }

      public PanelMode DisplayMode { get; set; }
      public bool ShowFieldBackground { get; set; } = true;
      private readonly Image? battingPracticeBackground;

      public HeatmapPanel()
      {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
        string imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "batting-practice-field.png");
        if (File.Exists(imagePath))
        {
          using var source = Image.FromFile(imagePath);
          battingPracticeBackground = new Bitmap(source);
        }
      }

      protected override void OnPaint(PaintEventArgs e)
      {
        base.OnPaint(e);
        if (DisplayMode == PanelMode.HitterDiamond)
        {
          PaintHitterDiamond(e);
          return;
        }

        int availableHeight = Math.Max(80, Height - 52);
        int size = Math.Max(70, Math.Min((int)(Width * 0.58), (int)(availableHeight * 0.72)));
        int left = (Width - size) / 2, top = 40 + (availableHeight - size) / 2;
        using var borderPen = new Pen(Color.FromArgb(188, 201, 214), 1F);
        using var gridPen = new Pen(Color.FromArgb(120, 145, 166), 1F);
        e.Graphics.DrawRectangle(borderPen, left, top, size, size);
        for (int index = 1; index < 3; index++)
        {
          e.Graphics.DrawLine(gridPen, left + size * index / 3, top, left + size * index / 3, top + size);
          e.Graphics.DrawLine(gridPen, left, top + size * index / 3, left + size, top + size * index / 3);
        }
        using var outer = new SolidBrush(Color.FromArgb(45, 50, 191, 190));
        using var middle = new SolidBrush(Color.FromArgb(75, 249, 206, 48));
        using var center = new SolidBrush(Color.FromArgb(175, 236, 48, 32));
        e.Graphics.FillEllipse(outer, left + size / 7, top + size / 4, size * 5 / 7, size / 2);
        e.Graphics.FillEllipse(middle, left + size / 4, top + size / 3, size / 2, size / 3);
        e.Graphics.FillEllipse(center, left + size * 2 / 5, top + size * 2 / 5, size / 5, size / 5);

        const double zoneWidthFeet = 17.0 / 12.0;
        const double zoneBottomFeet = 1.5;
        const double zoneTopFeet = 3.5;
        foreach (PlateCrossing crossing in plateCrossings)
        {
          bool missedLeft = crossing.LateralFeet < -zoneWidthFeet / 2;
          bool missedRight = crossing.LateralFeet > zoneWidthFeet / 2;
          bool missedLow = crossing.HeightFeet < zoneBottomFeet;
          bool missedHigh = crossing.HeightFeet > zoneTopFeet;
          bool isStrike = !missedLeft && !missedRight && !missedLow && !missedHigh;

          float x = left + size / 2F + (float)(crossing.LateralFeet / zoneWidthFeet * size);
          float y = top + size - (float)((crossing.HeightFeet - zoneBottomFeet) / (zoneTopFeet - zoneBottomFeet) * size);
          if (missedLeft) x = left - 24;
          else if (missedRight) x = left + size + 24;
          else x = Math.Clamp(x, left + 6, left + size - 6);
          if (missedHigh) y = top - 20;
          else if (missedLow) y = top + size + 20;
          else y = Math.Clamp(y, top + 6, top + size - 6);

          Color markerColor = isStrike ? Color.FromArgb(0, 151, 255) : Color.FromArgb(255, 159, 10);
          using var glow = new SolidBrush(Color.FromArgb(65, markerColor));
          using var ball = new SolidBrush(Color.FromArgb(235, 239, 247, 255));
          using var outline = new Pen(markerColor, 3F);
          e.Graphics.FillEllipse(glow, x - 13, y - 13, 26, 26);
          e.Graphics.FillEllipse(ball, x - 7, y - 7, 14, 14);
          e.Graphics.DrawEllipse(outline, x - 8, y - 8, 16, 16);
        }
      }

      private void PaintHitterDiamond(PaintEventArgs e)
      {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int fieldTop = 0;
        int centerX = Width / 2;
        int plateY = Height - 12;
        int horizon = Math.Max(fieldTop + 22, Height / 3);
        if (ShowFieldBackground && battingPracticeBackground != null)
        {
          e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          float scale = Math.Max((float)Width / battingPracticeBackground.Width, (float)(Height - fieldTop) / battingPracticeBackground.Height);
          int drawWidth = (int)Math.Ceiling(battingPracticeBackground.Width * scale);
          int drawHeight = (int)Math.Ceiling(battingPracticeBackground.Height * scale);
          int drawX = (Width - drawWidth) / 2;
          int drawY = fieldTop + (Height - fieldTop - drawHeight) / 2;
          e.Graphics.DrawImage(battingPracticeBackground, new Rectangle(drawX, drawY, drawWidth, drawHeight));
          using var tint = new SolidBrush(Color.FromArgb(38, 4, 13, 24));
          e.Graphics.FillRectangle(tint, 0, fieldTop, Width, Height - fieldTop);
        }
        else if (ShowFieldBackground)
        {
          using var fallback = new System.Drawing.Drawing2D.LinearGradientBrush(new Point(0, fieldTop), new Point(0, Height), Color.FromArgb(28, 65, 82), Color.FromArgb(29, 73, 47));
          e.Graphics.FillRectangle(fallback, 0, fieldTop, Width, Height - fieldTop);
        }

        foreach (List<FlightPoint> trajectory in trajectories)
        {
          double maxDistance = Math.Max(1, trajectory[^1].Distance);
          using var tracerGlowPen = new Pen(Color.FromArgb(85, 255, 194, 24), Math.Max(2.5F, Width / 320F));
          using var tracerPen = new Pen(Color.FromArgb(255, 235, 71), Math.Max(1.4F, Width / 550F));
          var screenPoints = trajectory.Select(point => new Point(
            centerX + (int)(point.Lateral / Math.Max(1, maxDistance) * Width * 0.42),
            plateY - (int)(point.Distance / maxDistance * (plateY - horizon) * 0.88) - (int)(point.Height * 1.2))).ToArray();
          e.Graphics.DrawLines(tracerGlowPen, screenPoints);
          e.Graphics.DrawLines(tracerPen, screenPoints);
          Point ball = screenPoints[^1];
          using var ballBrush = new SolidBrush(Color.White);
          e.Graphics.FillEllipse(ballBrush, ball.X - 7, ball.Y - 7, 14, 14);
          using var ballOutline = new Pen(Color.FromArgb(255, 190, 29), 2F);
          e.Graphics.DrawEllipse(ballOutline, ball.X - 7, ball.Y - 7, 14, 14);

          string distanceText = $"{maxDistance * 3.28084:0} FT";
          using var distanceFont = new Font("Segoe UI", 9F, FontStyle.Bold);
          SizeF textSize = e.Graphics.MeasureString(distanceText, distanceFont);
          float badgeWidth = textSize.Width + 14;
          float badgeHeight = textSize.Height + 6;
          float badgeX = ball.X + 12;
          if (badgeX + badgeWidth > Width - 6) badgeX = ball.X - badgeWidth - 12;
          float badgeY = Math.Clamp(ball.Y - badgeHeight / 2, 6, Height - badgeHeight - 6);
          var badgeBounds = new RectangleF(badgeX, badgeY, badgeWidth, badgeHeight);
          using var badgeBrush = new SolidBrush(Color.FromArgb(205, 7, 13, 21));
          using var badgeBorder = new Pen(Color.FromArgb(220, 255, 190, 29), 1F);
          using var textBrush = new SolidBrush(Color.White);
          e.Graphics.FillRectangle(badgeBrush, badgeBounds);
          e.Graphics.DrawRectangle(badgeBorder, badgeBounds.X, badgeBounds.Y, badgeBounds.Width, badgeBounds.Height);
          e.Graphics.DrawString(distanceText, distanceFont, textBrush, badgeX + 7, badgeY + 3);
        }
      }

      private readonly List<List<FlightPoint>> trajectories = new();
      private readonly List<PlateCrossing> plateCrossings = new();

      public void AddTrajectory(List<FlightPoint> points)
      {
        if (points.Count > 1) trajectories.Add(points.ToList());
        Invalidate();
        Update();
      }

      public void AddPlateCrossing(PlateCrossing crossing)
      {
        plateCrossings.Add(crossing);
        if (plateCrossings.Count > 30) plateCrossings.RemoveAt(0);
        Invalidate();
        Update();
      }

      public void ClearTrajectories()
      {
        trajectories.Clear();
        plateCrossings.Clear();
        Invalidate();
      }
    }

    public void UpdateShot(double ballSpeedMph, int shotId, BallMetrics? ballMetrics = null)
    {
      if (this.IsDisposed) return;

      if (this.InvokeRequired)
      {
        this.BeginInvoke(new Action(() => UpdateShot(ballSpeedMph, shotId, ballMetrics)));
        return;
      }

      mphLabel.Text = $"{ballSpeedMph:0.0}";
      shotLabel.Text = $"SHOT #{shotId}";
      shotLabel.ForeColor = Color.White;
      statusLabel.Text = "Bluetooth   Connected\nBattery        87%";
      var reading = new VelocityReading(DateTime.Now, shotId, ballSpeedMph);
      dashboardReadings.Add(reading);
      pitchCountLabel.Text = dashboardReadings.Count.ToString();
      maxSpeedLabel.Text = $"{dashboardReadings.Max(item => item.BallSpeedMph):0.0}";
      averageSpeedLabel.Text = $"{dashboardReadings.Average(item => item.BallSpeedMph):0.0}";
      double spread = dashboardReadings.Max(item => item.BallSpeedMph) - dashboardReadings.Min(item => item.BallSpeedMph);
      consistencyLabel.Text = $"{Math.Max(0, 100 - spread * 2):0}%";
      this.Text = $"R10 Radar App - Version 1.5 - {ballSpeedMph:0.0} MPH";
      double? distanceFeet = null;
      if (sessionType == "Batting Practice" && ballMetrics != null)
      {
        consistencyLabel.Text = $"{ballMetrics.LaunchAngle:0.0}\u00b0";
        List<FlightPoint> flightPath = CalculateFlightPath(ballMetrics);
        pitchLocationPanel.AddTrajectory(flightPath);
        if (flightPath.Count > 0)
        {
          distanceFeet = flightPath[^1].Distance * 3.28084;
          totalDistanceLabel.Text = $"TOTAL DISTANCE  {distanceFeet:0} FT";
        }
      }
      else if (sessionType == "Bullpen" && ballMetrics != null)
      {
        PlateCrossing? crossing = PredictPlateCrossing(ballMetrics);
        if (crossing.HasValue) pitchLocationPanel.AddPlateCrossing(crossing.Value);
      }

      string recentRow = sessionType == "Batting Practice"
        ? $"#{shotId,-4}  {ballSpeedMph,5:0.0} MPH   {(distanceFeet.HasValue ? $"{distanceFeet:0} FT" : "-- FT"),7}"
        : $"#{shotId,-5} {ballSpeedMph,5:0.0} MPH   {reading.Timestamp:h:mm:ss tt}";
      velocityHistory.Items.Insert(1, recentRow);
      while (velocityHistory.Items.Count > 31) velocityHistory.Items.RemoveAt(velocityHistory.Items.Count - 1);

      if (sessionActive)
      {
        sessionReadings.Add(reading);
        sessionSaved = false;
        UpdateSessionStatus();
      }

      resetTimer.Stop();
      resetTimer.Start();
    }

    private static List<FlightPoint> CalculateFlightPath(BallMetrics metrics)
    {
      const double metersPerSecondToMph = 2.236936;
      const double gravity = 9.80665;
      const double airDensity = 1.225;
      const double dragCoefficient = 0.35;
      const double ballMass = 0.04593;
      const double ballArea = 0.00426;
      double speed = metrics.BallSpeed * metersPerSecondToMph / 2.236936;
      double launchAngle = metrics.LaunchAngle * Math.PI / 180;
      double launchDirection = metrics.LaunchDirection * Math.PI / 180;
      double spinAxis = metrics.SpinAxis * Math.PI / 180;
      double horizontalSpeed = speed * Math.Cos(launchAngle);
      var velocity = new Vector3(
        horizontalSpeed * Math.Sin(launchDirection),
        speed * Math.Sin(launchAngle),
        horizontalSpeed * Math.Cos(launchDirection));
      double monitorHeightMeters = (BattingLaunchHeightInches - MonitorBelowBallInches) * InchesToMeters;
      double ballHeightAboveMonitorMeters = MonitorBelowBallInches * InchesToMeters;
      var position = new Vector3(0, monitorHeightMeters + ballHeightAboveMonitorMeters, 0);
      var points = new List<FlightPoint>();
      const double timeStep = 0.02;
      double spinLift = Math.Clamp(metrics.TotalSpin / 3500.0, 0.05, 1.2) * Math.Sin(spinAxis) * 1.4;

      for (double time = 0; time < 8 && position.Y >= 0; time += timeStep)
      {
        points.Add(new FlightPoint(position.X, position.Y, position.Z));
        double velocityMagnitude = velocity.Length;
        double drag = 0.5 * airDensity * dragCoefficient * ballArea * velocityMagnitude * velocityMagnitude / ballMass;
        var acceleration = new Vector3(
          -drag * velocity.X / velocityMagnitude + spinLift * velocity.Z / Math.Max(velocityMagnitude, 1),
          -gravity - drag * velocity.Y / velocityMagnitude,
          -drag * velocity.Z / velocityMagnitude);
        velocity += acceleration * timeStep;
        position += velocity * timeStep;
      }
      return points;
    }

    private static PlateCrossing? PredictPlateCrossing(BallMetrics metrics)
    {
      const double gravity = 9.80665;
      const double releaseToPlateMeters = 16.46;
      const double metersToFeet = 3.28084;
      double monitorHeightMeters = (PitchingReleaseHeightInches - MonitorBelowBallInches) * InchesToMeters;
      double releaseHeightMeters = monitorHeightMeters + MonitorBelowBallInches * InchesToMeters;
      double launchAngle = metrics.LaunchAngle * Math.PI / 180.0;
      double launchDirection = metrics.LaunchDirection * Math.PI / 180.0;
      double forwardVelocity = metrics.BallSpeed * Math.Cos(launchAngle) * Math.Cos(launchDirection);
      if (Math.Abs(forwardVelocity) <= 0.1) return null;
      double flightTime = releaseToPlateMeters / Math.Abs(forwardVelocity);
      if (flightTime <= 0 || flightTime > 2) return null;
      double lateralVelocity = metrics.BallSpeed * Math.Cos(launchAngle) * Math.Sin(launchDirection);
      double verticalVelocity = metrics.BallSpeed * Math.Sin(launchAngle);
      double lateralFeet = lateralVelocity * flightTime * metersToFeet;
      double heightFeet = (releaseHeightMeters + verticalVelocity * flightTime - 0.5 * gravity * flightTime * flightTime) * metersToFeet;
      if (!double.IsFinite(lateralFeet) || !double.IsFinite(heightFeet)) return null;
      return new PlateCrossing(lateralFeet, heightFeet);
    }

    private readonly record struct Vector3(double X, double Y, double Z)
    {
      public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
      public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
      public static Vector3 operator *(Vector3 value, double scalar) => new(value.X * scalar, value.Y * scalar, value.Z * scalar);
    }

    private readonly record struct FlightPoint(double Lateral, double Height, double Distance);
    private readonly record struct PlateCrossing(double LateralFeet, double HeightFeet);

    public void UpdateConnectionStatus(string status)
    {
      if (IsDisposed) return;
      if (InvokeRequired)
      {
        BeginInvoke(new Action(() => UpdateConnectionStatus(status)));
        return;
      }

      connectionStatus = status.ToUpperInvariant();
      if (!resetTimer.Enabled)
      {
        shotLabel.Text = connectionStatus;
        shotLabel.ForeColor = GetConnectionStatusColor(connectionStatus);
      }
    }

    private static Color GetConnectionStatusColor(string status)
    {
      return status.ToUpperInvariant() switch
      {
        "CONNECTING" or "RECONNECTING" => Color.MediumPurple,
        "CONNECTED" => Color.Gold,
        "READY" => Color.LimeGreen,
        "TRACKING SHOT" => Color.DeepSkyBlue,
        "PROCESSING SHOT" => Color.Gold,
        "STARTING" or "DEVICE NOT FOUND" or "SETUP FAILED" or
        "INTERFERENCE DETECTED" or "DEVICE ERROR" => Color.Red,
        _ => Color.White
      };
    }

    private void StartSession()
    {
      string? requestedName = PromptForText("Player name", "Enter the player's name:", sessionNameTextBox.Text.Trim());
      if (string.IsNullOrWhiteSpace(requestedName))
      {
        return;
      }

      string? requestedType = PromptForSessionType();
      if (requestedType is null) return;

      if (!sessionSaved && sessionReadings.Count > 0 &&
          MessageBox.Show(this, "Starting a new session will clear the unsaved session. Continue?",
            "Start new session", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

      sessionName = requestedName;
      sessionType = requestedType;
      pitchLocationPanel.DisplayMode = requestedType == "Batting Practice" ? HeatmapPanel.PanelMode.HitterDiamond : HeatmapPanel.PanelMode.Heatmap;
      pitchLocationTitleLabel.Text = requestedType == "Batting Practice" ? "BATTING PRACTICE" : "PREDICTED PLATE LOCATION";
      pitchLocationPanel.ClearTrajectories();
      SetBattingPracticeLayout(requestedType == "Batting Practice");
      pitchLocationPanel.Invalidate();
      sessionStartedAt = DateTime.Now;
      sessionReadings.Clear();
      sessionActive = true;
      sessionSaved = true;
      dashboardReadings.Clear();
      ResetRecentHistory(requestedType);
      ResetDashboardDisplay();
      sessionNameTextBox.Text = sessionName;
      sessionNameTextBox.Enabled = false;
      startSessionButton.Enabled = false;
      stopSessionButton.Enabled = true;
      UpdateSessionStatus();
    }

    private void StopSession()
    {
      if (sessionReadings.Count > 0 && !sessionSaved)
      {
        DialogResult saveChoice = MessageBox.Show(this, "Save this session before stopping?", "Save session",
          MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (saveChoice == DialogResult.Cancel) return;
        if (saveChoice == DialogResult.Yes && !SaveSession()) return;
      }

      sessionActive = false;
      sessionNameTextBox.Enabled = true;
      startSessionButton.Enabled = true;
      stopSessionButton.Enabled = false;
      resetTimer.Stop();
      pitchLocationPanel.DisplayMode = HeatmapPanel.PanelMode.Heatmap;
      pitchLocationTitleLabel.Text = "PREDICTED PLATE LOCATION";
      pitchLocationPanel.ClearTrajectories();
      SetBattingPracticeLayout(false);
      pitchLocationPanel.Invalidate();
      dashboardReadings.Clear();
      ResetRecentHistory("Bullpen");
      ResetDashboardDisplay();
      sessionNameTextBox.Clear();
      sessionStatusLabel.Text = "No session started";
      sessionStatusLabel.ForeColor = Color.FromArgb(151, 164, 180);
    }

    private void UpdateSessionStatus()
    {
      string state = sessionActive ? "RECORDING" : "STOPPED";
      sessionStatusLabel.Text = $"{state}: {sessionType} - {sessionName} - {sessionReadings.Count} reading(s)";
      sessionStatusLabel.ForeColor = sessionActive ? Color.LimeGreen : Color.Gold;
    }

    private void ResetDashboardDisplay()
    {
      mphLabel.Text = "0.0";
      maxSpeedLabel.Text = "0.0";
      averageSpeedLabel.Text = "0.0";
      pitchCountLabel.Text = "0";
      consistencyLabel.Text = "--";
      totalDistanceLabel.Text = "TOTAL DISTANCE  0 FT";
      shotLabel.Text = connectionStatus;
      shotLabel.ForeColor = GetConnectionStatusColor(connectionStatus);
      statusLabel.Text = "Bluetooth   Connected\nBattery        87%";
      Text = "R10 Radar App - Version 1.5";
    }

    private void ResetRecentHistory(string mode)
    {
      bool battingPractice = mode == "Batting Practice";
      recentPitchesTitleLabel.Text = battingPractice ? "RECENT HITS" : "RECENT PITCHES";
      velocityHistory.Items.Clear();
      velocityHistory.Items.Add(battingPractice
        ? "HIT      EXIT VELOCITY   DISTANCE"
        : "PITCH    SPEED         TIME");
    }

    private void SetBattingPracticeLayout(bool battingPractice)
    {
      rootLayout.SuspendLayout();
      contentPanel.SuspendLayout();
      topCards.SuspendLayout();
      bottom.SuspendLayout();
      contentPanel.Controls.Remove(metricsPanel);
      pitchLocationPanel.Controls.Remove(metricsPanel);
      topCards.Controls.Remove(pitchLocationPanel);
      bottom.Controls.Remove(pitchLocationPanel);
      bottom.Controls.Remove(chartPanel);
      topCards.Controls.Remove(chartPanel);

      rootLayout.BackgroundImage = battingPractice ? fullScreenFieldBackground : null;
      contentPanel.BackColor = battingPractice ? Color.Transparent : Color.FromArgb(7, 13, 21);
      navigationPanel.OverlayColor = Color.FromArgb(battingPractice ? 185 : 235, 12, 21, 32);
      if (recentPitchesPanel is GlassPanel recentGlass)
        recentGlass.OverlayColor = Color.FromArgb(battingPractice ? 185 : 235, 12, 21, 32);

      Control maxCard = metricsPanel.GetControlFromPosition(0, 0)!;
      Control averageCard = metricsPanel.GetControlFromPosition(1, 0)!;
      Control countCard = metricsPanel.GetControlFromPosition(2, 0)!;
      Control consistencyCard = metricsPanel.GetControlFromPosition(3, 0)!;
      liveCardPanel.OverlayColor = battingPractice ? Color.Transparent : Color.FromArgb(218, 15, 25, 37);
      deviceCardPanel.OverlayColor = battingPractice ? Color.Transparent : Color.FromArgb(218, 15, 25, 37);
      if (averageCard is GlassPanel averageGlass)
        averageGlass.OverlayColor = battingPractice ? Color.Transparent : Color.FromArgb(218, 19, 31, 45);
      if (countCard is GlassPanel countGlass)
        countGlass.OverlayColor = battingPractice ? Color.Transparent : Color.FromArgb(218, 19, 31, 45);
      if (consistencyCard is GlassPanel launchAngleGlass)
        launchAngleGlass.OverlayColor = battingPractice ? Color.Transparent : Color.FromArgb(218, 19, 31, 45);
      maxCard.Visible = !battingPractice;
      consistencyCard.Visible = true;
      metricsPanel.ColumnStyles[0].Width = battingPractice ? 0 : 25;
      metricsPanel.ColumnStyles[1].Width = battingPractice ? 33.3F : 25;
      metricsPanel.ColumnStyles[2].Width = battingPractice ? 33.3F : 25;
      metricsPanel.ColumnStyles[3].Width = battingPractice ? 33.4F : 25;
      SetCardTitle(averageCard, battingPractice ? "AVERAGE EXIT VELOCITY" : "AVERAGE SPEED");
      SetCardTitle(countCard, battingPractice ? "SWING COUNT" : "PITCH COUNT");
      SetCardSuffix(countCard, battingPractice ? "SWINGS" : "PITCHES");
      SetCardTitle(consistencyCard, battingPractice ? "LAUNCH ANGLE" : "CONSISTENCY");
      SetCardSuffix(consistencyCard, battingPractice ? "DEGREES" : "+/- 0.0 MPH");

      SetCardTitle(liveCardPanel, battingPractice ? "EXIT VELOCITY                         . LIVE" : "LIVE SPEED                              . LIVE");
      topCards.ColumnStyles[0].Width = battingPractice ? 50 : 34;
      topCards.ColumnStyles[1].Width = battingPractice ? 50 : 33;
      topCards.ColumnStyles[2].Width = battingPractice ? 0 : 33;

      if (battingPractice)
      {
        sessionHeaderPanel.Visible = false;
        headerPanel.ColumnStyles[0].Width = 0;
        headerPanel.ColumnStyles[1].Width = 100;
        contentPanel.RowStyles[2].Height = 0;
        contentPanel.RowStyles[3].Height = 61;
        chartPanel.Visible = false;
        mphLabel.TextAlign = ContentAlignment.MiddleLeft;
        mphLabel.Padding = new Padding(12, 0, 0, 0);
        liveSpeedCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        liveSpeedCaptionLabel.Padding = new Padding(12, 0, 0, 0);
        totalDistanceLabel.TextAlign = ContentAlignment.MiddleLeft;
        totalDistanceLabel.Padding = new Padding(12, 0, 0, 0);
        pitchLocationPanel.ShowFieldBackground = false;
        pitchLocationPanel.BackColor = Color.Transparent;
        pitchLocationPanel.Visible = true;
        bottom.Controls.Add(pitchLocationPanel, 0, 0);
        bottom.SetColumnSpan(pitchLocationPanel, 2);
        metricsPanel.Dock = DockStyle.Bottom;
        metricsPanel.Height = 148;
        metricsPanel.ColumnStyles[0].Width = 0;
        metricsPanel.ColumnStyles[1].Width = 33.3F;
        metricsPanel.ColumnStyles[2].Width = 33.3F;
        metricsPanel.ColumnStyles[3].Width = 33.4F;
        averageCard.Padding = new Padding(12, 16, 12, 16);
        averageSpeedLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        metricsPanel.Controls.Add(countCard, 2, 0);
        metricsPanel.Controls.Add(consistencyCard, 3, 0);
        pitchLocationPanel.Controls.Add(metricsPanel);
        metricsPanel.BringToFront();
      }
      else
      {
        sessionHeaderPanel.Visible = true;
        headerPanel.ColumnStyles[0].Width = 54;
        headerPanel.ColumnStyles[1].Width = 46;
        contentPanel.RowStyles[2].Height = 18;
        contentPanel.RowStyles[3].Height = 43;
        metricsPanel.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(metricsPanel, 0, 2);
        metricsPanel.ColumnStyles[0].Width = 25;
        metricsPanel.ColumnStyles[1].Width = 25;
        metricsPanel.ColumnStyles[2].Width = 25;
        metricsPanel.ColumnStyles[3].Width = 25;
        averageCard.Padding = new Padding(14);
        averageSpeedLabel.Font = new Font("Segoe UI", 27F, FontStyle.Bold);
        metricsPanel.Controls.Add(countCard, 2, 0);
        pitchLocationPanel.ShowFieldBackground = true;
        pitchLocationPanel.BackColor = Color.FromArgb(15, 25, 37);
        mphLabel.TextAlign = ContentAlignment.MiddleCenter;
        mphLabel.Padding = Padding.Empty;
        liveSpeedCaptionLabel.TextAlign = ContentAlignment.MiddleCenter;
        liveSpeedCaptionLabel.Padding = Padding.Empty;
        totalDistanceLabel.TextAlign = ContentAlignment.MiddleCenter;
        totalDistanceLabel.Padding = Padding.Empty;
        topCards.ColumnStyles[0].Width = 50;
        topCards.ColumnStyles[1].Width = 50;
        topCards.ColumnStyles[2].Width = 0;
        bottom.Controls.Add(pitchLocationPanel, 0, 0);
        bottom.SetColumnSpan(pitchLocationPanel, 2);
        pitchLocationPanel.Visible = true;
        chartPanel.Visible = true;
      }

      bottom.ResumeLayout(true);
      topCards.ResumeLayout(true);
      contentPanel.ResumeLayout(true);
      rootLayout.ResumeLayout(true);
      rootLayout.Invalidate(true);
      pitchLocationPanel.Invalidate();
    }

    private static void SetCardTitle(Control card, string title)
    {
      Label? label = card.Controls.OfType<Label>().FirstOrDefault(item => item.Dock == DockStyle.Top);
      if (label != null) label.Text = title;
    }

    private static void SetCardSuffix(Control card, string suffix)
    {
      Label? label = card.Controls.OfType<Label>().FirstOrDefault(item => item.Dock == DockStyle.Bottom);
      if (label != null) label.Text = suffix;
    }

    private string? PromptForText(string title, string prompt, string initialValue)
    {
      using var dialog = new Form
      {
        Text = title,
        ClientSize = new Size(360, 140),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MinimizeBox = false,
        MaximizeBox = false,
        ShowInTaskbar = false
      };
      var promptLabel = new Label { Dock = DockStyle.Top, Height = 38, Text = prompt, Padding = new Padding(10, 10, 10, 0) };
      var input = new TextBox { Dock = DockStyle.Top, Height = 28, Text = initialValue, Margin = new Padding(10) };
      var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
      var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
      var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
      buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
      dialog.Controls.Add(input); dialog.Controls.Add(promptLabel); dialog.Controls.Add(buttons);
      dialog.AcceptButton = ok; dialog.CancelButton = cancel;
      dialog.Shown += (_, _) => { input.Focus(); input.SelectAll(); };
      return dialog.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
    }

    private string? PromptForSessionType()
    {
      using var dialog = new Form
      {
        Text = "Session type",
        ClientSize = new Size(390, 145),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MinimizeBox = false,
        MaximizeBox = false,
        ShowInTaskbar = false
      };
      dialog.Controls.Add(new Label { Dock = DockStyle.Top, Height = 52, Text = "Select BP or Bullpen", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(12, 14, 12, 0) });
      var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
      var bullpen = new Button { Text = "Bullpen", Width = 105, Height = 34, DialogResult = DialogResult.Yes };
      var battingPractice = new Button { Text = "Batting Practice", Width = 125, Height = 34, DialogResult = DialogResult.OK };
      var cancel = new Button { Text = "Cancel", Width = 85, Height = 34, DialogResult = DialogResult.Cancel };
      buttons.Controls.Add(cancel); buttons.Controls.Add(bullpen); buttons.Controls.Add(battingPractice);
      dialog.Controls.Add(buttons);
      dialog.AcceptButton = battingPractice;
      dialog.CancelButton = cancel;
      return dialog.ShowDialog(this) switch
      {
        DialogResult.OK => "Batting Practice",
        DialogResult.Yes => "Bullpen",
        _ => null
      };
    }

    private bool SaveSession()
    {
      if (sessionReadings.Count == 0) return true;
      using var dialog = new SaveFileDialog
      {
        Title = "Save velocity session",
        Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
        DefaultExt = "txt",
        AddExtension = true,
        FileName = $"{MakeSafeFileName(sessionName)}-{sessionStartedAt:yyyy-MM-dd-HHmm}.txt"
      };
      if (dialog.ShowDialog(this) != DialogResult.OK) return false;

      try
      {
        File.WriteAllText(dialog.FileName, BuildSessionText(), Encoding.UTF8);
        sessionSaved = true;
        MessageBox.Show(this, $"Session saved to:\n{dialog.FileName}", "Session saved",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, $"The session could not be saved.\n\n{ex.Message}", "Save failed",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }
    }

    private string BuildSessionText()
    {
      var text = new StringBuilder();
      text.AppendLine($"Session: {sessionName}");
      text.AppendLine($"Started: {sessionStartedAt:yyyy-MM-dd h:mm:ss tt}");
      text.AppendLine($"Readings: {sessionReadings.Count}");
      text.AppendLine();
      text.AppendLine("Reading\tShot\tTime\tBall Speed (MPH)");
      for (int i = 0; i < sessionReadings.Count; i++)
      {
        VelocityReading reading = sessionReadings[i];
        text.AppendLine($"{i + 1}\t{reading.ShotId}\t{reading.Timestamp:h:mm:ss tt}\t{reading.BallSpeedMph:0.0}");
      }
      text.AppendLine();
      text.AppendLine($"Average: {sessionReadings.Average(r => r.BallSpeedMph):0.0} MPH");
      text.AppendLine($"Minimum: {sessionReadings.Min(r => r.BallSpeedMph):0.0} MPH");
      text.AppendLine($"Maximum: {sessionReadings.Max(r => r.BallSpeedMph):0.0} MPH");
      return text.ToString();
    }

    private static string MakeSafeFileName(string name)
    {
      char[] invalid = Path.GetInvalidFileNameChars();
      string safeName = new(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
      return string.IsNullOrWhiteSpace(safeName) ? "R10-Session" : safeName;
    }

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);
      this.Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
      if (!sessionSaved && sessionReadings.Count > 0 &&
          MessageBox.Show(this, "This session has not been saved. Close anyway?", "Unsaved session",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
      {
        e.Cancel = true;
        return;
      }
      base.OnFormClosing(e);
    }
  }
}
