﻿/*
Modified from RimWorld.ITab_Pawn_Gear
*/

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace Fortified
{
    public class ITab_Mech_Gear:ITab
    {
        
        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private const float TopPadding = 20f;

        public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private const float ThingIconSize = 28f;

        private const float ThingRowHeight = 28f;

        private const float ThingLeftX = 36f;

        private const float StandardLineHeight = 22f;

        private const float InitialHeight = 450f;

        private static List<Thing> workingInvList = new List<Thing>();

        private static List<Thing> workingEquipmentList = new List<Thing>();

        public override bool IsVisible
        {
            get
            {
                Pawn selPawnForGear = SelPawnForGear;
                if (selPawnForGear.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby)
                {
                    return false;
                }

                if (!ShouldShowInventory(selPawnForGear) && !ShouldShowApparel(selPawnForGear))
                {
                    return ShouldShowEquipment(selPawnForGear);
                }

                return true;
            }
        }

        private bool CanControl
        {
            get
            {
                Pawn selPawnForGear = SelPawnForGear;
                if (selPawnForGear.Downed || selPawnForGear.InMentalState)
                {
                    return false;
                }

                if (selPawnForGear.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                if (selPawnForGear.IsPrisonerOfColony)
                {
                    return false;
                }

                return true;
            }
        }

        private bool CanControlColonist
        {
            get
            {
                return (SelPawnForGear is HumanlikeMech || SelPawnForGear is WeaponUsableMech) && CanControl;
            }
        }

        private Pawn SelPawnForGear
        {
            get
            {
                if (base.SelPawn != null)
                {
                    return base.SelPawn;
                }

                Corpse corpse = base.SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }

                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + base.SelThing);
            }
        }

        public ITab_Mech_Gear()
        {
            size = new Vector2(460f, 450f);
            labelKey = "TabGear";
            tutorTag = "Gear";
        }

        protected override void FillTab()
        {
            //IL_040c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0412: Invalid comparison between Unknown and I4
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);
            if (Prefs.DevMode && Widgets.ButtonText(new Rect(rect.xMax - 18f - 125f, 5f, 115f, Text.LineHeight), "Dev tool..."))
            {
                Find.WindowStack.Add(new FloatMenu(DebugToolsPawns.PawnGearDevOptions(SelPawnForGear)));
            }

            Widgets.BeginGroup(rect3);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, rect3.width, rect3.height);
            Rect viewRect = new Rect(0f, 0f, rect3.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float curY = 0f;
            TryDrawMassInfo(ref curY, viewRect.width);
            TryDrawComfyTemperatureRange(ref curY, viewRect.width);
            if (ShouldShowOverallArmor(SelPawnForGear))
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "OverallArmor".Translate());
                TryDrawOverallArmor(ref curY, viewRect.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate());
                TryDrawOverallArmor(ref curY, viewRect.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate());
                TryDrawOverallArmor(ref curY, viewRect.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate());
            }

            if (ShouldShowEquipment(SelPawnForGear))
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Equipment".Translate());
                workingEquipmentList.Clear();
                workingEquipmentList.AddRange(SelPawnForGear.equipment.AllEquipmentListForReading);
                workingEquipmentList.AddRange(SelPawnForGear.apparel.WornApparel.Where((Apparel x) => x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt)));
                foreach (Thing workingEquipment in workingEquipmentList)
                {
                    DrawThingRow(ref curY, viewRect.width, workingEquipment, true);
                }
            }

            if (ShouldShowApparel(SelPawnForGear))
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Apparel".Translate());
                foreach (Apparel item in from x in SelPawnForGear.apparel.WornApparel
                                         where !x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt)
                                         select x into ap
                                         orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                         select ap)
                {
                    DrawThingRow(ref curY, viewRect.width, item, true);
                }
            }

            if (ShouldShowInventory(SelPawnForGear))
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Inventory".Translate());
                workingInvList.Clear();
                workingInvList.AddRange(SelPawnForGear.inventory.innerContainer);
                for (int i = 0; i < workingInvList.Count; i++)
                {
                    DrawThingRow(ref curY, viewRect.width, workingInvList[i], inventory: true);
                }

                workingInvList.Clear();
            }

            if ((int)Event.current.type == 8)
            {
                if (curY + 70f > 450f)
                {
                    size.y = Mathf.Min(curY + 70f, (float)(UI.screenHeight - 35) - 165f - 30f);
                }
                else
                {
                    size.y = 450f;
                }

                scrollViewHeight = curY + 20f;
            }

            Widgets.EndScrollView();
            Widgets.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = (TextAnchor)0;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool inventory = false)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            bool locked = false;
            if (CanControl && (inventory || CanControlColonist || (SelPawnForGear.Spawned && !SelPawnForGear.Map.IsPlayerHome)))
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                bool hasQuestLock = false;
                if (SelPawnForGear.IsQuestLodger())
                {
                    hasQuestLock = inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, SelPawnForGear);
                }

                bool isDestroyOnDrop = !inventory && SelPawnForGear.kindDef.destroyGearOnDrop;
                locked = hasQuestLock || isDestroyOnDrop;
                if (Mouse.IsOver(rect2))
                {
                    if (hasQuestLock)
                    {
                        TooltipHandler.TipRegion(rect2, "DropThingLodger".Translate());
                    }
                    else
                    {
                        TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                    }
                }

                Color color = (locked ? Color.grey : Color.white);
                Color mouseoverColor = (locked ? color : GenUI.MouseoverColor);
                if (Widgets.ButtonImage(rect2, TexButton.Drop, color, mouseoverColor, !locked) && !locked)
                {
                    Action action = delegate
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        InterfaceDrop(thing);
                    };
                    if (!ModsConfig.BiotechActive || !MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(SelPawnForGear, thing, action))
                    {
                        action();
                    }
                }

                rect.width -= 24f;
            }

            if (CanControlColonist)
            {
                if (FoodUtility.WillIngestFromInventoryNow(SelPawnForGear, thing))
                {
                    Rect rect3 = new Rect(rect.width - 24f, y, 24f, 24f);
                    TooltipHandler.TipRegionByKey(rect3, "ConsumeThing", thing.LabelNoCount, thing);
                    if (Widgets.ButtonImage(rect3, TexButton.Ingest))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        FoodUtility.IngestFromInventoryNow(SelPawnForGear, thing);
                    }
                }

                rect.width -= 24f;
            }

            Rect rect4 = rect;
            rect4.xMin = rect4.xMax - 60f;
            CaravanThingsTabUtility.DrawMass(thing, rect4);
            rect.width -= 60f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, (Texture)TexUI.HighlightTex);
            }

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }

            Text.Anchor = (TextAnchor)3;
            GUI.color = ThingLabelColor;
            Rect rect5 = new Rect(36f, y, rect.width - 36f, rect.height);
            string text = thing.LabelCap;
            Apparel apparel2 = thing as Apparel;
            if (apparel2 != null && SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced(apparel2))
            {
                text += ", " + "ApparelForcedLower".Translate();
            }

            if (locked)
            {
                text += " (" + "ApparelLockedLower".Translate() + ")";
            }

            Text.WordWrap = false;
            Widgets.Label(rect5, text.Truncate(rect5.width));
            Text.WordWrap = true;
            if (Mouse.IsOver(rect))
            {
                string text2 = thing.LabelNoParenthesisCap.AsTipTitle() + GenLabel.LabelExtras(thing, includeHp: true, includeQuality: true) + "\n\n" + thing.DescriptionDetailed;
                if (thing.def.useHitPoints)
                {
                    text2 = text2 + "\n" + thing.HitPoints + " / " + thing.MaxHitPoints;
                }

                TooltipHandler.TipRegion(rect, text2);
            }

            y += 28f;
        }

        private void TryDrawOverallArmor(ref float curY, float width, StatDef stat, string label)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(SelPawnForGear.GetStatValue(stat) / 2f);
            List<BodyPartRecord> allParts = SelPawnForGear.RaceProps.body.AllParts;
            List<Apparel> list = ((SelPawnForGear.apparel != null) ? SelPawnForGear.apparel.WornApparel : null);
            for (int i = 0; i < allParts.Count; i++)
            {
                float num3 = 1f - num2;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float num4 = Mathf.Clamp01(list[j].GetStatValue(stat) / 2f);
                            num3 *= 1f - num4;
                        }
                    }
                }

                num += allParts[i].coverageAbs * (1f - num3);
            }

            num = Mathf.Clamp(num * 2f, 0f, 2f);
            Rect rect = new Rect(0f, curY, width, 100f);
            Widgets.Label(rect, label.Truncate(120f));
            rect.xMin += 120f;
            Widgets.Label(rect, num.ToStringPercent());
            curY += 22f;
        }

        private void TryDrawMassInfo(ref float curY, float width)
        {
            if (!SelPawnForGear.Dead && ShouldShowInventory(SelPawnForGear))
            {
                Rect rect = new Rect(0f, curY, width, 22f);
                float num = MassUtility.GearAndInventoryMass(SelPawnForGear);
                Widgets.Label(rect, TranslatorFormattedStringExtensions.Translate(arg2: MassUtility.Capacity(SelPawnForGear).ToString("0.##"), key: "MassCarried", arg1: num.ToString("0.##")));
                curY += 22f;
            }
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (!SelPawnForGear.Dead)
            {
                Rect rect = new Rect(0f, curY, width, 22f);
                float statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin);
                float statValue2 = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax);
                Widgets.Label(rect, "ComfyTemperatureRange".Translate() + ": " + statValue.ToStringTemperature("F0") + " ~ " + statValue2.ToStringTemperature("F0"));
                curY += 22f;
            }
        }

        private void InterfaceDrop(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && SelPawnForGear.apparel != null && SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.RemoveApparel, apparel), JobTag.Misc);
            }
            else if (thingWithComps != null && SelPawnForGear.equipment != null && SelPawnForGear.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, thingWithComps), JobTag.Misc);
            }
            else if (!t.def.destroyOnDrop)
            {
                SelPawnForGear.inventory.innerContainer.TryDrop(t, SelPawnForGear.Position, SelPawnForGear.Map, ThingPlaceMode.Near, out var _);
            }
        }

        private bool ShouldShowInventory(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
            {
                return p.inventory.innerContainer.Any;
            }

            return true;
        }

        private bool ShouldShowApparel(Pawn p)
        {
            if (p.apparel == null)
            {
                return false;
            }

            if (!p.RaceProps.Humanlike)
            {
                return p.apparel.WornApparel.Any();
            }

            return true;
        }

        private bool ShouldShowEquipment(Pawn p)
        {
            return p.equipment != null;
        }

        private bool ShouldShowOverallArmor(Pawn p)
        {
            if (!p.RaceProps.Humanlike && !ShouldShowApparel(p) && !(p.GetStatValue(StatDefOf.ArmorRating_Sharp) > 0f) && !(p.GetStatValue(StatDefOf.ArmorRating_Blunt) > 0f))
            {
                return p.GetStatValue(StatDefOf.ArmorRating_Heat) > 0f;
            }

            return true;
        }
    }
}
