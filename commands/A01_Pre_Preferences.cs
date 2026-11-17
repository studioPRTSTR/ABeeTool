// #! csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Eto.Drawing;
using Eto.Forms;

using Rhino;


Preferences.Run(RhinoDoc.ActiveDoc);


enum HelpKind
{
    Heading,
    Text,
    Step,
    Button,
}


// One line of the how-to shown next to the tool list.
class HelpLine
{
    public HelpKind Kind;
    public string Text;
    public string Badge;       // Button: label of the on-screen button
    public Color BadgeColor;   // Button: its colour

    public static HelpLine H(string text) => new HelpLine { Kind = HelpKind.Heading, Text = text };
    public static HelpLine T(string text) => new HelpLine { Kind = HelpKind.Text, Text = text };
    public static HelpLine S(string number, string text) => new HelpLine { Kind = HelpKind.Step, Badge = number, Text = text };
    public static HelpLine B(string badge, Color color, string text) =>
        new HelpLine { Kind = HelpKind.Button, Badge = badge, BadgeColor = color, Text = text };
}


// A tool of the plugin: its command, icon, layer setting (if it draws on its own layer) and how-to.
class ToolInfo
{
    public string Name;
    public string Command;
    public string Png;   // base64
    public string ConfigFilename;   // null = no layer setting
    public string Key;
    public string Fallback;
    public HelpLine[] Help;
}


static class Preferences
{
    const string PluginName = "ABeeTool";

    // Dark window.
    static readonly Color Back = Color.FromArgb(14, 14, 14);
    static readonly Color PanelColor = Color.FromArgb(28, 28, 28);
    static readonly Color Selected = Color.FromArgb(40, 52, 75);
    static readonly Color Fore = Color.FromArgb(235, 235, 235);
    static readonly Color Dim = Color.FromArgb(140, 140, 140);
    static readonly Color Accent = Color.FromArgb(47, 111, 220);

    // Colours of the on-screen buttons of A02_Dim_Dimension.
    static readonly Color Teal = Color.FromArgb(0, 140, 160);
    static readonly Color Green = Color.FromArgb(40, 150, 80);
    static readonly Color Purple = Color.FromArgb(120, 80, 200);
    static readonly Color Red = Color.FromArgb(200, 60, 50);
    static readonly Color Grey = Color.FromArgb(110, 110, 110);
    static readonly Color Blue = Color.FromArgb(47, 111, 220);
    static readonly Color Orange = Color.FromArgb(230, 140, 30);
    static readonly Color Black = Color.FromArgb(70, 70, 70);

    // Icons as PNG (base64, 96 px), loaded with Eto only (the plugin build has no GDI+ bitmaps).
    const string PngPreferences = "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";
    const string PngDimension = "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";
    const string PngBattInsulation = "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";

    // One entry per tool, in toolbar order. Tools with a config file get a layer box.
    static readonly ToolInfo[] Tools =
    {
        new ToolInfo
        {
            Name = "Preferences",
            Command = "A01_Pre_Preferences",
            Png = PngPreferences,
            Help = new[]
            {
                HelpLine.T("Sets the layer each tool draws on."),
                HelpLine.S("1", "Click a tool on the left to see how it works."),
                HelpLine.S("2", "Pick a layer from this drawing or type a new name. A missing layer is created when the tool draws."),
                HelpLine.S("3", "Save."),
                HelpLine.H("Saved configs"),
                HelpLine.T("Keep several setups, e.g. one per office standard or project."),
                HelpLine.B("+", Accent, "Save as...: stores the current settings of all tools under a name."),
                HelpLine.B(">", Green, "Load: puts a saved config into the window. Click Save to use it."),
                HelpLine.B("x", Red, "Delete: removes the saved config."),
                HelpLine.H("Files"),
                HelpLine.T("Saved to %APPDATA%\\ABeeTool\\. Further defaults (text height, font, decimals, band width ...) can be set in Dimension.json and BattInsulationLine.json there."),
            },
        },
        new ToolInfo
        {
            Name = "Dimension",
            Command = "A02_Dim_Dimension",
            Png = PngDimension,
            ConfigFilename = "Dimension.json",
            Key = "layer_name",
            Fallback = "A_Bemassung_01",
            Help = new[]
            {
                HelpLine.T("Multi-row dimension chains: 80 below 1 m, 1.20 from 1 m."),
                HelpLine.H("Draw a chain"),
                HelpLine.S("1", "Run A02_Dim_Dimension, set Scale (1_100, 1_50, 1_25) and Rows."),
                HelpLine.S("2", "Pick the points, press Enter."),
                HelpLine.S("3", "Click where the first dimension line goes."),
                HelpLine.T("Every row gets all points; the last row is the overall dimension."),
                HelpLine.H("Edit a chain: select it, then use its buttons"),
                HelpLine.B("•", Teal, "Drag: move this point in this row only."),
                HelpLine.B("<>", Green, "Drag: move a measure point in every row."),
                HelpLine.B("↕", Purple, "Drag: move this dimension line."),
                HelpLine.B("+", Green, "At the row ends: add more points, Enter when done."),
                HelpLine.B("x", Red, "Remove a point (the two dimensions merge)."),
                HelpLine.B("-", Grey, "Delete a row."),
                HelpLine.B("+", Blue, "Add a row with all points."),
                HelpLine.B("T", Orange, "Text below a dimension, e.g. RPH 2.10."),
                HelpLine.B("...", Black, "Chain menu: scale, row spacing, texts below."),
                HelpLine.T("Hover a button to see what it does. Drag buttons also work with click + click."),
                HelpLine.H("Scale"),
                HelpLine.T("Styles Dimension_1_100, _1_50, _1_25 in Arial Narrow: text 0.2 / 0.1 / 0.05 m, rows 0.7 / 0.35 / 0.175 m apart. Changing the scale in the ... menu moves the rows to the new distance."),
                HelpLine.H("Tip: Rhino's grips work too"),
                HelpLine.T("Turn on the grips of a chain dimension (F10) and drag a point or the dimension line. The rest of the chain follows: a moved point moves in every row that has it, a moved dimension line takes its whole row along."),
                HelpLine.T("This only works once A02_Dim_Dimension has been started in the current Rhino session. To have it on as soon as Rhino opens, add this line under Tools > Options > General > 'Run these commands every time Rhino starts':"),
                HelpLine.B(">", Black, "A02_Dim_Dimension _Update"),
            },
        },
        new ToolInfo
        {
            Name = "BattInsulationLine",
            Command = "A03_BI_BattInsulation",
            Png = PngBattInsulation,
            ConfigFilename = "BattInsulationLine.json",
            Key = "layer_name",
            Fallback = "A_Isolation_01",
            Help = new[]
            {
                HelpLine.T("Draws the batt insulation line (loops) along a curve."),
                HelpLine.S("1", "Run A03_BI_BattInsulation."),
                HelpLine.S("2", "Pick a curve, or press Enter to draw a polyline."),
                HelpLine.S("3", "Type the band width and press Enter."),
                HelpLine.H("Options"),
                HelpLine.T("Spacing: distance of the loops, 0 = automatic."),
                HelpLine.T("Corner: Miter, ButtIncoming, ButtOutgoing."),
                HelpLine.T("CrossingLoops: Trim or Remove loops that cross at corners."),
                HelpLine.T("Rhythm: Continuous over corners, or Restart on every segment."),
            },
        },
    };


    static string UserConfigDir =>
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), PluginName);

    // Saved configs: one folder per name, holding a copy of every tool's config file.
    static string SavedConfigDir => Path.Combine(UserConfigDir, "configs");


    static List<string> SavedConfigNames()
    {
        try
        {
            if (Directory.Exists(SavedConfigDir))
                return Directory.GetDirectories(SavedConfigDir).Select(Path.GetFileName).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception)
        {
        }
        return new List<string>();
    }


    static string CleanName(string name)
    {
        string clean = new string((name ?? "").Trim().Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
        return clean.Trim('.', ' ');
    }


    // Same lookup order as the tools: user folder, next to the plugin, next to the script.
    static IEnumerable<string> ConfigCandidates(string configFilename, string scriptPath)
    {
        yield return Path.Combine(UserConfigDir, configFilename);

        string assemblyDir = null;
        try { assemblyDir = Path.GetDirectoryName(typeof(Preferences).Assembly.Location); } catch (Exception) { }
        if (!string.IsNullOrEmpty(assemblyDir))
            yield return Path.Combine(assemblyDir, configFilename);

        string scriptDir = Path.GetDirectoryName(scriptPath);
        if (!string.IsNullOrEmpty(scriptDir))
            yield return Path.Combine(scriptDir, configFilename);
    }


    static string ReadConfig(string configFilename, [CallerFilePath] string scriptPath = "")
    {
        try
        {
            string configPath = ConfigCandidates(configFilename, scriptPath).FirstOrDefault(File.Exists);
            if (configPath != null)
                return File.ReadAllText(configPath);
        }
        catch (Exception)
        {
        }
        return "{\n}\n";
    }


    static string ReadString(string json, string key, string fallback)
    {
        var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        return m.Success ? Regex.Unescape(m.Groups[1].Value) : fallback;
    }


    static string WriteString(string json, string key, string value)
    {
        string quoted = "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        var existing = new Regex("(\"" + Regex.Escape(key) + "\"\\s*:\\s*)\"(?:[^\"\\\\]|\\\\.)*\"");
        if (existing.IsMatch(json))
            return existing.Replace(json, m => m.Groups[1].Value + quoted, 1);

        int brace = json.IndexOf('{');
        if (brace < 0)
            return "{\n    \"" + key + "\": " + quoted + "\n}\n";
        bool isEmpty = json.Substring(brace + 1).TrimStart().StartsWith("}");
        return json.Insert(brace + 1, "\n    \"" + key + "\": " + quoted + (isEmpty ? "\n" : ","));
    }


    // ------------------------------------------------------------------ dialog

    static Image Icon(string png, int size)
    {
        try
        {
            return new Bitmap(Convert.FromBase64String(png));
        }
        catch (Exception)
        {
            return null;
        }
    }


    static Label Text(string text, Color color, Font font = null, int width = -1) => new Label
    {
        Text = text,
        TextColor = color,
        Font = font ?? SystemFonts.Default(),
        Wrap = WrapMode.Word,
        Width = width,
        VerticalAlignment = VerticalAlignment.Center,
    };


    static Control Badge(string text, Color color) => new Panel
    {
        BackgroundColor = color,
        Padding = new Padding(4, 1),
        Width = 30,
        Content = new Label
        {
            Text = text,
            TextColor = Colors.White,
            Font = SystemFonts.Bold(),
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };


    const int HelpWidth = 380;

    static Control HelpContent(ToolInfo tool)
    {
        var stack = new StackLayout { Spacing = 6, Padding = new Padding(16, 14), HorizontalContentAlignment = HorizontalAlignment.Stretch };

        var title = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 10, VerticalContentAlignment = VerticalAlignment.Center };
        Image icon = Icon(tool.Png, 32);
        if (icon != null)
            title.Items.Add(new ImageView { Image = icon, Size = new Size(32, 32) });
        title.Items.Add(new StackLayout
        {
            Spacing = 0,
            Items =
            {
                Text(tool.Name, Fore, SystemFonts.Bold(12)),
                Text(tool.Command, Dim),
            },
        });
        stack.Items.Add(title);

        foreach (HelpLine line in tool.Help)
        {
            switch (line.Kind)
            {
                case HelpKind.Heading:
                    stack.Items.Add(new Panel { Height = 4 });
                    stack.Items.Add(Text(line.Text.ToUpperInvariant(), Accent, SystemFonts.Bold(), HelpWidth));
                    break;
                case HelpKind.Text:
                    stack.Items.Add(Text(line.Text, Fore, null, HelpWidth));
                    break;
                case HelpKind.Step:
                case HelpKind.Button:
                    Control badge = line.Kind == HelpKind.Step ? Badge(line.Badge, Color.FromArgb(55, 55, 55)) : Badge(line.Badge, line.BadgeColor);
                    stack.Items.Add(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items = { badge, Text(line.Text, Fore, null, HelpWidth - 40) },
                    });
                    break;
            }
        }
        return stack;
    }


    // configs: the config text of each tool (null = no config); Load replaces them.
    static bool ShowDialog(RhinoDoc doc, string[] configs, string[] current, out string[] chosen)
    {
        List<string> layerNames = doc.Layers
            .Where(layer => !layer.IsDeleted)
            .OrderBy(layer => layer.SortIndex)
            .Select(layer => layer.FullPath)
            .ToList();

        var help = new Scrollable
        {
            BackgroundColor = PanelColor,
            Border = BorderType.None,
            Width = HelpWidth + 50,
            Height = 520,
            ExpandContentWidth = true,
        };

        var rows = new List<Panel>();
        var boxes = new ComboBox[Tools.Length];
        var list = new StackLayout { Spacing = 4, HorizontalContentAlignment = HorizontalAlignment.Stretch };

        void Show(int index)
        {
            for (int k = 0; k < rows.Count; k++)
                rows[k].BackgroundColor = k == index ? Selected : PanelColor;
            help.Content = HelpContent(Tools[index]);
        }

        for (int i = 0; i < Tools.Length; i++)
        {
            int index = i;
            ToolInfo tool = Tools[i];

            Control layerControl;
            if (tool.ConfigFilename != null)
            {
                var box = new ComboBox
                {
                    DataStore = layerNames,
                    Text = current[i],
                    AutoComplete = true,
                    Width = 230,
                };
                box.GotFocus += (sender, e) => Show(index);
                boxes[i] = box;
                layerControl = new StackLayout { Spacing = 2, Items = { Text("Layer", Dim), box } };
            }
            else
            {
                layerControl = Text("this window", Dim);
            }

            var icon = new ImageView { Image = Icon(tool.Png, 40), Size = new Size(40, 40) };
            var name = new StackLayout
            {
                Spacing = 0,
                Width = 170,
                Items = { Text(tool.Name, Fore, SystemFonts.Bold(10)), Text(tool.Command, Dim) },
            };
            var row = new Panel
            {
                BackgroundColor = PanelColor,
                Padding = new Padding(10, 8),
                Content = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Items = { icon, name, layerControl },
                },
            };
            EventHandler<MouseEventArgs> click = (sender, e) => Show(index);
            row.MouseDown += click;
            icon.MouseDown += click;
            foreach (Control c in name.Items.Select(item => item.Control))
                c.MouseDown += click;
            name.MouseDown += click;

            rows.Add(row);
            list.Items.Add(row);
        }

        var dialog = new Dialog<bool>
        {
            Title = "A Bee Tool - Preferences",
            Padding = 14,
            Resizable = true,
            BackgroundColor = Back,
        };
        var save = new Button { Text = "Save", Width = 90 };
        var cancel = new Button { Text = "Cancel", Width = 90 };
        save.Click += (sender, e) => dialog.Close(true);
        cancel.Click += (sender, e) => dialog.Close(false);
        dialog.DefaultButton = save;
        dialog.AbortButton = cancel;

        var header = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalContentAlignment = VerticalAlignment.Center,
            Items =
            {
                new ImageView { Image = Icon(PngPreferences, 28), Size = new Size(28, 28) },
                Text("A Bee Tool", Fore, SystemFonts.Bold(14)),
            },
        };

        // ---- saved configs
        var savedBox = new DropDown { Width = 200 };
        var loadButton = new Button { Text = "Load", Width = 70 };
        var saveAsButton = new Button { Text = "Save as...", Width = 80 };
        var deleteButton = new Button { Text = "Delete", Width = 70 };
        Label savedStatus = Text("", Dim, null, 450);

        void FillSaved(string select)
        {
            List<string> names = SavedConfigNames();
            savedBox.Items.Clear();
            foreach (string n in names)
                savedBox.Items.Add(n);
            int at = select == null ? -1 : names.FindIndex(n => string.Equals(n, select, StringComparison.OrdinalIgnoreCase));
            savedBox.SelectedIndex = at >= 0 ? at : (names.Count > 0 ? 0 : -1);
            loadButton.Enabled = deleteButton.Enabled = names.Count > 0;
        }

        // The current state: the configs with the layers from the boxes.
        string[] CurrentConfigs() => Tools.Select((t, i) =>
        {
            if (configs[i] == null)
                return null;
            string layer = (boxes[i]?.Text ?? "").Trim();
            return WriteString(configs[i], t.Key, layer.Length > 0 ? layer : t.Fallback);
        }).ToArray();

        saveAsButton.Click += (sender, e) =>
        {
            string suggestion = savedBox.SelectedIndex >= 0 ? savedBox.Items[savedBox.SelectedIndex].Text : "";
            if (!Rhino.UI.Dialogs.ShowEditBox("Save config", "Name of the config (e.g. Office, Competition 1_200):", suggestion, false, out string typed))
                return;
            string name = CleanName(typed);
            if (name.Length == 0)
                return;
            string folder = Path.Combine(SavedConfigDir, name);
            if (Directory.Exists(folder) && MessageBox.Show(dialog, "Replace the saved config '" + name + "'?", "Save config",
                    MessageBoxButtons.YesNo, MessageBoxType.Question) != DialogResult.Yes)
                return;
            try
            {
                Directory.CreateDirectory(folder);
                string[] state = CurrentConfigs();
                for (int i = 0; i < Tools.Length; i++)
                    if (state[i] != null)
                        File.WriteAllText(Path.Combine(folder, Tools[i].ConfigFilename), state[i]);
                FillSaved(name);
                savedStatus.Text = "Saved the current settings as '" + name + "'.";
            }
            catch (Exception ex)
            {
                savedStatus.Text = "Could not save: " + ex.Message;
            }
        };

        loadButton.Click += (sender, e) =>
        {
            if (savedBox.SelectedIndex < 0)
                return;
            string name = savedBox.Items[savedBox.SelectedIndex].Text;
            string folder = Path.Combine(SavedConfigDir, name);
            for (int i = 0; i < Tools.Length; i++)
            {
                if (configs[i] == null)
                    continue;
                string file = Path.Combine(folder, Tools[i].ConfigFilename);
                if (!File.Exists(file))
                    continue;
                configs[i] = File.ReadAllText(file);
                boxes[i].Text = ReadString(configs[i], Tools[i].Key, Tools[i].Fallback);
            }
            savedStatus.Text = "Loaded '" + name + "'. Click Save to use it.";
        };

        deleteButton.Click += (sender, e) =>
        {
            if (savedBox.SelectedIndex < 0)
                return;
            string name = savedBox.Items[savedBox.SelectedIndex].Text;
            if (MessageBox.Show(dialog, "Delete the saved config '" + name + "'?", "Delete config",
                    MessageBoxButtons.YesNo, MessageBoxType.Warning) != DialogResult.Yes)
                return;
            try
            {
                Directory.Delete(Path.Combine(SavedConfigDir, name), true);
                FillSaved(null);
                savedStatus.Text = "Deleted '" + name + "'.";
            }
            catch (Exception ex)
            {
                savedStatus.Text = "Could not delete: " + ex.Message;
            }
        };

        FillSaved(null);

        var saved = new Panel
        {
            BackgroundColor = PanelColor,
            Padding = new Padding(10, 8),
            Content = new StackLayout
            {
                Spacing = 6,
                Items =
                {
                    Text("SAVED CONFIGS", Accent, SystemFonts.Bold()),
                    Text("Layers and all other settings of every tool, under a name.", Dim, null, 450),
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Items = { savedBox, loadButton, saveAsButton, deleteButton },
                    },
                    savedStatus,
                },
            },
        };

        var left = new StackLayout
        {
            Spacing = 10,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Items =
            {
                Text("Tools - click one to see how it works", Dim),
                list,
                saved,
                new StackLayoutItem(null, true),
                Text("Saved to " + UserConfigDir, Dim, null, 470),
            },
        };

        dialog.Content = new StackLayout
        {
            Spacing = 12,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Items =
            {
                header,
                new StackLayoutItem(new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 14,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Items = { left, new StackLayoutItem(help, true) },
                }, true),
                new TableLayout(new TableRow(new TableCell(null, true), save, cancel)) { Spacing = new Size(6, 0) },
            },
        };

        // Dimension first: the tool with the most to explain.
        Show(1);

        bool ok = dialog.ShowModal(Rhino.UI.RhinoEtoApp.MainWindow);
        chosen = boxes.Select(box => (box?.Text ?? "").Trim()).ToArray();
        return ok;
    }


    public static void Run(RhinoDoc doc)
    {
        string[] configs = Tools.Select(t => t.ConfigFilename == null ? null : ReadConfig(t.ConfigFilename)).ToArray();
        string[] current = Tools.Select((t, i) => configs[i] == null ? "" : ReadString(configs[i], t.Key, t.Fallback)).ToArray();

        if (!ShowDialog(doc, configs, current, out string[] chosen))
        {
            RhinoApp.WriteLine("Cancelled.");
            return;
        }

        try
        {
            Directory.CreateDirectory(UserConfigDir);
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolInfo tool = Tools[i];
                if (tool.ConfigFilename == null)
                    continue;
                string layerName = chosen[i].Length > 0 ? chosen[i] : tool.Fallback;
                File.WriteAllText(Path.Combine(UserConfigDir, tool.ConfigFilename), WriteString(configs[i], tool.Key, layerName));
                RhinoApp.WriteLine("Preferences: {0} draws on layer '{1}'.", tool.Name, layerName);
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine("Preferences: could not save settings - {0}", ex.Message);
        }
    }
}
