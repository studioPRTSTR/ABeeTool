// #! csharp
using System;
using System.IO;
using System.Linq;

using Rhino;
using Rhino.PlugIns;
using Rhino.UI;


ReleaseTheBees.Run();


// Sets ABeeTool up after the .yak was dropped into Rhino: loads the plugin, opens and shows its
// toolbar and starts the dimension watcher. Runs once per installed version.
static class ReleaseTheBees
{
    const string PluginName = "ABeeTool";

    // Starts the watcher that keeps dimension chains together when edited with Rhino's grips.
    const string WatcherCommand = "_A02_Dim_Dimension _Update";

    static string UserDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PluginName);

    static string MarkerPath => Path.Combine(UserDir, "released.txt");


    static string PluginPath()
    {
        try
        {
            Guid id = PlugIn.IdFromName(PluginName);
            if (id != Guid.Empty)
            {
                string path = PlugIn.PathFromId(id);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }
        }
        catch (Exception) { }

        try
        {
            string location = typeof(ReleaseTheBees).Assembly.Location;
            if (!string.IsNullOrEmpty(location) && location.EndsWith(".rhp", StringComparison.OrdinalIgnoreCase))
                return location;
        }
        catch (Exception) { }

        return null;
    }


    // Opens the plugin's .rui (if Rhino has not yet) and makes its toolbars visible.
    // Returns the names of the toolbar groups shown.
    static string[] ShowToolbar(string pluginPath)
    {
        string dir = pluginPath == null ? null : Path.GetDirectoryName(pluginPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return new string[0];

        var shown = new System.Collections.Generic.List<string>();
        foreach (string rui in Directory.GetFiles(dir, "*.rui"))
        {
            ToolbarFile file =
                RhinoApp.ToolbarFiles.FindByPath(rui) ??
                RhinoApp.ToolbarFiles.FindByName(Path.GetFileNameWithoutExtension(rui), true) ??
                RhinoApp.ToolbarFiles.Open(rui);
            if (file == null)
                continue;

            for (int i = 0; i < file.GroupCount; i++)
            {
                ToolbarGroup group = file.GetGroup(i);
                if (group == null)
                    continue;
                group.Visible = true;
                shown.Add(group.Name);
            }
        }
        return shown.ToArray();
    }


    // Runs the watcher command once this command has finished.
    static void StartWatcherWhenIdle()
    {
        EventHandler handler = null;
        handler = (sender, e) =>
        {
            RhinoApp.Idle -= handler;
            RhinoApp.RunScript(WatcherCommand, false);
        };
        RhinoApp.Idle += handler;
    }


    public static void Run()
    {
        string pluginPath = PluginPath();
        string stamp = pluginPath ?? "script";

        try
        {
            if (File.Exists(MarkerPath) && File.ReadAllText(MarkerPath).Trim() == stamp)
            {
                RhinoApp.WriteLine("ReleaseTheBees: the bees are already out - nothing to do. " +
                                   "It only needs to run once after installing (or updating) ABeeTool.");
                return;
            }
        }
        catch (Exception) { }

        string[] toolbars = ShowToolbar(pluginPath);
        StartWatcherWhenIdle();

        try
        {
            Directory.CreateDirectory(UserDir);
            File.WriteAllText(MarkerPath, stamp);
        }
        catch (Exception) { }

        RhinoApp.WriteLine("ReleaseTheBees: the bees are out.");
        if (toolbars.Length > 0)
            RhinoApp.WriteLine("  Toolbar shown: {0}", string.Join(", ", toolbars.Select(t => "'" + t + "'")));
        else
            RhinoApp.WriteLine("  No toolbar found - restart Rhino to get it.");
        RhinoApp.WriteLine("  Dimension chains now follow Rhino's grips in this session.");
        RhinoApp.WriteLine("  To have that on every start, add '{0}' under Tools > Options > General > " +
                           "'Run these commands every time Rhino starts'.", WatcherCommand.TrimStart('_'));
    }
}
