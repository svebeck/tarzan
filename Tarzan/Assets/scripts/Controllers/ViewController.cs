using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ViewController : MonoBehaviour {
    
    public static ViewController instance;

    public View viewPlay;
    public View viewShop;
    public View viewLoading;
    public View viewDead;

    private View currentView;
    private List<View> previousViews = new List<View>();

    void Awake() {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }

    void Start()
    {
        //Set all views to active first, to trigger Start
        //Set inactive in update and change to start view
        viewPlay.SetActive(true);
        viewShop.SetActive(true);
        viewLoading.SetActive(true);
        viewDead.SetActive(true);
    }

    public void Init()
    {
        viewPlay.SetActive(false);
        viewShop.SetActive(false);
        viewLoading.SetActive(false);
        viewDead.SetActive(false);
    }

    public void ChangeView(View view)
    {
        if (view == currentView)
            return;

        Animate(view, currentView);

        if (currentView != null)
        {
            currentView.Exit();
            previousViews.Add(currentView);
        }

        view.Enter();

        currentView = view;
    }

    public void OnBack()
    {
        if (ModalController.instance.IsOpen())
        {
            ModalController.instance.ClosePanel();
            return;
        }

        if (previousViews.Count > 0)
        {
            View previousView = previousViews[previousViews.Count-1];

            //change to previous view
            previousViews.Remove(previousView);
            ChangeView(previousView);

            previousView = previousViews[previousViews.Count-1];
            previousViews.Remove(previousView);

        }
    }
	
	void Update () 
    {
        // Don't exit application on IOS, this will appear like a crash and 
        // will cause the application to loose saved data.
        #if !UNITY_IOS
        if (Input.GetKeyDown(KeyCode.Escape) && currentView != viewDead)
        {
            if (previousViews.Count == 0 && !ModalController.instance.IsOpen())
            {
                ModalController.instance.CreateDialog("Exit Game", "Are you sure you want to exit the game?", "Yes", delegate {
                    //do nothing
                }, delegate {
                    Application.Quit();
                });
            }
            else
                OnBack();
        }
        #endif
	}

    void Animate(View view, View currentView)
    {
        // Play -> Shop
        if (view == viewShop && currentView == viewPlay)
        {
            view.GetComponent<AnimateScreen>().Center(true);
            currentView.SetActive(false);
        }

        // Shop -> Play
        else if (view == viewPlay && currentView == viewShop)
        {
            view.GetComponent<AnimateScreen>().Center(true);
            currentView.SetActive(false);
        }

        //OTHER
        else
        {
            if (currentView != null)
                currentView.SetActive(false);
            
            view.SetActive(true);
        }
    }

    public bool IsInView(View view)
    {
        return view == currentView;
    }

    public void SetBackView(View view)
    {
        previousViews.Clear();
        previousViews.Add(view);
    }

    public void ClearHistory() 
    {
        previousViews.Clear();
    }
}
