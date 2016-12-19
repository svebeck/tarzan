using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ModalController : MonoBehaviour {

    public static ModalController instance;

    public GameObject modal;
    public Button closeButton;
    public Button confirmButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI confirmText;

    void Awake() 
    {
        if (instance == null)
        {
            DontDestroyOnLoad(this);
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }

    }

    public void Start()
    {
        ClosePanel();
    }

    public void CreateDialog(string title, string message, string confirm, UnityAction onClose, UnityAction onConfirm)
    {
        modal.SetActive(true);

        titleText.text = title;
        messageText.text = message;
        confirmText.text = confirm;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(onClose);
        closeButton.onClick.AddListener(ClosePanel);
        
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(onConfirm);
        confirmButton.onClick.AddListener(ClosePanel);
    }

    public void ClosePanel()
    {
        modal.SetActive(false);
    }

    public bool IsOpen()
    {
        return modal.activeSelf;
    }
}
