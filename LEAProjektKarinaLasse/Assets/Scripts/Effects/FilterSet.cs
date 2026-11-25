using UnityEngine;
using UnityEngine.Rendering;

// Liste von Kamerafiltern (Volume Profiles), zwischen denen der FilterSwitcher mit F6 umschaltet.
[CreateAssetMenu(menuName = "LEA/Filter Set", fileName = "FilterSet")]
public class FilterSet : ScriptableObject
{
    public VolumeProfile[] profiles;
}
