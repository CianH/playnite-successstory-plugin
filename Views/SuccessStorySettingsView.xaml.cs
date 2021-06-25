﻿using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared;
using SuccessStory.Clients;
using SuccessStory.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Diagnostics;
using SuccessStory.Services;

namespace SuccessStory.Views
{
    public partial class SuccessStorySettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IPlayniteAPI _PlayniteApi;

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;

        private ExophaseAchievements exophaseAchievements = new ExophaseAchievements();

        private string _PluginUserDataPath;

        public static bool WithoutMessage = false;
        public static CancellationTokenSource tokenSource;
        private CancellationToken ct;

        int SteamTotal;
        int SteamTotalAchievements;
        int GogTotal;
        int GogTotalAchievements;
        int OriginTotal;
        int OriginTotalAchievements;
        int XboxTotal;
        int XboxTotalAchievements;
        int RetroAchievementsTotal;
        int RetroAchievementsTotalAchievements;
        int Rpcs3Total;
        int Rpcs3TotalAchievements;

        int LocalTotal;
        int LocalTotalAchievements;


        public SuccessStorySettingsView(SuccessStory plugin, IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();

            SetTotal();

            switch (PluginDatabase.PluginSettings.Settings.NameSorting)
            {
                case "Name":
                    cbDefaultSorting.Text = resources.GetString("LOCGameNameTitle");
                    break;
                case "LastActivity":
                    cbDefaultSorting.Text = resources.GetString("LOCLastPlayed");
                    break;
                case "SourceName":
                    cbDefaultSorting.Text = resources.GetString("LOCSourceLabel");
                    break;
                case "ProgressionValue":
                    cbDefaultSorting.Text = resources.GetString("LOCSuccessStorylvGamesProgression");
                    break;
            }


            var task = Task.Run(() => CheckLogged())
                .ContinueWith(antecedent =>
                {
                    this.Dispatcher.Invoke(new Action(() => 
                    {
                        if (antecedent.Result)
                        {
                            lIsAuth.Content = resources.GetString("LOCLoggedIn");
                        }
                        else
                        {
                            lIsAuth.Content = resources.GetString("LOCNotLoggedIn");
                        }
                    }));
                });
        }

        private void SetTotal()
        {
            SteamTotal = 0;
            SteamTotalAchievements = 0;
            GogTotal = 0;
            GogTotalAchievements = 0;
            OriginTotal = 0;
            OriginTotalAchievements = 0;
            XboxTotal = 0;
            XboxTotalAchievements = 0;
            RetroAchievementsTotal = 0;
            RetroAchievementsTotalAchievements = 0;
            Rpcs3Total = 0;
            Rpcs3TotalAchievements = 0;

            LocalTotal = 0;
            LocalTotalAchievements = 0;

            try
            {
                foreach (var game in _PlayniteApi.Database.Games)
                {
                    string GameSourceName = PlayniteTools.GetSourceName(_PlayniteApi, game);

                    switch (GameSourceName.ToLower())
                    {
                        case "steam":
                            SteamTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                SteamTotalAchievements += 1;
                            }
                            break;
                        case "gog":
                            GogTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                GogTotalAchievements += 1;
                            }
                            break;
                        case "origin":
                            OriginTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                OriginTotalAchievements += 1;
                            }
                            break;
                        case "xbox":
                            XboxTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                XboxTotalAchievements += 1;
                            }
                            break;
                        case "retroachievements":
                            RetroAchievementsTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                RetroAchievementsTotalAchievements += 1;
                            }
                            break;
                        case "rpcs3":
                            Rpcs3Total += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                Rpcs3TotalAchievements += 1;
                            }
                            break;
                        case "playnite":
                            LocalTotal += 1;
                            if (PluginDatabase.VerifAchievementsLoad(game.Id))
                            {
                                LocalTotalAchievements += 1;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            SteamLoad.Content = SteamTotalAchievements + "/" + SteamTotal;
            GogLoad.Content = GogTotalAchievements + "/" + GogTotal;
            OriginLoad.Content = OriginTotalAchievements + "/" + OriginTotal;
            XboxLoad.Content = XboxTotalAchievements + "/" + XboxTotal;
            RetroAchievementsLoad.Content = RetroAchievementsTotalAchievements + "/" + RetroAchievementsTotal;
            LocalLoad.Content = LocalTotalAchievements + "/" + LocalTotal;
            Rpcs3Load.Content = Rpcs3TotalAchievements + "/" + Rpcs3Total;
        }


        private void cbDefaultSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PluginDatabase.PluginSettings.Settings.NameSorting = ((ComboBoxItem)cbDefaultSorting.SelectedItem).Tag.ToString();
        }


        private void ButtonSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string SelectedFolder = _PlayniteApi.Dialogs.SelectFolder();
            if (!SelectedFolder.IsNullOrEmpty())
            {
                PART_Rpcs3Folder.Text = SelectedFolder;
                PluginDatabase.PluginSettings.Settings.Rpcs3InstallationFolder = SelectedFolder;
            }
        }


        private void Button_Click_Wiki(object sender, RoutedEventArgs e)
        {
            Process.Start((string)((FrameworkElement)sender).Tag);
        }


        #region Tag
        private void ButtonAddTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.AddTagAllGame();
        }

        private void ButtonRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.RemoveTagAllGame();
        }
        #endregion


        #region Exophase
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lIsAuth.Content = resources.GetString("LOCLoginChecking");

            try
            {
                exophaseAchievements.Login();

                var task = Task.Run(() => CheckLogged())
                    .ContinueWith(antecedent =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            if (antecedent.Result)
                            {
                                lIsAuth.Content = resources.GetString("LOCLoggedIn");
                            }
                            else
                            {
                                lIsAuth.Content = resources.GetString("LOCNotLoggedIn");
                            }
                        }));
                    });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to authenticate user.");
            }
        }

        private bool CheckLogged()
        {
            return exophaseAchievements.GetIsUserLoggedIn();
        }
        #endregion
    }


    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
            {
                if ((value is bool) && (bool)value == false)
                {
                    return false;
                }
            }
            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
        }
    }
}