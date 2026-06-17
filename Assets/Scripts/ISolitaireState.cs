using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISolitaireState
{
    void Enter();
    void Update();
    void Exit();
}
