using UnityEngine;

namespace DragonHeist
{
    /// <summary>Брейнрот-охранник: патрулирует вокруг яйца, гонится за игроком, ловит и отправляет на базу.</summary>
    public class BrainrotGuard : MonoBehaviour
    {
        static readonly string[] NamesRu = { "Тун Тун Сахур", "Лирили Ларила", "Бомбардиро Крокодило", "Тралалеро Тралала", "Брр Брр Патапим", "Капучино Ассасино", "Ла Вака Сатурно" };
        static readonly string[] NamesEn = { "Tung Tung Sahur", "Lirili Larila", "Bombardiro Crocodilo", "Tralalero Tralala", "Brr Brr Patapim", "Cappuccino Assassino", "La Vaca Saturno" };

        enum State { Patrol, Chase, Return }

        public EggPedestal home;
        public TierInfo info;
        Blocky.Guard model;
        State state = State.Patrol;
        Vector3 patrolTarget;
        float patrolWait;
        float animPhase;
        float speedNow;
        float catchRadius;
        float cooldown;

        public string DisplayName { get { return Loc.Ru ? NamesRu[(int)info.guardKind] : NamesEn[(int)info.guardKind]; } }

        public static BrainrotGuard Build(Transform parent, EggPedestal home, TierInfo info, Vector3 pos)
        {
            var go = new GameObject("Guard_" + info.guardKind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var g = go.AddComponent<BrainrotGuard>();
            g.home = home;
            g.info = info;
            g.model = Blocky.BuildBrainrot(go.transform, info.guardKind, info.guardScale);
            g.catchRadius = 1.4f + info.guardScale * 0.7f;
            Blocky.Label(go.transform, g.DisplayName, new Vector3(0, 6.2f * info.guardScale, 0), 0.9f, Color.white);
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

        public void Alert()
        {
            var p = PlayerController.Instance;
            if (p != null && Flat(p.transform.position - home.transform.position).magnitude < GameConfig.LeashRadius)
                state = State.Chase;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            if (cooldown > 0) cooldown -= dt;
            var p = PlayerController.Instance;
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
                    (playerDist < info.aggroRadius || (p.Carrying != null && p.Carrying.from == home && playerFromHome < leash)))
                    state = State.Chase;

                if (state == State.Chase && (playerFromHome > leash || playerInBase || p.IsInvulnerable))
                    state = State.Return;

                if (state == State.Chase)
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
                transform.position = new Vector3(pos.x + step.x, 0, pos.z + step.z);
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
