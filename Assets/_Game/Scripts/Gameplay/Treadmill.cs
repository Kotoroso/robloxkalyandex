using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Беговая дорожка: бежишь вперёд — качается скорость. Закрытые покупаются за монеты.</summary>
    public class Treadmill : MonoBehaviour, IInteractable
    {
        public static readonly List<Treadmill> All = new List<Treadmill>();

        public int index;
        public TreadmillDef def;
        public Vector2 halfSize = new Vector2(2.2f, 4.5f);
        Material beltMat;
        TextMesh label;
        GameObject lockBar;
        float gainPopupTimer;
        double gainAccum;

        public bool Owned { get { return (SaveManager.Data.treadmillsMask & (1 << index)) != 0; } }

        public static Treadmill Build(Transform parent, int index, Vector3 pos, float yaw)
        {
            var def = GameConfig.Treadmills[index];
            var go = new GameObject("Treadmill_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = go.AddComponent<Treadmill>();
            t.index = index;
            t.def = def;
            var tr = go.transform;

            var frame = Mats.Plastic(new Color(0.25f, 0.25f, 0.28f));
            Blocky.Part(tr, new Vector3(0, 0.25f, 0), new Vector3(5.2f, 0.5f, 10f), frame, true);
            t.beltMat = Mats.Unique(new Color(0.12f, 0.12f, 0.12f), BeltTexture());
            t.beltMat.mainTextureScale = new Vector2(1, 4);
            Blocky.Part(tr, new Vector3(0, 0.52f, 0), new Vector3(4.4f, 0.05f, 9.6f), t.beltMat);
            // поручни и панель
            var rail = Mats.Plastic(def.color);
            Blocky.Part(tr, new Vector3(-2.5f, 1.6f, 3.5f), new Vector3(0.25f, 2.4f, 0.25f), rail, true);
            Blocky.Part(tr, new Vector3(2.5f, 1.6f, 3.5f), new Vector3(0.25f, 2.4f, 0.25f), rail, true);
            Blocky.Part(tr, new Vector3(0, 2.8f, 4.2f), new Vector3(5.2f, 0.9f, 0.5f), rail, true);
            t.label = Blocky.Label(tr, "", new Vector3(0, 4.3f, 4.2f), 1.1f, Color.white);

            t.lockBar = Blocky.Part(tr, new Vector3(0, 1.2f, -5.1f), new Vector3(5.2f, 1.4f, 0.3f),
                Mats.Plastic(new Color(0.9f, 0.2f, 0.2f)), true).gameObject;

            All.Add(t);
            PlayerController.Interactables.Add(t);
            t.Refresh();
            return t;
        }

        static Texture2D beltTex;
        static Texture2D BeltTexture()
        {
            if (beltTex != null) return beltTex;
            beltTex = new Texture2D(16, 16, TextureFormat.RGB24, false);
            beltTex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    px[y * 16 + x] = (y < 3) ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            beltTex.SetPixels(px);
            beltTex.Apply();
            return beltTex;
        }

        public void Refresh()
        {
            string name = Loc.Ru ? def.nameRu : def.nameEn;
            string gain = "+" + Loc.Num(def.gainPerSec) + "/" + (Loc.Ru ? "сек" : "s");
            if (Owned) label.text = name + "\n" + gain;
            else if (SaveManager.Data.rebirths < def.rebirthsRequired)
                label.text = name + " " + gain + "\n" + Loc.F("need_rebirth", def.rebirthsRequired);
            else label.text = name + " " + gain + "\n" + Loc.F("buy_for", Loc.Num(def.price));
            if (lockBar != null) lockBar.SetActive(!Owned);
        }

        public bool Contains(Vector3 worldPos)
        {
            Vector3 lp = transform.InverseTransformPoint(worldPos);
            return Mathf.Abs(lp.x) < halfSize.x && Mathf.Abs(lp.z) < halfSize.y && lp.y > -0.5f && lp.y < 2.5f;
        }

        public void Train(float dt)
        {
            var gm = GameManager.Instance;
            if (gm == null || !Owned) return;
            double g = def.gainPerSec * gm.TrainMultiplier * dt;
            gm.AddSpeedPoints(g);
            gainAccum += g;
            gainPopupTimer -= dt;
            if (gainPopupTimer <= 0)
            {
                gainPopupTimer = 0.6f;
                FloatingText.Spawn(PlayerController.Instance.transform.position + Vector3.up * 4f,
                    "+" + Loc.Num(System.Math.Max(1, gainAccum)) + " " + Loc.T("speed"), new Color(0.4f, 1f, 1f));
                gainAccum = 0;
                GameAudio.Play(Sfx.Tick, 0.3f);
            }
            beltMat.mainTextureOffset += new Vector2(0, -dt * 2.5f);
        }

        // ===== IInteractable (покупка) =====
        public Vector3 InteractPos { get { return transform.position + transform.forward * -5.5f; } }
        public float InteractRange { get { return 4f; } }
        public float HoldTime { get { return 0.35f; } }
        public bool CanInteract { get { return !Owned; } }
        public string Prompt
        {
            get
            {
                if (SaveManager.Data.rebirths < def.rebirthsRequired) return Loc.F("need_rebirth", def.rebirthsRequired);
                return Loc.F("buy_for", Loc.Num(def.price));
            }
        }

        public void Interact(PlayerController p)
        {
            var gm = GameManager.Instance;
            if (SaveManager.Data.rebirths < def.rebirthsRequired)
            {
                UIManager.Instance.Toast(Loc.F("need_rebirth", def.rebirthsRequired), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return;
            }
            if (!gm.TrySpend(def.price))
            {
                UIManager.Instance.Toast(Loc.T("no_money"), new Color(1f, 0.5f, 0.3f));
                GameAudio.Play(Sfx.Error);
                return;
            }
            SaveManager.Data.treadmillsMask |= 1 << index;
            GameAudio.Play(Sfx.Buy);
            UIManager.Instance.Toast(Loc.T("unlocked"), new Color(0.5f, 1f, 0.5f));
            Refresh();
            gm.SaveNow(true);
        }
    }
}
