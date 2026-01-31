

using TMPro;
using UnityEngine;
using UnityEngine.UI;

class UICommonButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _buttonText;

    public TextMeshProUGUI Text => this._buttonText;
    public Button Button => _button;
}