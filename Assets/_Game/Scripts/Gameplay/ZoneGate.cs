using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Барьер на входе в зону: пропускает, только если скорость (прокачанная на беговых дорожках)
    /// не ниже требования зоны. Обратно с зоны на базу пускает всегда.
    /// </summary>
    public class ZoneGate : MonoBehaviour
    {
        public TierInfo info;
        BoxCollider col;
        GameObject visual;
        Label3D label;
        bool open;
        float toastCooldown;

        public static ZoneGate Build(Transform parent, TierInfo info, float z, float width)
        {
            var go = new GameObject("Gate_" + info.tier);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0, 0, z);
            var g = go.AddComponent<ZoneGate>();
            g.info = info;
            g.col = go.AddComponent<BoxCollider>();
            g.col.center = new Vector3(0, 4f, 0);
            g.col.size = new Vector3(width, 8f, 0.6f);
            var c = info.color; c.a = 0.35f;
            g.visual = Blocky.Part(go.transform, new Vector3(0, 3.6f, 0), new Vector3(width - 4f, 7.2f, 0.15f), Mats.Transparent(c)).gameObject;
            Blocky.NoShadows(g.visual);
            g.label = Blocky.Label(go.transform, "", new Vector3(0, 4.2f, -0.4f), 1.4f, Color.white);
            g.Refresh(true);
            return g;
        }

        void Refresh(bool force)
        {
            var gm = GameManager.Instance;
            bool nowOpen = gm != null && SaveManager.Data.speedPoints + 0.001 >= info.reqPoints;
            if (!force && nowOpen == open) return;
            open = nowOpen;
            visual.SetActive(!open);
            label.text = open ? "" : Loc.F("gate_locked", Loc.Num(info.reqPoints));
            if (open && !force)
            {
                GameAudio.Play(Sfx.Success);
                Fx.Confetti(transform.position + Vector3.up * 3f, 40);
            }
        }

        void Update()
        {
            Refresh(false);
            var p = PlayerController.Instance;
            if (p == null) return;
            float pz = p.transform.position.z;
            // блокируем только вход со стороны базы
            bool block = !open && pz < transform.position.z - 0.3f;
            if (col.enabled != block) col.enabled = block;

            if (toastCooldown > 0) toastCooldown -= Time.deltaTime;
            if (block && toastCooldown <= 0 && Mathf.Abs(pz - transform.position.z) < 2.2f)
            {
                toastCooldown = 3f;
                UIManager.Instance.Toast(Loc.F("gate_block", Loc.Num(info.reqPoints)), new Color(1f, 0.6f, 0.3f));
                GameAudio.Play(Sfx.Error);
            }
        }
    }
}
