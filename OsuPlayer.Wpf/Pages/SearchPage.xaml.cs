﻿using System;
using System.Collections.Generic;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private BeatmapDbOperator _beatmapDbOperator;
        private MainWindow _mainWindow;

        public SearchPageViewModel ViewModel { get; set; }

        private static Binding _sourceBinding = new Binding(nameof(SearchPageViewModel.DisplayedMaps))
        {
            Mode = BindingMode.OneWay
        };

        private static bool _minimal = false;
        public SearchPage()
        {
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _beatmapDbOperator = new BeatmapDbOperator();

            InitializeComponent();
        }

        public SearchPage Search(string keyword)
        {
            SearchBox.Text = keyword;
            return this;
        }

        private async void SearchPage_Initialized(object sender, EventArgs e)
        {
            ViewModel = (SearchPageViewModel)DataContext;
            await ViewModel.PlayListQueryAsync();
        }

        private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            var minimal = AppSettings.Default.Interface.MinimalMode;
            if (minimal != _minimal)
            {
                if (minimal)
                {
                    ResultCardList.ItemsSource = null;
                    ResultList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    ResultCardList.Visibility = Visibility.Collapsed;
                    ResultList.Visibility = Visibility.Visible;
                }
                else
                {
                    ResultList.ItemsSource = null;
                    ResultCardList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    ResultList.Visibility = Visibility.Collapsed;
                    ResultCardList.Visibility = Visibility.Visible;
                }

                _minimal = minimal;
                await ViewModel.PlayListQueryAsync();
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = ((TextBox)sender).Text;
            await ViewModel.PlayListQueryAsync();
        }

        private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;
        private void Panel_Loaded(object sender, RoutedEventArgs e)
        {
            _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
            ViewModel.GalleryWrapPanel = _virtualizingGalleryWrapPanel;
        }

        private void ResultListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelectedDefault();
        }

        //private void ItemPlay_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ResultList.SelectedItem == null)
        //        return;
        //    var ok = (BeatmapDataModel)ResultList.SelectedItem;
        //    var control = new DiffSelectControl(
        //        _beatmapDbOperator.GetBeatmapsFromFolder(ok.GetIdentity().FolderName),
        //    async selected =>
        //    {
        //        var map = _beatmapDbOperator.GetBeatmapByIdentifiable(selected);
        //        await PlayController.Default.PlayNewFile(map);
        //        await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
        //    });
        //    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
        //}

        //private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null)
        //        return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.Creator));
        //}

        //private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null)
        //        return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.SongSource));
        //}

        //private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null)
        //        return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoArtist));
        //}

        //private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null)
        //        return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoTitle));
        //}

        //private void ItemExport_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null)
        //        return;
        //    ExportPage.QueueEntry(map);
        //}

        //private void ItemCollect_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ResultList.SelectedItem == null)
        //        return;
        //    var ok = (BeatmapDataModel)ResultList.SelectedItem;
        //    var control = new DiffSelectControl(
        //        _beatmapDbOperator.GetBeatmapsFromFolder(ok.GetIdentity().FolderName),
        //        selected =>
        //        {
        //            var entry = _beatmapDbOperator.GetBeatmapsFromFolder(selected.FolderName)
        //                .FirstOrDefault(k => k.Version == selected.Version);
        //            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(entry),
        //                DialogOptionFactory.SelectCollectionOptions);
        //        });
        //    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
        //}

        //private void ItemSet_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null) return;
        //    Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
        //}

        //private void ItemFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelectedDefault();
        //    if (map == null) return;
        //    Process.Start(Path.Combine(Domain.OsuSongPath, map.FolderName));
        //}

        private async void PlaySelectedDefault()
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //    map.BeatmapFileName));
            await PlayController.Default.PlayNewFile(map);
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private Beatmap GetSelectedDefault()
        {
            if (ResultList.SelectedItem == null)
                return null;
            var map = _beatmapDbOperator
                .GetBeatmapsFromFolder(((BeatmapDataModel)ResultList.SelectedItem).FolderName)
                .GetHighestDiff();
            return map;
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            List<Beatmap> beatmaps = ViewModel.SearchedDbMaps;
            if (beatmaps.Count <= 0) return;
            var group = beatmaps.GroupBy(k => k.FolderName);
            List<Beatmap> newBeatmaps = group
                .Select(sb => sb.GetHighestDiff())
                .ToList();

            //if (map == null) return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //     map.BeatmapFileName));
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.None, PlayListMode.Collection, newBeatmaps);
            await PlayController.Default.PlayNewFile(newBeatmaps[0]);

        }

        private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
        {
            var dataModel = ViewModel.DisplayedMaps[e.Index];
            try
            {
                var fileName = await Util.GetThumbByBeatmapDbId(dataModel).ConfigureAwait(false);
                Execute.OnUiThread(() => dataModel.ThumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine(e.Index);
        }
    }
}
