using Client.Combat;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    [DisallowMultipleComponent]
    public class CombatTargetSelection : MonoBehaviour
    {
        [SerializeField] private CombatEntity primaryTarget;
        [SerializeField] private bool hasGroundTarget;
        [SerializeField] private Vector3 groundTargetPoint;

        public CombatEntity PrimaryTarget => primaryTarget;
        public bool HasGroundTarget => hasGroundTarget;
        public Vector3 GroundTargetPoint => groundTargetPoint;

        public void SetPrimaryTarget(CombatEntity target)
        {
            primaryTarget = target;
        }

        public void SetGroundTarget(Vector3 point)
        {
            hasGroundTarget = true;
            groundTargetPoint = point;
        }

        public void ClearGroundTarget()
        {
            hasGroundTarget = false;
        }
    }
}
