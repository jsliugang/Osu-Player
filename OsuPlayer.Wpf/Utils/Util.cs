﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Metadata;

namespace Milky.OsuPlayer.Utils
{
    public static class Util
    {
        private static AppDbOperator _appDbOperator = new AppDbOperator();
        /// <summary>
        /// Copy resource to folder
        /// </summary>
        /// <param name="filename">File name in resource.</param>
        /// <param name="path">Path to save.</param>
        public static void ExportResource(string filename, string path)
        {
            System.Resources.ResourceManager rm = Properties.Resources.ResourceManager;
            byte[] obj = (byte[])rm.GetObject(filename, null);
            if (obj == null)
                return;
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                fs.Write(obj, 0, obj.Length);
                fs.Close();
            }
        }

        public static string CountSize(long size)
        {
            string strSize = "";
            long factSize = size;
            if (factSize < 1024)
                strSize = factSize.ToString("F2") + " B";
            else if (factSize >= 1024 && factSize < 1048576)
                strSize = (factSize / 1024f).ToString("F2") + " KB";
            else if (factSize >= 1048576 && factSize < 1073741824)
                strSize = (factSize / 1024f / 1024f).ToString("F2") + " MB";
            else if (factSize >= 1073741824)
                strSize = (factSize / 1024f / 1024f / 1024f).ToString("F2") + " GB";
            return strSize;
        }

        public static bool? BrowseDb(out string path)
        {
            var fbd = new OpenFileDialog
            {
                Title = @"请选择osu所在目录内的""osu!.db""",
                Filter = @"Beatmap Database|osu!.db"
            };
            var result = fbd.ShowDialog();
            path = fbd.FileName;
            return result;
        }

        public static IEnumerable<FileInfo> EnumerateFiles(string path, params string[] extName)
        {
            var lowerExtName = extName?.Select(k => k.ToLower()).ToArray();
            DirectoryInfo currentDir = new DirectoryInfo(path);
            //IEnumerable<string> subDirs = Directory.EnumerateDirectories(path).AsParallel(); // 目录列表                    
            IEnumerable<FileInfo> files = currentDir.EnumerateFiles().AsParallel();

            foreach (FileInfo f in files) // 显示当前目录所有文件   
            {
                if (lowerExtName == null || lowerExtName.Length == 0 || lowerExtName.Contains(f.Extension.ToLower()))
                {
                    yield return f;
                }
            }

            //foreach (string d in subDirs)
            //{
            //    var subFiles = EnumerateFiles(d, extName);
            //    foreach (var subFile in subFiles)
            //    {
            //        yield return subFile;
            //    }
            //}
        }

        private static readonly object CountLock = new object();
        private static int _thumbCount;

        public static async Task<string> GetThumbByBeatmapDbId(BeatmapDataModel dataModel)
        {
            if (_appDbOperator.GetMapThumb(dataModel.BeatmapDbId, out var path) && path != null)
            {
                return path;
            }

            var osuFilePath = dataModel.InOwnDb
                ? Path.Combine(Domain.CustomSongPath, dataModel.FolderName, dataModel.BeatmapFileName)
                : Path.Combine(Domain.OsuSongPath, dataModel.FolderName, dataModel.BeatmapFileName);

            if (!File.Exists(osuFilePath))
            {
                return null;
            }

            //lock (CountLock)
            //{
            //    _thumbCount++;
            //}

            var osuFile = await OSharp.Beatmap.OsuFile.ReadFromFileAsync(@"\\?\" + osuFilePath, options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                })
                .ConfigureAwait(false);
            var guidStr = Guid.NewGuid().ToString();

            //lock (CountLock)
            //{
            //    _thumbCount--;
            //}

            var sourceBgFile = osuFile.Events?.BackgroundInfo?.Filename;
            if (string.IsNullOrWhiteSpace(sourceBgFile))
            {
                _appDbOperator.SetMapThumb(dataModel.BeatmapDbId, null);
                return null;
            }

            var sourceBgPath = dataModel.InOwnDb
                ? Path.Combine(Domain.CustomSongPath, dataModel.FolderName, sourceBgFile)
                : Path.Combine(Domain.OsuSongPath, dataModel.FolderName, sourceBgFile);
            if (!File.Exists(sourceBgPath))
            {
                //_appDbOperator.SetMapThumb(dataModel.BeatmapDbId, null);
                return null;
            }

            ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
            _appDbOperator.SetMapThumb(dataModel.BeatmapDbId, guidStr);
            return guidStr;
        }

        private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
        {
            byte[] imageBytes = LoadImageData(sourcePath);
            BitmapSource bitmapSource = CreateImage(imageBytes, width, height);
            imageBytes = GetEncodedImageData(bitmapSource, ".jpg");
            SaveImageData(imageBytes, Path.Combine(Domain.ThumbCachePath, $"{targetName}.jpg"));
        }

        private static byte[] LoadImageData(string filePath)
        {
            byte[] imageBytes;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                imageBytes = br.ReadBytes((int)fs.Length);
            }

            return imageBytes;
        }

        private static void SaveImageData(byte[] imageData, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(imageData);
            }
        }

        private static BitmapSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight)
        {
            if (imageData == null) return null;
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            if (decodePixelWidth > 0)
            {
                result.DecodePixelWidth = decodePixelWidth;
            }

            if (decodePixelHeight > 0)
            {
                result.DecodePixelHeight = decodePixelHeight;
            }

            result.StreamSource = new MemoryStream(imageData);
            result.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            result.CacheOption = BitmapCacheOption.Default;
            result.EndInit();
            return result;
        }

        private static byte[] GetEncodedImageData(BitmapSource source, string preferredFormat)
        {
            byte[] result = null;
            BitmapEncoder encoder;
            switch (preferredFormat.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".tif":
                case ".tiff":
                    encoder = new TiffBitmapEncoder();
                    break;
                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;
                case ".wmp":
                    encoder = new WmpBitmapEncoder();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                result = new byte[stream.Length];
                using (var br = new BinaryReader(stream))
                {
                    br.Read(result, 0, (int)stream.Length);
                }
            }

            return result;
        }
    }
}
