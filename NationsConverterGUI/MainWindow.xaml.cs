using GBX.NET;
using GBX.NET.Engines.Game;
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

namespace NationsConverterGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ListViewMapItem> Maps { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Maps = new ObservableCollection<ListViewMapItem>();
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

                            if (gbx != null)
                            {
                                if (gbx is GameBox<CGameCtnChallenge> gbxMap)
                                {
                                    if (gbxMap.MainNode.Collection == "Stadium")
                                    {
                                        listViewMaps.Dispatcher.Invoke(() => Maps.Add(new ListViewMapItem(gbxMap)));
                                        return gbxMap;
                                    }
                                }
                            }
                        }
                        catch
                        {

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
    }
}
