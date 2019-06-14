using FIMSpace.Basics;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FTail
{
    /// <summary>
    /// FM: Base script for tail-like procedural animation
    /// </summary>
    public abstract class FTail_AnimatorBase : MonoBehaviour
    {
        [Header("[ Auto detection if left empty ]", order = 0)]
        [Space(-8f)]
        [Header("[ or put here first bone ]", order = 2)]
        /// <summary> List of tail bones </summary>
        public List<Transform> TailTransforms;

        [Tooltip("When you want pin controll rotation motion to parent instead of first bone in chain")]
        public bool RootToParent = false;

        [Tooltip("When you want to use auto get when you assigning one bone inside inspector window")]
        public bool AutoGetWithOne = true;

        [Tooltip("Safe variable to correctly support dynamic scalling. Not changing much visually but preventing from incorrect scalling when bones inside your model have animated scale.")]
        public bool InitBeforeAnimator = false;

        /// <summary> List of invisible for editor points which represents ghost animation for tail </summary>
        protected List<FTail_Point> proceduralPoints;

        [Header("[ Tail behaviour params ]")]
        [Tooltip("Position speed is defining how fast tail segments will return to target position, it gives animation more gummy feeling if it's lower")]
        [Range(5f, 60f)]
        public float PositionSpeed = 35f;
        [Tooltip("Rotation speed is defining how fast tail segments will return to target rotation, it gives animation more lazy feeling if it's lower")]
        [Range(5f, 60f)]
        public float RotationSpeed = 20f;

        protected List<Transform> editorGizmoTailList;

        //[Header("[ Tuning modificators ]")]
        [Tooltip("Automatically changing some tweaking settings to make tail animation look correctly")]
        public bool UseAutoCorrectLookAxis = true;


        public bool FullCorrection = false;

        [Tooltip("Use this option when your model is rolling strangely when waving")]
        public bool RolledBones = false;
        public bool AnimateCorrections = false;

        public float StretchMultiplier = 1f;

        [Tooltip("Bones wrong rotations axis corrector")]
        [Space(8f)]
        public Vector3 AxisCorrection = new Vector3(0f, 0f, 1f);
        public Vector3 AxisLookBack = new Vector3(0f, 1f, 0f);

        [HideInInspector]
        public bool ExtraCorrectionOptions = false;
        public Vector3 ExtraFromDirection = new Vector3(0f, 0f, 1f);
        public Vector3 ExtraToDirection = new Vector3(0f, 0f, 1f);

        [Tooltip("This option adding TailReference component to all tail segments, so you can access this component from tail's segment transform")]
        public bool AddTailReferences = false;

        // V1.2
        [Tooltip("Set update clock to LateUpdate if you want to use component over object with own animation")]
        public EFUpdateClock UpdateClock = EFUpdateClock.Update;

        [Tooltip("When you use Update or LateUpdate you can use smooth delta time, which can eliminate some chopping when framerate isn't stable")]
        public bool SmoothDeltaTime = true;

        // V1.2.2
        [Tooltip("To use for example when your model is posed during animations much different than it's initial T-Pose (when you use 'Animate Corrections')")]
        public bool RefreshHelpers = false;

        // V1.2.3
        [Tooltip("Useful when you use other components to affect bones hierarchy and you want this component to follow other component's changes")]
        public bool QueueToLastUpdate = true;

        // V1.2.6
        [Tooltip("[Experimental] Using some simple calculations to make tail bend on colliders")]
        public bool UseCollision = false;
        public bool CollideWithOtherTails = false;
        public FTailColliders CollidersType = FTailColliders.Spheres;
        public enum FTailColliders { Boxes, Spheres }
        public AnimationCurve CollidersScale = AnimationCurve.Linear(0, 1, 1, 1);
        public float CollidersScaleMul = 6.5f;
        public Vector3 BoxesDimensionsMul = Vector3.one;
        public List<Collider> IgnoredColliders;
        public bool CollidersSameLayer = true;
        [Tooltip("If you add rigidbodies to each tail segment's collider, collision will work on everything but it will be less optimal, you don't have to add here rigidbodies but then you must have not kinematic rigidbodies on objects segments can collide")]
        public bool CollidersAddRigidbody = true;

        [FPD_Layers]
        public LayerMask CollidersLayer = 0;
        [Range(0f, 1f)]
        public float DifferenceScaleFactor = 1f;

        // V1.2.6
        [Tooltip("If you want to simulate global additional force over tail animation, working not exacly like gravity but tries to mimic this with simple calculations")]
        public Vector2 GravityPower = Vector3.zero;

        /// <summary> Initialization method controll flag </summary>
		protected bool initialized;
        public bool IsInitialized { get; protected set; }

        /// <summary> Remember initial distances between tail transforms for right placement between tail segments during animation </summary>
        protected List<float> distances;

        /// <summary> Parent transform of first tail transform </summary>
        protected Transform rootTransform;

        /// <summary> Variables which are detected at Init() and allowing to configure tail follow rotations to look correctly independently of how bones rotations orientations are set </summary>
        protected List<Vector3> tailLookDirections;
        protected List<Vector3> lookBackDirections;
        protected List<Quaternion> lookBackOffsets;
        protected List<Quaternion> animatedCorrections;

        protected Vector3 firstBoneInitialRotation = Vector3.zero;
        protected Quaternion firstBoneInitialRotationQ = Quaternion.identity;

        protected bool preAutoCorrect = false;

        protected List<Vector3> collisionOffsets;
        protected List<float> collisionFlags;
        protected List<Collision> collisionContacts;

        protected virtual void Reset()
        {
            IsInitialized = false;
        }

        /// <summary>
        /// Method to initialize component, to have more controll than waiting for Start() method, init can be executed before or after start, as programmer need it.
        /// </summary>
        protected virtual void Init()
        {
            if (initialized) return;

            string name = transform.name;
            if (transform.parent) name = transform.parent.name;

            ConfigureBonesTransforms();

            CoputeHelperVariables();

            PrepareTailPoints();

            if (QueueToLastUpdate) QueueComponentToLastUpdate();

            if (TailTransforms.Count == 1)
            {
                // WARGING: Setting it automatically when it's detected that only one bone is used in chain
                RootToParent = true;

                if (TailTransforms[0].parent == null) Debug.LogError("You want use tail animator on single bone which don't have parent reference transform!");
            }

            if (UseCollision) AddColliders();

            initialized = true;
            IsInitialized = true;
        }


        /// <summary>
        /// Calculating helper and fixer variables
        /// </summary>
        protected virtual void CoputeHelperVariables()
        {
            // Extra fixing variables
            tailLookDirections = new List<Vector3>();
            lookBackDirections = new List<Vector3>();
            lookBackOffsets = new List<Quaternion>();
            animatedCorrections = new List<Quaternion>();

            // Precomputing tail look directions for some extra correcting bones structure for animating
            if (TailTransforms.Count > 0)
            {
                firstBoneInitialRotation = TailTransforms[0].localRotation.eulerAngles;
                firstBoneInitialRotationQ = TailTransforms[0].localRotation;

                if (AddTailReferences)
                {
                    for (int i = 0; i < TailTransforms.Count; i++)
                    {
                        if (TailTransforms[i] == transform) continue;
                        if (!TailTransforms[i].GetComponent<FTail_Reference>()) TailTransforms[i].gameObject.AddComponent<FTail_Reference>().TailReference = this;
                    }
                }

                for (int i = 0; i < TailTransforms.Count; i++)
                {
                    lookBackDirections.Add(TailTransforms[i].localRotation * Vector3.forward);
                    lookBackOffsets.Add(TailTransforms[i].localRotation);
                    animatedCorrections.Add(TailTransforms[i].localRotation);
                }

                for (int i = 0; i < TailTransforms.Count - 2; i++)
                {
                    tailLookDirections.Add(-
                        (
                        TailTransforms[i].InverseTransformPoint(TailTransforms[i + 1].position)
                        -
                        TailTransforms[i].InverseTransformPoint(TailTransforms[i].position)
                        )
                        .normalized);
                }


                if (TailTransforms.Count == 1)
                {
                    Vector3 rootLook = -(TailTransforms[0].parent.InverseTransformPoint(TailTransforms[0].position) - TailTransforms[0].parent.InverseTransformPoint(TailTransforms[0].parent.position)).normalized;
                    tailLookDirections.Add(rootLook);
                }
                else
                if (TailTransforms.Count == 2)
                {
                    Vector3 preLook = -(TailTransforms[0].InverseTransformPoint(TailTransforms[1].position) - TailTransforms[0].InverseTransformPoint(TailTransforms[0].position)).normalized;
                    tailLookDirections.Add(preLook);
                }
                else
                    tailLookDirections.Add(tailLookDirections[tailLookDirections.Count - 1]);
            }

            rootTransform = TailTransforms[0].parent;

            // Remembering initial distances between bones to calculate right positions in animation
            distances = new List<float>();
            if (TailTransforms[0].parent == null) distances.Add(0f); else distances.Add(Vector3.Distance(TailTransforms[0].position, TailTransforms[0].parent.position));

            for (int i = 1; i < TailTransforms.Count; i++)
                distances.Add(Vector3.Distance(TailTransforms[i].position, TailTransforms[i - 1].position));
        }


        /// <summary>
        /// Auto collect tail transforms if they're not defined from inspector
        /// also this is place for override and configure more
        /// </summary>
        protected virtual void ConfigureBonesTransforms()
        {
            AutoGetTailTransforms();
        }


        /// <summary>
        /// Getting child bones to auto define tail structure
        /// </summary>
        public void AutoGetTailTransforms(bool editor = false)
        {
            if (TailTransforms == null) TailTransforms = new List<Transform>();

            bool can = true;
            if (!AutoGetWithOne && !editor) can = false;
            if (!can) return;

            if (TailTransforms.Count < 2)
            {
                Transform lastParent = transform;

                bool boneDefined = true;

                // (V1.1) Start parent
                if (TailTransforms.Count == 0)
                {
                    boneDefined = false;
                    lastParent = transform;
                }
                else lastParent = TailTransforms[0];

                Transform rootTransform = lastParent;

                // 100 iterations because I am scared of while() loops :O so limit to 100 or 1000 if anyone would ever need
                for (int i = TailTransforms.Count; i < 100; i++)
                {
                    if (boneDefined)
                        if (lastParent == rootTransform)
                        {
                            if (lastParent.childCount == 0) break;
                            lastParent = lastParent.GetChild(0);
                            continue;
                        }

                    TailTransforms.Add(lastParent);

                    if (lastParent.childCount > 0) lastParent = lastParent.GetChild(0); else break;
                }
            }
        }


        /// <summary>
        /// Disconnecting all tail transforms so we can move them freely
        /// </summary>
        protected virtual void PrepareTailPoints()
        {
            proceduralPoints = new List<FTail_Point>();

            for (int i = 0; i < TailTransforms.Count; i++)
            {
                FTail_Point p = new FTail_Point();
                p.index = i;
                p.Position = TailTransforms[i].position;
                p.Rotation = TailTransforms[i].rotation;
                p.InitialPosition = TailTransforms[i].localPosition;
                p.InitialRotation = TailTransforms[i].localRotation;
                p.InitialLossyScale = TailTransforms[i].lossyScale;

                proceduralPoints.Add(p);
            }
        }

        /// <summary>
        /// Initialize component for correct work
        /// </summary>
        protected void Start()
        {
            if (InitBeforeAnimator)
                Init();
            else
                StartCoroutine(InitInLate());
        }

        /// <summary>
        /// Initializing component after animator frame step
        /// </summary>
        protected System.Collections.IEnumerator InitInLate()
        {
            yield return new WaitForEndOfFrame();
            Init();
            yield break;
        }

        /// <summary>
        /// Main method to put motion calculations methods order for derived classes
        /// </summary>
        public virtual void CalculateOffsets()
        {
            MotionCalculations();
        }

        /// <summary>
        /// Calculating tail-like movement animation logic for given transforms list
        /// </summary>
        protected virtual void MotionCalculations()
        {
            if (UseCollision)
            {
                if (collisionOffsets == null) AddColliders();
            }

            if (preAutoCorrect != UseAutoCorrectLookAxis)
            {
                ApplyAutoCorrection();
                preAutoCorrect = UseAutoCorrectLookAxis;
            }

            if (AnimateCorrections)
                for (int i = 0; i < TailTransforms.Count; i++)
                    animatedCorrections[i] = TailTransforms[i].localRotation;

            // Just calculating animation variables
            float posDelta;
            float rotDelta;

            if (UpdateClock == EFUpdateClock.FixedUpdate)
            {
                posDelta = Time.fixedDeltaTime * PositionSpeed;
                rotDelta = Time.fixedDeltaTime * RotationSpeed;
            }
            else
            {
                if (SmoothDeltaTime)
                {
                    posDelta = Time.smoothDeltaTime * PositionSpeed;
                    rotDelta = Time.smoothDeltaTime * RotationSpeed;
                }
                else
                {
                    posDelta = Time.deltaTime * PositionSpeed;
                    rotDelta = Time.deltaTime * RotationSpeed;
                }
            }

            if (!RootToParent)
            {
                proceduralPoints[0].Position = TailTransforms[0].position;
            }
            else
            {
                // Supporting root parent motion
                FTail_Point currentTailPoint = proceduralPoints[0];
                Vector3 startLookPosition = TailTransforms[0].parent.position;
                Vector3 translationVector;

                translationVector = TailTransforms[0].parent.TransformDirection(tailLookDirections[0]);

                float scaleDistDiff = 0f;
                if (currentTailPoint.InitialLossyScale.magnitude != 0f)
                    scaleDistDiff = TailTransforms[currentTailPoint.index].lossyScale.magnitude / currentTailPoint.InitialLossyScale.magnitude;

                Vector3 targetPosition = TailTransforms[0].parent.transform.position + (translationVector * -1f * (distances[0] * StretchMultiplier * scaleDistDiff));

                FTail_Point temporaryPoint = new FTail_Point
                {
                    index = 0,
                    Position = TailTransforms[0].parent.position,
                    Rotation = TailTransforms[0].parent.rotation
                };

                Quaternion targetLookRotation = CalculateTargetRotation(startLookPosition, currentTailPoint.Position, temporaryPoint, currentTailPoint, -1);

                proceduralPoints[0].Position = Vector3.Lerp(currentTailPoint.Position, targetPosition, posDelta);
                proceduralPoints[0].Rotation = Quaternion.Lerp(currentTailPoint.Rotation, targetLookRotation, rotDelta);
            }

            for (int i = 1; i < proceduralPoints.Count; i++)
            {
                FTail_Point previousTailPoint = proceduralPoints[i - 1];
                FTail_Point currentTailPoint = proceduralPoints[i];

                Vector3 startLookPosition = previousTailPoint.Position;

                Vector3 translationVector;

                if (FullCorrection)
                    translationVector = previousTailPoint.TransformDirection(tailLookDirections[i - 1]);
                else
                    translationVector = previousTailPoint.TransformDirection(AxisCorrection);

                float scaleDistDiff = 0f;
                if (currentTailPoint.InitialLossyScale.magnitude != 0f)
                    scaleDistDiff = TailTransforms[currentTailPoint.index].lossyScale.magnitude / currentTailPoint.InitialLossyScale.magnitude;

                Vector3 targetPosition = previousTailPoint.Position + (translationVector * -1f * (distances[i] * StretchMultiplier * scaleDistDiff));

                Quaternion targetLookRotation = CalculateTargetRotation(startLookPosition, currentTailPoint.Position, previousTailPoint, currentTailPoint, i - 1);

                proceduralPoints[i].Position = Vector3.Lerp(currentTailPoint.Position, targetPosition, posDelta);
                proceduralPoints[i].Rotation = Quaternion.Lerp(currentTailPoint.Rotation, targetLookRotation, rotDelta);

                if (UseCollision)
                {
                    if (collisionFlags[i] > 0f)
                        collisionFlags[i] -= Time.deltaTime * 4f;
                    else
                        collisionOffsets[i] = Vector3.zero;
                }
            }

            if (UseCollision) for (int i = 1; i < collisionContacts.Count; i++) UseCollisionContact(i);
            //if (UseCollision) for (int i = collisionContacts.Count-1; i >= 1; i--) UseCollisionContact(i);
        }

        /// <summary>
        /// Setting tail transforms positions and rotations in world from procedural points animation
        /// </summary>
        protected virtual void SetTailTransformsFromPoints()
        {
            for (int i = 0; i < TailTransforms.Count; i++)
            {
                TailTransforms[i].position = proceduralPoints[i].Position;
                TailTransforms[i].rotation = proceduralPoints[i].Rotation;
            }
        }

        // V1.1 and V1.1.1/2
        /// <summary>
        /// Calculates target rotation for one tail segment
        /// We will override it for some exception calculations like 2D rotation
        /// </summary>
        protected virtual Quaternion CalculateTargetRotation(Vector3 startLookPos, Vector3 currentPos, FTail_Point previousTailPoint = null, FTail_Point currentTailPoint = null, int lookDirectionFixIndex = 0)
        {
            Quaternion targetRotation;

            //V1.2.5
            int fixDirForw = lookDirectionFixIndex + 1;
            if (lookDirectionFixIndex == -1)
            {
                fixDirForw = 0;
                lookDirectionFixIndex = 0;
            }

            if (FullCorrection)
            {
                targetRotation = Quaternion.identity;

                bool rotationCollision = false;

                if (UseCollision)
                    if (collisionFlags[fixDirForw] > 0f)
                        if (collisionOffsets[fixDirForw] != Vector3.zero)
                        {
                            rotationCollision = true;
                        }

                if (!rotationCollision)
                {
                    if (RolledBones)
                        targetRotation = Quaternion.LookRotation(startLookPos - currentPos, previousTailPoint.TransformDirection(-lookBackDirections[fixDirForw] * 0.99f));
                    else
                        targetRotation = Quaternion.LookRotation(startLookPos - currentPos, previousTailPoint.TransformDirection(AxisLookBack));
                }
                else
                {
                    //Quaternion target = Quaternion.LookRotation(collisionOffsets[fixDirForw], previousTailPoint.TransformDirection(AxisLookBack));
                    //// Quaternion target = Quaternion.LookRotation(collisionOffsets[fixDirForw], previousTailPoint.TransformDirection(AxisLookBack)) * Quaternion.FromToRotation(tailLookDirections[lookDirectionFixIndex], ExtraToDirection);
                    //targetRotation *= Quaternion.Slerp(Quaternion.identity, target, collisionFlags[fixDirForw]);

                    Vector3 tailDirection = (startLookPos - currentPos).normalized;
                    Vector3 upwards;

                    if (RolledBones) upwards = previousTailPoint.TransformDirection(-lookBackDirections[fixDirForw] * 0.99f); else upwards = previousTailPoint.TransformDirection(AxisLookBack);

                    Vector3 smoothedDirection = Vector3.Slerp(tailDirection, (tailDirection + collisionOffsets[currentTailPoint.index]).normalized, collisionFlags[fixDirForw]);
                    targetRotation = Quaternion.LookRotation(smoothedDirection, upwards);
                }

                if (GravityPower != Vector2.zero)
                {
                    float mul = 10 / (fixDirForw * 2.5f + 1);
                    targetRotation *= Quaternion.Euler(GravityPower.y * mul, GravityPower.x * mul, 0f);
                }

                targetRotation *= Quaternion.FromToRotation(tailLookDirections[lookDirectionFixIndex], ExtraToDirection);

                if (AnimateCorrections)
                    targetRotation *= animatedCorrections[fixDirForw];
                else
                    targetRotation *= lookBackOffsets[fixDirForw];
            }
            else
            {
                targetRotation = Quaternion.identity;

                bool rotationCollision = false;
                if (UseCollision) if (collisionFlags[fixDirForw] > 0f) if (collisionOffsets[fixDirForw] != Vector3.zero)
                        {
                            #region Experiments

                            //Quaternion target = Quaternion.LookRotation(collisionOffsets[fixDirForw], previousTailPoint.TransformDirection(AxisLookBack));
                            ////Quaternion target = Quaternion.LookRotation(collisionOffsets[fixDirForw], previousTailPoint.TransformDirection(AxisLookBack)) * Quaternion.FromToRotation(tailLookDirections[lookDirectionFixIndex], ExtraToDirection);
                            //targetRotation *= Quaternion.Slerp(Quaternion.identity, target, collisionFlags[fixDirForw]);

                            //Quaternion target = Quaternion.LookRotation(collisionOffsets[fixDirForw], previousTailPoint.TransformDirection(AxisLookBack));
                            //targetRotation *= Quaternion.Slerp(Quaternion.identity, target, collisionFlags[fixDirForw]);


                            //Vector3 tailDirection = (startLookPos - currentPos).normalized;
                            //Vector3 smoothedDirection = Vector3.Slerp(tailDirection, (tailDirection + collisionOffsets[fixDirForw]).normalized, collisionFlags[fixDirForw]);
                            //targetRotation = Quaternion.LookRotation(smoothedDirection, previousTailPoint.TransformDirection(AxisLookBack * Mathf.Sign(FVectorMethods.VectorSum(AxisCorrection))));

                            //Vector3 smoothedDirection = Vector3.Slerp(tailDirection, (tailDirection + collisionOffsets[fixDirForw]).normalized, collisionFlags[fixDirForw]);

                            //Vector3 upwards = previousTailPoint.TransformDirection(AxisLookBack * Mathf.Sign(FVectorMethods.VectorSum(AxisCorrection)));
                            //Vector3 tailDirection = (startLookPos - currentPos).normalized;
                            //targetRotation = Quaternion.LookRotation(tailDirection, upwards);
                            //targetRotation *= Quaternion.Slerp( Quaternion.identity, Quaternion.LookRotation(collisionOffsets[currentTailPoint.index], upwards), collisionFlags[fixDirForw]);


                            #endregion

                            Vector3 tailDirection = (startLookPos - currentPos).normalized;
                            Vector3 smoothedDirection = Vector3.Slerp(tailDirection, (tailDirection + collisionOffsets[currentTailPoint.index]).normalized, collisionFlags[fixDirForw]);
                            targetRotation = Quaternion.LookRotation(smoothedDirection, previousTailPoint.TransformDirection(AxisLookBack));
                            rotationCollision = true;
                        }

                if (!rotationCollision)
                    targetRotation = Quaternion.LookRotation(startLookPos - currentPos, previousTailPoint.TransformDirection(AxisLookBack * Mathf.Sign(FVectorMethods.VectorSum(AxisCorrection))));


                if (GravityPower != Vector2.zero)
                {
                    float mul = 10 / (fixDirForw * 2.5f + 1);
                    targetRotation *= Quaternion.Euler(GravityPower.y * mul, GravityPower.x * mul, 0f);
                }

                if (ExtraCorrectionOptions)
                    targetRotation *= Quaternion.FromToRotation(ExtraFromDirection, ExtraToDirection);
            }

            return targetRotation;
        }

        /// <summary>
        /// Remove all disconnected transform when object is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        // V.1.1.1
        /// <summary>
        /// Auto correcting tail look axes
        /// </summary>
        protected void ApplyAutoCorrection()
        {
            ExtraCorrectionOptions = true;
            AxisCorrection = tailLookDirections[0];
            ExtraFromDirection = tailLookDirections[0];
        }

        /// <summary>
        /// Simple but effective, pushing component to be executed as last in the frame
        /// </summary>
        public void QueueComponentToLastUpdate()
        {
            enabled = false;
            enabled = true;
        }

        /// <summary>
        /// Method reserved for refreshing stuff in some derived classes every time when something is changed in the inspector
        /// </summary>
        public virtual void OnValidate()
        {

        }

        // V1.2.0
        /// <summary>
        /// Helper class to animate tail bones
        /// </summary>
        protected class FTail_Point
        {
            public int index = -1;
            public Vector3 Position = Vector3.zero;
            public Quaternion Rotation = Quaternion.identity;

            public Vector3 InitialPosition = Vector3.zero;
            public Quaternion InitialRotation = Quaternion.identity;
            public Vector3 InitialLossyScale = Vector3.one;

            public Vector3 TransformDirection(Vector3 dir)
            {
                return Rotation * dir;
            }
        }


        #region V1.2.6 Colliders Support

        /// <summary>
        /// Generating colliders on tail with provided settings
        /// </summary>
        private void AddColliders()
        {
            collisionOffsets = new List<Vector3>();
            collisionFlags = new List<float>();
            collisionContacts = new List<Collision>();

            collisionOffsets.Add(Vector3.zero);
            collisionFlags.Add(0f);
            collisionContacts.Add(null);

            for (int i = 1; i < TailTransforms.Count; i++)
            {
                collisionOffsets.Add(Vector3.zero);
                collisionContacts.Add(null);
                collisionFlags.Add(0f);
                if (CollidersSameLayer) TailTransforms[i].gameObject.layer = gameObject.layer; else TailTransforms[i].gameObject.layer = CollidersLayer;
            }

            if (CollidersType == FTailColliders.Boxes)
            {
                for (int i = 1; i < TailTransforms.Count; i++)
                {
                    BoxCollider b = TailTransforms[i].gameObject.AddComponent<BoxCollider>();
                    FTail_CollisionHelper tcol = TailTransforms[i].gameObject.AddComponent<FTail_CollisionHelper>().Init(CollidersAddRigidbody);
                    tcol.index = i;
                    tcol.ParentTail = this;
                    b.size = GetColliderBoxSizeFor(TailTransforms, i);
                }
            }
            else
            {
                for (int i = 1; i < TailTransforms.Count; i++)
                {
                    SphereCollider b = TailTransforms[i].gameObject.AddComponent<SphereCollider>();
                    FTail_CollisionHelper tcol = TailTransforms[i].gameObject.AddComponent<FTail_CollisionHelper>().Init(CollidersAddRigidbody);
                    tcol.index = i;
                    tcol.ParentTail = this;
                    b.radius = GetColliderSphereRadiusFor(TailTransforms, i);
                }
            }
        }


        //V1.2.6
        /// <summary>
        /// Collision data sent by single tail segment
        /// </summary>
        internal void CollisionDetection(int index, Collision collision)
        {
            collisionContacts[index] = collision;
        }

        /// <summary>
        /// Exitting collision
        /// </summary>
        internal void ExitCollision(int index)
        {
            collisionContacts[index] = null;
        }

        public bool CollisionLookBack = true;

        /// <summary>
        /// Use saved collision contact in right moment when uxecuting update methods
        /// </summary>
        protected void UseCollisionContact(int index)
        {
            if (collisionContacts[index] == null) return;
            if (collisionContacts[index].contacts.Length == 0) return; // In newest Unity 2018 versions 'Collision' class is generated even there are no collision contacts

            Collision collision = collisionContacts[index];
            Vector3 desiredDirection;

            desiredDirection = Vector3.Reflect((proceduralPoints[index - 1].Position - proceduralPoints[index].Position).normalized, collision.contacts[0].normal);

            #region Experiments

            //Quaternion segmentForwQ = Quaternion.LookRotation(proceduralPoints[index - 1].Position - proceduralPoints[index].Position);// * Quaternion.FromToRotation(tailLookDirections[index - 1], ExtraToDirection);
            //desiredDirection = Vector3.Reflect(segmentForwQ.eulerAngles.normalized, collision.contacts[0].normal);

            //Quaternion segmentForwQ = Quaternion.LookRotation(proceduralPoints[index].Position - transform.position);// * Quaternion.FromToRotation(tailLookDirections[index - 1], ExtraToDirection);
            //Quaternion segmentForwQ = proceduralPoints[index].Rotation * Quaternion.FromToRotation(tailLookDirections[index - 1], ExtraToDirection);

            //desiredDirection = Vector3.ProjectOnPlane(segmentForward, collision.contacts[0].normal);
            //desiredDirection = Vector3.Lerp(Vector3.Reflect(segmentForward, collision.contacts[0].normal), desiredDirection, collisionFlagsSlow[index]);

            //Vector3 segmentForward = segmentForwQ.eulerAngles.normalized;


            //desiredDirection = Vector3.Project(segmentForward, collision.contacts[0].normal);
            //Plane collisionPlane = new Plane(collision.contacts[0].normal, collision.contacts[0].point);

            //Vector3 norm = collision.contacts[0].normal;
            //Vector3 dir = segmentForward;
            //Vector3.OrthoNormalize(ref norm, ref dir);


            // desiredDirection = Vector3.ProjectOnPlane((TailTransforms[index].position - TailTransforms[index - 1].position).normalized, collision.contacts[0].normal);
            // Quaternion startDiffQuat = Quaternion.Inverse(proceduralPoints[index].InitialRotation * Quaternion.Inverse( TailTransforms[index].localRotation));

            //if (CollisionLookBack)
            //{
            //    Vector3 backCompensation = segmentForward + (Quaternion.Inverse(TailTransforms[index - 1].localRotation) * proceduralPoints[index - 1].InitialRotation) * Vector3.forward;
            //    desiredDirection -= backCompensation;

            //Quaternion backCompensation = Quaternion.Inverse(segmentForwQ) * (Quaternion.Inverse(TailTransforms[index - 1].localRotation) * proceduralPoints[index - 1].InitialRotation);
            //desiredDirection -= backCompensation * Vector3.forward;
            //}

            //desiredDirection = (transform.rotation * firstBoneInitialRotationQ ) * desiredDirection;
            //desiredDirection = (transform.rotation * firstBoneInitialRotationQ) * desiredDirection;

            #endregion

            //Debug.DrawRay(proceduralPoints[index].Position, desiredDirection, Color.yellow);

            collisionOffsets[index] = desiredDirection; //(Quaternion.Inverse(transform.rotation) * firstBoneInitialRotationQ) * desiredDirection;
            collisionFlags[index] = Mathf.Min(1f, collisionFlags[index] + Time.deltaTime * 8);
        }

        /// <summary>
        /// Calculating automatically scale for colliders on tail
        /// </summary>
        protected Vector3 GetColliderBoxSizeFor(List<Transform> transforms, int i)
        {
            float refDistance = 1f;
            if (transforms.Count > 1) refDistance = Vector3.Distance(transforms[1].position, transforms[0].position);

            float singleScale = Mathf.Lerp(refDistance, Vector3.Distance(transforms[i - 1].position, transforms[i].position) * 0.5f, DifferenceScaleFactor);
            float step = 1f / (float)(transforms.Count - 1);

            Vector3 newScale = Vector3.one * singleScale * CollidersScaleMul * CollidersScale.Evaluate(step * (float)i);
            newScale.x *= BoxesDimensionsMul.x;
            newScale.y *= BoxesDimensionsMul.y;
            newScale.z *= BoxesDimensionsMul.z;

            return newScale;
        }

        /// <summary>
        /// Calculating automatically scale for colliders on tail
        /// </summary>
        protected float GetColliderSphereRadiusFor(List<Transform> transforms, int i)
        {
            float refDistance = 1f;
            if (transforms.Count > 1) refDistance = Vector3.Distance(transforms[1].position, transforms[0].position);

            float singleScale = Mathf.Lerp(refDistance, Vector3.Distance(transforms[i - 1].position, transforms[i].position) * 0.5f, DifferenceScaleFactor);

            float step = 1f / (float)(transforms.Count - 1);

            return 0.5f * singleScale * CollidersScaleMul * CollidersScale.Evaluate(step * (float)i);
        }

        #endregion


        // V1.2.0
#if UNITY_EDITOR

        // Set it to false if you don't want any gizmo
        public static bool drawMainGizmo = true;
        public bool drawGizmos = false;


        /// <summary>
        /// Getting list of transforms in editor mode if we don't defined chain yet
        /// </summary>
        protected List<Transform> GetEditorGizmoTailList()
        {
            editorGizmoTailList = new List<Transform>();

            if (TailTransforms != null && TailTransforms.Count > 1)
            {
                editorGizmoTailList = TailTransforms;
            }
            else
            {
                Transform lastParent = transform;
                bool boneDefined = true;

                if (TailTransforms == null || TailTransforms.Count == 0)
                {
                    boneDefined = false;
                    lastParent = transform;
                }
                else lastParent = TailTransforms[0];

                Transform rootTransform = lastParent;

                for (int i = editorGizmoTailList.Count; i < 100; i++)
                {
                    if (boneDefined)
                        if (lastParent == rootTransform)
                        {
                            if (lastParent == null) break;
                            if (lastParent.childCount == 0) break;
                            lastParent = lastParent.GetChild(0);
                            continue;
                        }

                    editorGizmoTailList.Add(lastParent);

                    if (lastParent.childCount > 0) lastParent = lastParent.GetChild(0); else break;
                }
            }

            return editorGizmoTailList;
        }


        protected virtual void OnDrawGizmos()
        {
            // V1.2.6
            if (!Application.isPlaying)
                if (UseCollision)
                {
                    GetEditorGizmoTailList();

                    Color preCol = Gizmos.color;
                    Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.7f);

                    switch (CollidersType)
                    {
                        case FTailColliders.Boxes:

                            for (int i = 1; i < editorGizmoTailList.Count; i++)
                            {
                                if (editorGizmoTailList[i] == null) continue;
                                Gizmos.matrix = Matrix4x4.TRS(editorGizmoTailList[i].position, editorGizmoTailList[i].rotation, editorGizmoTailList[i].lossyScale);
                                Gizmos.DrawWireCube(Vector3.zero, GetColliderBoxSizeFor(editorGizmoTailList, i));
                            }

                            Gizmos.matrix = Matrix4x4.identity;

                            break;

                        case FTailColliders.Spheres:
                            for (int i = 1; i < editorGizmoTailList.Count; i++)
                            {
                                if (editorGizmoTailList[i] == null) continue;
                                Gizmos.matrix = Matrix4x4.TRS(editorGizmoTailList[i].position, editorGizmoTailList[i].rotation, editorGizmoTailList[i].lossyScale);
                                Gizmos.DrawWireSphere(Vector3.zero, GetColliderSphereRadiusFor(editorGizmoTailList, i));
                            }

                            break;
                    }

                    Gizmos.color = preCol;
                }


            if (!drawMainGizmo) return;

            Gizmos.DrawIcon(transform.position, "FIMSpace/FTail/SPR_TailAnimatorGizmoIcon.png", true);
        }
#endif
    }

}
