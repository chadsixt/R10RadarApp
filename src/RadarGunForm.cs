using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace gspro_r10
{
  public class RadarGunForm : Form
  {
    private readonly Label mphLabel;
    private readonly Label shotLabel;
    private readonly Label statusLabel;
    private readonly Label lastReadingsLabel;
    private readonly Button consoleButton;
    private readonly Timer resetTimer;
    private readonly Queue<double> lastThreeReadings = new Queue<double>(3);

    public RadarGunForm()
    {
      this.Text = "GSP-R10 Radar";
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.StartPosition = FormStartPosition.Manual;
      this.Location = new Point(100, 100);
      this.TopMost = false;
      this.ShowInTaskbar = true;
      this.BackColor = Color.FromArgb(8, 12, 18);
      this.ForeColor = Color.White;
      this.Width = 500;
      this.Height = 300;
      this.MinimumSize = new Size(260, 180);
      this.MaximizeBox = true;
      this.MinimizeBox = true;
      this.ControlBox = true;

      var panel = new Panel
      {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(8, 12, 18),
        BorderStyle = BorderStyle.None,
        Margin = new Padding(10)
      };

      panel.Controls.Add(mphLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 38, FontStyle.Bold),
        ForeColor = Color.LimeGreen,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "0.0",
        BackColor = Color.FromArgb(8, 12, 18)
      });

      shotLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Bottom,
        Height = 32,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.White,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "WAITING",
        BackColor = Color.FromArgb(8, 12, 18)
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
        Height = 28,
        FlatStyle = FlatStyle.Standard,
        BackColor = Color.FromArgb(18, 24, 32),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };
      consoleButton.Click += (_, _) => Program.ShowConsoleWindow();

      lastReadingsLabel = new Label
      {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 70,
        Font = new Font("Segoe UI", 8, FontStyle.Regular),
        ForeColor = Color.LimeGreen,
        TextAlign = ContentAlignment.TopRight,
        Text = "LAST 3\nREADINGS\n",
        BackColor = Color.FromArgb(8, 12, 18),
        Padding = new Padding(5)
      };

      panel.Controls.Add(lastReadingsLabel);
      panel.Controls.Add(shotLabel);
      panel.Controls.Add(statusLabel);
      panel.Controls.Add(consoleButton);
      panel.Controls.Add(mphLabel);

      this.Controls.Add(panel);

      resetTimer = new Timer
      {
        Interval = 2200
      };
      resetTimer.Tick += (_, _) =>
      {
        mphLabel.Text = "0.0";
        shotLabel.Text = "WAITING";
        statusLabel.Text = "BALL SPEED";
      };
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
      statusLabel.Text = "BALL SPEED";
      this.Text = $"GSP-R10 Radar: {ballSpeedMph:0.0} MPH";

      // Add to last three readings
      if (lastThreeReadings.Count >= 3)
      {
        lastThreeReadings.Dequeue();
      }
      lastThreeReadings.Enqueue(ballSpeedMph);

      // Update the display
      UpdateLastReadingsDisplay();

      resetTimer.Stop();
      resetTimer.Start();
    }

    private void UpdateLastReadingsDisplay()
    {
      var readings = lastThreeReadings.ToArray();
      string readingsText = "LAST 3\nREADINGS\n";
      for (int i = readings.Length - 1; i >= 0; i--)
      {
        readingsText += $"{readings[i]:0.0}\n";
      }
      lastReadingsLabel.Text = readingsText;
    }

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);
      this.Activate();
    }
  }
}
