using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    public interface IInteractable
    {
        Vector3 InteractPos { get; }
        float InteractRange { get; }
        float HoldTime { get; }
        bool CanInteract { get; }
        string Prompt { get; }
        void Interact(PlayerController p);
    }

    public class CarriedEgg
    {
        public Tier tier;
        public int dragonId;
        public EggPedestal from;
        public GameObject visual;
    }

    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;
        public static readonly List<IInteractable> Interactables = new List<IInteractable>();

        public CharacterController cc;
        public Blocky.Avatar avatar;
        public Vector3 spawnPoint = new Vector3(0, 1, -4);

        public CarriedEgg Carrying;
        public IInteractable Current;
        public float HoldProgress;
        public bool OnTreadmill;
        public Treadmill CurrentTreadmill;

        Vector3 velocity;
        float animPhase;
        float facingYaw;
        IInteractable holdTarget;
        float invulnerable;
        float stepTimer;
        bool wasGrounded = true;
        float landSquash;
        float airTime;
        GameObject heldModel;
        ParticleSystem dust, speedLines;
        bool subscribed;

        void Start()
        {
            dust = Fx.Dust(transform);
            speedLines = Fx.SpeedLines(avatar.root.transform);
        }

        readonly List<GameObject> pets = new List<GameObject>();
        TrailRenderer trail;
        ParticleSystem trailSparks;
        float trailHue;

        /// <summary>Надетые драконы летают за игроком как питомцы (выбранный — чуть выше и ближе).</summary>
        public void RebuildHeld()
        {
            foreach (var p in pets) if (p != null) Destroy(p);
            pets.Clear();
            heldModel = null;
            var d = SaveManager.Data;
            for (int i = 0; i < d.dragonInv.Count; i++)
            {
                int id = d.dragonInv[i];
                if (id < 0) continue;
                var def = GameConfig.GetDragon(id);
                var go = Blocky.BuildDragon(null, def);
                go.transform.localScale = Vector3.one * 0.42f;
                go.transform.position = transform.position + Vector3.up * 3f;
                Blocky.NoShadows(go);
                var f = go.AddComponent<PetFollow>();
                f.owner = transform;
                f.slot = i;
                f.order = pets.Count;
                pets.Add(go);
                if (d.selectedSlot == i) heldModel = go;
            }
            foreach (var p in pets) p.GetComponent<PetFollow>().total = pets.Count;
        }

        /// <summary>Эффект при беге из магазина трейлов.</summary>
        public void RebuildTrail()
        {
            if (trail != null) Destroy(trail.gameObject);
            if (trailSparks != null) Destroy(trailSparks.gameObject);
            trail = null; trailSparks = null;
            int t = SaveManager.Data.equippedTrail;
            if (t < 0) return;
            var def = GameConfig.Trails[t];
            var go = new GameObject("Trail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1.2f, -0.3f);
            trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = Mats.Particle(null, def.sparkles);
            trail.time = 0.45f;
            trail.minVertexDistance = 0.2f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1.6f, 1f, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(def.a, 0f), new GradientColorKey(def.b, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
            if (def.sparkles) trailSparks = Fx.Sparkles(go.transform, Vector3.zero, def.a, 0.6f, 0f);
        }

        public static PlayerController Create(Vector3 spawn)
        {
            var go = new GameObject("Player");
            go.transform.position = spawn;
            var p = go.AddComponent<PlayerController>();
            p.spawnPoint = spawn;
            p.cc = go.AddComponent<CharacterController>();
            p.cc.height = 3.1f;
            p.cc.radius = 0.7f;
            p.cc.center = new Vector3(0, 1.55f, 0);
            p.cc.stepOffset = 0.6f;
            p.cc.slopeLimit = 50f;
            p.cc.skinWidth = 0.05f;
            p.avatar = Blocky.BuildAvatar(go.transform,
                new Color(0.96f, 0.8f, 0.25f), new Color(0.05f, 0.42f, 0.85f), new Color(0.35f, 0.62f, 0.2f), 0.6f);
            BotPlayer.Hair(p.avatar.head, new Color(0.6f, 0.33f, 0.12f), 0);
            Instance = p;
            return p;
        }

        public bool IsInvulnerable { get { return invulnerable > 0; } }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            if (invulnerable > 0) invulnerable -= dt;

            var gm = GameManager.Instance;
            float walkSpeed = gm != null ? gm.WalkSpeed : GameConfig.BaseWalkSpeed;
            float jumpPower = gm != null ? gm.JumpPower : GameConfig.BaseJump;

            if (!subscribed && gm != null) { gm.OnHeldChanged += RebuildHeld; gm.OnTrailChanged += RebuildTrail; subscribed = true; RebuildHeld(); RebuildTrail(); }
            UpdateTrail(dt, InputState.Move.sqrMagnitude > 0.05f && !InputState.Blocked);
            for (int k = 0; k < GameConfig.InventorySlots; k++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + k) && gm != null && !InputState.Blocked) gm.SelectSlot(k);

            // движение относительно камеры
            Vector2 mv = InputState.Blocked ? Vector2.zero : InputState.Move;
            float camYaw = CameraRig.Cam != null ? CameraRig.Cam.transform.eulerAngles.y : 0f;
            Quaternion yawRot = Quaternion.Euler(0, camYaw, 0);
            Vector3 wish = yawRot * new Vector3(mv.x, 0, mv.y);
            Vector3 horiz = wish * walkSpeed;

            // беговая дорожка
            DetectTreadmill();
            if (OnTreadmill && CurrentTreadmill != null)
            {
                Vector3 fwd = CurrentTreadmill.transform.forward;
                float along = Vector3.Dot(wish, fwd);
                if (along > 0.25f)
                {
                    horiz -= fwd * (along * walkSpeed); // лента гасит движение вперёд — бег на месте
                    CurrentTreadmill.Train(dt);
                }
            }
            bool training = OnTreadmill && CurrentTreadmill != null && CurrentTreadmill.Training;
            Fx.SetRate(speedLines, training ? 40f : 0f);

            bool grounded = cc.isGrounded;
            if (grounded && velocity.y < 0) velocity.y = -2f;
            if (InputState.JumpPressed && grounded && !InputState.Blocked)
            {
                velocity.y = jumpPower;
                GameAudio.Play(Sfx.Jump);
            }
            velocity.y += Physics.gravity.y * 2.2f * dt;
            velocity.x = horiz.x; velocity.z = horiz.z;
            cc.Move(velocity * dt);

            // приземление: звук, пыль, "сплющивание"
            if (grounded && !wasGrounded && airTime > 0.25f)
            {
                GameAudio.Play(Sfx.Land, 0.7f);
                landSquash = 1f;
                if (dust != null) dust.Emit(8);
            }
            airTime = grounded ? 0 : airTime + dt;
            wasGrounded = grounded;
            Fx.SetRate(dust, grounded && mv.sqrMagnitude > 0.1f && walkSpeed > 18f ? Mathf.Min(30f, walkSpeed * 0.6f) : 0f);

            if (transform.position.y < -40f) Respawn();

            // поворот модели по направлению движения
            if (wish.sqrMagnitude > 0.01f)
                facingYaw = Mathf.MoveTowardsAngle(facingYaw, Mathf.Atan2(wish.x, wish.z) * Mathf.Rad2Deg, 720f * dt);
            avatar.root.transform.rotation = Quaternion.Euler(0, facingYaw, 0);

            Animate(dt, mv.magnitude * walkSpeed, grounded);

            // яйцо засчитывается только когда игрок донёс его внутрь базы
            if (Carrying != null && gm != null && transform.position.z < GameConfig.BaseMaxZ - 1f
                && Mathf.Abs(transform.position.x) < WorldBuilder.BaseHalfWidth)
            {
                gm.DepositEgg(Carrying);
                DropCarried(false);
            }

            UpdateInteraction(dt);
        }

        void UpdateTrail(float dt, bool moving)
        {
            if (trail == null) return;
            trail.emitting = moving;
            int t = SaveManager.Data.equippedTrail;
            if (t >= 0 && GameConfig.Trails[t].rainbow)
            {
                trailHue = Mathf.Repeat(trailHue + dt * 0.5f, 1f);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.HSVToRGB(trailHue, 0.8f, 1f), 0f), new GradientColorKey(Color.HSVToRGB(Mathf.Repeat(trailHue + 0.3f, 1f), 0.8f, 1f), 1f) },
                          new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = g;
            }
            Fx.SetRate(trailSparks, moving ? 20f : 0f);
        }

        void DetectTreadmill()
        {
            OnTreadmill = false;
            CurrentTreadmill = null;
            foreach (var t in Treadmill.All)
            {
                if (t.Contains(transform.position))
                {
                    CurrentTreadmill = t;
                    OnTreadmill = t.Owned;
                    return;
                }
            }
        }

        void Animate(float dt, float speed, bool grounded)
        {
            float t = Time.time;
            float amount = Mathf.Clamp01(speed / 10f);
            float freq = Mathf.Lerp(7f, 15f, Mathf.Clamp01(speed / 50f));
            animPhase += dt * freq * (amount > 0.05f ? 1 : 0);
            float swing = Mathf.Sin(animPhase) * 60f * amount;
            bool holding = false; // драконы теперь летают рядом как питомцы

            // руки
            Quaternion lArm, rArm;
            if (Carrying != null)
            {
                lArm = Quaternion.Euler(180, 0, 12);
                rArm = Quaternion.Euler(180, 0, -12);
            }
            else if (!grounded)
            {
                lArm = Quaternion.Euler(165, 0, 15);
                rArm = holding ? Quaternion.Euler(-80, 0, 0) : Quaternion.Euler(165, 0, -15);
            }
            else
            {
                float idle = Mathf.Sin(t * 2f) * 3f * (1f - amount);
                lArm = Quaternion.Euler(swing + idle, 0, -2f);
                rArm = holding ? Quaternion.Euler(-80 + swing * 0.15f, 0, 0) : Quaternion.Euler(-swing - idle, 0, 2f);
            }
            float blend = 1f - Mathf.Exp(-dt * 18f);
            avatar.lArm.localRotation = Quaternion.Slerp(avatar.lArm.localRotation, lArm, blend);
            avatar.rArm.localRotation = Quaternion.Slerp(avatar.rArm.localRotation, rArm, blend);

            // ноги (в прыжке — "ножницы" как в Роблоксе)
            Quaternion lLeg = grounded ? Quaternion.Euler(-swing, 0, 0) : Quaternion.Euler(-25, 0, 0);
            Quaternion rLeg = grounded ? Quaternion.Euler(swing, 0, 0) : Quaternion.Euler(15, 0, 0);
            avatar.lLeg.localRotation = Quaternion.Slerp(avatar.lLeg.localRotation, lLeg, blend);
            avatar.rLeg.localRotation = Quaternion.Slerp(avatar.rLeg.localRotation, rLeg, blend);

            // тело: покачивание при беге, дыхание в покое, наклон вперёд на скорости, сплющивание при приземлении
            landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 5f);
            float bob = grounded ? Mathf.Abs(Mathf.Sin(animPhase)) * 0.12f * amount : 0f;
            float breathe = Mathf.Sin(t * 2.2f) * 0.015f * (1f - amount);
            float lean = Mathf.Clamp(speed / 60f, 0f, 1f) * 12f;
            avatar.model.localPosition = new Vector3(0, bob - landSquash * 0.1f, 0);
            avatar.model.localRotation = Quaternion.Euler(lean, 0, 0);
            float sq = 1f - landSquash * 0.12f;
            avatar.model.localScale = new Vector3(0.6f * (2f - sq), 0.6f * (sq + breathe), 0.6f * (2f - sq));
            if (avatar.head != null) avatar.head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f) * 2f * (1f - amount), Mathf.Sin(t * 0.6f) * 6f * (1f - amount), 0);

            if (grounded && amount > 0.1f)
            {
                stepTimer -= dt * Mathf.Lerp(1f, 3.2f, Mathf.Clamp01(speed / 50f));
                if (stepTimer <= 0) { stepTimer = 0.3f; GameAudio.Play(Sfx.Step, 0.35f); }
            }
        }

        void UpdateInteraction(float dt)
        {
            IInteractable best = null;
            float bestD = float.MaxValue;
            Vector3 p = transform.position;
            for (int i = 0; i < Interactables.Count; i++)
            {
                var it = Interactables[i];
                if (it == null || !it.CanInteract) continue;
                float d = (it.InteractPos - p).sqrMagnitude;
                if (d < it.InteractRange * it.InteractRange && d < bestD) { bestD = d; best = it; }
            }
            Current = best;

            if (best != null && InputState.ActionHeld)
            {
                if (holdTarget != best) { holdTarget = best; HoldProgress = 0; }
                // на телефоне — мгновенно по одному тапу, на ПК — короткое удержание E
                HoldProgress += (InputState.Mobile || best.HoldTime <= 0) ? 1f : dt / best.HoldTime;
                if (HoldProgress >= 1f)
                {
                    HoldProgress = 0;
                    holdTarget = null;
                    best.Interact(this);
                    InputState.TouchAction = false; // одно действие на одно нажатие
                }
            }
            else
            {
                HoldProgress = 0;
                holdTarget = null;
            }
        }

        public void PickUp(CarriedEgg egg)
        {
            Carrying = egg;
            egg.visual = Blocky.BuildEgg(avatar.carryPoint, egg.tier, 1.4f);
            egg.visual.transform.localPosition = Vector3.zero;
            Blocky.NoShadows(egg.visual);
            GameAudio.Play(Sfx.Grab);
        }

        /// <summary>returnToPedestal — если поймали, яйцо возвращается охране.</summary>
        public void DropCarried(bool returnToPedestal)
        {
            if (Carrying == null) return;
            if (Carrying.visual != null) Destroy(Carrying.visual);
            if (returnToPedestal && Carrying.from != null) Carrying.from.ReturnEgg();
            Carrying = null;
        }

        public void Caught(string byName)
        {
            if (invulnerable > 0) return;
            DropCarried(true);
            GameAudio.Play(Sfx.Caught);
            if (UIManager.Instance != null) UIManager.Instance.Toast(Loc.F("caught", byName), new Color(1f, 0.4f, 0.4f));
            Respawn();
            var rig = CameraRig.Cam != null ? CameraRig.Cam.GetComponent<CameraRig>() : null;
            if (rig != null) rig.Shake(0.6f);
            if (GameManager.Instance != null) GameManager.Instance.OnCaught();
        }

        public void Respawn()
        {
            cc.enabled = false;
            transform.position = spawnPoint;
            cc.enabled = true;
            velocity = Vector3.zero;
            invulnerable = 2f;
        }
    }
}

namespace DragonHeist
{
    /// <summary>Дракон-питомец: летит за игроком полукругом, покачивается, выбранный — ближе и выше.</summary>
    public class PetFollow : MonoBehaviour
    {
        public Transform owner;
        public int slot, order, total = 1;
        float seed;

        void Start() { seed = Random.value * 10f; }

        void LateUpdate()
        {
            if (owner == null) { Destroy(gameObject); return; }
            bool selected = SaveManager.Data.selectedSlot == slot;
            float spread = total <= 1 ? 0f : (order / (float)(total - 1) - 0.5f) * 2f; // -1..1
            float yaw = owner.GetComponent<PlayerController>().avatar.root.transform.eulerAngles.y;
            Quaternion rot = Quaternion.Euler(0, yaw, 0);
            Vector3 offset = rot * new Vector3(spread * 3.2f, 0, -2.6f - Mathf.Abs(spread) * 0.8f);
            float bob = Mathf.Sin(Time.time * 3f + seed) * 0.35f;
            Vector3 target = owner.position + offset + Vector3.up * ((selected ? 3.6f : 2.6f) + bob);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-Time.deltaTime * 6f));
            Vector3 look = owner.position + rot * Vector3.forward * 5f - transform.position; look.y = 0;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
            transform.localScale = Vector3.one * (selected ? 0.52f : 0.42f);
        }
    }
}
