//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using JetBrains.Annotations;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using TLP.UdonRecyclingScrollRect.Runtime.Recycling_System;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TLP.UdonRecyclingScrollRect.Runtime
{
    public enum DirectionType
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    ///     Entry for the recycling system. Extends Unity's inbuilt ScrollRect.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingScrollRect), ExecutionOrder)]
    public class RecyclingScrollRect : View
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder + 200;
        #endregion

        #region Dependencies
        [FormerlySerializedAs("scrollRect")]
        [SerializeField]
        private ScrollRect ScrollRect;

        [FormerlySerializedAs("VerticalRecycling")]
        [FormerlySerializedAs("verticalRecycling")]
        [SerializeField]
        private VerticalRecyclingScrollView VerticalRecyclingScrollView;

        [FormerlySerializedAs("HorizontalRecycling")]
        [FormerlySerializedAs("horizontalRecycling")]
        [SerializeField]
        private HorizontalRecyclingScrollView HorizontalRecyclingScrollView;

        [FormerlySerializedAs("onValueChanged")]
        [SerializeField]
        private UdonEvent OnValueChanged;
        #endregion

        #region Configuration
        [FormerlySerializedAs("dataSource")]
        [Header("Configuration")]
        [SerializeField]
        protected internal RecyclingScrollRectDataSource DataSource;

        [FormerlySerializedAs("prototypeCell")]
        [Tooltip(
                "Prototype cell can either be a prefab or present as a child to the content(will automatically be disabled in runtime)"
        )]
        [SerializeField]
        private RectTransform PrototypeCell;

        [FormerlySerializedAs("direction")]
        [SerializeField]
        private DirectionType Direction;

        [FormerlySerializedAs("selfInitialize")]
        [Tooltip(
                "If true the initialization happens at Start. " +
                "Controller must assign the datasource in Awake. " +
                "Set to false if self init is not required and use public init API."
        )]
        [SerializeField]
        private bool SelfInitialize = true;

        [FormerlySerializedAs("isGrid")]
        [SerializeField]
        private bool IsGrid;

        [FormerlySerializedAs("segments")]
        [SerializeField]
        [Tooltip("Columns for vertical and rows for horizontal")]
        private int _segments;

        /// <summary>
        /// columns for vertical and rows for horizontal.
        /// </summary>
        public int Segments
        {
            set => _segments = Math.Max(value, 2);
            get => _segments;
        }
        #endregion

        #region State
        private Vector2 m_PrevAnchoredPos;
        private RecyclingScrollView _mRecyclingScrollView;
        #endregion

        #region Monobehaviour
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            //default(built-in) in scroll rect can have both directions enabled, Recycling scroll rect can be scrolled in only one direction.
            //setting default as vertical, Initialize() will set this again.
            ScrollRect.vertical = true;
            ScrollRect.horizontal = false;

            if (SelfInitialize) {
                Initialize(DataSource);
            }

            return true;
        }

        protected virtual void OnEnable() {
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif

            m_InitializingQueue = 0;
            if (Utilities.IsValid(m_PendingSource)) {
                Initialize(m_PendingSource);
            } else {
                Initialize(DataSource);
            }
        }
        #endregion

        #region Public API
        private int m_InitializingQueue = 0;
        private RecyclingScrollRectDataSource m_PendingSource;

        /// <summary>
        ///     public API for Initializing when datasource is not set in controller's Awake. Make sure selfInitialize is set to
        ///     false.
        /// </summary>
        [PublicAPI]
        [RecursiveMethod]
        public void Initialize(RecyclingScrollRectDataSource source) {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            ++m_InitializingQueue;
            m_PendingSource = source;

            if (!enabled) {
#if TLP_DEBUG
                DebugLog("Initalization will be performed once component is enabled again");
#endif
                return;
            }

            if (m_InitializingQueue > 1) {
                Warn("Already initializing, scheduling update");
                return;
            }

            if (!Utilities.IsValid(source)) {
                Error($"{nameof(source)} invalid");
                return;
            }

            DataSource = source;
            if (!Initialize()) {
                ClearPendingInitialization();
            }
        }

        private void ClearPendingInitialization() {
            m_InitializingQueue = 0;
            m_PendingSource = null;
        }

        /// <summary>
        ///     Reloads the data. Call this if a new datasource is assigned.
        /// </summary>
        [PublicAPI]
        public void ReloadData() {
#if TLP_DEBUG
            DebugLog(nameof(ReloadData));
#endif
            ReloadData(DataSource);
        }

        #endregion

        #region Internal
        [RecursiveMethod]
        private bool Initialize() {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif

            if (!Utilities.IsValid(DataSource)) {
                Error($"{nameof(DataSource)} not set");
                return false;
            }

            switch (Direction) {
                //Construct the recycling system.
                case DirectionType.Vertical:
                    _mRecyclingScrollView = VerticalRecyclingScrollView;
                    VerticalRecyclingScrollView.Initialize(
                            PrototypeCell,
                            ScrollRect.viewport,
                            ScrollRect.content,
                            DataSource,
                            IsGrid,
                            Segments
                    );
                    break;
                case DirectionType.Horizontal:
                    _mRecyclingScrollView = HorizontalRecyclingScrollView;
                    HorizontalRecyclingScrollView.Initialize(
                            PrototypeCell,
                            ScrollRect.viewport,
                            ScrollRect.content,
                            DataSource,
                            IsGrid,
                            Segments
                    );
                    break;
                default:
                    Error($"Unknown direction '{Direction}'");
                    return false;
            }

            ScrollRect.vertical = Direction == DirectionType.Vertical;
            ScrollRect.horizontal = Direction == DirectionType.Horizontal;

            m_PrevAnchoredPos = ScrollRect.content.anchoredPosition;

            OnValueChanged.RemoveListener(this, true);
            _mRecyclingScrollView.OnInitialized.AddListenerVerified(this, nameof(StartListeningForChange));
            return true;
        }

        private static Vector2 GetAnchoredPosition(Transform parent, Transform target) {
            return parent.InverseTransformPoint(target.position);
        }

        /// <summary>
        ///     Overloaded ReloadData with dataSource param
        ///     Reloads the data. Call this if a new datasource is assigned.
        /// </summary>
        private void ReloadData(RecyclingScrollRectDataSource source) {
#if TLP_DEBUG
            DebugLog(nameof(ReloadData));
#endif
            if (!Utilities.IsValid(_mRecyclingScrollView)) {
                Error($"{nameof(_mRecyclingScrollView)} invalid");
                return;
            }

            ScrollRect.StopMovement();
            OnValueChanged.RemoveListener(this, true);

            _mRecyclingScrollView.DataSource = source;
            _mRecyclingScrollView.OnInitialized.RemoveListener(this, true);
            _mRecyclingScrollView.OnInitialized.AddListenerVerified(this, nameof(StartListeningForChange));
            m_PrevAnchoredPos = ScrollRect.content.anchoredPosition;
        }
        #endregion

        #region Event Listeners
        public override void OnEvent(string eventName) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog_OnEvent(eventName);
#endif
            #endregion

            switch (eventName) {
                case nameof(OnValueChangedListener):

                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    OnValueChangedListener();
                    return;
                case nameof(StartListeningForChange):

                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    StartListeningForChange();
                    return;
                default:
                    base.OnEvent(eventName);
                    return;
            }
        }

        public override void OnModelChanged() {
        }

        private int m_LastUpdate;

        private void OnValueChangedListener() {
#if TLP_DEBUG
            DebugLog(nameof(OnValueChangedListener));
#endif
            if (!enabled) {
                return;
            }


            int frameCount = Time.frameCount;
            if (m_LastUpdate == frameCount) {
                return;
            }

            m_LastUpdate = frameCount;
            // TODO this is a dumb workaround, we are missing a RemoveListener somewhere or have to many AddListenerVerified calls
            OnValueChanged.RemoveListener(this, true);

            var delta = ScrollRect.content.anchoredPosition - m_PrevAnchoredPos;
            var velocity = ScrollRect.velocity;

            // TODO: find a way to move the content after the update by the _recyclingSystem
            // scrollRect.m_ContentStartPosition += _recyclingStrategy.ProcessChangeAndRecycle(direction);

            var recycledDistance = _mRecyclingScrollView.ProcessChangeAndRecycle(delta);
            if (recycledDistance.sqrMagnitude > 0.001f) {
                // disable and re-enable to invoke the StopDrag event,
                // otherwise we get very fast scrolling in the wrong direction.
                // This also means that currently the best way to navigate is scrolling instead of clicking and dragging :(
                ScrollRect.enabled = false;
                // ReSharper disable once Unity.InefficientPropertyAccess
                ScrollRect.enabled = true;

                // add the velocity again
                ScrollRect.velocity = velocity;
            }

            m_PrevAnchoredPos = ScrollRect.content.anchoredPosition;

            // start listening again
            OnValueChanged.AddListenerVerified(this, nameof(OnValueChangedListener));
        }

        private void StartListeningForChange() {
#if TLP_DEBUG
            DebugLog(nameof(StartListeningForChange));
#endif
            if (m_InitializingQueue > 1) {
                m_InitializingQueue = 0;
                Initialize(m_PendingSource);
                return;
            }

            OnValueChanged.AddListenerVerified(this, nameof(OnValueChangedListener));
            ClearPendingInitialization();
        }
        #endregion
    }
}