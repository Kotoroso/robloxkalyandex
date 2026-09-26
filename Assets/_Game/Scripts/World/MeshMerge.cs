using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonHeist
{
    /// <summary>
    /// Объединение мелких деталей "роблокс-моделей" в меши по материалам — главный источник экономии
    /// draw calls и CPU-отсечения (раньше каждая деталь была отдельным GameObject + MeshRenderer).
    ///
    /// <see cref="Merge"/> — для моделей (персонажи, драконы, брейнроты, тренажёры...): деталь присоединяется
    /// к ближайшему предку-"пивоту" (объекту БЕЗ MeshRenderer). Анимации крутят именно пивоты
    /// (крылья, голова, руки, ноги, пропеллеры), поэтому они продолжают работать: объединённый меш —
    /// дочерний объект пивота. Детали, которые двигаются сами (неон, рамки) передаются в exclude.
    ///
    /// <see cref="MergeStatic"/> — для неподвижного мира: объединение по ячейкам сетки и материалу.
    /// </summary>
    public static class MeshMerge
    {
        /// <summary>Мелкий статичный декор (цветы, камни, кристаллы) — отсекается по дистанции (Camera.layerCullDistances).</summary>
        public const int DecorLayer = 9;
        /// <summary>Персонажи, драконы, охрана — отсекаются по дистанции.</summary>
        public const int ActorLayer = 10;
        /// <summary>Чанки клавиатурного пола — далёкие отсекаются (под ними плоский пол с текстурой клавиш).</summary>
        public const int FloorLayer = 11;

        /// <summary>Выключатель на случай проблем (для отладки).</summary>
        public static bool Enabled = true;

        const int MaxVerts = 60000; // 16-битные индексы

        struct MatKey : System.IEquatable<MatKey>
        {
            public Material mat;
            public int flags;
            public int cx, cz; // ячейка (только для статики)
            public bool Equals(MatKey o) { return mat == o.mat && flags == o.flags && cx == o.cx && cz == o.cz; }
            public override bool Equals(object o) { return o is MatKey && Equals((MatKey)o); }
            public override int GetHashCode()
            {
                unchecked { return ((mat != null ? mat.GetInstanceID() : 0) * 397) ^ (flags * 31) ^ (cx * 7919) ^ (cz * 104729); }
            }
        }

        static int Flags(MeshRenderer r) { return (int)r.shadowCastingMode * 2 + (r.receiveShadows ? 1 : 0); }

        /// <summary>Деталь можно объединять: простой меш с одним материалом, без скриптов/коллайдеров на объекте.</summary>
        static bool Mergeable(MeshRenderer r, out MeshFilter mf, out bool onlyVisual)
        {
            mf = null; onlyVisual = false;
            if (r == null || !r.enabled || !r.gameObject.activeSelf) return false;
            var mat = r.sharedMaterial;
            if (mat == null) return false;
            mf = r.GetComponent<MeshFilter>();
            if (mf == null) return false;
            var mesh = mf.sharedMesh;
            if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1) return false;
            // на объекте только Transform + MeshFilter + MeshRenderer (+ коллайдер — тогда объект оставляем, снимаем только визуал)
            var comps = r.gameObject.GetComponents<Component>();
            onlyVisual = true;
            foreach (var c in comps)
            {
                if (c is Transform || c is MeshFilter || c is MeshRenderer) continue;
                if (c is Collider) { onlyVisual = false; continue; }
                return false; // скрипт или другой рендерер — не трогаем
            }
            return true;
        }

        static bool Excluded(Transform t, Transform root, Transform[] exclude)
        {
            if (exclude == null || exclude.Length == 0) return false;
            for (var p = t; p != null && p != root; p = p.parent)
                for (int i = 0; i < exclude.Length; i++)
                    if (exclude[i] == p) return true;
            return false;
        }

        /// <summary>
        /// Объединить детали модели под пивотами. layer ≥ 0 — слой для объединённых мешей (например ActorLayer).
        /// </summary>
        public static void Merge(Transform root, int layer = -1, params Transform[] exclude)
        {
            if (!Enabled || root == null) return;
            var rends = root.GetComponentsInChildren<MeshRenderer>(true);
            if (rends == null || rends.Length < 2) return;

            // цели RainbowTint (MaterialPropertyBlock на конкретном рендерере) не трогаем
            HashSet<Renderer> skip = null;
            var tints = root.GetComponentsInChildren<RainbowTint>(true);
            if (tints != null && tints.Length > 0)
            {
                skip = new HashSet<Renderer>();
                foreach (var t in tints) if (t != null && t.target != null) skip.Add(t.target);
            }

            var groups = new Dictionary<Transform, Dictionary<MatKey, List<MeshRenderer>>>();
            var keepObject = new HashSet<MeshRenderer>();
            foreach (var r in rends)
            {
                if (skip != null && skip.Contains(r)) continue;
                if (r.transform == root) continue;
                MeshFilter mf; bool onlyVisual;
                if (!Mergeable(r, out mf, out onlyVisual)) continue;
                if (Excluded(r.transform, root, exclude)) continue;

                // ближайший предок без MeshRenderer — пивот; по пути все объекты должны быть активны
                Transform pivot = r.transform.parent;
                bool ok = true;
                while (pivot != null && pivot != root && pivot.GetComponent<MeshRenderer>() != null)
                {
                    if (!pivot.gameObject.activeSelf) { ok = false; break; }
                    pivot = pivot.parent;
                }
                if (!ok || pivot == null) continue;

                Dictionary<MatKey, List<MeshRenderer>> byMat;
                if (!groups.TryGetValue(pivot, out byMat)) groups[pivot] = byMat = new Dictionary<MatKey, List<MeshRenderer>>();
                var key = new MatKey { mat = r.sharedMaterial, flags = Flags(r) };
                List<MeshRenderer> list;
                if (!byMat.TryGetValue(key, out list)) byMat[key] = list = new List<MeshRenderer>();
                list.Add(r);
                if (!onlyVisual) keepObject.Add(r); // объект с коллайдером — оставить
            }

            foreach (var g in groups)
                foreach (var kv in g.Value)
                {
                    if (kv.Value.Count < 2) continue;
                    Build(g.Key, kv.Value, layer >= 0 ? layer : g.Key.gameObject.layer, false, keepObject, false);
                }
        }

        /// <summary>
        /// Неподвижный мир: все детали под source объединяются по ячейкам cell×cell (XZ) и материалам,
        /// объединённые объекты кладутся в target (ожидается без поворота/масштаба). Мелкие детали —
        /// отдельно, в слой DecorLayer (отсекаются по дистанции). Меши остаются читаемыми (для static batching).
        /// Исходные детали удаляются сразу (DestroyImmediate), чтобы StaticBatchingUtility их не подхватил.
        /// </summary>
        public static void MergeStatic(Transform source, Transform target, float cell, float smallSize = 2.6f)
        {
            if (!Enabled || source == null || target == null) return;
            var rends = source.GetComponentsInChildren<MeshRenderer>(true);
            if (rends == null || rends.Length < 2) return;
            var groups = new Dictionary<MatKey, List<MeshRenderer>>();
            var keepObject = new HashSet<MeshRenderer>();
            foreach (var r in rends)
            {
                MeshFilter mf; bool onlyVisual;
                if (!Mergeable(r, out mf, out onlyVisual)) continue;
                if (!r.gameObject.activeInHierarchy) continue;
                var b = r.bounds;
                float maxSide = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                bool small = maxSide < smallSize;
                var key = new MatKey
                {
                    mat = r.sharedMaterial,
                    flags = Flags(r) + (small ? 1000 : 0),
                    cx = Mathf.FloorToInt(b.center.x / cell),
                    cz = Mathf.FloorToInt(b.center.z / cell)
                };
                List<MeshRenderer> list;
                if (!groups.TryGetValue(key, out list)) groups[key] = list = new List<MeshRenderer>();
                list.Add(r);
                if (!onlyVisual) keepObject.Add(r);
            }
            foreach (var kv in groups)
            {
                if (kv.Value.Count < 2) continue;
                bool small = kv.Key.flags >= 1000;
                Build(target, kv.Value, small ? DecorLayer : target.gameObject.layer, true, keepObject, true);
            }
        }

        static readonly List<CombineInstance> ci = new List<CombineInstance>();

        static void Build(Transform pivot, List<MeshRenderer> list, int layer, bool keepReadable, HashSet<MeshRenderer> keepObject, bool immediate)
        {
            Matrix4x4 toLocal = pivot.worldToLocalMatrix;
            int start = 0;
            while (start < list.Count)
            {
                ci.Clear();
                int verts = 0, end = start;
                while (end < list.Count)
                {
                    var mf = list[end].GetComponent<MeshFilter>();
                    int v = mf.sharedMesh.vertexCount;
                    if (verts + v > MaxVerts && end > start) break;
                    verts += v;
                    ci.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = 0, transform = toLocal * list[end].transform.localToWorldMatrix });
                    end++;
                }
                if (ci.Count >= 2)
                {
                    var first = list[start];
                    var mesh = new Mesh();
                    mesh.name = "Merged_" + pivot.name;
                    mesh.CombineMeshes(ci.ToArray(), true, true);
                    mesh.RecalculateBounds();
                    if (!keepReadable) mesh.UploadMeshData(true); // CPU-копия не нужна — экономим память

                    var go = new GameObject("Merged");
                    go.layer = layer;
                    go.transform.SetParent(pivot, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = first.sharedMaterial;
                    mr.shadowCastingMode = first.shadowCastingMode;
                    mr.receiveShadows = first.receiveShadows;
                    go.AddComponent<MergedMesh>().mesh = mesh;

                    for (int i = start; i < end; i++) Strip(list[i], keepObject != null && keepObject.Contains(list[i]), immediate);
                }
                start = end;
            }
        }

        /// <summary>Убрать визуал исходной детали: объект целиком (если он пустой) или только MeshRenderer/MeshFilter.</summary>
        static void Strip(MeshRenderer r, bool keepObject, bool immediate)
        {
            r.enabled = false; // не рисовать уже в этом кадре (Destroy отложенный)
            var go = r.gameObject;
            var mf = r.GetComponent<MeshFilter>();
            if (!keepObject && go.transform.childCount == 0)
            {
                if (immediate) Object.DestroyImmediate(go); else Object.Destroy(go);
            }
            else
            {
                if (immediate) { Object.DestroyImmediate(r); if (mf != null) Object.DestroyImmediate(mf); }
                else { Object.Destroy(r); if (mf != null) Object.Destroy(mf); }
            }
        }

        /// <summary>После StaticBatchingUtility.Combine объединённые меши заменены общим батчем — освобождаем их.</summary>
        public static void ReleaseReplaced(Transform root)
        {
            if (root == null) return;
            foreach (var m in root.GetComponentsInChildren<MergedMesh>(true)) m.ReleaseIfReplaced();
        }
    }

    /// <summary>Владелец объединённого меша: удаляет его вместе с объектом (иначе меши копятся при пересборке драконов).</summary>
    public class MergedMesh : MonoBehaviour
    {
        public Mesh mesh;

        public void ReleaseIfReplaced()
        {
            var mf = GetComponent<MeshFilter>();
            if (mesh != null && mf != null && mf.sharedMesh != mesh) { Destroy(mesh); mesh = null; }
        }

        void OnDestroy()
        {
            if (mesh == null) return;
            Destroy(mesh);
            mesh = null;
        }
    }
}
