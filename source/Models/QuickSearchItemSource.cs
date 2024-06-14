﻿using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using QuickSearch.SearchItems;
using SuccessStory.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SuccessStory.Models
{
    public class QuickSearchItemSource : ISearchSubItemSource<string>
    {
        private SuccessStoryDatabase PluginDatabase => SuccessStory.PluginDatabase;


        public string Prefix => PluginDatabase.PluginName;

        public bool DisplayAllIfQueryIsEmpty => true;

        public string Icon => Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "command-line.png");


        public IEnumerable<ISearchItem<string>> GetItems()
        {
            return null;
        }

        public IEnumerable<ISearchItem<string>> GetItems(string query)
        {
            if (query.IsEqual("time"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: time < 30 s", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: time 30 min <> 1 h", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: time > 2 h", Icon),

                    new CommandItem("-np (not played) (optional)", new List<CommandAction>(), "example: time > 2 h -np", Icon),
                }.AsEnumerable();
            }

            if (query.IsEqual("percent"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: percent < 50", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: percent 10 <> 30", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: percent > 90", Icon)
                }.AsEnumerable();
            }

            return new List<ISearchItem<string>>
            {
                new CommandItem("time", new List<CommandAction>(), ResourceProvider.GetString("LOCSsQuickSearchByTime"), Icon),
                new CommandItem("percent", new List<CommandAction>(), ResourceProvider.GetString("LOCSsQuickSearchByPercent"), Icon),
            }.AsEnumerable();
        }

        public Task<IEnumerable<ISearchItem<string>>> GetItemsTask(string query, IReadOnlyList<Candidate> addedItems)
        {
            List<string> parameters = GetParameters(query);
            if (parameters.Count > 0)
            {
                switch (parameters[0].ToLower())
                {
                    case "time":
                        return SearchByTime(query);

                    case "percent":
                        return SearchByPercent(query);

                    default:
                        break;
                }
            }
            return null;
        }


        private List<string> GetParameters(string query)
        {
            List<string> parameters = query.Split(' ').ToList();
            if (parameters.Count > 1 && parameters[0].IsNullOrEmpty())
            {
                parameters.RemoveAt(0);
            }
            return parameters;
        }

        private CommandItem GetCommandItem(GameAchievements data, string query)
        {
            DefaultIconConverter defaultIconConverter = new DefaultIconConverter();
            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();

            string title = data.Name;
            string icon = defaultIconConverter.Convert(data.Icon, null, null, null).ToString();
            string dateSession = localDateTimeConverter.Convert(data.LastActivity, null, null, CultureInfo.CurrentCulture).ToString();
            string LastSession = data.LastActivity == null ? string.Empty : ResourceProvider.GetString("LOCLastPlayedLabel") + " " + dateSession;
            string infoEstimate = data.EstimateTime?.EstimateTime.IsNullOrEmpty() ?? true ? string.Empty : "  -  [" + data.EstimateTime?.EstimateTime + "]";

            CommandItem item = new CommandItem(title, () => API.Instance.MainView.SelectGame(data.Id), "", null, icon)
            {
                IconChar = null,
                BottomLeft = PlayniteTools.GetSourceName(data.Id),
                BottomCenter = null,
                BottomRight = data.Unlocked + " / " + data.Total + "  -  (" + data.Progression + " %)" + infoEstimate,
                TopLeft = title,
                TopRight = LastSession,
                Keys = new List<ISearchKey<string>>() { new CommandItemKey() { Key = query, Weight = 1 } }
            };

            return item;
        }


        private List<KeyValuePair<Guid, GameAchievements>> GetDb(ConcurrentDictionary<Guid, GameAchievements> db)
        {
            return db.Where(x => API.Instance.Database.Games.Get(x.Key) != null && x.Value.HasAchievements).ToList();
        }


        private Task<IEnumerable<ISearchItem<string>>> SearchByTime(string query)
        {
            bool OnlyNp = query.Contains("-np", StringComparison.OrdinalIgnoreCase);
            query = query.Replace("-np", string.Empty).Trim();

            List<string> parameters = GetParameters(query);
            List<KeyValuePair<Guid, GameAchievements>> db = GetDb(PluginDatabase.Database.Items);

            if (OnlyNp)
            {
                db = db.Where(x => x.Value.LastActivity == null).ToList();
            }
            db = db.Where(x => x.Value.EstimateTime?.EstimateTimeMax != 0).ToList();

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();

                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                double s = Tools.GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if ((data.Value.EstimateTime?.EstimateTimeMax) != 0 && (data.Value.EstimateTime?.EstimateTimeMax * 3600) >= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        case "<":
                            try
                            {
                                double s = Tools.GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if ((data.Value.EstimateTime?.EstimateTimeMax) != 0 && (data.Value.EstimateTime?.EstimateTimeMax * 3600) <= s)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 6)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();
                    switch (parameters[3])
                    {
                        case "<>":
                            try
                            {
                                double sMin = Tools.GetElapsedSeconde(parameters[1], parameters[2]);
                                double sMax = Tools.GetElapsedSeconde(parameters[4], parameters[5]);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if ((data.Value.EstimateTime?.EstimateTimeMax) != 0 && (data.Value.EstimateTime?.EstimateTimeMax * 3600) >= sMin && (data.Value.EstimateTime?.EstimateTimeMax * 3600) <= sMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }

        private Task<IEnumerable<ISearchItem<string>>> SearchByPercent(string query)
        {
            List<string> parameters = GetParameters(query);
            List<KeyValuePair<Guid, GameAchievements>> db = GetDb(PluginDatabase.Database.Items);

            if (parameters.Count == 3)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();
                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                _ = int.TryParse(parameters[2], out int percent);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if (data.Value.Progression >= percent)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        case "<":
                            try
                            {
                                _ = int.TryParse(parameters[2], out int percent);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if (data.Value.Progression <= percent)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    List<ISearchItem<string>> search = new List<ISearchItem<string>>();
                    switch (parameters[2])
                    {
                        case "<>":
                            try
                            {
                                _ = int.TryParse(parameters[1], out int percentMin);
                                _ = int.TryParse(parameters[3], out int percentMax);
                                foreach (KeyValuePair<Guid, GameAchievements> data in db)
                                {
                                    if (data.Value.Progression >= percentMin && data.Value.Progression <= percentMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;

                        default:
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }
    }
}
