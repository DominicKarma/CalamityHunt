﻿using System;
using System.Collections.Generic;
using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Common.Utilities;
using CalamityHunt.Content.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityHunt.Content.NPCs.Bosses.GoozmaBoss
{
    public partial class Goozma : ModNPC
    {
        /// <summary>
        /// The set of all slime NPC IDs that Goozma can use.
        /// </summary>
        public static readonly List<int> SlimeTypes = new()
        {
            ModContent.NPCType<EbonianBehemuck>(),
            ModContent.NPCType<DivineGargooptuar>(),
            ModContent.NPCType<CrimulanGlopstrosity>(),
            ModContent.NPCType<StellarGeliath>()
        };

        /// <summary>
        /// How long Goozma spends redirecting to be near the player during his slime spawning state.
        /// </summary>
        public static int SpawnSlime_RedirectTime => 45;

        /// <summary>
        /// How long Goozma waits during his slime spawning state to spawn a new slime.
        /// </summary>
        public static int SpawnSlime_SummonDelay => 50;

        /// <summary>
        /// How long Goozma waits during his slime spawning state, after spawning a slime, to transition to attacking again.
        /// </summary>
        public static int SpawnSlime_StateTransitionDelay => 20;

        /// <summary>
        /// Handles the selection of a new slime attack for Goozma. Used during his slime spawning state for future state evaluations.
        /// </summary>
        public int GetSlimeAttack()
        {
            // TODO -- Document this better when I'm more familiar with the AI. -Lucille

            int gotten = -1;

            if (Main.netMode != NetmodeID.MultiplayerClient) {
                // Sanity check the upcoming attack list.
                if (nextAttack[currentSlime] == null || nextAttack[currentSlime].Length != 3 || (
                    nextAttack[currentSlime][0] == slimeAttackskipIt &&
                    nextAttack[currentSlime][1] == slimeAttackskipIt &&
                    nextAttack[currentSlime][2] == slimeAttackskipIt)) {
                    nextAttack[currentSlime] = new byte[] { 0, 1, 2 };
                }

                WeightedRandom<int> weightedRandom = new(Main.rand);
                foreach (int j in nextAttack[currentSlime]) {
                    if (j == slimeAttackskipIt) {
                        continue;
                    }

                    weightedRandom.Add(j, j == 2 ? SpecialAttackWeight : 1f);
                }

                gotten = weightedRandom.Get();
                nextAttack[currentSlime][gotten] = slimeAttackskipIt;

                NPC.netUpdate = true;
            }

            return gotten;
        }

        /// <summary>
        /// Performs Goozma's slime spawning animation.
        /// </summary>
        /// 
        /// <remarks>
        /// This handles the spawning of the initial slime after his spawn animation, <i>and</i> the cycling of the active slime during his first phase.
        /// </remarks>
        public void DoBehavior_SpawnSlime()
        {
            // Slow down as the state progresses.
            NPC.velocity *= 0.9f;

            if (Main.netMode != NetmodeID.MultiplayerClient) {
                NPC.defense = 1000;
                NPC.takenDamageMultiplier = 0.1f;

                // Handle target repositioning at first, to ensure that Goozma is near the target when he spawns a slime.
                if (Time > 5 && Time < SpawnSlime_RedirectTime) {
                    // Acquire a new target and look at them.
                    NPC.TargetClosestUpgraded();
                    NPC.direction = NPC.DirectionTo(NPC.GetTargetData().Center).X > 0 ? 1 : -1;

                    // Smoothly attempt to approach the target.
                    Vector2 idealVelocity = NPC.DirectionTo(NPC.GetTargetData().Center) * Math.Max(NPC.Distance(NPC.GetTargetData().Center) - 150f, 0f) * 0.12f;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.1f);

                    // Jitter in place a bit.
                    NPC.position += Main.rand.NextVector2Circular(6, 6);
                    NPC.netUpdate = true;
                }

                // Kill the current slime, assuming it's present, in preparation of the summoning of the next one.

                // TODO -- There seems to be a separate line below that does this same sort of logic.
                // But I just started refactoring and I could be wrong. Might be a nuance in the AI, or just a desperate attempt at getting Goozma to work. Either way, figure that out. -Lucille
                if (Time > SpawnSlime_SummonDelay - 8f && Time <= SpawnSlime_SummonDelay && !(ActiveSlimeIndex < 0 || ActiveSlimeIndex >= Main.maxNPCs)) {
                    KillSlime(currentSlime);
                    NPC.netUpdate = true;
                }
            }

            // Create inward-sucking particles before
            if (Main.netMode != NetmodeID.Server && Time > 5 && Time < SpawnSlime_RedirectTime) {
                for (int i = 0; i < 3; i++) {
                    Vector2 inward = NPC.Center + Main.rand.NextVector2Circular(70, 70) + Main.rand.NextVector2CircularEdge(100 - Time, 100 - Time);

                    CalamityHunt.particles.Add(Particle.Create<ChromaticEnergyDust>(particle => {
                        particle.position = inward;
                        particle.velocity = inward.DirectionTo(NPC.Center) * Main.rand.NextFloat(3f);
                        particle.scale = 1f;
                        particle.color = Color.White;
                        particle.colorData = new ColorOffsetData(true, NPC.localAI[0]);
                    }));
                }
            }

            if (Time == SpawnSlime_SummonDelay) {
                NPC.velocity *= -1f;

                if (Main.netMode != NetmodeID.Server) {
                    for (int i = 0; i < 45; i++) {
                        Vector2 outward = NPC.Center + Main.rand.NextVector2Circular(10, 10);

                        CalamityHunt.particles.Add(Particle.Create<ChromaticEnergyDust>(particle => {
                            particle.position = outward;
                            particle.velocity = outward.DirectionFrom(NPC.Center) * Main.rand.NextFloat(3f, 10f);
                            particle.scale = Main.rand.NextFloat(1f, 2f);
                            particle.color = Color.White;
                            particle.colorData = new ColorOffsetData(true, NPC.localAI[0]);
                        }));
                    }
                }

                // Spawn some Goozmites as well in GFB, because why not?
                if (Main.getGoodWorld && Main.netMode != NetmodeID.MultiplayerClient) {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X - 100, (int)NPC.Center.Y, ModContent.NPCType<Goozmite>(), ai2: NPC.whoAmI);
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X + 100, (int)NPC.Center.Y, ModContent.NPCType<Goozmite>(), ai2: NPC.whoAmI);
                }

                // Choose the next slime type to summon.
                // In GFB, this is completely random. Otherwise, it is cyclic.
                currentSlime = (short)((currentSlime + 1) % SlimeTypes.Count);
                if (Main.zenithWorld) {
                    currentSlime = (short)Main.rand.Next(SlimeTypes.Count);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    int slimeAttack = GetSlimeAttack();

                    // If there is no active slime at the moment, create one. This accounts for the spawn animation.
                    if (ActiveSlimeIndex < 0 || ActiveSlimeIndex >= Main.maxNPCs || !ActiveSlime.active) {
                        ActiveSlimeIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y - 50, SlimeTypes[currentSlime], ai0: -50, ai1: slimeAttack, ai2: NPC.whoAmI);
                        ActiveSlime.velocity.Y -= 16f;
                    }
                    else {
                        Vector2 pos = ActiveSlime.Bottom;
                        ActiveSlime.active = false;
                        ActiveSlimeIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y, SlimeTypes[currentSlime], ai0: -50, ai1: slimeAttack, ai2: NPC.whoAmI);
                    }

                    SoundStyle spawnSound = AssetDirectory.Sounds.Goozma.SpawnSlime;
                    SoundEngine.PlaySound(spawnSound, NPC.Center);

                    NPC.netUpdate = true;
                }
            }

            // The slime has been summoned and enough time has passed. Roll to the next attack.
            // TODO -- "(int)CurrentState + 1"? This seems a bit hacky. -Lucille
            if (Time > SpawnSlime_SummonDelay + SpawnSlime_StateTransitionDelay) {
                SetAttack((int)CurrentState + 1, true);
            }
        }
    }
}
