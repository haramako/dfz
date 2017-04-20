using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WmTest1 : FrameBase {

    public void OnTestClick(GameObject target){
        var id = target.GetId();
        switch (id)
        {
            case 1:
                WindowManager.GoTo("/fuga");
                break;
        }
    }
}
