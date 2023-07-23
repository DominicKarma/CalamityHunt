﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityHunt
{
    public static class HuntOfTheOldGodUtils
    {
        public static Vector2 GetDesiredVelocityForDistance(Vector2 start, Vector2 end, float slowDownFactor, int time)
        {
            Vector2 velocity = start.DirectionTo(end).SafeNormalize(Vector2.Zero);
            float distance = start.Distance(end);
            float velocityFactor = (distance * (float)Math.Log(slowDownFactor)) / ((float)Math.Pow(slowDownFactor, time) - 1);
            return velocity * velocityFactor;
        }

        public static float Modulo(float dividend, float divisor) => dividend - (float)Math.Floor(dividend / divisor) * divisor;

        public static string ShortTooltip => "Whispers from on high dance in your ears...";
        public static Color ShortTooltipColor => new(227, 175, 64); // #E3AF40

        // This line is what tells the player to hold Shift. There is essentially no reason to change it
        public static string LeftShiftExpandTooltip => "Press REPLACE THIS NOW to listen closer";
        public static Color LeftShiftExpandColor => new(190, 190, 190); // #BEBEBE

    }
}
