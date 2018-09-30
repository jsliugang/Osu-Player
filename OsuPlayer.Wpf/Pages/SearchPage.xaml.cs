﻿using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private readonly Stopwatch _querySw = new Stopwatch();
        private bool _queryLock;
        public MainWindow ParentWindow { get; set; }

        public SearchPage(MainWindow mainWindow)
        {
            ParentWindow = mainWindow;
            InitializeComponent();

        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await PlayListQueryAsync();
        }

        private async Task PlayListQueryAsync()
        {
            if (App.Beatmaps == null)
                return;

            //SortEnum sortEnum = (SortEnum)cbSortType.SelectedItem;
            SortMode sortMode = SortMode.Artist;
            _querySw.Restart();
            if (_queryLock)
                return;
            _queryLock = true;
            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 300)
                    Thread.Sleep(1);
                _querySw.Stop();
                _queryLock = false;
                string keyword = null;
                Dispatcher.Invoke(() => { keyword = SearchBox.Text; });
                IEnumerable<BeatmapViewModel> sorted =
                     EntryQuery.GetListByKeyword(keyword, App.Beatmaps).SortBy(sortMode).Transform(false);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResultList.DataContext = sorted;
                }));
            });
        }

        private void LblCreator_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var label = (Label)sender;
            Process.Start($"https://osu.ppy.sh/u/{((string)label.Content).Replace("__", "_")}");
        }

        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultList.SelectedItem == null)
                return;
            if (e.OriginalSource is TextBlock)
                return;
            var map = App.Beatmaps.GetBeatmapsetsByFolder(((BeatmapViewModel)ResultList.SelectedItem).FolderName)
                .GetHighestDiff();
            ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                map.BeatmapFileName));
            App.PlayerControl.RefreshPlayList(PlayerControl.FreshType.All, PlayListMode.RecentList);
        }
    }
}

