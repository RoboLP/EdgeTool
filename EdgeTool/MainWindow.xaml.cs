﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Xml.XPath;
using Mygod.Edge.Tool.LibTwoTribes;
using Mygod.Edge.Tool.LibTwoTribes.Util;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using Mygod.Net;
using Mygod.Windows;
using Mygod.Xml.Linq;
using MouseButtons = System.Windows.Forms.MouseButtons;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;

#pragma warning disable 665

namespace Mygod.Edge.Tool
{
    public sealed partial class MainWindow
    {
        #region Main

        public MainWindow()
        {
            notifyIcon = new NotifyIcon { Icon = CurrentApp.DrawingIcon, Text = "EdgeTool" };
            notifyIcon.MouseClick += OnHideWindow;
            notifyIcon.BalloonTipClicked += OnBalloonClosed;
            notifyIcon.BalloonTipClosed += OnBalloonClosed;
            InitializeComponent();
            DecompileHistoryBox.ItemsSource = decompileHistory;
            checkBoxes = CheckBoxPanel.Children.OfType<CheckBox>().ToDictionary(box => box.Content as string);
            LevelList.ItemsSource = levels;
            GamePath.ItemsSource = Settings.Instance.RecentPaths;
            GamePath.Text = Settings.Instance.CurrentPath;
            foreach (var name in ModelNames) ModelNameBox.Items.Add(name);
            foreach (var name in AnimationNames) AnimationNameBox.Items.Add(name);
            if (!string.IsNullOrWhiteSpace(App.GamePath)) GamePath.Text = App.GamePath;
            Load(null, null);
            foreach (var edgemod in App.EdgeMods) InstallEdgeMod(edgemod);
            if (!Directory.Exists(Users.Root)) return;
            var watcher = new FileSystemWatcher(Users.Root) { IncludeSubdirectories = true };
            watcher.Created += RefreshAchievements;
            watcher.Changed += RefreshAchievements;
            watcher.Deleted += RefreshAchievements;
            watcher.Renamed += RefreshAchievements;
            watcher.EnableRaisingEvents = true;
            GC.KeepAlive(watcher);
            RefreshAchievements();
        }

        private static readonly string[]
            ModelNames =
            {
                "bumper_bottom", "bumper_right", "bumper_roof", "cam_entry", "cam_entry_target", "cube_finish_shadow",
                "cube_idle", "cube_idle_shadow", "cubeanimation_d_front", "cubeanimation_e_middle",
                "cubeanimation_full_d", "cubeanimation_full_e", "cubeanimation_full_g", "cubeanimation_full_last_e",
                "cubeanimation_g_hook", "cubeanimation_last_e_bottom", "cubeanimation_shadow", "falling_platform",
                "finish", "holoswitch", "menu_background", "menu_background_shadow", "menu_background_skybox",
                "platform", "platform_active", "platform_active_small", "platform_edges_active",
                "platform_edges_active_small", "platform_small", "prism", "prism_finish", "prism_shadow",
                "shrinker_tobig", "shrinker_tomini", "skybox_1", "skybox_2", "skybox_3", "skybox_4", "switch",
                "switch_done", "switch_ghost", "switch_ghost_done"
            },
            AnimationNames =
            {
                "bumper_bottom", "bumper_right", "bumper_roof", "cam_entry_target__loop", "cam_entry__loop",
                "cubeanimation_d_front", "cubeanimation_d_front_shadow", "cubeanimation_e_middle",
                "cubeanimation_e_middle_shadow", "cubeanimation_full_d", "cubeanimation_full_d_shadow",
                "cubeanimation_full_e", "cubeanimation_full_e_shadow", "cubeanimation_full_g",
                "cubeanimation_full_g_shadow", "cubeanimation_full_last_e", "cubeanimation_full_last_e_shadow",
                "cubeanimation_g_hook", "cubeanimation_g_hook_shadow", "cubeanimation_last_e_bottom",
                "cubeanimation_last_e_bottom_shadow", "cube_climbdown", "cube_climbdown_shadow", "cube_climbleft",
                "cube_climbleft_shadow", "cube_climbright", "cube_climbright_shadow", "cube_climbup",
                "cube_climbup_shadow", "cube_finish", "cube_finish_shadow", "cube_idle_shadow", "cube_movedown",
                "cube_movedown_shadow", "cube_moveleft", "cube_moveleft_shadow", "cube_moveright",
                "cube_moveright_shadow", "cube_moveup", "cube_moveup_shadow", "menu_background",
                "menu_background_shadow", "prism", "prism_finish", "prism_shadow", "shrinker_tobig", "shrinker_tomini"
            },
            MobileStandardLevels =
            {
                "level309", "level300", "level310", "level36", "level201", "level37", "level311", "level38", "level59",
                "level301", "level40", "level43", "level206", "level312", "level44", "level42", "level315", "level45",
                "level46", "level60", "level307", "level48", "level202", "level305", "level41", "level303", "level49",
                "level302", "level204", "level304", "level52", "level308", "level50", "level306", "level51",
                "level313", "level47", "level314", "level53", "level54", "level317", "level319", "level318",
                "level222", "level221", "level220", "level400", "level401"
            },
            MobileBonusLevels =
            {
                "hangout_815", "hammer_810", "compost_805", "babylonian_817", "swirl_801", "density_806", "magic_811",
                "cubism_808", "mystic_813", "indiana_804", "chunk_816", "goliath_812", "fourohfour_818", "gears_802",
                "fireworks_800", "zias_850", "winners_820"
            },
            MobileStandardLevelSounds =
            {
                "1st_contact", "training", "playground", "pushing_stars", "bump", "city_rythm", "speedrun",
                "milky_way", "8bit", "metro", "mini_me", "vertex", "equalizer", "peripherique", "time_machine",
                "mind_the_gap", "edge_code", "edge_time", "chase", "landing", "chess", "switch_keep", "mecanic",
                "higher", "squadron", "metronome", "orion", "try_again", "hypnozone", "beat", "star_castle", "sticker",
                "sync_the_wall", "snap", "braintonic", "2nd_contact", "jungle_fever", "speedrun_2", "edge_master",
                "cube_invaders", "starfield", "bonus", "extra_cube", "sliced", "earthquake", "vertigo", "push_me",
                "perfect_cell"
            },
            MobileBonusLevelSounds =
            {
                "hangout", "hammer", "compost", "babylonian", "swirl", "density", "magic", "cubism", "mystic",
                "indiana", "chunk", "goliath", "404", "gears", "fireworks", "zias", "winners"
            };

        private static void ShowInExplorer(string path)
        {
            Process.Start("explorer", "/select,\"" + path + '"');
        }

        private void OnHideWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (ShowInTaskbar)
            {
                ShowInTaskbar = false;
                Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowInTaskbar = true;
                Visibility = Visibility.Visible;
            }
        }

        private void OnBalloonClosed(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref balloonShown, 0);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            KeyEventRecorder.Shutdown(true);
        }

        private readonly NotifyIcon notifyIcon;
        private int balloonShown;
        public static Edge Edge;
        private readonly ObservableCollection<Level> levels = new ObservableCollection<Level>();
        private Thread searcher;

        private readonly CommonOpenFileDialog
            exeSelector = new CommonOpenFileDialog
            {
                Title = Localization.GameSelectorTitle,
                DefaultFileName = "edge.exe",
                Filters = { new CommonFileDialogFilter(Localization.ExecutableFilter, "*.exe") }
            },
            outputSelector = new CommonOpenFileDialog
            {
                Title = Localization.OutputPathSelectorTitle,
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false
            },
            projectSelector = new CommonOpenFileDialog
            {
                Title = Localization.ProjectPathSelectorTitle,
                Filters = { new CommonFileDialogFilter(Localization.ProjectFilter, "*.xml") }
            };

        private static readonly CommonOpenFileDialog LevelSelector = new CommonOpenFileDialog
        {
            Title = Localization.LevelFileSelectorTitle,
            Multiselect = true,
            AddToMostRecentlyUsedList = false,
            Controls = { new CommonFileDialogCheckBox(Localization.ConvertToPcBox) },
            Filters = { new CommonFileDialogFilter(Localization.EdgeLevelFilter, "*.bin") }
        };

        private void Browse(object sender, RoutedEventArgs e)
        {
            if (exeSelector.ShowDialog() == CommonFileDialogResult.Ok) GamePath.Text = exeSelector.FileName;
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GamePath.Text) || !File.Exists(GamePath.Text))
            {
                if (sender != null) MessageBox.Show(this, Localization.LoadingFailed + Environment.NewLine + Localization.InvalidPath,
                                                    Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                Edge = new Edge(GamePath.Text);
                Edge.DisabledEdgeModsChanged += (a, b) => isDirty = true;
                EdgeModGrid.ItemsSource = Edge.EdgeMods;
                searcher?.Abort();
                try
                {
                    MappingLevels.Current = new MappingLevels(Edge.LevelsDirectory);
                }
                catch
                {
                    Trace.WriteLine("Failed to load mappings.xml!");
                }
                levels.Clear();
                searcher = new Thread(Load);
                searcher.Start();
                User current = null;
                if (Edge.SteamOtl != null)
                    current = Users.Current.FirstOrDefault(user => user.SteamID == Edge.SteamOtl.SettingsUserName);
                else if (Users.Current.CurrentUser == null) current = Users.Current.FirstOrDefault();
                if (current != null) Users.Current.CurrentUser = current;
                AchievementsList.Items.Refresh();
                RunGameButton.IsEnabled = true;
                SwitchProfileButton.IsEnabled = Edge.SteamOtl != null;
                Settings.Instance.SetCurrentPath(GamePath.Text);
                Settings.Save();
                GamePath.ItemsSource = Settings.Instance.RecentPaths;
            }
            catch (Exception exc)
            {
                if (sender != null) MessageBox.Show(this, Localization.LoadingFailed + Environment.NewLine + exc.Message,
                                                    Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                SwitchProfileButton.IsEnabled = RunGameButton.IsEnabled = false;
            }
        }

        private void RefreshAchievementsList(object sender, RoutedEventArgs e)
        {
            AchievementsList.Items.Refresh();
        }

        private void Load()
        {
            foreach (var file in Directory.EnumerateFiles(Edge.LevelsDirectory, "*.bin", SearchOption.AllDirectories))
                try
                {
                    var level = Level.CreateFromCompiled(file);
                    Dispatcher.Invoke(() => levels.Add(level));
                }
                catch (Exception exc)
                {
                    Trace.WriteLine($"{Path.GetFileNameWithoutExtension(file)} error: {exc.Message}");
                }
        }

        private void RunGame(object sender = null, EventArgs e = null)
        {
            if (Edge == null || (isDirty && MessageBox.Show(this, Localization.ProceedConfirm + Environment.NewLine + Localization.EdgeModsChangesNotAppliedMessage,
                                                            Localization.Ask, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)) return;
            Process.Start(new ProcessStartInfo(Edge.GamePath) { WorkingDirectory = Edge.GameDirectory });
            isDirty = false;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? e.AllowedEffects & DragDropEffects.Copy : DragDropEffects.None;
            DropTargetHelper.DragOver(e.GetPosition(this), e.Effects);
        }
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
            DropTargetHelper.DragLeave(e.Data);
        }

        private void RecordKeyEvent(object sender, RoutedEventArgs e)
        {
            KeyEventRecorder.Instance.Show();
            KeyEventRecorder.Instance.Activate();
        }

        private void ConvertMobiLevel(object sender, RoutedEventArgs e)
        {
            ConvertMobiLevel(this);
        }
        public static void ConvertMobiLevel(Window window = null)
        {
            if (LevelSelector.ShowDialog(window) != CommonFileDialogResult.Ok) return;
            var pc = ((CommonFileDialogCheckBox)LevelSelector.Controls[0]).IsChecked;
            var count = 0;
            foreach (var file in LevelSelector.FileNames)
                try
                {
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        var reader = new BinaryReader(stream);
                        var writer = new BinaryWriter(stream);
                        stream.Position = 4;
                        stream.Position = reader.ReadUInt32() + 8;
                        for (var i = 0; i < 5; i++)
                        {
                            var temp = reader.ReadUInt16();
                            stream.Seek(-2, SeekOrigin.Current);
                            writer.Write((ushort)(pc ? temp / 100 : temp * 100));
                        }
                    }
                    count++;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(window, Localization.ConversionFailed + Environment.NewLine + file + Environment.NewLine + exc.GetMessage(),
                                    Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            if (count > 0) MessageBox.Show(window, Localization.ConversionFinished + Environment.NewLine + string.Format(Localization.ConversionFinishedDetails, count),
                                           Localization.Finished, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static readonly Lazy<Level> PlaceholderLevel = new Lazy<Level>(() =>
            new Level("PLACEHOLDER", new Size3D(1, 1, 1)) { ModelTheme = 99, SpawnPoint = new Point3D16(0, 0, 1) });
        private string sfxPath, configDir, ffmpeg, outputDir;
        private bool ios;
        private string SfxPath
        {
            get
            {
                if (sfxPath == null)
                {
                    sfxPath = Helper.GetRandomDirectory();
                    Compiler.Compile(false, Edge.AudioDirectory, sfxPath);
                }
                return sfxPath;
            }
        }
        private Regex keepRegex;
        private static readonly Regex VersionMatcher = new Regex("(?<=<integer tag=\"version\">)\\d+(?=<\\/integer>)");
        private bool KeepRegexCheck(string name)
        {
            return keepRegex != null && keepRegex.IsMatch(name);
        }
        private void CompileMobileVersion(object sender, RoutedEventArgs e)
        {
            if (projectSelector.ShowDialog(this) != CommonFileDialogResult.Ok) return;
            dialog?.Dispose();
            dialog = new TaskDialog
            {
                ProgressBar = new TaskDialogProgressBar(),
                StandardButtons = TaskDialogStandardButtons.Cancel,
                OwnerWindowHandle = WindowHandle,
                InstructionText = Localization.CompileMobileVersionTitle,
                Caption = Localization.CompileMobileVersionTitle,
                Text = Localization.Preparing
            };
            cancelled = false;
            new Thread(CompileMobileVersion).Start(projectSelector.FileName);
            var result = dialog.Show();
            if (result == TaskDialogResult.Cancel || result == TaskDialogResult.Close) cancelled = true;
        }

        private static void Start(string path, string args = null)
        {
            Process.Start(new ProcessStartInfo(path, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path)
            }).WaitForExit();
        }
        private void Ffmpeg(string input, string output, bool caf = true)
        {
            Start(ffmpeg, string.Format("-i \"{0}\"{2} \"{1}\" -y", input, output,
                                        ios ? caf ? " -acodec adpcm_ima_qt" : string.Empty : " -f ogg"));
        }
        private string FallbackPath(string path, bool isSfx = false)
        {
            var result = Path.Combine(configDir, path);
            if (File.Exists(result)) return result;
            result = Path.Combine(isSfx ? SfxPath : Edge.GameDirectory, path);
            return File.Exists(result) ? result : null;
        }

        private void UpdateProgress(int value = -1, int maximum = -1)
        {
            Dispatcher.Invoke(() =>
            {
                if (value < 0) TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                else
                {
                    if (maximum >= 0) dialog.ProgressBar.Maximum = maximum;
                    if (value > dialog.ProgressBar.Maximum) value = dialog.ProgressBar.Maximum;
                    dialog.ProgressBar.Value = value;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    TaskbarItemInfo.ProgressValue = (double)value / dialog.ProgressBar.Maximum;
                }
            });
        }
        private void GenerateMobileLevelFiles(int offset, int count, IReadOnlyList<MappingLevel> levelPack,
                                              IList<string> levelList, IList<string> levelSoundList, bool easy = false)
        {
            for (var i = 0; i < count; ++i)
            {
                if (cancelled) return;
                string name;
                UpdateProgress(offset + i);
                dialog.Text = name = levelList[i] + ".bin";
                try
                {
                    string target = Path.Combine(outputDir, name), source = levelPack[i].FileName == null ? null
                        : FallbackPath(Path.Combine("levels", name = levelPack[i].FileName + ".bin"));
                    if (!KeepRegexCheck(name))
                    {
                        if (source == null) PlaceholderLevel.Value.EasyCompile(target);
                        else if (easy) File.Copy(source, target, true);
                        else
                        {
                            var level = Level.CreateFromCompiled(source);
                            level.SPlusTime *= 100;
                            level.STime *= 100;
                            level.ATime *= 100;
                            level.BTime *= 100;
                            level.CTime *= 100;
                            level.EasyCompile(target);
                        }
                    }
                    dialog.Text = name = levelSoundList[i] + ".caf.ogg";
                    if (!KeepRegexCheck(name))
                    {
                        source = levelPack[i].NameSfx == null ? null
                            : FallbackPath(Path.Combine("sfx", "levelsfx_" + levelPack[i].NameSfx + ".wav"), true);
                        target = Path.Combine(outputDir, name);
                        if (source != null) Ffmpeg(source, target);
                        else File.WriteAllBytes(target, CurrentApp.ReadResourceBytes("Resources/placeholder.ogg"));
                    }
                }
                catch (Exception exc)
                {
                    throw new Exception(name, exc);
                }
            }
        }

        private void CloseDialog()
        {
            dialog.Text = Localization.Done;
            if (!cancelled) dialog.Close();
            try
            {
                Dispatcher.Invoke(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None);
            }
            catch (TaskCanceledException) { }
        }
        private void CompileMobileVersion(object arg)
        {
            try
            {
                configDir = Path.GetDirectoryName(arg.ToString());
                ffmpeg = Path.Combine(configDir, "ffmpeg.exe");
                var root = XHelper.Load(arg.ToString()).Root;
                outputDir = Path.Combine(configDir, (ios = root.GetAttributeValue("preset") == "ios")
                                                         ? @"Payload\EDGE Epic.app" : @"src\assets");
                var regex = root.GetAttributeValue("keep");
                keepRegex = regex == null ? null : new Regex(regex, RegexOptions.Compiled);
                List<MappingLevel>
                    levelPackA = new List<MappingLevel>(root.XPathSelectElements("levelpackA/level").Take(48)
                        .Select((c, k) => new MappingLevel(LevelType.Standard, k, c))),
                    levelPackB = new List<MappingLevel>(root.XPathSelectElements("levelpackB/level").Take(17)
                        .Select((c, k) => new MappingLevel(LevelType.Bonus, k, c)));
                while (levelPackA.Count < 48) levelPackA.Add(new MappingLevel(LevelType.Standard, levelPackA.Count));
                while (levelPackB.Count < 17) levelPackB.Add(new MappingLevel(LevelType.Bonus, levelPackB.Count));
                sfxPath = null;
                var sprs = Directory.EnumerateFiles(Path.Combine(Edge.GameDirectory, "sprites"), "*.spr")
                                    .Select(Path.GetFileName).ToList();
                if (cancelled) return;
                var sounds = Directory.EnumerateFiles(outputDir, "*.caf.ogg").Select(file =>
                    Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file))).ToList();
                if (cancelled) return;
                UpdateProgress(0, 92 + sounds.Count + sprs.Count + (ios ? 1 : 0));
                GenerateMobileLevelFiles(0, 48, levelPackA, MobileStandardLevels, MobileStandardLevelSounds);
                if (cancelled) return;
                GenerateMobileLevelFiles(48, 17, levelPackB, MobileBonusLevels, MobileBonusLevelSounds, true);
                if (cancelled) return;
                UpdateProgress(65);
                dialog.Text = "cos.bin";
                if (!KeepRegexCheck("cos.bin"))
                    File.Copy(FallbackPath("cos.bin"), Path.Combine(outputDir, "cos.bin"), true);
                if (cancelled) return;
                UpdateProgress(66);
                dialog.Text = "font.bin";
                if (!KeepRegexCheck("font.bin"))
                    File.Copy(FallbackPath("font.bin"), Path.Combine(outputDir, "font.bin"), true);
                int i;
                for (i = 0; i <= 24; ++i)
                {
                    if (cancelled) return;
                    var filename = Level.Musics[i] + (i == 4 ? ".wav" : ".mp3") + ".ogg";
                    if (KeepRegexCheck(filename)) continue;
                    UpdateProgress(67 + i);
                    dialog.Text = filename;
                    string source = FallbackPath(Path.Combine("music", Level.Musics[i] + ".ogg")),
                           target = Path.Combine(outputDir, filename);
                    if (ios) Ffmpeg(source, target, false);
                    else File.Copy(source, target, true);
                }
                i = 91;
                foreach (var sound in sounds)
                {
                    if (cancelled) return;
                    string name = sound + ".wav", target = Path.Combine(outputDir, name + ".caf.ogg");
                    if (!KeepRegexCheck(name)) continue;
                    UpdateProgress(++i);
                    dialog.Text = name;
                    name = Path.Combine("sfx", name);
                    if (File.Exists(name)) Ffmpeg(FallbackPath(name, true), target);
                }
                foreach (var name in sprs)
                {
                    if (cancelled) return;
                    if (!KeepRegexCheck(name)) continue;
                    UpdateProgress(++i);
                    dialog.Text = name;
                    File.Copy(FallbackPath(Path.Combine("sprites", name)), Path.Combine(outputDir, name), true);
                }
                if (cancelled) return;
                if (ios)
                {
                    UpdateProgress(++i);
                    dialog.Text = "iTunesMetadata.plist";
                    var target = Path.Combine(configDir, "iTunesMetadata.plist");
                    File.WriteAllText(target, VersionMatcher.Replace(File.ReadAllText(target),
                                      m => (int.Parse(m.Value, CultureInfo.InvariantCulture) + 1).ToStringInvariant()));
                }
                if (cancelled) return;
                UpdateProgress(++i);
                dialog.Text = Localization.AlmostThere;
                Start(Path.Combine(configDir, "compile.bat"));
            }
            catch (Exception exc)
            {
                Dispatcher.Invoke(() => MessageBox.Show(this, Localization.CompileMobileVersionFailed + Environment.NewLine + exc.GetMessage(),
                                                        Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                CloseDialog();
                if (sfxPath != null)
                {
                    try
                    {
                        Directory.Delete(sfxPath, true);
                    }
                    catch { }
                    sfxPath = null;
                }
            }
        }

        private void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            CheckForUpdates(this);
        }
        public static void CheckForUpdates(Window window = null)
        {
            WebsiteManager.CheckForUpdates(
                () => MessageBox.Show(window, Localization.NoUpdatesAvailable, Localization.Finished,
                                      MessageBoxButton.OK, MessageBoxImage.Information),
                exc => MessageBox.Show(window, Localization.CheckForUpdatesFailed + Environment.NewLine + exc.GetMessage(),
                                       Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }
        private void Help(object sender, RoutedEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/developers.html");
        }

        private void PopContextMenu(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement)sender;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
            CompileMobileVersionButton.IsEnabled = RunGameButton.IsEnabled;
        }

        private static string GetPath(object parameter)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "Two Tribes", parameter.ToString(), "settings.ini");
        }

        private void OpenSettingsIni(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(GetPath(e.Parameter));
        }

        private void SettingsIniCanOpen(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = File.Exists(GetPath(e.Parameter));
        }

        #endregion

        #region Browse levels

        private void ShowMinimap(object sender, RoutedEventArgs e)
        {
            foreach (var level in LevelList.SelectedItems.OfType<Level>()) new MinimapWindow(level).Show();
        }

        private void SaveAsImage(object sender, RoutedEventArgs e)
        {
            foreach (var level in LevelList.SelectedItems.OfType<Level>()) new MinimapWindow(level).Save(sender, e);
        }

        private void Decompile(object sender, RoutedEventArgs e)
        {
            if (outputSelector.ShowDialog(this) != CommonFileDialogResult.Ok) return;
            ProcessCore(LevelList.SelectedItems.OfType<Level>().Select(level => level.FilePath),
                        outputSelector.FileName);
            if (!string.IsNullOrWhiteSpace(WarningBox.Text)) Tabs.SelectedItem = CompileTab;
        }

        private void ShowLevelInExplorer(object sender, RoutedEventArgs e)
        {
            foreach (var level in LevelList.SelectedItems.OfType<Level>()) ShowInExplorer(level.FilePath);
        }

        private void OnSort(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = e.Column.Header.ToString() == "#";
            if (!e.Handled) return;
            var descending = e.Column.SortDirection == ListSortDirection.Ascending;
            e.Column.SortDirection = descending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            ((ListCollectionView)CollectionViewSource.GetDefaultView(levels)).CustomSort =
                new LevelSorter(descending);
        }

        private void OnGridInitialized(object sender, EventArgs e)
        {
            var column = LevelList.Columns.First();
            column.SortDirection = ListSortDirection.Ascending;
            LevelList.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath,
                                                                     ListSortDirection.Ascending));
        }

        private void DrawLevelModelTree(object sender, RoutedEventArgs e)
        {
            var level = LevelList.SelectedItem as Level;
            if (level == null) return;
            ModelNameBox.Text = Path.GetFileNameWithoutExtension(level.FilePath);
            Tabs.SelectedItem = DrawModelTreeTab;
            DrawModelTree(sender, e);
        }

        private void ViewInModelViewer(object sender, RoutedEventArgs e)
        {
            Level level = LevelList.SelectedItem as Level;
            if (level == null)
            {
                return;
            }

            if (ModelWindow == null)
            {
                (ModelWindow = new ModelWindow(ModelNameBox.Text)).Show();
            }
            else
            {
                ModelWindow.Clear(sender, e);
                ModelWindow.SetModelName(ModelNameBox.Text);
            }

            ModelWindow.DrawChildModels = true;
            ModelWindow.DebugMode = false;

            string path = Path.Combine(Edge.ModelsDirectory, AssetUtil.CrcFullName(Path.GetFileNameWithoutExtension(level.FilePath), "models", false) + ".eso");
            ModelWindow.Draw(path);

            Point3D16 y = new Point3D16(0, (short) level.Size.Length, 0);
            
            ModelWindow.DrawElement("finish", level.ExitPoint - y);

            foreach (FallingPlatform p in level.FallingPlatforms)
            {
                ModelWindow.DrawElement("falling_platform", p.Position - y);
            }

            foreach (OtherCube c in level.OtherCubes)
            {
                ModelWindow.DrawElement("holoswitch", c.PositionTrigger - y);
            }

            foreach (Prism p in level.Prisms)
            {
                ModelWindow.DrawElement("prism", p.Position - y);
                ModelWindow.ApplyAnimationToElement("prism");
                ModelWindow.DrawElement("prism_shadow", p.Position - y);
                ModelWindow.ApplyAnimationToElement("prism_shadow");
            }

            foreach (MovingPlatform p in level.MovingPlatforms)
            {
                string platform = p.FullBlock ? "platform" : "platform_small";
                ModelWindow.DrawElement(platform, p.Waypoints[0].Position - y);

                if (p.AutoStart)
                {
                    ModelWindow.AnimateMovingPlatform(p);
                    ModelWindow.DrawElement("platform_edges_active" + (p.FullBlock ? "" : "_small"), p.Waypoints[0].Position - y);
                    ModelWindow.AnimateMovingPlatform(p);
                }
            }

            foreach (Button b in level.Buttons)
            {
                string type = b.Visible == NullableBoolean.True ? "switch" : "switch_ghost";
                if (b.MovingPlatformID != null)
                {
                    MovingPlatform p = level.MovingPlatforms[b.MovingPlatformID.Index];
                    ModelWindow.DrawElement(type, p.Waypoints[0].Position - y);
                    if (p.AutoStart)
                    {
                        ModelWindow.AnimateMovingPlatform(p);
                    }
                }
                else
                {
                    ModelWindow.DrawElement(type, b.Position - y);
                }
            }

            foreach (Resizer r in level.Resizers)
            {
                if (r.Direction == ResizeDirection.Grow)
                {
                    ModelWindow.DrawElement("shrinker_tomini", r.Position - y);
                    ModelWindow.ApplyAnimationToElement("shrinker_tobig");
                }
                else
                {
                    ModelWindow.DrawElement("shrinker_tomini", r.Position - y);
                    ModelWindow.ApplyAnimationToElement("shrinker_tomini");
                }
            }

            foreach (Bumper b in level.Bumpers)
            {
                ModelWindow.DrawElement("bumper_bottom", b.Position - y);
                ModelWindow.ApplyAnimationToElement("bumper_bottom");
                ModelWindow.DrawElement("bumper_right", b.Position - y);
                ModelWindow.ApplyAnimationToElement("bumper_right");
                ModelWindow.DrawElement("bumper_roof", b.Position - y);
                //ModelWindow.ApplyAnimationToElement("bumper_roof");
            }

            ModelWindow.Activate();
        }

        private void ModifyMappingXml(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(Edge.LevelsDirectory, "mapping.xml"));
        }

        private void ShowMappingXmlHelp(object sender, RoutedEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/developers/file-formats/mapping-xml.html");
        }

        #endregion

        #region Browse achievements

        private void RefreshGlobalPercent(object sender, RoutedEventArgs e)
        {
            Achievements.RefreshGlobalPercents();
        }

        private void SetDefaultProfile(object sender, RoutedEventArgs e)
        {
            if (Edge.SteamOtl != null) Edge.SteamOtl.SettingsUserName = Users.Current.CurrentUser.SteamID;
        }

        private void ForceUnlockAchievement(object sender, MouseButtonEventArgs e)
        {
            var item = AchievementsList.SelectedItem as Achievement;
            if (item != null && Users.Current.CurrentUser != null && MessageBox.Show(this, Localization.AchievementUnlockConfirm, Localization.Ask, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Users.Current.CurrentUser.SetAchieved(item, !Users.Current.CurrentUser.GetAchieved(item));
        }

        private void RefreshAchievements(object sender = null, FileSystemEventArgs e = null)
        {
            var achievedBefore = new HashSet<string>();
            Dispatcher.Invoke(() =>
            {
                if (Users.Current.CurrentUser != null) achievedBefore = new HashSet<string>(
                    from a in Achievements.Current where Users.Current.CurrentUser.GetAchieved(a) select a.ApiName);
                Users.Current.Refresh();
                AchievementsList.Items.Refresh();
                AchievementsTip.Visibility = Visibility.Visible;
            });
            if (Users.Current.CurrentUser == null) return;
            foreach (var achievement in Achievements.Current.Where(achievement =>
                !achievedBefore.Contains(achievement.ApiName) && Users.Current.CurrentUser.GetAchieved(achievement)))
            {
                while (Interlocked.Exchange(ref balloonShown, 1) == 1) Thread.Sleep(500);
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(5000, string.Format(Localization.AchievementUnlocked, achievement.Title),
                    Localization.AchievementUnlockedDetails + achievement.Description, ToolTipIcon.Info);
            }
        }

        private void SortAchievements(object sender, RoutedEventArgs e)
        {
            var tag = (((FrameworkElement)e.OriginalSource).Tag ?? string.Empty).ToString();
            AchievementsList.Items.SortDescriptions.Clear();
            if (string.IsNullOrWhiteSpace(tag)) return;
            var descending = tag.EndsWith("_DESCENDING", true, CultureInfo.InvariantCulture);
            if (descending) tag = tag.Substring(0, tag.Length - 11);
            AchievementsList.Items.SortDescriptions.Add(new SortDescription(tag,
                descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
        }

        #endregion

        #region Compile & Decompile

        private readonly ObservableCollection<List<string>>
            decompileHistory = new ObservableCollection<List<string>>();
        private readonly Dictionary<string, CheckBox> checkBoxes;

        private void ShowCommandLineHelp(object sender, RoutedEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/edgetool/command-line-arguments.html");
        }
        private void OpenReference(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/developers/file-formats/" +
                ((FrameworkElement)sender).Tag + ".html");
        }

        private void OnBinaryDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = e.AllowedEffects & DragDropEffects.Copy;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects, Localization.Decompile);
            }
            else
            {
                e.Effects = DragDropEffects.None;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
            }
        }
        private void OnBinaryDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? e.AllowedEffects & DragDropEffects.Copy : DragDropEffects.None;
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), e.Effects);
            if (e.Effects != DragDropEffects.Copy) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files == null) return;
            var count = ProcessCore(files);
            WarningBox.Text += string.Format(Localization.DecompileFinishedDetails, count);
        }

        private int ProcessCore(IEnumerable<string> allFiles, string directory = null, bool addToHistory = true)
        {
            var files = allFiles.ToList();
            if (addToHistory) decompileHistory.Add(files);
            var count = 0;
            bool? exFormat = null;
            WarningBox.Text = string.Empty;
            foreach (var file in files)
            {
                if (file.EndsWith(".png", true, CultureInfo.InvariantCulture) && !exFormat.HasValue)
                    exFormat = MessageBox.Show(this, Localization.NewEtxConfirmTitle + Environment.NewLine + Localization.NewEtxConfirmDetails,
                                               Localization.Ask, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                var result = Compiler.Compile(exFormat ?? false, file, directory);
                if (result.Item1 == null)
                {
                    count++;
                    foreach (var entry in result.Item3.Where(entry => checkBoxes[entry.Type].IsChecked == true))
                        try
                        {
                            Process.Start(entry.FileName);
                        }
                        catch (Win32Exception exc)
                        {
                            MessageBox.Show(this, Localization.StartDecompiledFileFailed + Environment.NewLine + exc.Message,
                                            Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                }
                else MessageBox.Show(this, string.Format(Path.GetFileNameWithoutExtension(file), Localization.DecompileFailed) + Environment.NewLine + result.Item1.GetMessage(),
                                     Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                if (!string.IsNullOrWhiteSpace(result.Item2))
                    WarningBox.Text += string.Format("{0}{1}{2}{1}", file, Environment.NewLine, result.Item2);
            }
            return count;
        }

        private void DecompileHistory(object sender, RoutedEventArgs e)
        {
            var count = ProcessCore(DecompileHistoryBox.SelectedItems.OfType<List<string>>()
                            .SelectMany(a => a).Distinct(), addToHistory: false);
            WarningBox.Text += string.Format(Localization.DecompileFinishedDetails, count);
        }
        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            decompileHistory.Clear();
        }

        #endregion

        #region EdgeMod

        private void UpdateDescription(object sender, SelectionChangedEventArgs e)
        {
            var edgeMod = EdgeModGrid.SelectedItem as EdgeMod;
            if (!string.IsNullOrWhiteSpace(edgeMod?.Description)) DescriptionBlock.Text = edgeMod.Description;
        }

        private void RefreshEdgeMods(object sender = null, RoutedEventArgs e = null)
        {
            var result = Edge.RefreshEdgeMods();
            if (!string.IsNullOrWhiteSpace(result)) MessageBox.Show(this, Localization.LoadEdgeModsError + Environment.NewLine + result,
                                                    Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private TaskDialog dialog;
        private int currentFileIndex;
        private bool isDirty, cancelled, needsRunning;

        private void InstallEdgeMods(object sender, EventArgs e)
        {
            if (!Settings.Instance.EdgeModLoaded)
            {
                Settings.Instance.EdgeModLoaded = true;
                Settings.Save();
                MessageBox.Show(this, Localization.EdgeModsFirstInstallHint, Localization.Information,
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            dialog?.Dispose();
            dialog = new TaskDialog
            {
                ProgressBar = new TaskDialogProgressBar(),
                StandardButtons = TaskDialogStandardButtons.Cancel,
                OwnerWindowHandle = WindowHandle,
                InstructionText = Localization.InstallEdgeModsTitle,
                Caption = Localization.InstallEdgeModsTitle,
                Text = Localization.Preparing,
            };
            UpdateProgress();
            cancelled = false;
            new Thread(InstallEdgeMods).Start();
            var result = dialog.Show();
            if (result == TaskDialogResult.Cancel || result == TaskDialogResult.Close) cancelled = true;
        }

        private void InstallEdgeModsAndRun(object sender, EventArgs e)
        {
            needsRunning = true;
            InstallEdgeMods(sender, e);
        }

        private void InstallEdgeMods()
        {
            currentFileIndex = 0;
            UpdateProgress(0, Edge.EdgeMods.Where(edgeMod => edgeMod.Enabled).Sum(edgeMod => edgeMod.FilesCount));
            var result = Edge.Install(additionalMessage =>
            {
                UpdateProgress(++currentFileIndex);
                dialog.Text = additionalMessage;
            }, ref cancelled);
            CloseDialog();
            Dispatcher.Invoke(() =>
            {
                isDirty = false;
                if (!string.IsNullOrWhiteSpace(result))
                {
                    DescriptionBlock.Text = result;
                    MessageBox.Show(this, Localization.InstallEdgeModsFinished + Environment.NewLine + Localization.InstallEdgeModsFinishedError,
                                    Localization.Finished, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (needsRunning) RunGame();
                needsRunning = false;
            });
        }

        private void CleanUpInstall(object sender, RoutedEventArgs e)
        {
            Edge.CleanUp();
            MessageBox.Show(this, Localization.CleanupFinished + Environment.NewLine + Localization.CleanupFinishedDetails,
                            Localization.Finished, MessageBoxButton.OK, MessageBoxImage.Information);
            isDirty = true;
        }

        private void OnEdgeModDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = e.AllowedEffects & DragDropEffects.Copy;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects,
                                           Localization.InstallEdgeMods.Replace("EdgeMods", "%1"), "EdgeMods");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
            }
        }
        private void OnEdgeModDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? e.AllowedEffects & DragDropEffects.Copy : DragDropEffects.None;
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), e.Effects);
            if (e.Effects != DragDropEffects.Copy) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files?.Where(file => file.EndsWith(".edgemod", true, CultureInfo.InvariantCulture))
                .Count(InstallEdgeMod) > 0) RefreshEdgeMods();
        }

        private bool InstallEdgeMod(string file)
        {
            string id = Path.GetFileNameWithoutExtension(file),
                   target = Path.Combine(Edge.ModsDirectory, Path.GetFileName(file));
            if (File.Exists(target) && MessageBox.Show(this, string.Format(Localization.FileAlreadyExists, id) + Environment.NewLine + Localization.FileAlreadyExistsDetails,
                Localization.Ask, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
            try
            {
                File.Copy(file, target, true);
                isDirty = true;
                return true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(this, string.Format(Localization.InstallEdgeModFailed, id) + Environment.NewLine + exc.Message,
                                Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private void DeleteCurrentEdgeMod(object sender, RoutedEventArgs e)
        {
            var edgeMod = EdgeModGrid.SelectedItem as EdgeMod;
            if (edgeMod == null) return;
            if (!Edge.GetIsDisabled(edgeMod)) isDirty = true;
            File.Delete(edgeMod.FilePath);
            RefreshEdgeMods();
        }

        private void DeleteDisabledEdgeMods(object sender, RoutedEventArgs e)
        {
            foreach (var edgeMod in Edge.EdgeMods.Where(edgeMod => !edgeMod.Enabled)) File.Delete(edgeMod.FilePath);
            RefreshEdgeMods();
        }

        #endregion

        #region Draw Tree

        private void DrawAnimationTree(object sender, RoutedEventArgs e)
        {
            AnimationTreeView.Items.Clear();
            DrawEan(AnimationTreeView.Items, AssetUtil.CrcFullName(AnimationNameBox.Text, "models", false));
        }

        private void DrawModelTree(object sender, RoutedEventArgs e)
        {
            ModelTreeView.Items.Clear();
            DrawEso(ModelTreeView.Items, AssetUtil.CrcFullName(ModelNameBox.Text, "models", false));
        }

        private static void DrawEan(IList parent, string fileName)
        {
            var item = new TreeViewItem { IsExpanded = true };
            parent.Add(item);
            var path = Path.Combine(Edge.ModelsDirectory, fileName + ".ean");
            item.Tag = path;
            if (!File.Exists(path))
            {
                item.Header = fileName + ".ean" + Localization.NotExists;
                item.Foreground = Brushes.Red;
                return;
            }
            var ean = EAN.FromFile(path);
            item.Header = $"{fileName}.ean ({Helper.GetDecompiledFileName(fileName, ean)}.xml)";
            if (!ean.Header.NodeChild.IsZero()) DrawEan(item.Items, ean.Header.NodeChild.ToString());
            if (!ean.Header.NodeSibling.IsZero()) DrawEan(parent, ean.Header.NodeSibling.ToString());
        }

        private static void DrawEso(IList parent, string fileName)
        {
            var item = new TreeViewItem { IsExpanded = true };
            parent.Add(item);
            var path = Path.Combine(Edge.ModelsDirectory, fileName + ".eso");
            item.Tag = path;
            if (!File.Exists(path))
            {
                item.Header = fileName + ".eso" + Localization.NotExists;
                item.Foreground = Brushes.Red;
                return;
            }
            var eso = ESO.FromFile(path);
            item.Header = $"{fileName}.eso ({Helper.GetDecompiledFileName(fileName, eso)}.xml)";
            foreach (var model in eso.Models.Where(model => !model.MaterialAsset.IsZero()))
                DrawEma(item.Items, model.MaterialAsset.ToString());
            if (!eso.Header.NodeChild.IsZero()) DrawEso(item.Items, eso.Header.NodeChild.ToString());
            if (!eso.Header.NodeSibling.IsZero()) DrawEso(parent, eso.Header.NodeSibling.ToString());
        }

        private static void DrawEma(IList parent, string fileName)
        {
            var item = new TreeViewItem { IsExpanded = true };
            parent.Add(item);
            var path = Path.Combine(Edge.ModelsDirectory, fileName + ".ema");
            item.Tag = path;
            if (!File.Exists(path))
            {
                item.Header = fileName + ".ema" + Localization.NotExists;
                item.Foreground = Brushes.Red;
                return;
            }
            var ema = EMA.FromFile(path);
            item.Header = $"{fileName}.ema ({Helper.GetDecompiledFileName(fileName, ema)}.xml)";
            foreach (var texture in ema.Textures) DrawEtx(item.Items, texture.Asset.ToString());
        }

        private static void DrawEtx(IList parent, string fileName)
        {
            var item = new TreeViewItem { IsExpanded = true };
            parent.Add(item);
            var path = Path.Combine(Edge.TexturesDirectory, fileName + ".etx");
            item.Tag = path;
            if (!File.Exists(path))
            {
                item.Header = fileName + ".etx" + Localization.NotExists;
                item.Foreground = Brushes.Red;
                return;
            }
            var etx = ETX.FromFile(path);
            item.Header = $"{fileName}.etx ({etx.AssetHeader.Name}.png)";
        }

        private void ReloadModelViewer(object sender, SelectionChangedEventArgs e)
        {
            if (ModelWindow == null)
            {
                return;
            }
            ViewInModelViewer(sender, e);
        }

        private void GetModelTreeHelp(object sender, RoutedEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/developers/file-formats/asset/" +
                          "drawing-model-tree.html");
        }

        private void GetAnimationTreeHelp(object sender, RoutedEventArgs e)
        {
            Process.Start("https://edgefans.mygod.be/edgefans.tk/developers/file-formats/asset/ean/" +
                          "drawing-animation-tree.html");
        }

        private void ShowFileInExplorer(object sender, RoutedEventArgs e)
        {
            var item = (Equals(Tabs.SelectedItem, DrawModelTreeTab) ? ModelTreeView : AnimationTreeView)
                            .SelectedItem as TreeViewItem;
            if (item != null) ShowInExplorer(item.Tag.ToString());
        }

        private void OnModelDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = e.AllowedEffects & DragDropEffects.Copy;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects, Localization.DrawModelTree);
            }
            else
            {
                e.Effects = DragDropEffects.None;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
            }
        }

        private void OnModelDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? e.AllowedEffects & DragDropEffects.Copy : DragDropEffects.None;
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), e.Effects);
            if (e.Effects != DragDropEffects.Copy) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files == null || files.Length == 0) return;
            using (var stream = File.OpenRead(files[0]))
            {
                var name = new AssetHeader(stream).Name;
                ModelNameBox.Text = name.EndsWith(".rmdl", true, CultureInfo.InvariantCulture)
                    ? name.Remove(name.Length - 5) : Path.GetFileNameWithoutExtension(files[0]);
            }
            DrawModelTree(sender, e);
        }

        private void OnAnimationDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = e.AllowedEffects & DragDropEffects.Copy;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects, Localization.DrawAnimTree);
            }
            else
            {
                e.Effects = DragDropEffects.None;
                DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
            }
        }

        private void OnAnimationDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop, true)
                ? e.AllowedEffects & DragDropEffects.Copy : DragDropEffects.None;
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), e.Effects);
            if (e.Effects != DragDropEffects.Copy) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files == null || files.Length == 0) return;
            using (var stream = File.OpenRead(files[0]))
            {
                var name = new AssetHeader(stream).Name;
                AnimationNameBox.Text = name.EndsWith(".rcha", true, CultureInfo.InvariantCulture)
                    ? name.Remove(name.Length - 5) : Path.GetFileNameWithoutExtension(files[0]);
            }
            DrawAnimationTree(sender, e);
        }

        public static ModelWindow ModelWindow;
        private void ViewModel(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = ModelTreeView.SelectedItem as TreeViewItem;
            if (item == null)
            {
                item = ModelTreeView.Items.GetItemAt(0) as TreeViewItem;
            }

            string path = item.Tag.ToString();
            if (!path.EndsWith(".eso", false, CultureInfo.InvariantCulture))
            {
                return;
            }

            if (ModelWindow == null)
            {
                (ModelWindow = new ModelWindow(ModelNameBox.Text)).Show();
            }
            else
            {
                ModelWindow.SetModelName(ModelNameBox.Text);
            }

            ModelWindow.DrawChildModels = DrawChildModelsBox.IsChecked == true;
            ModelWindow.DebugMode = EnableDebugModeBox.IsChecked == true;
            ModelWindow.Draw(path);
            ModelWindow.Activate();
        }

        private void ViewAnimation(bool loop = true)
        {
            var item = AnimationTreeView.SelectedItem as TreeViewItem;
            if (item == null) return;
            var path = item.Tag.ToString();
            if (!path.EndsWith(".ean", false, CultureInfo.InvariantCulture)) return;
            if (ModelWindow == null) MessageBox.Show(this, Localization.ApplyAnimNoModelTitle + Environment.NewLine + Localization.ApplyAnimNoModelDescription,
                                                     Localization.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                ModelWindow.ApplyAnimation(path, loop);
                ModelWindow.Activate();
            }
        }
        private void ViewAnimation(object sender, RoutedEventArgs e)
        {
            ViewAnimation();
        }
        private void ViewAnimationNonLoop(object sender, RoutedEventArgs e)
        {
            ViewAnimation(false);
        }

        #endregion
    }

    public sealed class LevelSorter : IComparer
    {
        public LevelSorter(bool descending)
        {
            this.descending = descending;
        }

        private readonly bool @descending;

        public int Compare(object x, object y)
        {
            var result = ((Level)x).Mapping.CompareTo(((Level)y).Mapping);
            return descending ? -result : result;
        }
    }

    [ValueConversion(typeof(string), typeof(Image))]
    public sealed class AchievementStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var achievement = value as Achievement;
            if (achievement == null || Users.Current.CurrentUser == null)
            {
                return Application.Current.Resources["Disabled"];
            }

            if (Users.Current.CurrentUser.GetAchieved(achievement))
            {
                return Application.Current.Resources["Achieved"];
            }

            var result = (Image)Application.Current.Resources["Help"];
            result.Tag = achievement.Help;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class VisibleWhileNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(ushort), typeof(string))]
    public sealed class SecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ushort)) return null;
            var seconds = (ushort)value;
            if (seconds < 60) return seconds.ToStringInvariant() + '"';
            return FormattableString.Invariant($"{seconds / 60}'{seconds % 60:00}\"");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public sealed class BinFilePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().GetRelativePath(MainWindow.Edge.LevelsDirectory)
                        .RemoveExtension().ToCorrectPath();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Path.Combine(MainWindow.Edge.LevelsDirectory, value + ".bin");
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public sealed class GlobalPercentTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.ToString() + '%' : Localization.Unknown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(List<string>), typeof(string))]
    public sealed class FilesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = value as List<string>;
            if (list == null || list.Count == 0) return Localization.HistoryListEmpty;
            return (list.Count > 1 ? string.Format(Localization.HistoryList, list[0], list.Count) : list[0]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
