using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// =====================================================
// FILTER-UMSCHALTER zum Vergleichen der Kamerafilter
// =====================================================
// Nur im Editor und in Development Builds aktiv.
//   F6         = naechster Filter
//   Shift + F6 = vorheriger Filter
// Ersetzt das Profil von jedem Volume, das gerade einen Filter aus dem
// FilterSet benutzt (Resources/Story2FilterSet). Die Wahl bleibt ueber
// Szenenwechsel und Neustarts erhalten (PlayerPrefs).
// In normalen Builds passiert nichts – dort gilt das Profil aus der Szene.
public class FilterSwitcher : MonoBehaviour
{
    private const string SetResource = "Story2FilterSet";
    private const string PrefKey = "Story2FilterIndex";

    private FilterSet set;
    private int index = -1;
    private float labelUntil;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var s = Resources.Load<FilterSet>(SetResource);
        if (s == null || s.profiles == null || s.profiles.Length == 0) return;
        var go = new GameObject("[FilterSwitcher]");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        var sw = go.AddComponent<FilterSwitcher>();
        sw.set = s;
        sw.index = PlayerPrefs.GetInt(PrefKey, -1);
        SceneManager.sceneLoaded += (sc, m) => sw.Apply(false);
        sw.Apply(false);
    }
#endif

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F6)) return;
        int n = set.profiles.Length;
        if (index < 0) index = CurrentInScene();
        bool back = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        index = ((index < 0 ? 0 : index) + (back ? -1 : 1) + n) % n;
        PlayerPrefs.SetInt(PrefKey, index);
        Apply(true);
    }

    // welcher Filter aus dem Set gerade in der Szene liegt
    private int CurrentInScene()
    {
        foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            int i = System.Array.IndexOf(set.profiles, v.sharedProfile);
            if (i >= 0) return i;
        }
        return -1;
    }

    private void Apply(bool showLabel)
    {
        if (set == null || index < 0 || index >= set.profiles.Length) return;
        var target = set.profiles[index];
        bool any = false;
        foreach (var v in FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(set.profiles, v.sharedProfile) < 0) continue;
            v.sharedProfile = target;
            any = true;
        }
        if (showLabel || any) labelUntil = Time.unscaledTime + (showLabel ? 3f : 0f);
    }

    void OnGUI()
    {
        if (Time.unscaledTime > labelUntil || set == null || index < 0) return;
        string name = set.profiles[index] != null ? set.profiles[index].name : "?";
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        string text = "Filter " + (index + 1) + "/" + set.profiles.Length + ":  " + name + "   (F6 / Shift+F6)";
        GUI.color = Color.black;
        GUI.Label(new Rect(22, 22, 1200, 40), text, style);
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 20, 1200, 40), text, style);
    }
}
