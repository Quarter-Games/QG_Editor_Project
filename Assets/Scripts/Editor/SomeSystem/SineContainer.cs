
using UnityEngine;

[CreateAssetMenu(fileName = "Sine Wave", menuName = "QG/HW", order = 1)]
internal class SineContainer : ScriptableObject
{
    [SerializeField] public SineWave sine;
}