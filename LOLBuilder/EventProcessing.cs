﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using LeagueSharp;
using LeagueSharp.Common;
using System.Security.Permissions;

namespace LolBuilder
{
    internal class EventProcessing
    {
        public static Menu Config;

        public static void GameLoad(EventArgs args)
        {
            Game.PrintChat("LolBuilder Loaded - By Seph");
            String championname = ObjectManager.Player.ChampionName.Replace(" ", "").Replace("'", "");
            var main = new System.Threading.Thread(() =>
            {
                ProBuilds(championname);
                CreateMenu(Config);
                if (AutoLevOn())
                {
                    var sequence = BuildData.SkillSequence;
                    new CommonAutoLevel(sequence);
                }
            });

            main.Start();
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public static void ProBuilds(string cname)
        {
            BuildData.BuildsList = new List<BuildData.BuildInfo>();
            WebClient pbClient = new WebClient();
            String Data = null;
            try
            {
                Data = pbClient.DownloadString("http://lolbuilder.net/" + cname);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            String SkillSeq = ExtractString(Data, "window.skillOrder[0] = [", "];");
            string[] seqinstringarray = SkillSeq.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int[] OrderedSequence = new int[seqinstringarray.Length];
            for (int i = 0; i < seqinstringarray.Length; i++)
            {
                try
                {
                    OrderedSequence[i] = int.Parse(seqinstringarray[i]);
                    Console.Write(OrderedSequence[i]);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }

                BuildData.SkillSequence = OrderedSequence;
            }
            MatchCollection Builds = Regex.Matches(Data, "<div class=\"build-body\"[\\S\\s]*?id=\"build-content-");
            foreach (var b in Builds)
            {
                List<String> Starting = new List<string>();
                List<String> Buildorders = new List<string>();
                List<String> Final = new List<string>();
                List<String> BuildSummary = new List<string>();
                BuildData.BuildInfo BuildInfo = new BuildData.BuildInfo();

                //Specific Build info
                string buildinfo = b.ToString();

                //Extraction 
                String sitemsect = ExtractString(buildinfo, "<div class=\"shortcut-area starting-item-sets row",
                    "</section>");
                MatchCollection StartItems = Regex.Matches(sitemsect, "<small class=\"t-overflow[\\S\\s]*?</small>");
                foreach (var si in StartItems)
                {
                    String ItemNameFixed = HTMLStrip(si.ToString());
                    Starting.Add(ItemNameFixed);

                }
                String BOItemsect = ExtractString(buildinfo, "<h4 class=\"block-title\">Build Order", "</section>");
                MatchCollection BuildOrder = Regex.Matches(BOItemsect, "<small class=\"t-overflow\">[\\S\\s]*?</small>");
                foreach (var item in BuildOrder)
                {
                    String ItemNameFixed = HTMLStrip(item.ToString());
                    Buildorders.Add(ItemNameFixed);

                }
                String FinalItemsect = ExtractString(buildinfo, "<section class=\"final-items\">", "</section>");
                MatchCollection FinalBuild = Regex.Matches(FinalItemsect,
                    "<small class=\"t-overflow\">[\\S\\s]*?</small>");
                foreach (var item in FinalBuild)
                {
                    String ItemNameFixed = HTMLStrip(item.ToString());
                    Final.Add(ItemNameFixed);
                    //Console.WriteLine(ItemNameFixed);
                }

                String BuildSummarysect = ExtractString(buildinfo, "<div class=\"shortcut-area build-summary\">", "</section>");
                MatchCollection Summary = Regex.Matches(BuildSummarysect,
                    "<small class=\"t-overflow\">[\\S\\s]*?</small>");
                foreach (var item in Summary)
                {
                    String ItemNameFixed = HTMLStrip(item.ToString());
                    BuildSummary.Add(ItemNameFixed);
                }


                // Add to Lists
                BuildInfo.startingitems = Starting;
                BuildInfo.buildorder = Buildorders;
                BuildInfo.finalitems = Final;
                BuildInfo.buildsummary = BuildSummary;
                BuildData.BuildsList.Add(BuildInfo);
            }

        }

        private static bool NotifOn()
        {
            //return false;
            return Config.Item("notif").GetValue<bool>();
        }

        private static bool AutoLevOn()
        {
            return Config.Item("leveler").GetValue<bool>();
        }


        public static void CreateMenu(Menu Menu)
        {
            string champname = ObjectManager.Player.ChampionName;
            Config = new Menu("ProBuilds", "ProBuilds", true);
            var settings = new Menu("Misc", "Misc");
            Config.AddItem(new MenuItem("leveler", "ProLeveler").SetValue(true));
            Config.Item("leveler").ValueChanged += (s, e) =>
            {
                AutoLevel.Enabled(e.GetNewValue<bool>());
                Console.WriteLine("ProLeveler: " + e.GetNewValue<bool>());
                if (e.GetNewValue<bool>())
                {
                    var sequence = BuildData.SkillSequence;
                    Console.Write(sequence);
                    new AutoLevel(sequence);
                }
            };

            settings.AddItem(new MenuItem("notif", "Enable Notifications")).SetValue(false);
            Config.AddSubMenu(settings);
            foreach (var build in BuildData.BuildsList)
            {

                Random Random = new Random();
                var BuildName = "Build " + BuildData.BuildsList.IndexOf(build);
                var BuildMenu = new Menu(BuildName, BuildName);
                var starting = BuildMenu.AddSubMenu(new Menu("Starting", "Starting"));
                var Buildorder = BuildMenu.AddSubMenu(new Menu("Order", "Build Order"));
                var Final = BuildMenu.AddSubMenu(new Menu("Final Items", "Final"));
                var Summary = BuildMenu.AddSubMenu(new Menu("Build Summary", "Summary"));

                foreach (var si in build.startingitems)
                {
                    var s = starting.Item(si + "." + champname);
                    if (s != null)
                    {
                        s.ValueSet = true;
                    }
                    starting.AddItem(new MenuItem(si + "." + champname, si));
                }

                foreach (var bo in build.buildorder)
                {
                    var b = Buildorder.Item(bo + "." + champname);
                    if (b != null)
                    {
                        b.ValueSet = true;
                    }
                    Buildorder.AddItem(new MenuItem(bo + "." + champname, bo));

                }
                foreach (var finalitem in build.finalitems)
                {
                    var f = Final.Item(finalitem + "." + champname);
                    if (f != null)
                    {
                        f.ValueSet = true;
                    }
                    Final.AddItem(new MenuItem(finalitem + "." + champname, finalitem));
                }

                foreach (var summitem in build.buildsummary)
                {
                    var s = Summary.Item(summitem + "." + champname);
                    if (s != null)
                    {
                        s.ValueSet = true;
                    }
                    Summary.AddItem(new MenuItem(summitem + "." + champname, summitem));
                }

                Config.AddSubMenu(BuildMenu);
            }
            Config.AddToMainMenu();
        }


        public static string HTMLStrip(string htmlString)
        {
            string pattern = @"<(.|\n)*?>";
            string removeus = htmlString.Replace("&", string.Empty).Replace(";", string.Empty);
            return Regex.Replace(removeus, pattern, string.Empty);
        }

        private static string ConstructPattern(string start, string end)
        {
            string rstart = Regex.Escape(start);
            string rend = Regex.Escape(end);
            string regexpattern = rstart + @"(.*?)" + rend;
            return regexpattern;
        }

        private static Match RegexExtract(string bundleoftext, string start, string end)
        {
            string rstart = Regex.Escape(start);
            string rend = Regex.Escape(end);
            string matchedstring = rstart + @"(.*?)" + rend;
            Match match = Regex.Match(bundleoftext, @matchedstring);
            return match;
        }

        private static string ExtractString(string s, string start, string end)
        {
            if (s.Contains(start) && s.Contains(end))
            {
                int startIndex = s.IndexOf(start) + start.Length;
                int endIndex = s.IndexOf(end, startIndex);

                return s.Substring(startIndex, endIndex - startIndex);
            }

            return "";
        }

    }


}