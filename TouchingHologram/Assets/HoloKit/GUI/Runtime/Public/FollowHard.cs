using UnityEngine;
namespace Holoi.Library.ARUX
{
    public class FollowHard : MonoBehaviour
    {
        enum RotateType
        {
            None,
            FacingTargetInvert,
            FacingTarget,
            IdenticalToTarget,
            FacingTargetHorizentally
        }

        [SerializeField] private Transform _followTarget;

        public Transform FollowTarget
        {
            set
            {
                _followTarget = value;
            }
        }

        [Header("Rotation")]
        [SerializeField] RotateType _rotateType;
        [SerializeField] Vector3 _axisWeight = Vector3.one;

        [Header("Offset & Space")]
        //[SerializeField] bool _worldSpace = false;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, 0.5f);

        Vector3 targetPosition;

        void Start()
        {
            if (_followTarget == null) { Debug.Log("Target of Hard Follow can not be null!"); return; }
        }

        void FixedUpdate()
        {
            if (_followTarget == null) { Debug.Log("Target of Hard Follow can not be null!"); return; }
            UpdatePosition();
            UpdateRotation();
        }

        void UpdatePosition()
        {

            targetPosition = GetTargetPosition(_followTarget.position, _offset);
            transform.position = targetPosition;

        }

        void UpdateRotation()
        {
            switch (_rotateType)
            {
                case RotateType.FacingTargetInvert:
                    transform.rotation = Quaternion.Euler(_followTarget.rotation.eulerAngles.x, _followTarget.rotation.eulerAngles.y, _followTarget.rotation.eulerAngles.z);
                    break;
                case RotateType.FacingTarget:
                    transform.LookAt(_followTarget);
                    break;
                case RotateType.None:
                    break;
                case RotateType.IdenticalToTarget:
                    transform.rotation = _followTarget.rotation;
                    break;
                case RotateType.FacingTargetHorizentally:
                    var pos = new Vector3(_followTarget.position.x, transform.position.y, _followTarget.position.z);
                    transform.LookAt(pos);
                    break;
            }
        }

        Vector3 GetTargetPosition(Vector3 targetPosition, Vector3 offset)
        {
            var localOffset = _followTarget.TransformVector(_offset);
            var worldOffset = offset;

            return targetPosition + localOffset;

            //if (_worldSpace)
            //{
            //    return targetPosition + worldOffset;
            //}
            //else
            //{
            //    return targetPosition + localOffset;
            //}
        }
    }
}