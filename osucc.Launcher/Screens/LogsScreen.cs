using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace osucc.Launcher.Screens
{
    public partial class LogsScreen : Screen
    {
        private OsuDropdown<string> tagDropdown = null!;
        private TextFlowContainer logsFlow = null!;
        private OsuScrollContainer scrollContainer = null!;
        private OsuTextBox searchTextBox = null!;

        private string logsDirectory = "";
        private string activeTag = "all";
        private Dictionary<string, FileInfo> currentLogFiles = new();
        private Dictionary<string, long> filePositions = new();
        private string currentSearchTerm = "";

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            string osuDir = osucc.Launcher.Core.OsuCcPaths.ResolveOsuDirectory(null) ?? "";
            logsDirectory = Path.Combine(osucc.Common.OsuCcDataRootResolver.Resolve(osuDir), "logs");

            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Distributed)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 60,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background4,
                                        Alpha = 0.5f
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(10),
                                        ColumnDimensions = new[]
                                        {
                                            new Dimension(GridSizeMode.Absolute, 150),
                                            new Dimension(GridSizeMode.Absolute, 10),
                                            new Dimension(GridSizeMode.Absolute, 100),
                                            new Dimension(GridSizeMode.Absolute, 10),
                                            new Dimension(GridSizeMode.Distributed)
                                        },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                tagDropdown = new OsuDropdown<string>
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Items = new[] { "all" }
                                                },
                                                new Container(),
                                                new ShearedButton
                                                {
                                                    Width = 100,
                                                    Height = 40,
                                                    Text = "Clear",
                                                    DarkerColour = colours.RedDark,
                                                    LighterColour = colours.Red,
                                                    Action = clearLogs
                                                },
                                                new Container(),
                                                searchTextBox = new OsuTextBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    PlaceholderText = "Search logs..."
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Drawable[]
                        {
                            scrollContainer = new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(10),
                                Child = logsFlow = new TextFlowContainer(t => t.Font = OsuFont.GetFont(size: 14, weight: FontWeight.Regular))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y
                                }
                            }
                        }
                    }
                }
            };

            searchTextBox.Current.ValueChanged += e =>
            {
                currentSearchTerm = e.NewValue;
                reloadLogs();
            };

            tagDropdown.Current.ValueChanged += e =>
            {
                if (!string.IsNullOrEmpty(e.NewValue))
                    selectTag(e.NewValue);
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            refreshTags();
            tagDropdown.Current.Value = activeTag;
            selectTag(activeTag);
            Scheduler.AddDelayed(pollLogs, 1000, true);
        }

        private void clearLogs()
        {
            if (Directory.Exists(logsDirectory))
            {
                foreach (var f in new DirectoryInfo(logsDirectory).GetFiles("*.osu-cc.log"))
                {
                    try { f.Delete(); } catch { }
                }
            }
            logsFlow.Clear();
            currentLogFiles.Clear();
            filePositions.Clear();
            refreshTags();
            tagDropdown.Current.Value = "all";
            selectTag("all");
        }

        private string getTagName(FileInfo f)
        {
            var parts = f.Name.Split('.');
            return parts.Length >= 4 ? parts[1] : "main";
        }

        private void refreshTags()
        {
            if (!Directory.Exists(logsDirectory)) return;

            var files = new DirectoryInfo(logsDirectory).GetFiles("*.osu-cc.log");
            var tags = files.Select(getTagName).Distinct().OrderBy(t => t).ToList();

            if (!tags.Contains("main")) tags.Insert(0, "main");
            if (!tags.Contains("all")) tags.Insert(0, "all");

            tagDropdown.Items = tags;
        }

        private void selectTag(string tag)
        {
            if (activeTag == tag && currentLogFiles.Count > 0) return;
            activeTag = tag;

            currentLogFiles.Clear();
            filePositions.Clear();

            if (Directory.Exists(logsDirectory))
            {
                var files = new DirectoryInfo(logsDirectory).GetFiles("*.osu-cc.log");

                if (tag == "all")
                {
                    var allTags = files.Select(getTagName).Distinct();
                    foreach (var t in allTags)
                    {
                        var latest = files.Where(f => getTagName(f) == t).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                        if (latest != null)
                        {
                            currentLogFiles[t] = latest;
                            filePositions[t] = 0;
                        }
                    }
                }
                else
                {
                    var latest = files.Where(f => getTagName(f) == tag).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                    if (latest != null)
                    {
                        currentLogFiles[tag] = latest;
                        filePositions[tag] = 0;
                    }
                }
            }

            reloadLogs();
        }

        private void reloadLogs()
        {
            logsFlow.Clear();
            foreach (var key in filePositions.Keys.ToList())
            {
                filePositions[key] = 0;
            }
            pollLogs();
        }

        private void pollLogs()
        {
            bool scrolled = false;

            // Check for new files just in case
            if (Directory.Exists(logsDirectory))
            {
                var files = new DirectoryInfo(logsDirectory).GetFiles("*.osu-cc.log");
                if (activeTag == "all")
                {
                    var allTags = files.Select(getTagName).Distinct();
                    foreach (var t in allTags)
                    {
                        var latest = files.Where(f => getTagName(f) == t).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                        if (latest != null)
                        {
                            if (!currentLogFiles.ContainsKey(t) || currentLogFiles[t].FullName != latest.FullName)
                            {
                                currentLogFiles[t] = latest;
                                filePositions[t] = 0;
                            }
                        }
                    }
                }
                else
                {
                    var latest = files.Where(f => getTagName(f) == activeTag).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                    if (latest != null)
                    {
                        if (!currentLogFiles.ContainsKey(activeTag) || currentLogFiles[activeTag].FullName != latest.FullName)
                        {
                            currentLogFiles[activeTag] = latest;
                            filePositions[activeTag] = 0;
                        }
                    }
                }
            }

            foreach (var kvp in currentLogFiles)
            {
                string tag = kvp.Key;
                FileInfo file = kvp.Value;

                file.Refresh();
                if (!file.Exists) continue;

                long lastPos = filePositions.ContainsKey(tag) ? filePositions[tag] : 0;

                if (file.Length > lastPos)
                {
                    using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.Seek(lastPos, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(currentSearchTerm) || line.Contains(currentSearchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            appendLogLine(line, activeTag == "all" ? tag : null);
                        }
                    }

                    filePositions[tag] = fs.Position;
                    scrolled = true;
                }
            }

            if (scrolled)
            {
                scrollContainer.ScrollToEnd();
            }
        }

        private void appendLogLine(string line, string? tagPrefix)
        {
            logsFlow.AddParagraph("");

            string prefix = tagPrefix != null ? $"[{tagPrefix}] " : "";

            int firstClose = line.IndexOf(']');
            if (firstClose != -1)
            {
                int secondOpen = line.IndexOf('[', firstClose);
                if (secondOpen != -1)
                {
                    int secondClose = line.IndexOf(']', secondOpen + 1);
                    if (secondClose != -1)
                    {
                        string timeAndCategory = line.Substring(0, secondOpen);
                        string level = line.Substring(secondOpen, secondClose - secondOpen + 1);
                        string message = line.Substring(secondClose + 1);

                        logsFlow.AddText(prefix + timeAndCategory, t => t.Colour = colourProvider.Content2);

                        Color4 levelColor = colourProvider.Content1;
                        if (level.Contains("verbose", StringComparison.OrdinalIgnoreCase) || level.Contains("trace", StringComparison.OrdinalIgnoreCase)) levelColor = colourProvider.Content2;
                        else if (level.Contains("debug", StringComparison.OrdinalIgnoreCase)) levelColor = colourProvider.Colour1;
                        else if (level.Contains("info", StringComparison.OrdinalIgnoreCase)) levelColor = colourProvider.Colour2;
                        else if (level.Contains("warn", StringComparison.OrdinalIgnoreCase)) levelColor = colours.Yellow;
                        else if (level.Contains("error", StringComparison.OrdinalIgnoreCase) || level.Contains("fail", StringComparison.OrdinalIgnoreCase) || level.Contains("exception", StringComparison.OrdinalIgnoreCase)) levelColor = colours.Red;
                        else if (level.Contains("fatal", StringComparison.OrdinalIgnoreCase)) levelColor = colours.RedDark;

                        logsFlow.AddText(level, t => t.Colour = levelColor);
                        logsFlow.AddText(message, t => t.Colour = colourProvider.Content1);
                        return;
                    }
                }
            }

            logsFlow.AddText(prefix + line, t => t.Colour = colourProvider.Content1);
        }
    }
}
