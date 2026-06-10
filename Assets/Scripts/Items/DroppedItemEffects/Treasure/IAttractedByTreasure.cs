using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttractedByTreasure 
{
    void AttractedByTreasure(Vector2 TreasurePosition,PickableObject pickableObject);
    // 将鼹鼠吸引来，等鼹鼠拾取动画后销毁pickableObject.gameObject
}
