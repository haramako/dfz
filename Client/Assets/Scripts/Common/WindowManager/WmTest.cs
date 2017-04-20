using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WmTest : FrameBase {

    public void OnTestClick(GameObject target){
        var id = target.GetId();
        switch (id)
        {
            case 1:
                WindowManager.GoTo("/TestModal");
                break;
        }
    }
}
