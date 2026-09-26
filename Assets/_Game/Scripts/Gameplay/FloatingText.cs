using System.Collections.Generic;
using UnityEngine;

namespace DragonHeist
{
    /// <summary>Всплывающие цифры "+10 Скорость" с пулом объектов (без мусора для GC на мобилках).</summary>
    public class FloatingText : MonoBehaviour
    {
        static readonly Stack<FloatingText> pool = new Stack<FloatingText>();
        Label3D tm;
        float life;
        Color baseColor;

        public static void Spawn(Vector3 pos, string text, Color color)
        {
            FloatingText ft = pool.Count > 0 ? pool.Pop() : null;
            if (ft == null)
            {
                var tm = Blocky.Label(null, text, pos, 1.2f, color);
                ft = tm.gameObject.AddComponent<FloatingText>();
                ft.tm = tm;
            }
            ft.gameObject.SetActive(true);
            ft.transform.position = pos + new Vector3(Random.Range(-0.6f, 0.6f), 0, Random.Range(-0.6f, 0.6f));
            ft.tm.text = text;
            ft.baseColor = color;
            ft.tm.color = color;
            ft.life = 1.1f;
        }

        void Update()
        {
            life -= Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 2.5f;
            var c = baseColor; c.a = Mathf.Clamp01(life * 2f);
            tm.color = c;
            if (life <= 0)
            {
                gameObject.SetActive(false);
                pool.Push(this);
            }
        }
    }
}
