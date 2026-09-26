using UnityEngine;

namespace DragonHeist
{
    /// <summary>Брейнрот-охранник: патрулирует вокруг яйца, гонится за игроком, ловит и отправляет на базу.</summary>
    public class BrainrotGuard : MonoBehaviour
    {
        static readonly string[] NamesRu = { "Тун Тун Сахур", "Лирили Ларила", "Бомбардиро Крокодило", "Тралалеро Тралала", "Брр Брр Патапим", "Капучино Ассасино", "Ла Вака Сатурно" };
        static readonly string[] NamesEn = { "Tung Tung Sahur", "Lirili Larila", "Bombardiro Crocodilo", "Tralalero Tralala", "Brr Brr Patapim", "Cappuccino Assassino", "La Vaca Saturno" };

        enum State { Sleep, Patrol, Chase, Return }

        public EggPedestal home;
        public TierInfo info;
        Blocky.Guard model;
        State state = State.Sleep;
        Label3D zzz;
        float wakeTimer, idleTimer;
        Vector3 patrolTarget;
        float patrolWait;
        float animPhase;
        float speedNow;
        float catchRadius;
        float cooldown;
        BotPlayer botTarget; // фейк-игрок с нашим яйцом

        /// <summary>Высота пола в зонах (верх клавиш клавиатурного пола).</summary>
        public const float GroundY = 0.58f; // верх клавиш пола (0.13 + 0.45)

        public string DisplayName { get { return Loc.Ru ? NamesRu[(int)info.guardKind] : NamesEn[(int)info.guardKind]; } }

        public static BrainrotGuard Build(Transform parent, EggPedestal home, TierInfo info, Vector3 pos)
        {
            var go = new GameObject("Guard_" + info.guardKind);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, GroundY, pos.z);
            var g = go.AddComponent<BrainrotGuard>();
            g.home = home;
            g.info = info;
            g.model = Blocky.BuildBrainrot(go.transform, info.guardKind, info.guardScale);
            g.catchRadius = 1.4f + info.guardScale * 0.7f;
            Blocky.Label(go.transform, g.DisplayName, new Vector3(0, 6.2f * info.guardScale, 0), 0.9f, Color.white);
            g.zzz = Blocky.Label(go.transform, "Z z z", new Vector3(0.8f, 5f * info.guardScale, 0), 1.1f, new Color(0.55f, 0.8f, 1f));
            g.zzz.maxDistance = 60f;
            g.PickPatrolPoint();
            home.guards.Add(g);
            return g;
        }

        void PickPatrolPoint()
        {
            Vector2 r = Random.insideUnitCircle * 11f;
            patrolTarget = home.transform.position + new Vector3(r.x, 0, r.y);
            patrolTarget.y = 0;
            patrolWait = Random.Range(0.3f, 1.5f);
        }

        /// <summary>Яйцо украли — брейнрот просыпается и бежит за вором.</summary>
        public void Alert()
        {
            var p = PlayerController.Instance;
            if (state == State.Sleep)
            {
                wakeTimer = 0.45f; // короткая пауза "проснулся!"
                zzz.text = "!";
                zzz.color = new Color(1f, 0.3f, 0.2f);
                GameAudio.Play(Sfx.Laugh, 0.7f);
            }
            idleTimer = 0;
            if (p != null && ((p.Carrying != null && p.Carrying.from == home) || Flat(p.transform.position - home.transform.position).magnitude < GameConfig.LeashRadius + 10f))
                state = State.Chase;
            else state = State.Patrol;
        }

        /// <summary>Фейк-игрок украл наше яйцо — гонимся за ним до мирной зоны (если не заняты настоящим игроком).</summary>
        public void AlertBot(BotPlayer bot)
        {
            if (state == State.Chase && botTarget == null) return; // уже гонимся за игроком
            if (state == State.Sleep)
            {
                wakeTimer = 0.45f;
                zzz.text = "!";
                zzz.color = new Color(1f, 0.3f, 0.2f);
            }
            idleTimer = 0;
            botTarget = bot;
            state = State.Chase;
        }

        void GoToSleep()
        {
            state = State.Sleep;
            zzz.text = "Z z z";
            zzz.color = new Color(0.55f, 0.8f, 1f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            if (cooldown > 0) cooldown -= dt;
            var p = PlayerController.Instance;

            // сон: сидит, дышит, над головой Z z z
            if (state == State.Sleep)
            {
                float tt = Time.time + transform.position.x;
                model.model.localPosition = new Vector3(0, -0.35f * info.guardScale, 0);
                model.model.localRotation = Quaternion.Euler(18f + Mathf.Sin(tt * 1.5f) * 3f, 0, 0);
                model.model.localScale = Vector3.one * info.guardScale * (1f + Mathf.Sin(tt * 1.5f) * 0.03f);
                zzz.transform.localPosition = new Vector3(0.8f, 5f * info.guardScale + Mathf.Repeat(tt * 0.6f, 1f) * 1.2f, 0);
                return;
            }
            if (wakeTimer > 0)
            {
                wakeTimer -= dt;
                model.model.localRotation = Quaternion.identity;
                model.model.localScale = Vector3.one * info.guardScale * (1f + wakeTimer * 0.4f);
                model.model.localPosition = new Vector3(0, Mathf.Sin(wakeTimer * 7f) * 0.6f, 0);
                if (wakeTimer <= 0) zzz.text = "";
                return;
            }
            model.model.localRotation = Quaternion.identity;
            model.model.localScale = Vector3.one * info.guardScale;
            Vector3 pos = transform.position;
            Vector3 homePos = home.transform.position;
            Vector3 target = pos;
            float wantSpeed = 0;

            if (p != null)
            {
                Vector3 pp = p.transform.position;
                float playerFromHome = Flat(pp - homePos).magnitude;
                float playerDist = Flat(pp - pos).magnitude;
                bool playerInBase = pp.z < GameConfig.BaseMaxZ;
                float leash = GameConfig.LeashRadius + (int)info.tier * 1.5f;

                if (state != State.Chase && !playerInBase && !p.IsInvulnerable &&
                    (playerDist < info.aggroRadius * 0.6f || (p.Carrying != null && p.Carrying.from == home)))
                { state = State.Chase; botTarget = null; } // настоящий игрок важнее бота
                // вор далеко и яйцо на месте — через 8 сек снова засыпает
                if (state == State.Patrol && playerFromHome > leash) { idleTimer += dt; if (idleTimer > 8f) { GoToSleep(); return; } }
                else if (state == State.Patrol) idleTimer = 0;

                // вор с НАШИМ яйцом — гонимся до самого выхода (до базы), без поводка
                bool thief = p.Carrying != null && p.Carrying.from == home;
                if (botTarget == null && state == State.Chase && (playerInBase || p.IsInvulnerable || (!thief && playerFromHome > leash)))
                    state = State.Return;

                if (state == State.Chase && botTarget == null)
                {
                    target = pp;
                    wantSpeed = info.guardSpeed;
                    if (cooldown <= 0 && playerDist < catchRadius && Mathf.Abs(pp.y - pos.y) < 3.5f * info.guardScale)
                    {
                        cooldown = 1f;
                        p.Caught(DisplayName);
                        GameAudio.Play(Sfx.Laugh);
                        state = State.Return;
                    }
                }
            }

            // погоня за фейк-игроком: до мирной зоны, потом назад к яйцу
            if (state == State.Chase && botTarget != null)
            {
                if (!botTarget.IsThiefOf(home) || botTarget.InSafeZone) { botTarget = null; state = State.Return; }
                else
                {
                    Vector3 bp = botTarget.transform.position;
                    target = bp;
                    wantSpeed = info.guardSpeed * 0.85f;
                    if (cooldown <= 0 && Flat(bp - pos).magnitude < catchRadius)
                    {
                        cooldown = 1f;
                        botTarget.Caught();
                        botTarget = null;
                        state = State.Return;
                    }
                }
            }
            else if (state == State.Chase && p == null) state = State.Return;

            if (state == State.Return)
            {
                target = homePos;
                wantSpeed = info.guardSpeed * 0.6f;
                if (Flat(pos - homePos).magnitude < 4f) { state = State.Patrol; PickPatrolPoint(); }
            }
            else if (state == State.Patrol)
            {
                if (Flat(patrolTarget - pos).magnitude < 0.8f)
                {
                    patrolWait -= dt;
                    if (patrolWait <= 0) PickPatrolPoint();
                }
                else
                {
                    target = patrolTarget;
                    wantSpeed = Mathf.Max(4f, info.guardSpeed * 0.3f);
                }
            }

            speedNow = Mathf.MoveTowards(speedNow, wantSpeed, dt * 60f);
            Vector3 dir = Flat(target - pos);
            if (dir.sqrMagnitude > 0.01f)
            {
                Vector3 step = dir.normalized * Mathf.Min(speedNow * dt, dir.magnitude);
                transform.position = new Vector3(pos.x + step.x, GroundY, pos.z + step.z);
                Quaternion look = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 540f * dt);
            }
            else speedNow = 0;

            Animate(dt);
        }

        void Animate(float dt)
        {
            float amount = Mathf.Clamp01(speedNow / 8f);
            animPhase += dt * Mathf.Lerp(5f, 16f, Mathf.Clamp01(speedNow / 40f));
            float swing = Mathf.Sin(animPhase) * 45f * amount;
            if (model.legs.Length > 1 && model.legs[0] != null)
            {
                model.legs[0].localRotation = Quaternion.Euler(swing, 0, 0);
                model.legs[1].localRotation = Quaternion.Euler(-swing, 0, 0);
            }
            if (model.arms.Length > 1)
            {
                // тянет руки к игроку (у коровы "руки" — это задние ноги)
                float armX = (state == State.Chase && info.guardKind != BrainrotKind.VacaSaturno) ? -70f : 0f;
                model.arms[0].localRotation = Quaternion.Euler(armX - swing, 0, 0);
                model.arms[1].localRotation = Quaternion.Euler(armX + swing, 0, 0);
            }
            model.model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(animPhase)) * 0.25f * amount, 0);
        }
    }
}
