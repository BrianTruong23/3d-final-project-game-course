using UnityEngine;
using UnityEngine.UI;

public class CompassMarkerBar : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform marker;
    [SerializeField] private Text headingText;

    public void Initialize(Transform targetTransform, RectTransform markerTransform, Text headingLabel)
    {
        target = targetTransform;
        marker = markerTransform;
        headingText = headingLabel;
        UpdateHeading();
    }

    private void Update()
    {
        UpdateHeading();
    }

    private void UpdateHeading()
    {
        if (target == null)
        {
            return;
        }

        float heading = Mathf.Repeat(target.eulerAngles.y, 360f);

        if (marker != null)
        {
            marker.localRotation = Quaternion.Euler(0f, 0f, -heading);
        }

        if (headingText != null)
        {
            headingText.text = $"{GetCardinalDirection(heading)} {Mathf.RoundToInt(heading):000}\u00b0";
        }
    }

    private static string GetCardinalDirection(float heading)
    {
        string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int index = Mathf.RoundToInt(heading / 45f) % directions.Length;
        return directions[index];
    }
}
