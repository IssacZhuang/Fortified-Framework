﻿using Fortified;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fortified
{
    [StaticConstructorOnStartup]
    public class CompProperties_AbilityDirectionalExplosion : CompProperties_AbilityEffect
    {
        public float range;
        public float lineWidthEnd;
        public DamageDef explosionDamage;
        public int damageAmount = 5;

        public CompProperties_AbilityDirectionalExplosion()
        {
            compClass = typeof(CompAbilityEffect_DirectionalExplosion);
        }
        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            if(explosionDamage ==null) explosionDamage = DamageDefOf.Stun;
            return base.ConfigErrors(parentDef);
        }
    }
    public class CompAbilityEffect_DirectionalExplosion : CompAbilityEffect
    {
        private List<IntVec3> tmpCells = new List<IntVec3>();

        private new CompProperties_AbilityDirectionalExplosion Props => (CompProperties_AbilityDirectionalExplosion)props;

        private Pawn Pawn => parent.pawn;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 position = parent.pawn.Position;
            float num = Mathf.Atan2(-(target.Cell.z - position.z), target.Cell.x - position.x) * 57.29578f;
            GenExplosion.DoExplosion(affectedAngle: new FloatRange(num - 10f, num + 10f), center: position, map: parent.pawn.MapHeld, radius: Props.range, damType: Props.explosionDamage, instigator: Pawn, damAmount: Props.damageAmount, armorPenetration: -1f, explosionSound: null, weapon: null, projectile: null, intendedTarget: null, postExplosionSpawnThingDef: null, postExplosionSpawnChance: 0f, postExplosionSpawnThingCount: 0, postExplosionGasType: null, applyDamageToExplosionCellsNeighbors: true, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 0, chanceToStartFire: 0f, damageFalloff: false, direction: null, ignoredThings: null, doVisualEffects: false, propagationSpeed: -1f, excludeRadius: 0f, doSoundEffects: false);
            base.Apply(target, dest);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Pawn.Faction == null) return true;

            foreach (var item in Pawn.MapHeld.mapPawns.PawnsInFaction(Pawn.Faction))
            {
                if (AffectedCells(target).Contains(item.PositionHeld)) return false;
            }
            return true;
        }

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();
            Vector3 vector = Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(Pawn.Map);
            if (Pawn.Position == intVec)
            {
                return tmpCells;
            }

            float lengthHorizontal = (intVec - Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)Pawn.Position.x + num * Props.range);
            intVec.z = Mathf.RoundToInt((float)Pawn.Position.z + num2 * Props.range);
            float target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num3 = Props.lineWidthEnd / 2f;
            float num4 = Mathf.Sqrt(Mathf.Pow((intVec - Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
            float num5 = 57.29578f * Mathf.Asin(num3 / num4);
            int num6 = GenRadial.NumCellsInRadius(Props.range);
            for (int i = 0; i < num6; i++)
            {
                IntVec3 intVec2 = Pawn.Position + GenRadial.RadialPattern[i];
                if (CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), target2)) <= num5)
                {
                    tmpCells.Add(intVec2);
                }
            }

            List<IntVec3> list = GenSight.BresenhamCellsBetween(Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!tmpCells.Contains(intVec3) && CanUseCell(intVec3))
                {
                    tmpCells.Add(intVec3);
                }
            }

            return tmpCells;

            bool CanUseCell(IntVec3 c)
            {
                if (!c.InBounds(Pawn.Map)) return false;

                if (c == Pawn.Position) return false;

                if (c.Filled(Pawn.Map)) return false;

                if (!c.InHorDistOf(Pawn.Position, Props.range)) return false;

                return GenSight.LineOfSight(Pawn.Position, c, Pawn.Map, skipFirstCell: true);
            }
        }
    }
}