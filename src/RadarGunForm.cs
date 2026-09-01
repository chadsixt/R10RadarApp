using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
    private readonly Label sessionStatusLabel;
    private readonly Button consoleButton;
    private readonly Label pitchCountLabel;
    private readonly Label averageSpeedLabel;
    private readonly Label maxSpeedLabel;
    private readonly Label consistencyLabel;
    private readonly Label liveSpeedCaptionLabel;
    private readonly HeatmapPanel pitchLocationPanel;
    private readonly Label pitchLocationTitleLabel;
    private readonly Timer resetTimer;
    private readonly List<VelocityReading> sessionReadings = new();
    private readonly List<VelocityReading> dashboardReadings = new();
    private bool sessionActive;
    private bool sessionSaved = true;
    private string sessionName = string.Empty;
    private string sessionType = string.Empty;
    private string connectionStatus = "STARTING";
    private DateTime? sessionStartedAt;

    private sealed record VelocityReading(DateTime Timestamp, int ShotId, double BallSpeedMph);

    public RadarGunForm()
    {
      Text = "R10 Radar App - Version 1.4";
      StartPosition = FormStartPosition.CenterScreen;
      BackColor = Color.FromArgb(7, 13, 21);
      ForeColor = Color.FromArgb(224, 231, 240);
      ClientSize = new Size(1280, 780);
      MinimumSize = new Size(980, 650);
      Font = new Font("Segoe UI", 10F);

      Color surface = Color.FromArgb(15, 25, 37);
      Color surfaceRaised = Color.FromArgb(19, 31, 45);
      Color blue = Color.FromArgb(0, 151, 255);
      Color muted = Color.FromArgb(151, 164, 180);

      var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = BackColor };
      root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
      root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

      var navigation = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 21, 32) };
      var brand = new Label { Location = new Point(0, 8), Size = new Size(190, 78), Text = "R10\nRADAR APP", ForeColor = Color.White, Font = new Font("Segoe UI", 16F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
      navigation.Controls.Add(brand);
      var navItems = new FlowLayoutPanel { Location = new Point(17, 94), Size = new Size(170, 270), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };
      navItems.Controls.Add(CreateNavItem("[ ]   Dashboard", true));
      navItems.Controls.Add(CreateNavItem("[o]   Session", false));
      navItems.Controls.Add(CreateNavItem("[o]   History", false));
      navItems.Controls.Add(CreateNavItem("[o]   Players", false));
      navItems.Controls.Add(CreateNavItem("[o]   Settings", false));
      navigation.Controls.Add(navItems);
      navigation.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 55, Text = ".  App v1.4.0\n    Up to date", ForeColor = Color.FromArgb(91, 213, 126), Font = new Font("Segoe UI", 9F), Padding = new Padding(0, 10, 0, 0) });
      root.Controls.Add(navigation, 0, 0);

      var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = BackColor, Padding = new Padding(24, 20, 24, 20) };
      content.RowStyles.Add(new RowStyle(SizeType.Percent, 9)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 18)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 43)); root.Controls.Add(content, 1, 0);

      var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
      header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
      var sessionHeader = new Panel { Dock = DockStyle.Fill };
      sessionHeader.Controls.Add(new Label { Dock = DockStyle.Top, Height = 22, Text = "SESSION NAME", ForeColor = muted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) });
      sessionNameTextBox = new TextBox { Dock = DockStyle.Bottom, Height = 32, Text = "Bullpen - May 20, 2025", BackColor = surfaceRaised, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 13F) }; sessionHeader.Controls.Add(sessionNameTextBox); header.Controls.Add(sessionHeader, 0, 0);
      var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
      startSessionButton = CreateButton("[>]  Start Session", 136, Color.FromArgb(17, 177, 71)); stopSessionButton = CreateButton("[ ]  Stop", 104, surfaceRaised); stopSessionButton.Enabled = false;
      actions.Controls.Add(stopSessionButton); actions.Controls.Add(startSessionButton); header.Controls.Add(actions, 1, 0); content.Controls.Add(header, 0, 0);
      startSessionButton.Click += (_, _) => StartSession(); stopSessionButton.Click += (_, _) => StopSession();

      var topCards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
      topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34)); topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); topCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
      var liveCard = CreateCard(surface); liveCard.Controls.Add(new Label { Dock = DockStyle.Top, Height = 24, Text = "LIVE SPEED                              . LIVE", ForeColor = Color.FromArgb(255, 166, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
      mphLabel = new Label { Dock = DockStyle.Fill, Text = "0.0", ForeColor = Color.FromArgb(255, 159, 10), Font = new Font("Segoe UI", 68F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; liveCard.Controls.Add(mphLabel);
      liveSpeedCaptionLabel = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "MPH", ForeColor = muted, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }; liveCard.Controls.Add(liveSpeedCaptionLabel); topCards.Controls.Add(liveCard, 0, 0);
      var deviceCard = CreateCard(surface); deviceCard.Controls.Add(new Label { Dock = DockStyle.Top, Height = 28, Text = "DEVICE STATUS", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
      shotLabel = new Label { Dock = DockStyle.Top, Height = 42, Text = ".  R10 Connected", ForeColor = Color.FromArgb(43, 226, 112), Font = new Font("Segoe UI", 15F, FontStyle.Bold), Padding = new Padding(0, 8, 0, 0) }; deviceCard.Controls.Add(shotLabel);
      statusLabel = new Label { Dock = DockStyle.Top, Height = 36, Text = "Bluetooth   Connected\nBattery        87%", ForeColor = muted, Font = new Font("Segoe UI", 10F), Padding = new Padding(8, 2, 0, 0) }; deviceCard.Controls.Add(statusLabel); topCards.Controls.Add(deviceCard, 1, 0);
      pitchLocationPanel = new HeatmapPanel { Dock = DockStyle.Fill, BackColor = surface }; pitchLocationTitleLabel = new Label { Dock = DockStyle.Top, Height = 28, Text = "PITCH LOCATION (HEATMAP)", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(10, 8, 0, 0) }; pitchLocationPanel.Controls.Add(pitchLocationTitleLabel); topCards.Controls.Add(pitchLocationPanel, 2, 0); content.Controls.Add(topCards, 0, 1);

      var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.Transparent }; for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      maxSpeedLabel = AddMetric(metrics, 0, "MAX SPEED", "0.0", "MPH", Color.White, surfaceRaised); averageSpeedLabel = AddMetric(metrics, 1, "AVERAGE SPEED", "0.0", "MPH", Color.White, surfaceRaised); pitchCountLabel = AddMetric(metrics, 2, "PITCH COUNT", "0", "PITCHES", Color.White, surfaceRaised); consistencyLabel = AddMetric(metrics, 3, "CONSISTENCY", "--", "+/- 0.0 MPH", Color.FromArgb(42, 222, 111), surfaceRaised); content.Controls.Add(metrics, 0, 2);

      var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent }; bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
      var chart = new VelocityChartPanel { Dock = DockStyle.Fill, BackColor = surface }; chart.Controls.Add(new Label { Dock = DockStyle.Top, Height = 34, Text = "VELOCITY OVER TIME", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(14, 12, 0, 0) }); bottom.Controls.Add(chart, 0, 0);
      var recent = CreateCard(surface); recent.Controls.Add(new Label { Dock = DockStyle.Top, Height = 34, Text = "RECENT PITCHES", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(14, 12, 0, 0) }); velocityHistory = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10F), ForeColor = Color.FromArgb(228, 235, 243), BackColor = surface, BorderStyle = BorderStyle.None, HorizontalScrollbar = true }; velocityHistory.Items.Add("PITCH #     SPEED (MPH)     TIME"); recent.Controls.Add(velocityHistory); bottom.Controls.Add(recent, 1, 0);
      sessionStatusLabel = new Label { Dock = DockStyle.Fill, Text = "No session started", ForeColor = muted, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(2, 8, 0, 0) }; bottom.Controls.Add(sessionStatusLabel, 0, 1);
      consoleButton = CreateButton("Show Console", 120, surfaceRaised); consoleButton.Dock = DockStyle.Right; consoleButton.Height = 28; consoleButton.Click += (_, _) => ToggleConsole(); bottom.Controls.Add(consoleButton, 1, 1); content.Controls.Add(bottom, 0, 3);
      Controls.Add(root);
      Resize += (_, _) => UpdateResponsiveLayout();
      pitchLocationPanel.Resize += (_, _) => pitchLocationPanel.Invalidate();
      UpdateResponsiveLayout();

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

    private static Panel CreateCard(Color backColor) => new Panel { Dock = DockStyle.Fill, BackColor = backColor, Padding = new Padding(14), Margin = new Padding(5) };

    private static Label CreateNavItem(string text, bool active) => new Label { Width = 156, Height = 48, Text = text, ForeColor = active ? Color.White : Color.FromArgb(169, 181, 195), BackColor = active ? Color.FromArgb(24, 46, 69) : Color.Transparent, Font = new Font("Segoe UI", 10F, active ? FontStyle.Bold : FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Margin = new Padding(0, 2, 0, 2) };

    private static Label AddMetric(TableLayoutPanel parent, int column, string title, string value, string suffix, Color valueColor, Color backColor)
    {
      var card = CreateCard(backColor); card.Controls.Add(new Label { Dock = DockStyle.Top, Height = 24, Text = title, ForeColor = Color.FromArgb(165, 177, 191), Font = new Font("Segoe UI", 8F, FontStyle.Bold) });
      var valueLabel = new Label { Dock = DockStyle.Fill, Text = value, ForeColor = valueColor, Font = new Font("Segoe UI", 27F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }; card.Controls.Add(valueLabel); card.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 18, Text = suffix, ForeColor = Color.FromArgb(165, 177, 191), Font = new Font("Segoe UI", 8F) }); parent.Controls.Add(card, column, 0); return valueLabel;
    }

    private void ToggleConsole()
    {
      bool consoleVisible = Program.ToggleConsoleWindow();
      consoleButton.Text = consoleVisible ? "Hide Console" : "Show Console";
    }

    private void UpdateResponsiveLayout()
    {
      int availableHeight = Math.Max(540, ClientSize.Height);
      float scale = Math.Clamp(availableHeight / 780F, 0.82F, 1.35F);
      mphLabel.Font = new Font("Segoe UI", 68F * scale, FontStyle.Bold);
      liveSpeedCaptionLabel.Font = new Font("Segoe UI", 14F * scale, FontStyle.Bold);
      maxSpeedLabel.Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold);
      averageSpeedLabel.Font = new Font("Segoe UI", 27F * scale, FontStyle.Bold);
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

      protected override void OnPaint(PaintEventArgs e)
      {
        base.OnPaint(e);
        if (DisplayMode == PanelMode.HitterDiamond)
        {
          PaintHitterDiamond(e);
          return;
        }

        int size = Math.Max(70, Math.Min(Width - 38, Height - 48));
        int left = (Width - size) / 2, top = 47;
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
      }

      private void PaintHitterDiamond(PaintEventArgs e)
      {
        int fieldTop = 28;
        int centerX = Width / 2;
        int fieldBottom = Height;
        using var skyBrush = new System.Drawing.Drawing2D.LinearGradientBrush(new Point(0, fieldTop), new Point(0, Height / 2), Color.FromArgb(42, 126, 211), Color.FromArgb(151, 206, 240));
        using var grassBrush = new SolidBrush(Color.FromArgb(42, 111, 65));
        using var dirtBrush = new SolidBrush(Color.FromArgb(170, 108, 61));
        using var warningTrackBrush = new SolidBrush(Color.FromArgb(113, 91, 59));
        using var fieldLinePen = new Pen(Color.FromArgb(241, 239, 218), 2F);
        using var plateBrush = new SolidBrush(Color.FromArgb(245, 241, 226));
        e.Graphics.FillRectangle(skyBrush, 0, fieldTop, Width, Height / 2);
        e.Graphics.FillRectangle(grassBrush, 0, Height / 3, Width, Height - Height / 3);
        e.Graphics.FillRectangle(warningTrackBrush, 0, Height / 3 - 5, Width, 16);

        int horizon = Math.Max(fieldTop + 20, Height / 3);
        int dirtTop = Math.Max(horizon + 20, Height / 2 - 8);
        Point[] infield = { new(centerX, fieldBottom + 35), new(Width - 10, fieldBottom + 35), new(Width - 10, dirtTop), new(centerX, dirtTop - 12), new(10, dirtTop), new(10, fieldBottom + 35) };
        e.Graphics.FillPolygon(dirtBrush, infield);

        int plateY = Height - 28;
        int boxWidth = Math.Max(22, Width / 9);
        int boxHeight = Math.Max(30, Height / 5);
        e.Graphics.DrawLine(fieldLinePen, centerX, plateY, 8, dirtTop);
        e.Graphics.DrawLine(fieldLinePen, centerX, plateY, Width - 8, dirtTop);
        e.Graphics.DrawRectangle(fieldLinePen, centerX - boxWidth - 16, plateY - boxHeight, boxWidth, boxHeight);
        e.Graphics.DrawRectangle(fieldLinePen, centerX + 16, plateY - boxHeight, boxWidth, boxHeight);
        e.Graphics.FillPolygon(plateBrush, new[] { new Point(centerX - 12, plateY - 8), new Point(centerX + 12, plateY - 8), new Point(centerX + 12, plateY), new Point(centerX, plateY + 8), new Point(centerX - 12, plateY) });

        using var treeBrush = new SolidBrush(Color.FromArgb(25, 78, 57));
        for (int treeX = 18; treeX < Width; treeX += 34) e.Graphics.FillEllipse(treeBrush, treeX, horizon - 12 - (treeX % 3) * 5, 42, 28);
      }
    }

    public void UpdateShot(double ballSpeedMph, int shotId)
    {
      if (this.IsDisposed) return;

      if (this.InvokeRequired)
      {
        this.BeginInvoke(new Action(() => UpdateShot(ballSpeedMph, shotId)));
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
      this.Text = $"R10 Radar App - Version 1.4 - {ballSpeedMph:0.0} MPH";

      velocityHistory.Items.Insert(1, $"#{shotId,-4} {reading.Timestamp:h:mm:ss tt}  {ballSpeedMph,6:0.0} MPH");

      if (sessionActive)
      {
        sessionReadings.Add(reading);
        sessionSaved = false;
        UpdateSessionStatus();
      }

      resetTimer.Stop();
      resetTimer.Start();
    }

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
      pitchLocationTitleLabel.Text = requestedType == "Batting Practice" ? "BATTING PRACTICE" : "PITCH LOCATION (HEATMAP)";
      pitchLocationPanel.Invalidate();
      sessionStartedAt = DateTime.Now;
      sessionReadings.Clear();
      sessionActive = true;
      sessionSaved = true;
      dashboardReadings.Clear();
      velocityHistory.Items.Clear();
      velocityHistory.Items.Add("PITCH #     SPEED (MPH)     TIME");
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
      pitchLocationTitleLabel.Text = "PITCH LOCATION (HEATMAP)";
      pitchLocationPanel.Invalidate();
      dashboardReadings.Clear();
      velocityHistory.Items.Clear();
      velocityHistory.Items.Add("PITCH #     SPEED (MPH)     TIME");
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
      shotLabel.Text = connectionStatus;
      shotLabel.ForeColor = GetConnectionStatusColor(connectionStatus);
      statusLabel.Text = "Bluetooth   Connected\nBattery        87%";
      Text = "R10 Radar App - Version 1.4";
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
