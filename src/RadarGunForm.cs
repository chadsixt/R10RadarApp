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
    private readonly Button saveSessionButton;
    private readonly Label sessionStatusLabel;
    private readonly Button consoleButton;
    private readonly Timer resetTimer;
    private readonly List<VelocityReading> sessionReadings = new();
    private bool sessionActive;
    private bool sessionSaved = true;
    private string sessionName = string.Empty;
    private string connectionStatus = "STARTING";
    private DateTime? sessionStartedAt;

    private sealed record VelocityReading(DateTime Timestamp, int ShotId, double BallSpeedMph);

    public RadarGunForm()
    {
      this.Text = "R10 Radar App - Version 1.3";
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.StartPosition = FormStartPosition.Manual;
      this.Location = new Point(100, 100);
      this.TopMost = false;
      this.ShowInTaskbar = true;
      this.BackColor = Color.FromArgb(8, 12, 18);
      this.ForeColor = Color.White;
      this.Width = 760;
      this.Height = 580;
      this.MinimumSize = new Size(600, 400);
      this.MaximizeBox = true;
      this.MinimizeBox = true;
      this.ControlBox = true;

      var panel = new Panel
      {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(8, 12, 18),
        BorderStyle = BorderStyle.None,
        Margin = new Padding(10),
        Padding = new Padding(0, 38, 0, 0)
      };

      var appTitleLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.None,
        Location = new Point(0, 0),
        Height = 38,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        ForeColor = Color.White,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "R10 Radar App   Version 1.3",
        BackColor = Color.FromArgb(8, 12, 18)
      };

      panel.Controls.Add(mphLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 72, FontStyle.Bold),
        ForeColor = Color.LimeGreen,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "0.0",
        BackColor = Color.FromArgb(8, 12, 18)
      });

      shotLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Bottom,
        Height = 56,
        Font = new Font("Segoe UI", 20, FontStyle.Bold),
        ForeColor = Color.Red,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = connectionStatus,
        BackColor = Color.FromArgb(8, 12, 18),
        Padding = new Padding(0, 0, 0, 16)
      };

      statusLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Bottom,
        Height = 26,
        Font = new Font("Segoe UI", 9, FontStyle.Regular),
        ForeColor = Color.Gray,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "BALL SPEED",
        BackColor = Color.FromArgb(8, 12, 18)
      };

      consoleButton = new Button
      {
        Text = "Show Console",
        Dock = DockStyle.Bottom,
        Height = 46,
        FlatStyle = FlatStyle.Standard,
        BackColor = Color.FromArgb(18, 24, 32),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11, FontStyle.Bold)
      };
      consoleButton.Click += (_, _) =>
      {
        bool consoleVisible = Program.ToggleConsoleWindow();
        consoleButton.Text = consoleVisible ? "Hide Console" : "Show Console";
      };

      var sessionPanel = new FlowLayoutPanel
      {
        Dock = DockStyle.Bottom,
        Height = 58,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        Padding = new Padding(3),
        BackColor = Color.FromArgb(8, 12, 18)
      };
      sessionNameTextBox = new TextBox { Width = 190, PlaceholderText = "Session name", Font = new Font("Segoe UI", 12), Margin = new Padding(3, 9, 3, 3) };
      startSessionButton = CreateButton("Start Session", 125);
      stopSessionButton = CreateButton("Stop", 85);
      saveSessionButton = CreateButton("Save Text File", 135);
      stopSessionButton.Enabled = false;
      saveSessionButton.Enabled = false;
      startSessionButton.Click += (_, _) => StartSession();
      stopSessionButton.Click += (_, _) => StopSession();
      saveSessionButton.Click += (_, _) => SaveSession();
      sessionPanel.Controls.AddRange(new Control[] { sessionNameTextBox, startSessionButton, stopSessionButton, saveSessionButton });

      sessionStatusLabel = new Label
      {
        Dock = DockStyle.Bottom,
        Height = 25,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.Gray,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "No session started",
        Padding = new Padding(5, 0, 0, 0)
      };

      velocityHistory = new ListBox
      {
        Dock = DockStyle.Right,
        Width = 215,
        Font = new Font("Consolas", 10),
        ForeColor = Color.LimeGreen,
        BackColor = Color.FromArgb(18, 24, 32),
        BorderStyle = BorderStyle.FixedSingle,
        HorizontalScrollbar = true
      };
      velocityHistory.Items.Add("VELOCITY LOG");

      panel.Controls.Add(appTitleLabel);
      panel.Controls.Add(velocityHistory);
      panel.Controls.Add(shotLabel);
      panel.Controls.Add(statusLabel);
      panel.Controls.Add(consoleButton);
      panel.Controls.Add(sessionStatusLabel);
      panel.Controls.Add(sessionPanel);
      panel.Controls.Add(mphLabel);

      void SizeAppTitle()
      {
        appTitleLabel.SetBounds(0, 0, panel.ClientSize.Width, 38);
        appTitleLabel.BringToFront();
      }
      panel.SizeChanged += (_, _) => SizeAppTitle();
      SizeAppTitle();

      this.Controls.Add(panel);

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
        statusLabel.Text = "BALL SPEED";
      };
    }

    private static Button CreateButton(string text, int width) => new Button
    {
      Text = text,
      Width = width,
      Height = 46,
      BackColor = Color.FromArgb(28, 36, 46),
      ForeColor = Color.White,
      Font = new Font("Segoe UI", 11, FontStyle.Bold)
    };

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
      statusLabel.Text = "BALL SPEED";
      this.Text = $"R10 Radar App - Version 1.3 - {ballSpeedMph:0.0} MPH";

      var reading = new VelocityReading(DateTime.Now, shotId, ballSpeedMph);
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
      string requestedName = sessionNameTextBox.Text.Trim();
      if (string.IsNullOrWhiteSpace(requestedName))
      {
        MessageBox.Show(this, "Enter a name before starting the session.", "Session name required",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
        sessionNameTextBox.Focus();
        return;
      }

      if (!sessionSaved && sessionReadings.Count > 0 &&
          MessageBox.Show(this, "Starting a new session will clear the unsaved session. Continue?",
            "Start new session", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

      sessionName = requestedName;
      sessionStartedAt = DateTime.Now;
      sessionReadings.Clear();
      sessionActive = true;
      sessionSaved = true;
      sessionNameTextBox.Enabled = false;
      startSessionButton.Enabled = false;
      stopSessionButton.Enabled = true;
      saveSessionButton.Enabled = false;
      UpdateSessionStatus();
    }

    private void StopSession()
    {
      sessionActive = false;
      sessionNameTextBox.Enabled = true;
      startSessionButton.Enabled = true;
      stopSessionButton.Enabled = false;
      saveSessionButton.Enabled = sessionReadings.Count > 0;
      UpdateSessionStatus();
    }

    private void UpdateSessionStatus()
    {
      string state = sessionActive ? "RECORDING" : "STOPPED";
      sessionStatusLabel.Text = $"{state}: {sessionName} - {sessionReadings.Count} reading(s)";
      sessionStatusLabel.ForeColor = sessionActive ? Color.LimeGreen : Color.Gold;
    }

    private void SaveSession()
    {
      if (sessionReadings.Count == 0) return;
      using var dialog = new SaveFileDialog
      {
        Title = "Save velocity session",
        Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
        DefaultExt = "txt",
        AddExtension = true,
        FileName = $"{MakeSafeFileName(sessionName)}-{sessionStartedAt:yyyy-MM-dd-HHmm}.txt"
      };
      if (dialog.ShowDialog(this) != DialogResult.OK) return;

      try
      {
        File.WriteAllText(dialog.FileName, BuildSessionText(), Encoding.UTF8);
        sessionSaved = true;
        MessageBox.Show(this, $"Session saved to:\n{dialog.FileName}", "Session saved",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, $"The session could not be saved.\n\n{ex.Message}", "Save failed",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
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
