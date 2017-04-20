using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowManagerListener : MonoBehaviour, WindowManager.IWindowManagerListener {

    public void BeforeChange (){
    }

    public IEnumerator Fadeout()
    {
        yield return null;
    }

    public IEnumerator Fadein(){
        yield return null;
    }
    public IEnumerator BeforeLoadFrame(){
        yield return null;
    }
    public IEnumerator AfterLoadFrame(){
        yield return null;
    }
    public IEnumerator AfterActivate(){
        yield return null;
    }

}
