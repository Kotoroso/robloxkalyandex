using UnityEngine;

namespace DragonHeist
{
    /// <summary>Продавец драконов на базе: у него продаются ненужные драконы из слотов.</summary>
    public class SellerNPC : MonoBehaviour, IInteractable
    {
        Blocky.Avatar npc;

        public static SellerNPC Build(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("Seller");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var s = go.AddComponent<SellerNPC>();
            var tr = go.transform;

            // прилавок с полосатым навесом
            Blocky.Round = true; Blocky.RoundFactor = 0.15f;
            Blocky.Part(tr, new Vector3(0, 0.9f, 1.6f), new Vector3(5f, 1.8f, 1.2f), Mats.Plastic(new Color(0.62f, 0.4f, 0.22f)));
            Blocky.Part(tr, new Vector3(0, 1.85f, 1.6f), new Vector3(5.3f, 0.15f, 1.4f), Mats.Plastic(new Color(0.8f, 0.6f, 0.35f)));
            Blocky.Round = false; Blocky.RoundFactor = 0.2f;
            var post = Mats.Plastic(new Color(0.55f, 0.35f, 0.2f));
            Blocky.Part(tr, new Vector3(-2.4f, 2.2f, 2.1f), new Vector3(0.25f, 4.4f, 0.25f), post, true);
            Blocky.Part(tr, new Vector3(2.4f, 2.2f, 2.1f), new Vector3(0.25f, 4.4f, 0.25f), post, true);
            Blocky.Part(tr, new Vector3(-2.4f, 2.2f, -0.8f), new Vector3(0.25f, 4.4f, 0.25f), post, true);
            Blocky.Part(tr, new Vector3(2.4f, 2.2f, -0.8f), new Vector3(0.25f, 4.4f, 0.25f), post, true);
            for (int i = 0; i < 6; i++)
            {
                var stripe = Mats.Plastic(i % 2 == 0 ? new Color(0.95f, 0.25f, 0.25f) : Color.white);
                var st = Blocky.Part(tr, new Vector3(-2.3f + i * 0.92f, 4.55f, 0.65f), new Vector3(0.92f, 0.15f, 3.6f), stripe);
                st.localRotation = Quaternion.Euler(-12f, 0, 0);
            }
            // монетки и яйцо на прилавке
            Blocky.Part(tr, new Vector3(-1.5f, 2.05f, 1.6f), new Vector3(0.5f, 0.12f, 0.5f), Mats.Plastic(new Color(1f, 0.8f, 0.2f)), false, PrimitiveType.Cylinder);
            Blocky.Part(tr, new Vector3(-1.2f, 2.15f, 1.4f), new Vector3(0.5f, 0.12f, 0.5f), Mats.Plastic(new Color(1f, 0.8f, 0.2f)), false, PrimitiveType.Cylinder);
            var egg = Blocky.BuildEgg(tr, Tier.Rare, 0.7f);
            egg.transform.localPosition = new Vector3(1.4f, 1.95f, 1.6f);

            // сам продавец: зелёная рубашка, коричневые штаны, шляпа
            s.npc = Blocky.BuildAvatar(tr, new Color(0.96f, 0.8f, 0.25f), new Color(0.2f, 0.6f, 0.3f), new Color(0.4f, 0.28f, 0.18f), 0.6f);
            s.npc.root.transform.localPosition = new Vector3(0, 0, 0.3f);
            Blocky.Round = true;
            Blocky.Part(s.npc.head, new Vector3(0, 0.75f, 0), new Vector3(1.9f, 0.12f, 1.9f), Mats.Plastic(new Color(0.35f, 0.22f, 0.12f)));
            Blocky.Part(s.npc.head, new Vector3(0, 1.1f, 0), new Vector3(1.15f, 0.65f, 1.15f), Mats.Plastic(new Color(0.35f, 0.22f, 0.12f)));
            Blocky.Round = false;

            Blocky.Label(tr, Loc.T("seller"), new Vector3(0, 6.2f, 1f), 1.2f, new Color(1f, 0.85f, 0.3f));
            PlayerController.Interactables.Add(s);
            return s;
        }

        void Update()
        {
            // продавец машет рукой и смотрит на игрока
            float t = Time.time;
            npc.rArm.localRotation = Quaternion.Euler(0, 0, 150f + Mathf.Sin(t * 5f) * 20f);
            npc.lArm.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.5f) * 5f, 0, 0);
            var p = PlayerController.Instance;
            if (p != null)
            {
                Vector3 d = p.transform.position - npc.root.transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.1f && d.magnitude < 25f)
                    npc.root.transform.rotation = Quaternion.Slerp(npc.root.transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
            }
            npc.model.localPosition = new Vector3(0, Mathf.Sin(t * 2f) * 0.03f, 0);
        }

        public Vector3 InteractPos { get { return transform.position + transform.forward * -0.5f; } }
        public float InteractRange { get { return 5f; } }
        public float HoldTime { get { return 0.1f; } }
        public bool CanInteract { get { return true; } }
        public string Prompt { get { return Loc.T("talk_seller"); } }
        public void Interact(PlayerController p) { UIManager.Instance.OpenSell(); }
    }
}
