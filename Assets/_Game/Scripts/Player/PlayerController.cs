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

            // движение относительно камеры
            Vector2 mv = InputState.Move;
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

            bool grounded = cc.isGrounded;
            if (grounded && velocity.y < 0) velocity.y = -2f;
            if (InputState.JumpPressed && grounded)
            {
                velocity.y = jumpPower;
                GameAudio.Play(Sfx.Jump);
            }
            velocity.y += Physics.gravity.y * 2.2f * dt;
            velocity.x = horiz.x; velocity.z = horiz.z;
            cc.Move(velocity * dt);

            if (transform.position.y < -40f) Respawn();

            // поворот модели по направлению движения
            if (wish.sqrMagnitude > 0.01f)
                facingYaw = Mathf.MoveTowardsAngle(facingYaw, Mathf.Atan2(wish.x, wish.z) * Mathf.Rad2Deg, 720f * dt);
            avatar.root.transform.rotation = Quaternion.Euler(0, facingYaw, 0);

            Animate(dt, mv.magnitude * walkSpeed, grounded);

            if (Carrying != null && transform.position.z < GameConfig.BaseMaxZ && gm != null)
            {
                gm.DepositEgg(Carrying);
                DropCarried(false);
            }

            UpdateInteraction(dt);
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
            float amount = Mathf.Clamp01(speed / 10f);
            animPhase += dt * Mathf.Lerp(6f, 14f, Mathf.Clamp01(speed / 40f)) * (amount > 0.05f ? 1 : 0);
            float swing = Mathf.Sin(animPhase) * 55f * amount;
            if (!grounded) swing = 0;

            if (Carrying != null)
            {
                // держим яйцо над головой
                avatar.lArm.localRotation = Quaternion.Euler(180, 0, 12);
                avatar.rArm.localRotation = Quaternion.Euler(180, 0, -12);
            }
            else if (!grounded)
            {
                avatar.lArm.localRotation = Quaternion.Euler(170, 0, 0);
                avatar.rArm.localRotation = Quaternion.Euler(170, 0, 0);
            }
            else
            {
                avatar.lArm.localRotation = Quaternion.Euler(swing, 0, 0);
                avatar.rArm.localRotation = Quaternion.Euler(-swing, 0, 0);
            }
            avatar.lLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            avatar.rLeg.localRotation = Quaternion.Euler(swing, 0, 0);

            if (grounded && amount > 0.1f)
            {
                stepTimer -= dt * Mathf.Lerp(1f, 3f, Mathf.Clamp01(speed / 40f));
                if (stepTimer <= 0) { stepTimer = 0.32f; GameAudio.Play(Sfx.Step, 0.25f); }
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
                HoldProgress += best.HoldTime <= 0 ? 1f : dt / best.HoldTime;
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
