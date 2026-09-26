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
            Blocky.Part(tr, new Vector3(0, 0.5f, 0), new Vector3(4f, 1f, 4f), Mats.Studs(new Color(0.35f, 0.35f, 0.38f), 4, 4), true);
            Blocky.Part(tr, new Vector3(0, 1.4f, 0), new Vector3(2.4f, 0.8f, 2.4f), Mats.Studs(info.color * 0.8f, 2, 2), true);
            p.eggAnchor = Blocky.Pivot(tr, "EggAnchor", new Vector3(0, 1.8f, 0));
            p.SpawnEgg();
            PlayerController.Interactables.Add(p);
            return p;
        }

        void SpawnEgg()
        {
            if (egg != null) Destroy(egg);
            egg = Blocky.BuildEgg(eggAnchor, tier, 1.3f + (int)tier * 0.08f);
            hasEgg = true;
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
        public float HoldTime { get { return 0.6f; } }
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
            var dragon = GameConfig.RollDragon(tier);
            p.PickUp(new CarriedEgg { tier = tier, dragonId = dragon.id, from = this });
            UIManager.Instance.Toast(Loc.F("carrying", Loc.TierName(tier)), GameConfig.GetTier(tier).color);
            // охрана поднимает тревогу
            foreach (var g in guards) g.Alert();
        }
    }
}
