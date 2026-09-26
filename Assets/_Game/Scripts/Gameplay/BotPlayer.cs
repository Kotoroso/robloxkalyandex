using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Фейк-игроки для живости спавна (как в роблокс-режимах): бегают по дорожкам, гуляют по базе,
    /// топчут ASMR-клавиатуру, крутятся у грядок и продавца. С системами игры не взаимодействуют.
    /// </summary>
    public class BotPlayer : MonoBehaviour
    {
        static readonly string[] Names =
        {
            "Kirill2012", "noob_master", "DragonHunter", "Sasha_PRO", "xX_Tima_Xx", "Egorka777", "MegaVanya",
            "LizaPlays", "Dima_Roblox", "KOTIK_228", "Artem_Top", "Nastya2011", "BrainrotFan", "Maks_YT"
        };
        static readonly Color[] Shirts = { new Color(0.1f, 0.1f, 0.12f), new Color(0.85f, 0.2f, 0.25f), new Color(0.2f, 0.6f, 0.95f), new Color(0.95f, 0.6f, 0.1f), new Color(0.55f, 0.3f, 0.85f), new Color(0.2f, 0.7f, 0.35f), Color.white };
        static readonly Color[] Pants = { new Color(0.12f, 0.15f, 0.2f), new Color(0.2f, 0.25f, 0.5f), new Color(0.3f, 0.3f, 0.32f), new Color(0.45f, 0.3f, 0.2f) };
        static readonly Color[] Skins = { new Color(0.96f, 0.8f, 0.25f), new Color(1f, 0.87f, 0.75f), new Color(0.85f, 0.65f, 0.5f), Color.white };
        static readonly Color[] Hairs = { new Color(0.55f, 0.3f, 0.12f), new Color(0.1f, 0.08f, 0.06f), new Color(0.95f, 0.8f, 0.35f), new Color(0.9f, 0.35f, 0.2f), new Color(0.4f, 0.4f, 0.45f) };

        enum Mode { Walk, Idle, Treadmill, Jump }

        Blocky.Avatar av;
        Mode mode;
        Vector3 target;
        float timer, phase, speed, yVel, yOff;
        Treadmill mill;
        System.Random rnd;

        Vector3 home;

        /// <summary>3 фейк-игрока без ников, у каждого своё стойло (дом).</summary>
        public static void SpawnAll(Transform parent, List<Vector3> homes)
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < homes.Count; i++)
            {
                var go = new GameObject("Bot_" + i);
                go.transform.SetParent(parent, false);
                go.transform.position = homes[i];
                var b = go.AddComponent<BotPlayer>();
                b.home = homes[i];
                b.rnd = new System.Random(100 + i);
                b.av = Blocky.BuildAvatar(go.transform, Skins[rnd.Next(Skins.Length)], Shirts[(i * 2 + 1) % Shirts.Length], Pants[rnd.Next(Pants.Length)], 0.6f);
                Hair(b.av.head, Hairs[rnd.Next(Hairs.Length)], i % 3);
                b.speed = 10f + (float)rnd.NextDouble() * 5f;
                b.PickTask();
            }
        }

        /// <summary>Причёска из блоков (как в Роблоксе): 0 — чёлка, 1 — ёжик, 2 — кепка.</summary>
        public static void Hair(Transform head, Color c, int style)
        {
            var m = Mats.Plastic(c);
            Blocky.Round = true; Blocky.RoundFactor = 0.35f;
            if (style == 2)
            {
                Blocky.Part(head, new Vector3(0, 0.62f, 0), new Vector3(1.32f, 0.3f, 1.32f), m);
                Blocky.Part(head, new Vector3(0, 0.52f, 0.75f), new Vector3(1.1f, 0.1f, 0.6f), m);
            }
            else
            {
                Blocky.Part(head, new Vector3(0, 0.6f, -0.05f), new Vector3(1.36f, 0.4f, 1.36f), m);
                Blocky.Part(head, new Vector3(0, 0.25f, -0.6f), new Vector3(1.36f, 0.8f, 0.3f), m);
                if (style == 0) Blocky.Part(head, new Vector3(0.25f, 0.45f, 0.62f), new Vector3(0.8f, 0.3f, 0.2f), m);
                else for (int i = 0; i < 4; i++) Blocky.Part(head, new Vector3(-0.45f + i * 0.3f, 0.85f, 0), new Vector3(0.22f, 0.35f, 0.9f), m);
            }
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
        }

        static Vector3 RandomBasePoint(System.Random r)
        {
            float x = -30f + (float)r.NextDouble() * 60f;
            float z = GameConfig.BaseMinZ + 4f + (float)r.NextDouble() * (GameConfig.BaseMaxZ - GameConfig.BaseMinZ - 8f);
            return new Vector3(x, 0.6f, z);
        }

        void PickTask()
        {
            if (mill != null) { mill.BotOccupied = false; mill = null; }
            int roll = rnd.Next(100);
            if (roll < 40)
            {
                // на свободную открытую дорожку
                var free = new List<Treadmill>();
                foreach (var t in Treadmill.All) if (!t.BotOccupied && t.index <= 2) free.Add(t);
                if (free.Count > 0)
                {
                    mill = free[rnd.Next(free.Count)];
                    mill.BotOccupied = true;
                    mode = Mode.Walk;
                    target = mill.transform.position + new Vector3(0, 0.6f, 0);
                    timer = 999f;
                    return;
                }
            }
            if (roll < 55) { mode = Mode.Walk; target = home + new Vector3((float)rnd.NextDouble() * 10f - 5f, 0f, (float)rnd.NextDouble() * 10f - 5f); timer = 20f; }
            else if (roll < 70) { mode = Mode.Walk; target = RandomBasePoint(rnd); timer = 20f; }
            else if (roll < 85) { mode = Mode.Idle; timer = 2f + (float)rnd.NextDouble() * 4f; }
            else { mode = Mode.Jump; timer = 3f + (float)rnd.NextDouble() * 3f; }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            float moveAmount = 0;
            timer -= dt;

            if (mode == Mode.Walk)
            {
                Vector3 d = target - transform.position; d.y = 0;
                if (d.magnitude < 0.6f)
                {
                    if (mill != null) { mode = Mode.Treadmill; timer = 10f + (float)rnd.NextDouble() * 20f; transform.position = target; }
                    else PickTask();
                }
                else
                {
                    Vector3 step = d.normalized * Mathf.Min(speed * dt, d.magnitude);
                    var np = transform.position + step;
                    np.y = Mathf.MoveTowards(np.y, target.y, dt * 4f);
                    transform.position = np;
                    av.root.transform.rotation = Quaternion.Slerp(av.root.transform.rotation, Quaternion.LookRotation(d), dt * 8f);
                    moveAmount = 1f;
                }
                if (timer <= 0) PickTask();
            }
            else if (mode == Mode.Treadmill)
            {
                // бег на месте по ленте
                av.root.transform.rotation = Quaternion.Slerp(av.root.transform.rotation, mill.transform.rotation, dt * 8f);
                mill.BotRun();
                moveAmount = 1.4f;
                if (timer <= 0) PickTask();
            }
            else if (mode == Mode.Jump)
            {
                if (yOff <= 0 && yVel <= 0) yVel = 9f;
                if (timer <= 0) PickTask();
            }
            else if (timer <= 0) PickTask();

            // прыжок/гравитация
            if (yOff > 0 || yVel > 0)
            {
                yVel -= 30f * dt;
                yOff = Mathf.Max(0, yOff + yVel * dt);
                if (yOff <= 0) yVel = 0;
            }
            av.model.localPosition = new Vector3(0, yOff + Mathf.Abs(Mathf.Sin(phase)) * 0.1f * Mathf.Min(1, moveAmount), 0);

            // анимация конечностей
            phase += dt * 11f * (moveAmount > 0 ? moveAmount : 0);
            float swing = Mathf.Sin(phase) * 55f * Mathf.Min(1f, moveAmount);
            bool air = yOff > 0.05f;
            av.lArm.localRotation = air ? Quaternion.Euler(165, 0, 15) : Quaternion.Euler(swing, 0, 0);
            av.rArm.localRotation = air ? Quaternion.Euler(165, 0, -15) : Quaternion.Euler(-swing, 0, 0);
            av.lLeg.localRotation = Quaternion.Euler(air ? -25 : -swing, 0, 0);
            av.rLeg.localRotation = Quaternion.Euler(air ? 15 : swing, 0, 0);
        }
    }
}
