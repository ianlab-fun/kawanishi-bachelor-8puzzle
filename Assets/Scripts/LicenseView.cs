using System.Text;
using TMPro;
using UnityEngine;

public class LicenseView : MonoBehaviour
{
    [SerializeField] TextAsset[] licenses;
    [SerializeField] TextMeshProUGUI textMeshProUGUI;
    [SerializeField] GameObject viewObject;

    public void OnButtonClicked(bool isOn)
    {
        viewObject.SetActive(isOn);
        if (!isOn)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        foreach (var license in licenses)
        {
            builder.AppendLine(license.text);
        }
        textMeshProUGUI.text = builder.ToString();
    }
}
