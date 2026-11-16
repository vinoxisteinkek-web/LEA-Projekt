using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================
// TASTENBELEGUNG (Barrierefreiheit)
// =====================================
// Alle Spiel-Aktionen laufen ueber diese Klasse statt ueber feste Tasten.
// Der Spieler kann die Tasten im Settings-Menue aendern,
// die Belegung wird in PlayerPrefs gespeichert.
//
// Benutzung in Skripten:
//   InputBindings.GetDown(GameAction.Interact)   statt Input.GetKeyDown(KeyCode.E)
//   InputBindings.Get(GameAction.Handbrake)      statt Input.GetKey(KeyCode.Space)
//   InputBindings.GetHorizontal() / GetVertical() statt Input.GetAxisRaw(...)
public enum GameAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    Interact,
    Drop,
    Use,
    Flashlight,
    Handbrake
}

public static class InputBindings
{
    private const string KeyPrefix = "Binding_";

    // Standardbelegung
    private static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.MoveForward,  KeyCode.W },
        { GameAction.MoveBackward, KeyCode.S },
        { GameAction.MoveLeft,     KeyCode.A },
        { GameAction.MoveRight,    KeyCode.D },
        { GameAction.Interact,     KeyCode.E },
        { GameAction.Drop,         KeyCode.Q },
        { GameAction.Use,          KeyCode.Mouse0 },
        { GameAction.Flashlight,   KeyCode.F },
        { GameAction.Handbrake,    KeyCode.Space },
    };

    // Anzeigenamen im Menue
    private static readonly Dictionary<GameAction, string> Names = new Dictionary<GameAction, string>
    {
        { GameAction.MoveForward,  "Move Forward" },
        { GameAction.MoveBackward, "Move Backward" },
        { GameAction.MoveLeft,     "Move Left" },
        { GameAction.MoveRight,    "Move Right" },
        { GameAction.Interact,     "Interact / Pick Up" },
        { GameAction.Drop,         "Drop Item" },
        { GameAction.Use,          "Use Item" },
        { GameAction.Flashlight,   "Flashlight" },
        { GameAction.Handbrake,    "Handbrake (Car)" },
    };

    private static readonly Dictionary<GameAction, KeyCode> current = new Dictionary<GameAction, KeyCode>();

    public static event Action OnBindingsChanged;

    public static GameAction[] AllActions => (GameAction[])Enum.GetValues(typeof(GameAction));

    // ==================================================
    // LADEN
    // ==================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        current.Clear();
        foreach (GameAction a in AllActions)
            current[a] = (KeyCode)PlayerPrefs.GetInt(KeyPrefix + a, (int)Defaults[a]);
    }

    private static void EnsureLoaded()
    {
        if (current.Count == 0)
            Load();
    }

    // ==================================================
    // ABFRAGEN
    // ==================================================

    public static KeyCode GetKeyFor(GameAction action)
    {
        EnsureLoaded();
        return current[action];
    }

    public static bool Get(GameAction action)     => Input.GetKey(GetKeyFor(action));
    public static bool GetDown(GameAction action) => Input.GetKeyDown(GetKeyFor(action));
    public static bool GetUp(GameAction action)   => Input.GetKeyUp(GetKeyFor(action));

    // Pfeiltasten funktionieren immer zusaetzlich zur eigenen Belegung
    public static float GetHorizontal()
    {
        float v = 0f;
        if (Get(GameAction.MoveRight) || Input.GetKey(KeyCode.RightArrow)) v += 1f;
        if (Get(GameAction.MoveLeft)  || Input.GetKey(KeyCode.LeftArrow))  v -= 1f;
        return v;
    }

    public static float GetVertical()
    {
        float v = 0f;
        if (Get(GameAction.MoveForward)  || Input.GetKey(KeyCode.UpArrow))   v += 1f;
        if (Get(GameAction.MoveBackward) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        return v;
    }

    // ==================================================
    // AENDERN
    // ==================================================

    // Belegt eine Aktion neu. Ist die Taste schon vergeben,
    // werden die beiden Tasten getauscht (keine Doppelbelegung).
    public static void SetKey(GameAction action, KeyCode key)
    {
        EnsureLoaded();
        KeyCode old = current[action];

        foreach (GameAction other in AllActions)
        {
            if (other != action && current[other] == key)
            {
                current[other] = old;
                PlayerPrefs.SetInt(KeyPrefix + other, (int)old);
            }
        }

        current[action] = key;
        PlayerPrefs.SetInt(KeyPrefix + action, (int)key);
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (GameAction a in AllActions)
        {
            current[a] = Defaults[a];
            PlayerPrefs.DeleteKey(KeyPrefix + a);
        }
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    // ==================================================
    // ANZEIGE
    // ==================================================

    public static string GetActionName(GameAction action) => Names[action];

    public static string GetKeyName(GameAction action) => KeyToString(GetKeyFor(action));

    public static string KeyToString(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "Left Mouse";
            case KeyCode.Mouse1: return "Right Mouse";
            case KeyCode.Mouse2: return "Middle Mouse";
            case KeyCode.Mouse3: return "Mouse 4";
            case KeyCode.Mouse4: return "Mouse 5";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.LeftAlt: return "Left Alt";
            case KeyCode.RightAlt: return "Right Alt";
            case KeyCode.Return: return "Enter";
            case KeyCode.UpArrow: return "Arrow Up";
            case KeyCode.DownArrow: return "Arrow Down";
            case KeyCode.LeftArrow: return "Arrow Left";
            case KeyCode.RightArrow: return "Arrow Right";
        }

        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            return ((int)(key - KeyCode.Alpha0)).ToString();

        return key.ToString();
    }

    // Ersetzt "[E]" in Hinweistexten durch die aktuell belegte Taste,
    // z.B. "[E] Tanken" -> "[G] Tanken"
    public static string FormatPrompt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("[E]", "[" + GetKeyName(GameAction.Interact) + "]")
            .Replace("[Q]", "[" + GetKeyName(GameAction.Drop) + "]")
            .Replace("[F]", "[" + GetKeyName(GameAction.Flashlight) + "]");
    }
}
