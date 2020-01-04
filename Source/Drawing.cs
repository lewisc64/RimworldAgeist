using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Ageist
{

    [HarmonyPatch(typeof(PawnRenderer))]
    [HarmonyPatch("RenderPawnInternal", new[] { typeof(Vector3), typeof(float), typeof(Boolean), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(Boolean), typeof(Boolean) })]
    class PawnRenderer_Patch
    {
        [HarmonyPrefix]
        public static void PatchScale(ref PawnRenderer __instance, ref Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait, bool headStump)
        {
            Drawing.ScaleGraphicSetForAge(__instance.graphics);
        }
    }

    class Drawing
    {
        internal static void ScaleGraphicSetForAge(PawnGraphicSet pawnGraphics)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (!pawnGraphics.pawn.RaceProps.Humanlike)
                {
                    return;
                }

                float desiredScale = 1.5f;
                if (pawnGraphics.pawn.ageTracker.CurLifeStageIndex >= (int)Age.Adult)
                {
                    desiredScale = 1.0f;
                }

                List<Graphic> graphics = new List<Graphic>()
                {
                    pawnGraphics.nakedGraphic,
                };

                foreach (ApparelGraphicRecord record in pawnGraphics.apparelGraphics)
                {
                    if (record.sourceApparel.DescriptionFlavor.Contains("hat"))
                    {
                        continue;
                    }
                    graphics.Add(record.graphic);
                }

                foreach (Graphic graphic in graphics)
                {
                    if (graphic == null)
                    {
                        continue;
                    }

                    IEnumerable<Material> materials = typeof(Graphic).GetProperties()
                        .Where(x => x.PropertyType == typeof(Material))
                        .Select(x => x.GetValue(graphic, null)).Cast<Material>();

                    foreach (Material material in materials)
                    {
                        if (material == null)
                        {
                            continue;
                        }
                        //material.mainTextureScale = new Vector2(desiredScale, desiredScale);
                        //material.mainTextureOffset = new Vector2(0, -0.3f);
                    }
                }
            });
        }
    }
}
