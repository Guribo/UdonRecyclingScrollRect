//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com

// Converted to UdonSharp and modified (U#) by Guribo

using JetBrains.Annotations;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Pool;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonRecyclingScrollRect.Runtime.Recycling_System
{
    /// <summary>
    ///     Abstract Class for creating a Recycling system.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingScrollView), ExecutionOrder)]
    public abstract class RecyclingScrollView : View
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RecyclingScrollRectCellView.ExecutionOrder + 10;
        #endregion

        #region Events
        [FormerlySerializedAs("onInitialized")]
        public UdonEvent OnInitialized;
        #endregion

        #region Settings
        [FormerlySerializedAs("dataSource")]
        public RecyclingScrollRectDataSource DataSource;

        protected bool IsGrid;

        protected const float MinPoolCoverage = 2f; // The Recycling pool must cover (viewPort * _poolCoverage) area.
        protected const int MinPoolSize = 10; // Cell pool must have a min size
        protected const float RecyclingThreshold = 0f; //Threshold for recycling above and below viewport
        #endregion

        #region UI
        protected RectTransform PrototypeCell;
        protected RectTransform Viewport;
        protected RectTransform Content;

        protected Bounds RecyclingViewBounds;
        protected readonly Vector3[] Corners = new Vector3[4];
        protected int Dimension;

        protected RectTransform[] CellPool = new RectTransform[0];
        protected RecyclingScrollRectCellView[] CachedCellViews = new RecyclingScrollRectCellView[0];
        public Pool UiCellPool;
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(UiCellPool)) {
                Error($"{nameof(UiCellPool)} not set");
                return false;
            }

            return true;
        }

        #region Public API
        [PublicAPI]
        public abstract Vector2 ProcessChangeAndRecycle(Vector2 direction);

        [PublicAPI]
        public void Initialize(
                RectTransform prototypeCell,
                RectTransform viewport,
                RectTransform content,
                RecyclingScrollRectDataSource data,
                bool isGrid,
                int dimension
        ) {
#if TLP_DEBUG
            DebugLog(nameof(Initialize));
#endif
            PrototypeCell = prototypeCell;
            Viewport = viewport;
            Content = content;
            DataSource = data;
            IsGrid = isGrid;
            Dimension = isGrid ? dimension : 1;
            RecyclingViewBounds = new Bounds();
            SendCustomEventDelayedFrames(nameof(Delayed_InitializeCells), 1);
        }

        /// <summary>
        ///     Coroutine for initialization.
        ///     Using coroutine for init because few UI stuff requires a frame to update
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>
        public void Delayed_InitializeCells() {
#if TLP_DEBUG
            DebugLog(nameof(Delayed_InitializeCells));
#endif
            InitializeCells();
            CompleteDelayedInitialization();
        }

        protected virtual void InitializeCells() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeCells));
#endif
#endregion
        }

        [PublicAPI]
        public virtual void CompleteDelayedInitialization() {
#if TLP_DEBUG
            DebugLog(nameof(CompleteDelayedInitialization));
#endif
            OnInitialized.Raise(this);
        }

        #endregion

        #region Internal
        protected void AppendCachedCell(RecyclingScrollRectCellView cellView) {
#if TLP_DEBUG
            DebugLog(nameof(AppendCachedCell));
#endif
            var temp = new RecyclingScrollRectCellView[CachedCellViews.Length + 1];
            CachedCellViews.CopyTo(temp, 0);
            temp[CachedCellViews.Length] = cellView;
            CachedCellViews = temp;
        }

        protected void AppendPoolCell(RectTransform cell) {
#if TLP_DEBUG
            DebugLog(nameof(AppendPoolCell));
#endif
            var temp = new RectTransform[CachedCellViews.Length + 1];
            CellPool.CopyTo(temp, 0);
            temp[CellPool.Length] = cell;
            CellPool = temp;
        }

        protected static bool IsTotalItemCountNotReached(int itemCount, int totalItemCount) {
            return itemCount < totalItemCount;
        }
        #endregion
    }
}