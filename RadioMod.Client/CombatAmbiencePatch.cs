using System.Collections.Generic;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace RadioMod.Client
{

    [HarmonyPatch(typeof(Player), "OnMakingShot")]
    internal static class CombatAmbiencePatch
    {
        private struct ShotEvent
        {
            public Vector3 Position;
            public float Time;
        }

        private static readonly List<ShotEvent> RecentShots = new List<ShotEvent>();

        private static void Postfix(Player __instance)
        {
            if (__instance == null)
            {
                return;
            }

            RecentShots.Add(new ShotEvent { Position = __instance.Position, Time = Time.time });

            if (RecentShots.Count > 128)
            {
                RecentShots.RemoveRange(0, RecentShots.Count - 128);
            }
        }

        internal static bool IsCombatNearby(Vector3 position, float radiusMeters, float windowSeconds)
        {
            float now = Time.time;
            float radiusSqr = radiusMeters * radiusMeters;

            for (int i = RecentShots.Count - 1; i >= 0; i--)
            {
                ShotEvent shot = RecentShots[i];
                if (now - shot.Time > windowSeconds)
                {
                    continue;
                }

                if ((shot.Position - position).sqrMagnitude <= radiusSqr)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

