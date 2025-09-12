using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SuccessStory.Clients
{
    public enum OriginData { Steam, Xbox, PlayStation }

    public static class OriginDataExtensions
    {
        public static string GetSearchUrl(this OriginData origin)
        {
            switch (origin)
            {
                case OriginData.Steam:
                    return @"https://truesteamachievements.com/searchresults.aspx?search={0}";
                case OriginData.Xbox:
                    return @"https://www.trueachievements.com/searchresults.aspx?search={0}";
                case OriginData.PlayStation:
                    return @"https://www.truetrophies.com/searchresults.aspx?search={0}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
        }

        public static string GetBaseUrl(this OriginData origin)
        {
            switch (origin)
            {
                case OriginData.Steam:
                    return @"https://truesteamachievements.com";
                case OriginData.Xbox:
                    return @"https://www.trueachievements.com";
                case OriginData.PlayStation:
                    return @"https://www.truetrophies.com";
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
        }
    }

    public class TrueAchievements
    {
        private static ILogger Logger => LogManager.GetLogger();

        internal static SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        /// <summary>
        /// Search list game on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="originData"></param>
        /// <returns></returns>
        public static List<TrueAchievementSearch> SearchGame(Game game, OriginData originData)
        {
            List<TrueAchievementSearch> listSearchGames = new List<TrueAchievementSearch>();

            //TODO: Decide if editions should be removed here
            string url = string.Format(originData.GetSearchUrl(), WebUtility.UrlEncode(PlayniteTools.NormalizeGameName(game.Name, true)));
            string urlBase = originData.GetBaseUrl();

            try
            {
                string reponse = string.Empty;
                WebViewSettings webViewSettings = new WebViewSettings
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
                    JavaScriptEnabled = true
                };
                using (IWebView webViewOffscreen = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    webViewOffscreen.NavigateAndWait(url);
                    reponse = webViewOffscreen.GetPageSource();
                }

                if (reponse.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {url}");
                    return listSearchGames;
                }

                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(reponse);

                if (reponse.IndexOf("There are no matching search results, please change your search terms") > -1)
                {
                    return listSearchGames;
                }

                IElement SectionGames = htmlDocument.QuerySelector("#oSearchResults");
                if (SectionGames == null)
                {
                    string gameUrl = htmlDocument.QuerySelector("link[rel=\"canonical\"]")?.GetAttribute("href");
                    string gameImage = htmlDocument.QuerySelector("div.info img")?.GetAttribute("src");

                    listSearchGames.Add(new TrueAchievementSearch
                    {
                        GameUrl = gameUrl,
                        GameName = game.Name,
                        GameImage = gameImage
                    });
                }
                else
                {
                    foreach (IElement SearchGame in SectionGames.QuerySelectorAll("tr"))
                    {
                        try
                        {
                            IHtmlCollection<IElement> GameInfos = SearchGame.QuerySelectorAll("td");
                            if (GameInfos.Count() > 2)
                            {
                                string GameUrl = urlBase + GameInfos[0].QuerySelector("a")?.GetAttribute("href");
                                string GameName = GameInfos[1].QuerySelector("a")?.InnerHtml;
                                string GameImage = urlBase + GameInfos[0].QuerySelector("a img")?.GetAttribute("src");

                                string ItemType = GameInfos[2].InnerHtml;

                                if (ItemType.IsEqual("game"))
                                {
                                    listSearchGames.Add(new TrueAchievementSearch
                                    {
                                        GameUrl = GameUrl,
                                        GameName = GameName,
                                        GameImage = GameImage
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, PluginDatabase.PluginName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return listSearchGames;
        }


        /// <summary>
        /// Get the estimate time from game url on truesteamachievements or trueachievements.
        /// </summary>
        /// <param name="UrlTrueAchievement"></param>
        /// <returns></returns>
        public static EstimateTimeToUnlock GetEstimateTimeToUnlock(string UrlTrueAchievement)
        {
            EstimateTimeToUnlock EstimateTimeToUnlock = new EstimateTimeToUnlock();

            if (UrlTrueAchievement.IsNullOrEmpty())
            {
                Logger.Warn($"No url for GetEstimateTimeToUnlock()");
                return EstimateTimeToUnlock;
            }

            try
            {
                string reponse = string.Empty;
                WebViewSettings webViewSettings = new WebViewSettings
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
                    JavaScriptEnabled = true
                };
                using (IWebView webViewOffscreen = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    webViewOffscreen.NavigateAndWait(UrlTrueAchievement);
                    reponse = webViewOffscreen.GetPageSource();
                }

                if (reponse.IsNullOrEmpty())
                {
                    Logger.Warn($"No data from {UrlTrueAchievement}");
                    return EstimateTimeToUnlock;
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument htmlDocument = parser.Parse(reponse);

                int NumberDataCount = 0;
                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l1 div"))
                {
                    string title = SearchElement.GetAttribute("title");
                    if (!title.IsNullOrEmpty() && (title == "Maximum TrueAchievement" || title == "Maximum TrueSteamAchievement" || title == "Maximum TrueTrophy"))
                    {
                        string data = SearchElement.InnerHtml;
                        _ = int.TryParse(Regex.Replace(data, "[^0-9]", ""), out NumberDataCount);
                        break;
                    }
                }

                foreach (IElement SearchElement in htmlDocument.QuerySelectorAll("div.game div.l2 a"))
                {
                    string Title = SearchElement.GetAttribute("title");
                    if (!Title.IsNullOrEmpty() && (Title == "Estimated time to unlock all achievements" || Title == "Estimated time to unlock all trophies"))
                    {
                        string EstimateTime = SearchElement.InnerHtml
                            .Replace("<i class=\"fa fa-hourglass-end\"></i>", string.Empty)
                            .Replace("<i class=\"fa fa-clock-o\"></i>", string.Empty)
                            .Trim();

                        int EstimateTimeMin = 0;
                        int EstimateTimeMax = 0;
                        int index = 0;
                        foreach (string item in EstimateTime.Replace("h", string.Empty).Split('-'))
                        {
                            _ = index == 0 ? int.TryParse(item.Replace("+", string.Empty), out EstimateTimeMin) : int.TryParse(item, out EstimateTimeMax);
                            index++;
                        }

                        EstimateTimeToUnlock = new EstimateTimeToUnlock
                        {
                            DataCount = NumberDataCount,
                            EstimateTime = EstimateTime,
                            EstimateTimeMin = EstimateTimeMin,
                            EstimateTimeMax = EstimateTimeMax
                        };
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            if (EstimateTimeToUnlock.EstimateTimeMin == 0)
            {
                string siteName = UrlTrueAchievement.ToLower().Contains("truesteamachievements") ? "TrueSteamAchievements" : 
                                 UrlTrueAchievement.ToLower().Contains("truetrophies") ? "TrueTrophies" :
                                 "TrueAchievements";
                Logger.Warn($"No {siteName} data found");
            }

            return EstimateTimeToUnlock;
        }
    }


    public class TrueAchievementSearch
    {
        public string GameUrl { get; set; }
        public string GameName { get; set; }
        public string GameImage { get; set; }
    }

    public class EstimateTimeToUnlock
    {
        public int DataCount { get; set; }
        public string EstimateTime { get; set; }
        public int EstimateTimeMin { get; set; }
        public int EstimateTimeMax { get; set; }
    }
}
