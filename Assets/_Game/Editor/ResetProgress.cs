using UnityEditor;
using UnityEngine;

namespace DragonHeist.EditorTools
{
    /// <summary>
    /// Сброс прогресса для тестов в редакторе. Прогресс в Unity хранится только на этом компьютере (PlayerPrefs)
    /// и в сборку/игрокам не попадает; у игроков на Яндексе — своё облачное сохранение.
    /// </summary>
    public static class ResetProgress
    {
        [MenuItem("Dragon Heist/Сбросить мой прогресс (только в редакторе)")]
        public static void Reset()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dragon Heist", "Сначала останови игру (кнопка Play), потом сбрасывай прогресс.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Dragon Heist", "Удалить весь прогресс в редакторе (монеты, драконы, покупки, обучение)?", "Удалить", "Отмена")) return;
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Dragon Heist] Прогресс в редакторе сброшен. Нажми Play — игра начнётся с нуля.");
        }
    }
}
