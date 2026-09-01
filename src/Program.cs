using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace gspro_r10
{
  class Program
  {
    public static RadarGunForm? RadarWindow { get; private set; }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern void GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private static bool consoleVisible;

    public static bool ToggleConsoleWindow()
    {
      try
      {
        IntPtr consoleHandle = GetConsoleWindow();

        if (consoleHandle == IntPtr.Zero)
        {
          FreeConsole();
          AllocConsole();
          Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
          Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
          consoleHandle = GetConsoleWindow();
        }

        if (consoleHandle != IntPtr.Zero)
        {
          if (consoleVisible)
          {
            ShowWindow(consoleHandle, SW_HIDE);
            consoleVisible = false;
            return false;
          }

          ShowWindow(consoleHandle, SW_RESTORE);
          ShowWindow(consoleHandle, SW_SHOW);
          SetForegroundWindow(consoleHandle);
          consoleVisible = true;
          return true;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Console visibility error: {ex.Message}");
      }

      return false;
    }

    private static void EnsureConsoleHidden()
    {
      try
      {
        IntPtr consoleHandle = GetConsoleWindow();
        
        // If running in GUI mode without a console, create one
        if (consoleHandle == IntPtr.Zero)
        {
          FreeConsole();
          AllocConsole();
          Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
          Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
          consoleHandle = GetConsoleWindow();
        }

        if (consoleHandle != IntPtr.Zero)
        {
          ShowWindow(consoleHandle, SW_HIDE);
          consoleVisible = false;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Console visibility error: {ex.Message}");
      }
    }

    [STAThread]
    public static void Main()
    {
      EnsureConsoleHidden();
      ApplicationConfiguration.Initialize();

      IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory());

      if (File.Exists(Path.Join(Directory.GetCurrentDirectory(), "settings.json")))
      {
        builder.AddJsonFile("settings.json");
      }
      else
      {
        BaseLogger.LogMessage($"settings.json file not found or could not be opened in {Directory.GetCurrentDirectory()}", "Main", LogMessageType.Error);
      }

      IConfigurationRoot configuration = builder.Build();

      Console.Title = "GSP-R10 Connect";
      BaseLogger.LogMessage("GSP - R10 Bridge starting. Press enter key to close", "Main");

      try
      {
        BaseLogger.LogMessage("Creating radar window...", "Main");
        RadarWindow = new RadarGunForm();
        RadarWindow.WindowState = FormWindowState.Normal;
        RadarWindow.Visible = true;
        RadarWindow.TopMost = false;
        RadarWindow.Show();
        
        // Explicitly show the window using P/Invoke
        IntPtr radarHandle = RadarWindow.Handle;
        if (radarHandle != IntPtr.Zero)
        {
          ShowWindow(radarHandle, SW_SHOW);
          BaseLogger.LogMessage("Radar window handle shown explicitly", "Main");
        }
        
        BaseLogger.LogMessage("Radar window created and displayed", "Main");
      }
      catch (Exception ex)
      {
        BaseLogger.LogMessage($"Error creating radar window: {ex.Message}\n{ex.StackTrace}", "Main", LogMessageType.Error);
        throw;
      }

      ConnectionManager manager = new ConnectionManager(configuration);

      Application.Run(RadarWindow);

      BaseLogger.LogMessage("Shutting down...", "Main");
      manager.Dispose();
      BaseLogger.LogMessage("Exiting...", "Main");
    }
  }
}
