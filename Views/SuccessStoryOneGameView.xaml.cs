﻿using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Controls;
using SuccessStory.Models;
using SuccessStory.Services;
using SuccessStory.Views.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace SuccessStory.Views
{
    /// <summary>
    /// Logique d'interaction pour SuccessStoryOneGameView.xaml
    /// </summary>
    public partial class SuccessStoryOneGameView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SuccessStoryDatabase PluginDatabase = SuccessStory.PluginDatabase;


        public SuccessStoryOneGameView(Game GameContext)
        {
            InitializeComponent();


            GameAchievements gameAchievements = PluginDatabase.Get(PluginDatabase.GameContext, true);
            if (gameAchievements.SourcesLink != null)
            {
                PART_SourceLabel.Text = gameAchievements.SourcesLink.GameName + " (" + gameAchievements.SourcesLink.Name + ")";
                PART_SourceLink.Tag = gameAchievements.SourcesLink.Url;
            }
            else
            {
                PART_SourceLabel.Text = string.Empty;
                PART_SourceLabel.Tag = string.Empty;
            }


            if (gameAchievements.HasData)
            {
                var AchCommon = gameAchievements.Common;
                var AchNoCommon = gameAchievements.NoCommon;
                var AchRare = gameAchievements.Rare;

                PART_AchCommon.Content = AchCommon.UnLocked;
                PART_AchNoCommon.Content = AchNoCommon.UnLocked;
                PART_AchRare.Content = AchRare.UnLocked;

                PART_AchCommonTotal.Content = AchCommon.Total;
                PART_AchNoCommonTotal.Content = AchNoCommon.Total;
                PART_AchRareTotal.Content = AchRare.Total;
            }


            this.DataContext = new
            {
                GameContext
            };
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((Hyperlink)sender).Tag is string)
            {
                if (!((string)((Hyperlink)sender).Tag).IsNullOrEmpty())
                {
                    Process.Start((string)((Hyperlink)sender).Tag);
                }
            }
        }
    }
}
