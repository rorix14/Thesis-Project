using System.Collections.Generic;
using UnityEngine;

namespace Stealth_Game
{
    public class PlayerAgent : MonoBehaviour, IResettable
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        [SerializeField] private Vector3[] startingPositions;

        // line of sight variables
        [SerializeField] private float viewRadius;
        [SerializeField] private int wallChecks;
        [SerializeField] private LayerMask obstacleMask;

        //cached variables
        private CharacterController _characterController;
        private Vector3 _moveDirection;
        private Quaternion _initRotation;

        private float _viewStepSize;

        private bool _isActive;

        // This could be a list of objects that share an interface, this way the player character can interact with
        // more than just enemies
        public List<Transform> IterableObjects { get; private set; }
        public Vector3[] ViewPoints { get; private set; }
        public float[] ViewDistances { get; private set; }
        public bool GoalReached { get; private set; }

        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public float ViewRadius => viewRadius;
        public int WallChecks => wallChecks;
        public LayerMask ObstacleMask => obstacleMask;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _viewStepSize = 360 / (float)wallChecks;
            ViewPoints = new Vector3[wallChecks];
            ViewDistances = new float[wallChecks];

            IterableObjects = new List<Transform>();

            var transformRef = transform;
            _initRotation = transformRef.rotation;

            if (startingPositions.Length == 0)
            {
                startingPositions = new[] { transform.position };
            }
        }

        public void SoftMovePlayer(Vector3 velocity)
        {
            if (!_isActive)
            {
                GoalReached = false;
                IterableObjects.Clear();
                _isActive = true;
            }

            _characterController.Move(velocity);
        }

        public void MovePlayer(Vector3 movementDir)
        {
            if (!_isActive)
            {
                GoalReached = false;
                IterableObjects.Clear();
                _isActive = true;
            }
            
            _moveDirection = movementDir;
            //_moveDirection.Normalize();
            _characterController.Move(_moveDirection * (moveSpeed * Time.fixedDeltaTime));

            if (_moveDirection == Vector3.zero) return;

            var lookRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);
        }

        public void CheckObstacles()
        {
            for (int i = 0; i < wallChecks; i++)
            {
                var angleRadians = Mathf.Deg2Rad * ( /*transform.eulerAngles.y +*/ _viewStepSize * i);
                var direction = new Vector3(Mathf.Sin(angleRadians), 0.0f, Mathf.Cos(angleRadians));

                var impactPoint = Physics.Raycast(transform.position, direction, out var hit, viewRadius, obstacleMask)
                    ? hit.point
                    : transform.position + direction * viewRadius;

                ViewPoints[i] = impactPoint;
                //TODO: should take into account when nothing is hit
                ViewDistances[i] = hit.distance;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Goal"))
            {
                GoalReached = false;
                return;
            }

            if (other.CompareTag("Enemy") && IterableObjects.Contains(other.transform))
            {
                IterableObjects.Remove(other.transform);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            if (other.CompareTag("Goal"))
            {
                GoalReached = true;
                return;
            }

            // If we are using an interface there is no need to check the tag, check the interface instead, although it might be slower
            if (other.CompareTag("Enemy") && !IterableObjects.Contains(other.transform))
            {
                IterableObjects.Add(other.transform);
            }
        }

        public void SetPosition(Vector3 newPosition)
        {
            transform.position = newPosition;
            //Physics.SyncTransforms();
        }

        public void ResetAgent()
        {
            _moveDirection = Vector3.zero;

            var transformRef = transform;
            var index = Random.Range(0, startingPositions.Length);
            transformRef.position = startingPositions[index];
            transformRef.rotation = _initRotation;
            //Physics.SyncTransforms();

            GoalReached = false;
            IterableObjects.Clear();

            _isActive = false;
        }
    }
}