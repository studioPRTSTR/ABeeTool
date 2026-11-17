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
    public string Svg;
    public string ConfigFilename;   // null = no layer setting
    public string Key;
    public string Fallback;
    public HelpLine[] Help;
}


static class Preferences
{
    const string PluginName = "ArchitecturalAtelierTools";

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

    const string SvgPreferences = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""256"" height=""256"" viewBox=""0 0 64 64"">
  <g fill=""none"" stroke=""#2f6fdc"" stroke-width=""3.5"" stroke-linejoin=""round"" stroke-linecap=""round"">
    <polygon points=""34.00,17.00 46.99,24.50 46.99,39.50 34.00,47.00 21.01,39.50 21.01,24.50""/>
    <line x1=""21.01"" y1=""24.50"" x2=""10"" y2=""20""/>
    <line x1=""21.01"" y1=""39.50"" x2=""10"" y2=""44""/>
  </g>
  <g fill=""#2f6fdc"">
    <circle cx=""10"" cy=""20"" r=""3.6""/>
    <circle cx=""10"" cy=""44"" r=""3.6""/>
    <circle cx=""40.5"" cy=""32"" r=""2.4""/>
  </g>
  <g fill=""#8a8a8a"">
    <circle cx=""53"" cy=""29"" r=""2.0""/>
    <circle cx=""57"" cy=""35"" r=""1.6""/>
    <circle cx=""60"" cy=""47"" r=""1.3""/>
  </g>
</svg>";
    const string SvgDimension = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""256"" height=""256"" viewBox=""0 0 64 64"">
  <g fill=""none"" stroke=""#8a8a8a"" stroke-width=""2"" stroke-linecap=""round"">
    <line x1=""8"" y1=""54"" x2=""8"" y2=""12""/>
    <line x1=""24"" y1=""54"" x2=""24"" y2=""34""/>
    <line x1=""40"" y1=""54"" x2=""40"" y2=""34""/>
    <line x1=""56"" y1=""54"" x2=""56"" y2=""12""/>
  </g>
  <g fill=""none"" stroke=""#2f6fdc"" stroke-width=""3"" stroke-linecap=""round"">
    <line x1=""4"" y1=""38"" x2=""60"" y2=""38""/>
    <line x1=""4"" y1=""18"" x2=""60"" y2=""18""/>
    <line x1=""4.5"" y1=""42.5"" x2=""11.5"" y2=""33.5""/>
    <line x1=""20.5"" y1=""42.5"" x2=""27.5"" y2=""33.5""/>
    <line x1=""36.5"" y1=""42.5"" x2=""43.5"" y2=""33.5""/>
    <line x1=""52.5"" y1=""42.5"" x2=""59.5"" y2=""33.5""/>
    <line x1=""4.5"" y1=""22.5"" x2=""11.5"" y2=""13.5""/>
    <line x1=""52.5"" y1=""22.5"" x2=""59.5"" y2=""13.5""/>
  </g>
  <g fill=""#2f6fdc"">
    <rect x=""12"" y=""27"" width=""8"" height=""4"" rx=""1""/>
    <rect x=""28"" y=""27"" width=""8"" height=""4"" rx=""1""/>
    <rect x=""44"" y=""27"" width=""8"" height=""4"" rx=""1""/>
    <rect x=""26"" y=""7"" width=""12"" height=""4"" rx=""1""/>
  </g>
</svg>";
    const string SvgBattInsulation = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""256"" height=""256"" viewBox=""0 0 64 64"">
  <line x1=""3"" y1=""10"" x2=""61"" y2=""10"" stroke=""#8a8a8a"" stroke-width=""2"" stroke-dasharray=""5 3""/>
  <line x1=""3"" y1=""54"" x2=""61"" y2=""54"" stroke=""#8a8a8a"" stroke-width=""2"" stroke-dasharray=""5 3""/>
  <path d=""M8.42,32 A11.79,11.79 0 1 1 20.21,32 A11.79,11.79 0 1 0 32,32 A11.79,11.79 0 1 1 43.79,32 A11.79,11.79 0 1 0 55.58,32 "" fill=""none"" stroke=""#2f6fdc"" stroke-width=""3.5"" stroke-linejoin=""round""/>
</svg>";

    // One entry per tool, in toolbar order. Tools with a config file get a layer box.
    static readonly ToolInfo[] Tools =
    {
        new ToolInfo
        {
            Name = "Preferences",
            Command = "A01_Pre_Preferences",
            Svg = SvgPreferences,
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
                HelpLine.T("Saved to %APPDATA%\\ArchitecturalAtelierTools\\. Further defaults (text height, font, decimals, band width ...) can be set in Dimension.json and BattInsulationLine.json there."),
            },
        },
        new ToolInfo
        {
            Name = "Dimension",
            Command = "A02_Dim_Dimension",
            Svg = SvgDimension,
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
            Svg = SvgBattInsulation,
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

    static Image Icon(string svg, int size)
    {
        try
        {
            using (System.Drawing.Bitmap bmp = Rhino.UI.DrawingUtilities.BitmapFromSvg(svg, size * 2, size * 2))
            using (var stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                return new Bitmap(stream);
            }
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
        Image icon = Icon(tool.Svg, 32);
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

            var icon = new ImageView { Image = Icon(tool.Svg, 40), Size = new Size(40, 40) };
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
            Title = "ArchitecturalAtelierTools Preferences",
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
                new ImageView { Image = Icon(SvgPreferences, 28), Size = new Size(28, 28) },
                Text("ArchitecturalAtelierTools", Fore, SystemFonts.Bold(14)),
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
