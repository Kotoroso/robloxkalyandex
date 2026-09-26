using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Пьедестал с яйцом дракона в зоне тира. Охраняется брейнротами.</summary>
    public class EggPedestal : MonoBehaviour, IInteractable
    {
        public Tier tier;
        public readonly List<BrainrotGuard> guards = new List<BrainrotGuard>();
        GameObject egg;
        float respawnTimer;
        bool hasEgg;
        Transform eggAnchor;

        public static EggPedestal Build(Transform parent, Tier tier, Vector3 pos)
        {
            var go = new GameObject("Pedestal_" + tier);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var p = go.AddComponent<EggPedestal>();
            p.tier = tier;
            var info = GameConfig.GetTier(tier);
            var tr = go.transform;
            Blocky.Part(tr, new Vector3(0, 0.5f, 0), new Vector3(4f, 1f, 4f), Mats.Studs(new Color(0.35f, 0.35f, 0.4f)), true);
            Blocky.Round = true; Blocky.RoundFactor = 0.25f;
            Blocky.Part(tr, new Vector3(0, 1.4f, 0), new Vector3(2.4f, 0.8f, 2.4f), Mats.Plastic(Color.Lerp(info.color, Color.white, 0.2f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            // светящиеся углы постамента
            var glow = Mats.Glow(info.color);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? -1.8f : 1.8f, sz = (i < 2) ? -1.8f : 1.8f;
                Blocky.Part(tr, new Vector3(sx, 1.2f, sz), new Vector3(0.3f, 0.4f, 0.3f), glow);
            }
            Fx.Sparkles(tr, new Vector3(0, 2.8f, 0), info.color, 1.4f, 5f + (int)tier * 2f);
            // табличка "Рекомендуется" (как в роблокс-режимах): нужная скорость для этой зоны
            var sign = Blocky.Pivot(tr, "Recommend", new Vector3(-4.2f, 0, -3.5f));
            sign.localRotation = Quaternion.Euler(0, 180f, 0);
            Blocky.Part(sign, new Vector3(0, 0.8f, 0), new Vector3(0.25f, 1.6f, 0.25f), Mats.Plastic(new Color(0.12f, 0.15f, 0.3f)));
            Blocky.Round = true; Blocky.RoundFactor = 0.2f;
            Blocky.Part(sign, new Vector3(0, 2f, 0), new Vector3(2.6f, 1.2f, 0.2f), Mats.Plastic(new Color(0.1f, 0.14f, 0.32f)));
            Blocky.Round = false;
            double rec = System.Math.Max(10, GameConfig.Tiers[System.Math.Min((int)tier + 1, GameConfig.Tiers.Length - 1)].reqPoints * 0.5 + info.reqPoints);
            var l1 = Blocky.Label(sign, Loc.Num(rec), new Vector3(0, 2.25f, 0.12f), 0.75f, new Color(0.5f, 0.9f, 1f));
            var l2 = Blocky.Label(sign, Loc.Ru ? "Рекомендуется" : "Recommended", new Vector3(0, 1.8f, 0.12f), 0.5f, new Color(0.4f, 1f, 0.5f));
            l1.billboard = false; l2.billboard = false;
            l1.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            l2.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            l1.maxDistance = 50f; l2.maxDistance = 50f;
            p.eggAnchor = Blocky.Pivot(tr, "EggAnchor", new Vector3(0, 1.8f, 0));
            MeshMerge.Merge(tr); // постамент + табличка → по мешу на материал (до яйца: оно пересоздаётся)
            p.SpawnEgg();
            PlayerController.Interactables.Add(p);
            if (GameManager.Instance != null) GameManager.Instance.Pedestals.Add(p);
            return p;
        }

        void SpawnEgg()
        {
            if (egg != null) Destroy(egg);
            egg = Blocky.BuildEgg(eggAnchor, tier, 1.3f + (int)tier * 0.08f);
            hasEgg = true;
            Fx.Burst(eggAnchor.position + Vector3.up, GameConfig.GetTier(tier).color, 12, 3f, 0.35f, 0.6f);
        }

        void Update()
        {
            if (egg != null)
            {
                egg.transform.localRotation = Quaternion.Euler(0, Time.time * 40f, 0);
                egg.transform.localPosition = new Vector3(0, Mathf.Sin(Time.time * 2f) * 0.15f + 0.15f, 0);
            }
            if (!hasEgg)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0) SpawnEgg();
            }
        }

        public void ReturnEgg()
        {
            if (!hasEgg) SpawnEgg();
        }

        // ===== IInteractable =====
        public Vector3 InteractPos { get { return transform.position; } }
        public float InteractRange { get { return 4.2f; } }
        public float HoldTime { get { return 0.25f; } }
        public bool CanInteract
        {
            get { return hasEgg && PlayerController.Instance != null && PlayerController.Instance.Carrying == null; }
        }
        public string Prompt { get { return Loc.T("steal") + " (" + Loc.TierName(tier) + ")"; } }

        public void Interact(PlayerController p)
        {
            if (!hasEgg || p.Carrying != null) return;
            hasEgg = false;
            respawnTimer = 6f;
            if (egg != null) Destroy(egg);
            egg = null;
            Fx.Burst(eggAnchor.position + Vector3.up, GameConfig.GetTier(tier).color, 30, 6f, 0.5f, 0.7f);
            GameAudio.Play(Sfx.Whoosh);
            p.PickUp(new CarriedEgg { tier = tier, dragonId = -1, from = this });
            UIManager.Instance.Toast(Loc.F("carrying", Loc.TierName(tier)), GameConfig.GetTier(tier).color);
            // охрана поднимает тревогу
            foreach (var g in guards) g.Alert();
        }
    }
}
