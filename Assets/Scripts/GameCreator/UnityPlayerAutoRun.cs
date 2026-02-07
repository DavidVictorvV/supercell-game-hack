using System;
using GameCreator.Runtime.Characters;
using GameCreator.Runtime.Common;
using UnityEngine;

[Title("AutoRun")]
[Category("AutoRun")]
[Description("Moves the Player left and right relative to the Main Camera")]
[Serializable]
public class UnitPlayerAutoRun : UnitPlayerDirectional
{
    protected override Vector3 GetMoveDirection(Vector3 input)
    {
        // Only use X input (Left/Right), ignore Y input (Forward/Back)
        Vector3 constrainedInput = new Vector3(input.x, 1f, 1f);
        return base.GetMoveDirection(constrainedInput);
    }
}
