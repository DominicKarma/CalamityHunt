﻿using CalamityHunt.Common.Players;
using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Content.Buffs;
using CalamityHunt.Content.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityHunt.Content.Projectiles.Weapons.Summoner
{
    public class StellarInky : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            Main.projFrames[Type] = 11;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.manualDirectionChange = true;
        }

        public override bool? CanDamage() => State == (int)SlimeMinionState.Attacking;

        public ref float State => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public ref float AttackCount => ref Projectile.ai[2];

        public Player Player => Main.player[Projectile.owner];

        public override void AI()
        {
            if (!Player.GetModPlayer<SlimeCanePlayer>().slimes || Player.dead)
                Projectile.Kill();
            else
                Projectile.timeLeft = 2;

            if (Projectile.Distance(HomePosition) > 1000)
            {
                State = (int)SlimeMinionState.Idle;
                Projectile.Center = HomePosition;
                Projectile.tileCollide = false;
            }       
            
            if (Projectile.Distance(HomePosition) > 800)
                State = (int)SlimeMinionState.IdleMoving;

            iAmInAir = false;

            Projectile.damage = Player.GetModPlayer<SlimeCanePlayer>().highestOriginalDamage;
            int target = -1;
            Projectile.Minion_FindTargetInRange(800, ref target, true);
            bool hasTarget = false;
            if (target > -1)
            {
                hasTarget = true;
                if (Main.npc[target].active && Main.npc[target].CanBeChasedBy(Projectile))
                    Attack(target);
                else
                    hasTarget = false;
            }
            if (!hasTarget)
                Idle();

            if (iAmInAir)
            {
                if (Math.Abs(Projectile.velocity.Length()) > 3)
                {
                    Projectile.rotation += Projectile.velocity.X * 0.02f;
                    Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation);
                }
                else
                    Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0, 0.1f);
            }
            else
                Projectile.rotation = Utils.AngleLerp(Projectile.rotation, 0f, 0.5f);

            //if (iAmInAir && Main.rand.NextBool(3))
            //{
            //    Color color = Color.Lerp(new Color(130, 170, 255, 60), new Color(255, 110, 255, 60), Main.rand.Next(2));
            //    Dust sparkle = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(13, 12), DustID.SparkForLightDisc, Main.rand.NextVector2Circular(1, 1), 0, color, 0.2f + Main.rand.NextFloat());
            //    sparkle.noGravity = Main.rand.NextBool(3);
            //}

            if (AttackCount > 0)
                AttackCount--;
        }

        public Vector2 HomePosition => InAir ? Player.Bottom + new Vector2(-160 * Player.direction, -60) : Player.Bottom + new Vector2(-190 * Player.direction, -20);

        public bool InAir => !Collision.SolidCollision(Player.MountedCenter - new Vector2(20, 0), 40, 150);

        public bool iAmInAir;

        public int teleportTime;

        public void Idle()
        {
            Time = 0;
            if (InAir)
                iAmInAir = true;

            bool tooFar = Projectile.Distance(HomePosition) > 900 && State != (int)SlimeMinionState.Attacking;
            if (tooFar)
            {
                State = (int)SlimeMinionState.IdleMoving;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(HomePosition).SafeNormalize(Vector2.Zero) * Projectile.Distance(HomePosition) * 0.05f, 0.1f);
                {
                    Projectile.rotation += Projectile.direction * 0.2f;
                    Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation);
                }

                if (Projectile.velocity.Y > 0)
                    Projectile.frame = 5;
                else
                    Projectile.frame = 0;
            }

            Projectile.velocity.X *= 0.95f;

            if (Math.Abs(Projectile.Center.X - HomePosition.X) > 4 || InAir)
            {
                State = (int)SlimeMinionState.IdleMoving;
                if (!InAir)
                    Projectile.velocity.X = (HomePosition.X - Projectile.Center.X) * 0.05f;
                else
                {
                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, (HomePosition.X - Projectile.Center.X) * 0.05f, 0.002f);
                    if (Projectile.velocity.Length() > 5 && Main.myPlayer == Projectile.owner)
                    {
                        if (++teleportTime > 150 && Main.rand.NextBool(40))
                        {
                            teleportTime = 0;
                            Projectile.Center -= Projectile.velocity.RotatedByRandom(2f) * Main.rand.Next(8, 15);
                            Projectile.netUpdate = true;

                            //SoundStyle warpSound = SoundID.Item135;
                        }
                    }
                }
            }

            if (InAir)
            {
                Projectile.tileCollide = false;

                if (Projectile.Distance(HomePosition) > 14)
                    Projectile.velocity += Projectile.DirectionTo(HomePosition).SafeNormalize(Vector2.Zero) * MathF.Max(0.1f, Projectile.Distance(HomePosition) * 0.005f);
                else
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Main.rand.NextVector2Circular(5, 5), 0.1f);
                    Projectile.netUpdate = true;
                }
                Projectile.velocity *= 0.95f;
            }
            else
                Projectile.tileCollide = true;

            if (InAir)
                Projectile.frame = 6;

            else
            {
                if (State == (int)SlimeMinionState.IdleMoving)
                {
                    if (++Projectile.frameCounter >= 9)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame = Math.Clamp(Projectile.frame + 1, 0, 5);
                    }
                }
                else
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame = 0;
                }
            }

            if (!iAmInAir)
                Projectile.velocity.Y += 0.2f;

            if (Math.Abs(Projectile.velocity.X) < 1f)
                Projectile.direction = Player.direction;
            else
                Projectile.direction = Math.Sign(Projectile.velocity.X);
        }

        public Vector2 targetPositionOffset;

        public void Attack(int whoAmI)
        {
            NPC target = Main.npc[whoAmI];
            int maxAttacks = 1 + 5;// Player.GetModPlayer<SlimeCanePlayer>().ValueFromSlimeRank(1, 2, 3, 4, 5);

            iAmInAir = true;
            Projectile.tileCollide = false;

            if (Projectile.Distance(target.Center) < 250)
                State = (int)SlimeMinionState.Attacking;

            if (Projectile.Distance(target.Center) > 400 || State == (int)SlimeMinionState.IdleMoving || AttackCount > 0)
            {
                State = (int)SlimeMinionState.IdleMoving;

                Projectile.frame = 6;

                Projectile.velocity += Projectile.DirectionTo(target.Center).SafeNormalize(Vector2.Zero) * MathF.Max(0.5f, Projectile.Distance(target.Center) * 0.5f);
                Projectile.velocity *= 0.94f;
            }

            if (State == (int)SlimeMinionState.Attacking && AttackCount < maxAttacks)
            {
                //Projectile.velocity += Projectile.DirectionTo(target.Center).SafeNormalize(Vector2.Zero) * Projectile.Distance(target.Center) * 0.01f;
                //Projectile.velocity *= 0.93f;

                Projectile.frame = 7;

                Time++;

                if (AttackCount == 0)
                {
                    if (Time < 2 && Main.myPlayer == Projectile.owner)
                    {
                        targetPositionOffset = new Vector2(Projectile.Center.X > target.Center.X ? target.Right.X + Main.rand.Next(5, 15) : target.Left.X - Main.rand.Next(5, 15), Main.rand.Next(-15, 15));
                        Projectile.netUpdate = true;
                    }
                }
                else
                {
                    if (Math.Abs(Projectile.velocity.X) > 0)
                        Projectile.direction = Math.Sign(Projectile.velocity.X);


                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(teleportTime);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            teleportTime = reader.Read();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State == (int)SlimeMinionState.Attacking)
            {
                Time *= -1;
                AttackCount++;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.velocity = Projectile.velocity.RotatedByRandom(0.2f);
                    Projectile.netUpdate = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (State == (int)SlimeMinionState.IdleMoving)
            {
                if (Projectile.velocity.Y >= 0)
                    Jump(-4 - Math.Max(Math.Abs(HomePosition.X - Projectile.Center.X) * 0.01f + (iAmInAir ? Math.Abs(HomePosition.Y - Projectile.Center.Y) * 0.026f : 0) + 0.5f, 0), iAmInAir);
            }

            return false;
        }

        public void Jump(float height, bool air)
        {
            if (air)
            {
                Color color = new Color(255, 150, 150, 60);
                color.A = 0;
                Particle wave = Particle.NewParticle(Particle.ParticleType<MicroShockwave>(), Projectile.Bottom, Vector2.Zero, color, 1.5f);
                wave.data = new Color(255, 255, 168, 120);
                for (int i = 0; i < Main.rand.Next(3, 7); i++)
                {
                    Dust sparkle = Dust.NewDustPerfect(Projectile.Bottom + Main.rand.NextVector2Circular(9, 4), DustID.SparkForLightDisc, Main.rand.NextVector2Circular(3, 1) - Vector2.UnitY * (i + 1) * 0.7f, 0, color, 1f + Main.rand.NextFloat());
                    sparkle.noGravity = Main.rand.NextBool(3);
                }

                SoundEngine.PlaySound(SoundID.Item24 with { MaxInstances = 0, Pitch = 0.6f, PitchVariance = 0.3f, Volume = 0.4f }, Projectile.Center);
            }
            else
                SoundEngine.PlaySound(SoundID.NPCDeath9 with { MaxInstances = 0, Pitch = -0.3f, PitchVariance = 0.3f, Volume = 0.3f }, Projectile.Center);

            Projectile.frame = 0;

            if (Math.Abs(Projectile.Center.X - HomePosition.X) < 4 && !air)
                State = (int)SlimeMinionState.Idle;
            else
                Projectile.velocity.Y = iAmInAir ? height * 0.9f : height;

            if (AttackCount >= 3)
                AttackCount = 0;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(5, 8, Player.GetModPlayer<SlimeCanePlayer>().SlimeRank(), Projectile.frame, -2, -2);
            SpriteEffects direction = Projectile.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 scale = Projectile.scale * Vector2.One;

            if (iAmInAir && Player.GetModPlayer<SlimeCanePlayer>().SlimeRank() > 0)
            {
                Texture2D hatTexture = AssetDirectory.Textures.Extras.InkyHats;
                Rectangle hatFrame = hatTexture.Frame(1, 4, 0, Player.GetModPlayer<SlimeCanePlayer>().SlimeRank() - 1);
                Vector2 hatOffset = new Vector2(0, -(18 + Projectile.velocity.Length())).RotatedBy(-Projectile.velocity.X * 0.05f + (-0.75f + Projectile.velocity.Y * 0.05f) * Projectile.direction) * scale;
                float hatRotation = hatOffset.AngleFrom(Vector2.Zero) + MathHelper.PiOver2 + 0.5f * Projectile.direction;//
                Main.EntitySpriteDraw(hatTexture, Projectile.Center + hatOffset - Main.screenPosition, hatFrame, lightColor, hatRotation, hatFrame.Size() * 0.5f, scale, direction, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Bottom - Vector2.UnitY * 8 - Main.screenPosition, frame, lightColor, Projectile.rotation, new Vector2(frame.Width * (0.5f + 0.1f * Projectile.direction), 36), scale, direction, 0);


            return false;
        }
    }
}