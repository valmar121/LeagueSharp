﻿using System.Linq;
using Evade;
using LeagueSharp;
using LeagueSharp.Common;

namespace YasuoPro
{
    class YasuoEvade
    {

        internal static void Evade()
        {
            if (!Helper.GetBool("Evade.Enabled"))
            {
                return;
            }


            foreach (var skillshot in Program.DetectedSkillshots.ToList())
            {
                if (skillshot.Dodged)
                {
                    if (Helper.Debug)
                        Game.PrintChat(skillshot.SpellData.SpellName + " Dodged already");
                    continue;
                }

                //Avoid trying to evade while dashing
                if (Helper.Yasuo.IsDashing())
                {
                    return;
                }

                if (Helper.GetBool("Evade.OnlyDangerous") && !skillshot.SpellData.IsDangerous)
                {
                    continue;
                }

                if ((skillshot.SpellData.Type == SkillShotType.SkillshotCircle || (skillshot.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)) && !SpellSlot.E.IsReady()))
                {
                    continue;
                }


                if (((Program.NoSolutionFound ||
                      !Program.IsSafePath(Helper.Yasuo.GetWaypoints(), 250).IsSafe &&
                      !Program.IsSafe(Helper.Yasuo.Position.To2D()).IsSafe)))
                {
                    if (skillshot.IsAboutToHit(700, Helper.Yasuo) && skillshot.SpellData.Type != SkillShotType.SkillshotCircle && Helper.GetBool("Evade.UseW"))
                    {
                        if (skillshot.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall) && skillshot.Evade(SpellSlot.W)
                             && skillshot.SpellData.DangerValue >= Helper.GetSlider("Evade.MinDangerLevelWW"))
                        {
                            var castpos = Helper.Yasuo.ServerPosition.Extend(skillshot.MissilePosition.To3D(), 50);
                            bool WCasted = Helper.Spells[Helper.W].Cast(castpos);
                            Program.DetectedSkillshots.Remove(skillshot);
                            skillshot.Dodged = WCasted;
                            if (WCasted)
                            {
                                if (Helper.Debug)
                                {
                                    Game.PrintChat("Blocked " + skillshot.SpellData.SpellName + " with Windwall ");
                                }
                                continue;
                            }
                        }
                    }
                    if (skillshot.IsAboutToHit(500, Helper.Yasuo) && skillshot.Evade(SpellSlot.E) && !skillshot.Dodged && Helper.GetBool("Evade.UseE") && skillshot.SpellData.DangerValue >= Helper.GetSlider("Evade.MinDangerLevelE"))
                    {
                        var evadetarget =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(
                                    x =>
                                        x.IsDashable() &&
                                        Program.IsSafe(Helper.GetDashPos(x)).IsSafe)
                                .OrderBy(x => x.IsMinion)
                                .ThenByDescending(x => x.CountEnemiesInRange(400)).ThenBy(x => x.HealthPercent)
                                .FirstOrDefault();
                        if (evadetarget != null)
                        {
                            Helper.Spells[Helper.E].CastOnUnit(evadetarget);
                            Program.DetectedSkillshots.Remove(skillshot);
                            skillshot.Dodged = true;
                            if (Helper.Debug)
                            {
                                Game.PrintChat("Evading " + skillshot.SpellData.SpellName + " " + "using E to " + evadetarget.BaseSkinName);
                            }
                        }
                    }
                }
            }
        }

    }
}