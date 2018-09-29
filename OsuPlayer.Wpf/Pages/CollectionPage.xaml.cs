﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Models;
using osu_database_reader.Components.Beatmaps;
using Collection = Milkitic.OsuPlayer.Data.Collection;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private MainWindow ParentWindow { get; set; }
        private readonly Collection _collection;
        private List<BeatmapViewModel> _maps;
        private IEnumerable<BeatmapEntry> _entry;

        public CollectionPage(MainWindow mainWindow, Collection collection)
        {
            ParentWindow = mainWindow;
            _collection = collection;
            InitializeComponent();
            UpdateList();
            if (collection.Locked)
                BtnDelCol.Visibility = Visibility.Collapsed;
            LblTitle.Content = _collection.Name;
        }

        private void UpdateList()
        {
            var infos = (List<MapInfo>)DbOperator.GetMapsFromCollection(_collection);
            _entry = App.Beatmaps.GetMapListFromDb(infos);
            _maps = _entry.Transform(true).ToList();
            MapList.DataContext = _maps;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private void RecentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            if (e.OriginalSource is TextBlock)
                return;
            var searchInfo = (BeatmapViewModel)MapList.SelectedItem;
            var map = App.Beatmaps.GetBeatmapsetsByFolder(searchInfo.FolderName)
                .FirstOrDefault(k => k.Version == searchInfo.Version);
            if (map != null)
            {
                ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                    map.BeatmapFileName));
                ParentWindow.FillPlayList(false, false, PlayListMode.RecentList);
            }
            else
            {
                //todo
            }
        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)MapList.SelectedItem;
            DbOperator.RemoveMapFromCollection(searchInfo.GetIdentity(), _collection);
            UpdateList();
            ParentWindow.FillPlayList(true, true, PlayListMode.Collection, _collection);
        }

        private void LblCreator_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.PageBox.Show("提示", "确认删除收藏夹？", () =>
            {
                DbOperator.RemoveCollection(_collection);
                ParentWindow.MainFrame.Navigate(ParentWindow.Pages.RecentPlayPage);
                ParentWindow.UpdateCollections();
            });
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entry);
        }
    }
}