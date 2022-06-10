using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PopupType
{
    Error,
    Info,
}
public class PopupManager : MonoBehaviour
{
    public static PopupManager instance;
    public List<Popup> Popups;
    private void Awake()
    {
        instance = this;
    }

    public static void CreatePopup(PopupType type, string msg, float duration = 2f)
    {
        instance.StartCoroutine(instance.CreatePopupRoutine(type, msg, duration));
    }
    private IEnumerator CreatePopupRoutine(PopupType type, string msg, float duration)
    {
        var popup = Instantiate(Popups.Find(i => i.Type == type),transform);
        popup.TextField.text = msg;
        popup.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(duration);

        popup.gameObject.SetActive(false);
        Destroy(popup.gameObject);
    }
}
