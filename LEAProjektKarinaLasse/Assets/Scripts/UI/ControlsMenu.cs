using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================
// CONTROLS-UEBERSICHT IM SETTINGS-MENUE
// =====================================
// Zeigt alle belegbaren Aktionen mit ihrer aktuellen Taste.
// Klick auf eine Taste -> "Press a key..." -> naechste gedrueckte Taste
// wird uebernommen. ESC bricht ab. Ist die Taste schon vergeben,
// werden die beiden Belegungen getauscht.
public class ControlsMenu : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public GameAction action;
        public Button keyButton;
        public TextMeshProUGUI keyText;
    }

    [SerializeField] private List<Row> rows = new List<Row>();
    [SerializeField] private Button resetButton;

    [Header("Farben")]
    [SerializeField] private Color normalKeyColor = new Color(0.85f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color listeningKeyColor = new Color(1f, 0.85f, 0.85f, 1f);

    private static KeyCode[] allKeys;

    private Row listeningRow;
    private int listenStartFrame;

    // Wahr, solange auf einen Tastendruck gewartet wird
    // (andere Skripte koennen das abfragen, z.B. um ESC zu ignorieren)
    public static bool IsRebinding { get; private set; }

    private void Awake()
    {
        if (allKeys == null)
            allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        foreach (Row row in rows)
        {
            Row r = row;
            if (r.keyButton != null)
                r.keyButton.onClick.AddListener(() => StartListening(r));
        }

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetControls);
    }

    private void OnEnable()
    {
        InputBindings.OnBindingsChanged += RefreshAll;
        RefreshAll();
    }

    private void OnDisable()
    {
        InputBindings.OnBindingsChanged -= RefreshAll;
        StopListening();
    }

    // ==================================================
    // NEU BELEGEN
    // ==================================================

    private void StartListening(Row row)
    {
        StopListening();

        listeningRow = row;
        listenStartFrame = Time.frameCount;
        IsRebinding = true;

        if (row.keyText != null)
        {
            row.keyText.text = "Press a key...";
            row.keyText.color = listeningKeyColor;
        }
    }

    private void StopListening()
    {
        if (listeningRow == null)
            return;

        Row row = listeningRow;
        listeningRow = null;
        IsRebinding = false;
        RefreshRow(row);
    }

    private void Update()
    {
        if (listeningRow == null)
            return;

        // Den Klick, der das Warten gestartet hat, nicht mitzaehlen
        if (Time.frameCount <= listenStartFrame)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopListening();
            return;
        }

        foreach (KeyCode key in allKeys)
        {
            if (key == KeyCode.None || key == KeyCode.Escape)
                continue;

            // Controller-Tasten ueberspringen
            if (key >= KeyCode.JoystickButton0)
                continue;

            if (Input.GetKeyDown(key))
            {
                GameAction action = listeningRow.action;
                listeningRow = null;
                IsRebinding = false;
                InputBindings.SetKey(action, key); // loest RefreshAll aus
                return;
            }
        }
    }

    // ==================================================
    // ANZEIGE
    // ==================================================

    private void ResetControls()
    {
        StopListening();
        InputBindings.ResetToDefaults();
    }

    private void RefreshAll()
    {
        foreach (Row row in rows)
        {
            if (row != listeningRow)
                RefreshRow(row);
        }
    }

    private void RefreshRow(Row row)
    {
        if (row.keyText == null)
            return;

        row.keyText.text = InputBindings.GetKeyName(row.action);
        row.keyText.color = normalKeyColor;
    }
}
