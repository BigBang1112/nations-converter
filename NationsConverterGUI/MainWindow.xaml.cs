using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using NationsConverter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NationsConverterGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ListViewMapItem> Maps { get; private set; }

        public int SelectedMapIndex => listViewMaps.SelectedIndex;
        public ListViewMapItem SelectedMap => (ListViewMapItem)listViewMaps.SelectedItem;

        public Dictionary<string, Task<GameBox<CGameItemModel>>> Collectors { get; }

        public string SelectedMapName
        {
            get
            {
                if (SelectedMap == null)
                    return "";
                return Formatter.Deformat(SelectedMap.GBX.MainNode.MapName);
            }
        }

        public SortedDictionary<string, SheetBlock> SelectedMapSheetBlocks
        {
            get
            {
                return SelectedMap?.SheetBlocks;
            }
        }

        public Sheet[] Sheets { get; }

        DispatcherTimer loadMapMsgTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Maps = new ObservableCollection<ListViewMapItem>();
            Maps.CollectionChanged += Maps_CollectionChanged;

            Sheets = new Sheet[]
            {
                YamlManager.Parse<Sheet>(AppDomain.CurrentDomain.BaseDirectory + "Sheets/Stock.yml"),
                YamlManager.Parse<Sheet>(AppDomain.CurrentDomain.BaseDirectory + "Sheets/Custom.yml")
            };

            loadMapMsgTimer = new DispatcherTimer();
            loadMapMsgTimer.Interval = TimeSpan.FromSeconds(2);
            loadMapMsgTimer.Tick += LoadMapMsgTimer_Tick;

            Collectors = new Dictionary<string, Task<GameBox<CGameItemModel>>>();

            foreach(var fileName in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "UserData", "*.Gbx", SearchOption.AllDirectories))
            {
                var relativeFileName = fileName.Substring(AppDomain.CurrentDomain.BaseDirectory.Length);
                Collectors[relativeFileName] = Task.Run(() =>
                {
                    var gbx = GameBox.ParseHeader(fileName);

                    if(gbx is GameBox<CGameItemModel> gbxItem)
                        return gbxItem;
                    return null;
                });
            }
        }

        private void Maps_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Maps.Count > 0)
                textBlockDragDropMsg.Visibility = Visibility.Hidden;
            else
                textBlockDragDropMsg.Visibility = Visibility.Visible;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e) => Close();
        private void buttonMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void buttonMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private async void listViewMaps_Drop(object sender, DragEventArgs e)
        {
            var formats = e.Data.GetFormats();

            if (formats.Contains("FileDrop"))
            {
                var fileNames = (string[])e.Data.GetData("FileDrop");

                List<Task<GameBox<CGameCtnChallenge>>> loadingMaps = new List<Task<GameBox<CGameCtnChallenge>>>();

                foreach (var file in fileNames)
                {
                    loadingMaps.Add(Task.Run(() =>
                    {
                        try
                        {
                            var gbx = GameBox.Parse(file);

                            if (gbx == null)
                            {
                                LoadMapMessage($"{file} is not a GBX file.", Brushes.Red);
                            }
                            else
                            {
                                if (gbx is GameBox<CGameCtnChallenge> gbxMap)
                                {
                                    if (gbxMap.MainNode.Collection == "Stadium")
                                    {
                                        listViewMaps.Dispatcher.Invoke(() => Maps.Add(new ListViewMapItem(gbxMap)));
                                        LoadMapMessage($"{Formatter.Deformat(gbxMap.MainNode.MapName)} loaded successfully!", Brushes.Green);
                                        return gbxMap;
                                    }
                                    else
                                        LoadMapMessage($"{Formatter.Deformat(gbxMap.MainNode.MapName)} is not a Stadium map.", Brushes.Red);
                                }
                                else
                                    LoadMapMessage($"{file} is not a map.", Brushes.Red);
                            }
                        }
                        catch
                        {
                            LoadMapMessage($"An error occured when loading {file}.", Brushes.Red);
                        }

                        return null;
                    }));
                }

                await Task.WhenAll(loadingMaps);
            }
        }

        private void listViewMaps_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                while (listViewMaps.SelectedIndex > -1)
                {
                    Maps.RemoveAt(listViewMaps.SelectedIndex);
                }
            }
        }

        private void listViewMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            imageThumbnail.Source = SelectedMap?.Thumbnail;
            textBlockMapName.Text = SelectedMapName;
            UpdateSelectedMapSheet();
            listViewPlacedBlocks.ItemsSource = SelectedMapSheetBlocks;

            if (SelectedMap == null)
            {
                
            }
            else
            {

            }
        }

        private void UpdateSelectedMapSheet()
        {
            if (SelectedMap == null) return;
            if (SelectedMap.Updated) return;

            var usedConversions = new Dictionary<string, Dictionary<int, ConversionView>[]>();

            foreach (var block in SelectedMap.Map.Blocks)
            {
                for (var i = 0; i < Sheets.Length; i++)
                {
                    var sheet = Sheets[i];

                    if (sheet.Blocks.TryGetValue(block.Name, out Conversion[] conversions))
                    {
                        if (conversions != null && block.Variant.HasValue)
                        {
                            if (conversions.Length > block.Variant.Value) // If the variant is available in the possible variants
                            {
                                var conversion = conversions[block.Variant.Value]; // Reference it with 'conversion'

                                if (conversion != null)
                                {
                                    if (!usedConversions.TryGetValue(block.Name, out Dictionary<int, ConversionView>[] convs))
                                    {
                                        convs = new Dictionary<int, ConversionView>[Sheets.Length];
                                        usedConversions[block.Name] = convs;
                                    }

                                    if (usedConversions[block.Name][i] == null)
                                        usedConversions[block.Name][i] = new Dictionary<int, ConversionView>();

                                    usedConversions[block.Name][i][block.Variant.Value] = new ConversionView()
                                    {
                                        SheetName = sheet.Name,
                                        Conversion = conversion
                                    };
                                }
                            }
                        }
                    }
                }
            }

            foreach (var block in SelectedMap.Map.Blocks.GroupBy(x => x.Name))
            {
                var name = block.Key;

                if (usedConversions.TryGetValue(name, out Dictionary<int, ConversionView>[] sheets))
                {
                    if (sheets.Where(x => x != null).Count() > 1)
                    {
                        var icons = new List<string>();

                        if (SelectedMapSheetBlocks.TryGetValue(name, out SheetBlock sheetBlock))
                        {
                            foreach (var c in sheets[sheets.Length - 1 - sheetBlock.SelectedSheet])
                                AddConversionReference(c.Value.Conversion);
                        }
                        else
                        {
                            foreach (var c in sheets.Last())
                                AddConversionReference(c.Value.Conversion);
                        }

                        void AddConversionReference(Conversion c)
                        {
                            if (c != null)
                            {
                                if (c.Air != null)
                                    AddConversionReference(c.Air);
                                if (c.Ground != null)
                                    AddConversionReference(c.Ground);
                                if (c.GrassGround != null)
                                    AddConversionReference(c.GrassGround);
                                if (c.DirtGround != null)
                                    AddConversionReference(c.DirtGround);
                                if (c.FabricGround != null)
                                    AddConversionReference(c.FabricGround);

                                if (c.Item != null)
                                    AddItemIcon(c.Item);

                                if (c.Items != null)
                                    foreach (var item in c.Items)
                                        AddItemIcon(item);

                                void AddItemIcon(ConversionItem item)
                                {
                                    if (item.Name != null)
                                    {
                                        var meta = item.Name.Split(' ');
                                        if (meta.Length == 3)
                                        {
                                            var itemName = meta[0];
                                            if (!icons.Contains(itemName))
                                                icons.Add($"Items\\{itemName}");
                                        }
                                    }
                                }
                            }
                        }

                        var counter = sheets.Length-1;
                        var conversiones = sheets.Where(x => x != null).Select(x =>
                        {
                            var list = new SheetList(Sheets[counter].Name, x.Values.ToList());
                            list.BlockName = name;
                            counter -= 1;
                            return list;
                        }).ToArray();

                        if (SelectedMapSheetBlocks.TryGetValue(name, out SheetBlock sb))
                        {
                            
                        }
                        else
                        {
                            SelectedMapSheetBlocks[name] = new SheetBlock()
                            {
                                BlockName = name,
                                Conversions = conversiones.ToDictionary(x => Array.IndexOf(conversiones, x)),
                                Sheets = Sheets.Select(x => x.Name).ToArray()
                            };
                        }

                        Dispatcher.Invoke(() =>
                        {
                            SelectedMapSheetBlocks[name].Icons = new List<BitmapImage>();

                            foreach (var icon in icons)
                            {
                                if (Collectors.TryGetValue($"UserData\\{icon}", out Task<GameBox<CGameItemModel>> item))
                                {
                                    var iconData = item.Result.MainNode.Icon;

                                    if (iconData != null)
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            iconData.Result.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                                            var img = new BitmapImage();
                                            img.BeginInit();
                                            img.StreamSource = ms;
                                            img.CacheOption = BitmapCacheOption.OnLoad;
                                            img.DecodePixelWidth = 128;
                                            img.EndInit();

                                            SelectedMapSheetBlocks[name].Icons.Add(img);
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            }

            SelectedMap.Updated = true;

            listViewPlacedBlocks.ItemsSource = null;
            listViewPlacedBlocks.ItemsSource = SelectedMapSheetBlocks;
        }

        private void LoadMapMessage(string text, Brush color)
        {
            textBlockLoadMapMsg.Dispatcher.Invoke(() =>
            {
                textBlockLoadMapMsg.Foreground = color;
                textBlockLoadMapMsg.Text = text;
                loadMapMsgTimer.Start();
            });
        }

        private void LoadMapMsgTimer_Tick(object sender, EventArgs e)
        {
            textBlockLoadMapMsg.Text = "";
            loadMapMsgTimer.Stop();
        }

        private void buttonConvert_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void comboBoxSheet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selected = (KeyValuePair<int, SheetList>)e.AddedItems[0];
                var blockName = selected.Value.BlockName;
                var sheetBlock = SelectedMapSheetBlocks[blockName];
                sheetBlock.SelectedSheet = selected.Key;

                SelectedMap.Updated = false;
                UpdateSelectedMapSheet();
            }
        }
    }
}
