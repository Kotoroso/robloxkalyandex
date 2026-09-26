using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>
    /// Процедурные "пластиковые" детали со скруглёнными краями — как у лего и роблокс-персонажей.
    /// Меш запекается под реальный размер детали (скругление не растягивается) и кэшируется.
    /// </summary>
    public static class RoundedMesh
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// <param name="radius">радиус скругления в юнитах</param>
        /// <param name="steps">1 = фаска (дёшево), 3+ = плавное скругление</param>
        public static Mesh Box(Vector3 size, float radius, int steps)
        {
            radius = Mathf.Max(0f, Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f - 0.001f));
            if (steps <= 0) radius = 0f;
            string key = size.x.ToString("0.00") + "_" + size.y.ToString("0.00") + "_" + size.z.ToString("0.00") + "_" + radius.ToString("0.000") + "_" + steps;
            Mesh cached;
            if (cache.TryGetValue(key, out cached) && cached != null) return cached;

            Vector3 h = size * 0.5f;
            Vector3 inner = new Vector3(h.x - radius, h.y - radius, h.z - radius);
            float[] sx = Samples(h.x, inner.x, radius, steps);
            float[] sy = Samples(h.y, inner.y, radius, steps);
            float[] sz = Samples(h.z, inner.z, radius, steps);

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 6 граней: (нормаль, ось u, ось v)
            AddFace(verts, norms, uvs, tris, Vector3.forward, Vector3.right, Vector3.up, sx, sy, h.z, inner, radius);
            AddFace(verts, norms, uvs, tris, Vector3.back, Vector3.left, Vector3.up, sx, sy, h.z, inner, radius);
            AddFace(verts, norms, uvs, tris, Vector3.right, Vector3.back, Vector3.up, sz, sy, h.x, inner, radius);
            AddFace(verts, norms, uvs, tris, Vector3.left, Vector3.forward, Vector3.up, sz, sy, h.x, inner, radius);
            AddFace(verts, norms, uvs, tris, Vector3.up, Vector3.right, Vector3.forward, sx, sz, h.y, inner, radius);
            AddFace(verts, norms, uvs, tris, Vector3.down, Vector3.right, Vector3.back, sx, sz, h.y, inner, radius);

            var mesh = new Mesh { name = "RoundedBox_" + key };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Обычный куб с UV в юнитах мира (для стен и полов со студами).</summary>
        public static Mesh FlatBox(Vector3 size) { return Box(size, 0f, 0); }

        /// <summary>Координаты вдоль оси: край → скругление → плоская часть → скругление → край.</summary>
        static float[] Samples(float h, float inner, float r, int steps)
        {
            if (steps <= 0 || r <= 0.0001f) return new[] { -h, h };
            var list = new List<float>();
            for (int k = 0; k <= steps; k++)
            {
                float a = (k / (float)steps) * Mathf.PI * 0.5f;
                list.Add(-inner - r * Mathf.Cos(a));
            }
            for (int k = steps; k >= 0; k--)
            {
                float a = (k / (float)steps) * Mathf.PI * 0.5f;
                list.Add(inner + r * Mathf.Cos(a));
            }
            return list.ToArray();
        }

        static void AddFace(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
            Vector3 n, Vector3 u, Vector3 v, float[] su, float[] sv, float hn, Vector3 inner, float r)
        {
            int start = verts.Count;
            int nu = su.Length, nv = sv.Length;
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    Vector3 p = n * hn + AbsAxis(u) * su[i] + AbsAxis(v) * sv[j];
                    Vector3 c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 d = p - c;
                    Vector3 nrm = d.sqrMagnitude > 1e-8f ? d.normalized : n;
                    verts.Add(c + nrm * r);
                    norms.Add(nrm);
                    uvs.Add(new Vector2(su[i], sv[j])); // UV в юнитах мира — текстура не растягивается
                }

            // проверяем направление обхода по первому квадрату, чтобы грань смотрела наружу
            bool flip;
            {
                Vector3 a = verts[start], b = verts[start + 1], c = verts[start + nu];
                flip = Vector3.Dot(Vector3.Cross(c - a, b - a), n) < 0;
            }
            for (int j = 0; j < nv - 1; j++)
                for (int i = 0; i < nu - 1; i++)
                {
                    int a = start + j * nu + i, b = a + 1, c = a + nu, d = c + 1;
                    if (!flip) { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                    else { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c); }
                }
        }

        static Vector3 AbsAxis(Vector3 axis) { return new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)); }
    }
}
