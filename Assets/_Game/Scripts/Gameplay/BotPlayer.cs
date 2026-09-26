using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Фейк-игроки (как живые игроки в роблокс-режимах). У каждого своя база в скале.
    /// Они по-настоящему "играют": бегут по дороге в зоны за яйцами и несут их над головой к себе на грядки,
    /// качаются на беговых дорожках, гуляют по спавну, прыгают по клавиатуре, заходят к продавцу.
    /// С прогрессом игрока не взаимодействуют.
    /// </summary>
    public class BotPlayer : MonoBehaviour
    {
        static readonly Color[] Shirts = { new Color(0.1f, 0.1f, 0.12f), new Color(0.85f, 0.2f, 0.25f), new Color(0.2f, 0.6f, 0.95f), new Color(0.95f, 0.6f, 0.1f), new Color(0.55f, 0.3f, 0.85f), new Color(0.2f, 0.7f, 0.35f), Color.white };
        static readonly Color[] Pants = { new Color(0.12f, 0.15f, 0.2f), new Color(0.2f, 0.25f, 0.5f), new Color(0.3f, 0.3f, 0.32f), new Color(0.45f, 0.3f, 0.2f) };
        static readonly Color[] Skins = { new Color(0.96f, 0.8f, 0.25f), new Color(1f, 0.87f, 0.75f), new Color(0.85f, 0.65f, 0.5f), Color.white };
        static readonly Color[] Hairs = { new Color(0.55f, 0.3f, 0.12f), new Color(0.1f, 0.08f, 0.06f), new Color(0.95f, 0.8f, 0.35f), new Color(0.9f, 0.35f, 0.2f), new Color(0.4f, 0.4f, 0.45f) };

        const float Y = 0.6f; // высота верха клавиш пола

        enum Act { None, Walk, Wait, Treadmill, Grab, Plant, Jump }
        struct Step { public Act act; public Vector3 pos; public float time; }

        Blocky.Avatar av;
        Vector3 home, entrance;
        List<Vector3> plots;
        readonly Queue<Step> plan = new Queue<Step>();
        Step cur;
        float timer, phase, speed, yVel, yOff;
        Treadmill mill;
        GameObject carried;
        System.Random rnd;

        public static void SpawnAll(Transform parent, List<Vector3> homes, List<Vector3> entrances, List<List<Vector3>> plots)
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < homes.Count; i++)
            {
                var go = new GameObject("Bot_" + i);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(homes[i].x, Y, homes[i].z);
                var b = go.AddComponent<BotPlayer>();
                b.home = new Vector3(homes[i].x, Y, homes[i].z);
                b.entrance = i < entrances.Count ? new Vector3(entrances[i].x, Y, entrances[i].z) : b.home;
                b.plots = i < plots.Count ? plots[i] : new List<Vector3>();
                b.rnd = new System.Random(100 + i);
                b.av = Blocky.BuildAvatar(go.transform, Skins[rnd.Next(Skins.Length)], Shirts[(i * 2 + 1) % Shirts.Length], Pants[rnd.Next(Pants.Length)], 0.6f);
                Hair(b.av.head, Hairs[rnd.Next(Hairs.Length)], i % 3);
                b.speed = 13f + (float)rnd.NextDouble() * 5f;
                b.timer = i * 2f; // разный старт
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

        float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }
        void Go(Vector3 p) { plan.Enqueue(new Step { act = Act.Walk, pos = new Vector3(p.x, Y, p.z) }); }
        void Do(Act a, float t, Vector3 p = default(Vector3)) { plan.Enqueue(new Step { act = a, time = t, pos = p }); }

        /// <summary>Выбираем, чем заняться, и строим маршрут (выход из своей базы — через вход в скале).</summary>
        void PlanNext()
        {
            if (mill != null) { mill.BotOccupied = false; mill = null; }
            int roll = rnd.Next(100);
            var gm = GameManager.Instance;
            Vector3 gateIn = new Vector3(R(-4f, 4f), Y, GameConfig.BaseMaxZ - 7f);
            Vector3 gateOut = new Vector3(R(-4f, 4f), Y, GameConfig.BaseMaxZ + 8f);

            if (roll < 45 && gm != null && gm.Pedestals.Count > 0 && plots.Count > 0)
            {
                // за яйцом: база → ворота → дорога → яйцо → обратно → своя грядка
                int maxTier = Mathf.Min(3, gm.Pedestals.Count - 1);
                var ped = gm.Pedestals[rnd.Next(0, maxTier + 1)];
                Vector3 pp = ped.transform.position;
                Go(entrance); Go(gateIn); Go(gateOut);
                Go(new Vector3(R(-6f, 6f), Y, pp.z - 14f));
                Go(pp + new Vector3(R(-1.5f, 1.5f), 0, -3f));
                Do(Act.Grab, 0.6f, pp);
                Go(new Vector3(R(-6f, 6f), Y, pp.z - 14f));
                Go(gateOut); Go(gateIn); Go(entrance);
                var plot = plots[rnd.Next(plots.Count)];
                Go(plot + new Vector3(0, 0, 2.8f));
                Do(Act.Plant, 0.8f, plot);
                Do(Act.Wait, R(1f, 3f));
                return;
            }
            if (roll < 72)
            {
                // качаться на свободной беговой дорожке
                var free = new List<Treadmill>();
                foreach (var t in Treadmill.All) if (!t.BotOccupied && t.index <= 2) free.Add(t);
                if (free.Count > 0)
                {
                    mill = free[rnd.Next(free.Count)];
                    mill.BotOccupied = true;
                    Go(entrance);
                    Go(mill.transform.position - mill.transform.forward * 7f);
                    Go(mill.transform.position);
                    Do(Act.Treadmill, R(12f, 28f));
                    Go(mill.transform.position - mill.transform.forward * 7f);
                    return;
                }
            }
            if (roll < 85)
            {
                // прогулка по спавну и прыжки по клавишам
                Go(entrance);
                for (int i = 0; i < 3; i++) Go(new Vector3(R(-30f, 30f), Y, R(-40f, 0f)));
                Do(Act.Jump, R(2f, 4f));
                Go(entrance); Go(home);
                return;
            }
            if (roll < 92)
            {
                // к продавцу
                Go(entrance); Go(new Vector3(-37f, Y, -17f)); Do(Act.Wait, R(2f, 4f)); Go(entrance); Go(home);
                return;
            }
            // у себя на базе возле грядок
            Go(home + new Vector3(R(-4f, 4f), 0, R(-4f, 4f)));
            Do(Act.Wait, R(2f, 5f));
        }

        void NextStep()
        {
            if (plan.Count == 0) PlanNext();
            cur = plan.Count > 0 ? plan.Dequeue() : new Step { act = Act.Wait, time = 1f };
            timer = cur.time;
            if (cur.act == Act.Grab)
            {
                // яйцо над головой (визуально — настоящий пьедестал игрока не трогаем)
                if (carried != null) Destroy(carried);
                var ped = FindPedestal(cur.pos);
                carried = Blocky.BuildEgg(av.carryPoint, ped != null ? ped.tier : Tier.Common, 1.3f);
                carried.transform.localPosition = Vector3.zero;
                Blocky.NoShadows(carried);
                Fx.Burst(transform.position + Vector3.up * 3f, Color.white, 10, 3f, 0.3f, 0.5f);
            }
            else if (cur.act == Act.Plant && carried != null)
            {
                Destroy(carried);
                carried = null;
                Fx.Burst(cur.pos + Vector3.up * 1.2f, new Color(0.6f, 1f, 0.6f), 16, 4f, 0.4f, 0.6f);
            }
        }

        static EggPedestal FindPedestal(Vector3 p)
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            EggPedestal best = null; float bd = float.MaxValue;
            foreach (var e in gm.Pedestals) { float d = (e.transform.position - p).sqrMagnitude; if (d < bd) { bd = d; best = e; } }
            return best;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            float moveAmount = 0;

            if (cur.act == Act.Walk)
            {
                Vector3 d = cur.pos - transform.position; d.y = 0;
                if (d.magnitude < 0.5f) { transform.position = new Vector3(cur.pos.x, transform.position.y, cur.pos.z); NextStep(); }
                else
                {
                    float sp = carried != null ? speed * 1.1f : speed;
                    Vector3 step = d.normalized * Mathf.Min(sp * dt, d.magnitude);
                    var np = transform.position + step;
                    bool toMill = mill != null && (new Vector3(cur.pos.x, 0, cur.pos.z) - new Vector3(mill.transform.position.x, 0, mill.transform.position.z)).sqrMagnitude < 1f;
                    np.y = Mathf.MoveTowards(np.y, toMill ? mill.transform.position.y + 0.66f : Y, dt * 4f);
                    transform.position = np;
                    av.root.transform.rotation = Quaternion.Slerp(av.root.transform.rotation, Quaternion.LookRotation(d), dt * 10f);
                    moveAmount = 1f;
                }
            }
            else if (cur.act == Act.Treadmill && mill != null)
            {
                av.root.transform.rotation = Quaternion.Slerp(av.root.transform.rotation, mill.transform.rotation, dt * 8f);
                mill.BotRun();
                moveAmount = 1.4f;
                timer -= dt;
                if (timer <= 0) NextStep();
            }
            else if (cur.act == Act.Jump)
            {
                if (yOff <= 0 && yVel <= 0) yVel = 9f;
                timer -= dt;
                if (timer <= 0) NextStep();
            }
            else
            {
                timer -= dt;
                if (timer <= 0) NextStep();
            }

            if (yOff > 0 || yVel > 0)
            {
                yVel -= 30f * dt;
                yOff = Mathf.Max(0, yOff + yVel * dt);
                if (yOff <= 0) yVel = 0;
            }
            av.model.localPosition = new Vector3(0, yOff + Mathf.Abs(Mathf.Sin(phase)) * 0.1f * Mathf.Min(1, moveAmount), 0);

            phase += dt * 11f * (moveAmount > 0 ? moveAmount : 0);
            float swing = Mathf.Sin(phase) * 55f * Mathf.Min(1f, moveAmount);
            bool air = yOff > 0.05f;
            if (carried != null)
            {
                av.lArm.localRotation = Quaternion.Euler(180, 0, 12);
                av.rArm.localRotation = Quaternion.Euler(180, 0, -12);
            }
            else
            {
                av.lArm.localRotation = air ? Quaternion.Euler(165, 0, 15) : Quaternion.Euler(swing, 0, 0);
                av.rArm.localRotation = air ? Quaternion.Euler(165, 0, -15) : Quaternion.Euler(-swing, 0, 0);
            }
            av.lLeg.localRotation = Quaternion.Euler(air ? -25 : -swing, 0, 0);
            av.rLeg.localRotation = Quaternion.Euler(air ? 15 : swing, 0, 0);
        }
    }
}
