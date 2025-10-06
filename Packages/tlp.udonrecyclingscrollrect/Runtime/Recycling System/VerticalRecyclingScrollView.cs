//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using JetBrains.Annotations;
using TLP.RecyclingScrollRect.Runtime.Utils;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Experimental.Tasks;
using TLP.UdonUtils.Runtime.Extensions;
using UnityEngine;
using VRC.SDKBase;

// ReSharper disable UseArrayEmptyMethod

namespace TLP.UdonRecyclingScrollRect.Runtime.Recycling_System
{
    /// <summary>
    ///     Recycling system for Vertical type.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VerticalRecyclingScrollView), ExecutionOrder)]
    public class VerticalRecyclingScrollView : RecyclingScrollView
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = HorizontalRecyclingScrollView.ExecutionOrder + 1;
        #endregion

        public override void OnModelChanged() {
        }

        private float m_CellWidth;
        private float m_CellHeight;
        private bool m_Recycling;

        #region Recycling Trackers
        private int m_TotalDataItems;

        private int m_TopMostCellColumn;
        private int m_TopMostCellIndex;
        private int m_BottomMostCellColumn;
        private int m_BottomMostCellIndex;
        #endregion


        #region TESTING
        public void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                    RecyclingViewBounds.min - new Vector3(2000, 0),
                    RecyclingViewBounds.min + new Vector3(2000, 0)
            );
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                    RecyclingViewBounds.max - new Vector3(2000, 0),
                    RecyclingViewBounds.max + new Vector3(2000, 0)
            );
        }
        #endregion

        #region INIT
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(VerticalRecyclingInitTask)) {
                Error($"{nameof(VerticalRecyclingInitTask)} not set");
                return false;
            }

            return true;
        }

        #region Initial Cell creation
        public VerticalRecyclingInitTask VerticalRecyclingInitTask;

        /// <summary>
        ///     Creates cell Pool for recycling, Caches ICells
        /// </summary>
        private void CreateCellPool() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateCellPool));
#endif
            #endregion

            if (VerticalRecyclingInitTask.State == TaskState.Running) {
                VerticalRecyclingInitTask.Abort();
            }

            VerticalRecyclingInitTask.VerticalRecyclingScrollView = this;
            if (!VerticalRecyclingInitTask.TryScheduleTask(this)) {
                Error(
                        $"{nameof(CreateCellPool)}: Failed to schedule {VerticalRecyclingInitTask.GetScriptPathInScene()}");
                return;
            }
        }

        internal void CreateNeededCells() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateNeededCells));
#endif
            #endregion

            if (!Utilities.IsValid(DataSource)) {
                Error($"{nameof(DataSource)} invalid");
                return;
            }

            //Get the required pool coverage and minimum size for the Cell pool
            float requiredCoverage = GetRequiredCoverage();
            int minPoolSize = GetMinPoolSize();

            //create cells until the Pool area is covered and pool size is the minimum required
            //Temps
            float currentPoolCoverage = 0;
            float posY = 0;
            int totalItemCount = DataSource.GetItemCount();
            // TODO performance: turn into task
            int poolSize = 0;
            for (;
                 PoolIsNotFilled(
                         poolSize,
                         minPoolSize,
                         currentPoolCoverage,
                         requiredCoverage,
                         totalItemCount
                 );
                 ++poolSize) {
                var createdRectTransform = AddNewCellToPool();
                PositionCell(createdRectTransform, ref posY, ref currentPoolCoverage);
                AddCellWithData(createdRectTransform, poolSize);
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(CreateNeededCells)}: {nameof(poolSize)}={poolSize}");
#endif
            #endregion


            UpdateBottomMostCellColumn();
        }


        internal void UpdateBottomMostCellColumn() {
#if TLP_DEBUG
            DebugLog(nameof(UpdateBottomMostCellColumn));
#endif
            //TODO : you already have a _currentColumn variable. Why this calculation?????
            if (IsGrid) {
                m_BottomMostCellColumn = (m_BottomMostCellColumn - 1 + Dimension) % Dimension;
            }
        }

        internal void DeactivateCellPrototype() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(DeactivateCellPrototype));
#endif
            #endregion

            PrototypeCell.gameObject.SetActive(false);
        }


        internal void PositionCell(RectTransform createdRectTransform, ref float posY, ref float currentPoolCoverage) {
#if TLP_DEBUG
            DebugLog(nameof(PositionCell));
#endif
            var cellRect = createdRectTransform.rect;
            if (IsGrid) {
                PositionCellInGrid(createdRectTransform, posY);
                GoToNextColumn(ref posY, ref currentPoolCoverage, cellRect.height);
            } else {
                PositionCell(createdRectTransform, posY);
                posY = GoToNextLine(createdRectTransform, ref currentPoolCoverage, cellRect.height);
            }
        }


        internal void AddCellWithData(RectTransform cell, int index) {
#if TLP_DEBUG
            DebugLog(nameof(AddCellWithData));
#endif
            var recyclingScrollRectCell = cell.GetComponent<RecyclingScrollRectCellView>();
            AppendCachedCell(recyclingScrollRectCell);
            DataSource.SetCell(recyclingScrollRectCell, index);
        }

        private static float GoToNextLine(
                RectTransform createdRectTransform,
                ref float currentPoolCoverage,
                float cellHeight
        ) {
#if TLP_DEBUG
            Debug.Log(nameof(GoToNextLine));
#endif
            currentPoolCoverage += cellHeight;
            return createdRectTransform.anchoredPosition.y - cellHeight;
        }


        private static void PositionCell(RectTransform createdRectTransform, float posY) {
            createdRectTransform.anchoredPosition = new Vector2(0, posY);
        }


        private void PositionCellInGrid(RectTransform createdRectTransform, float posY) {
#if TLP_DEBUG
            DebugLog(nameof(PositionCellInGrid));
#endif
            float posX = m_BottomMostCellColumn * m_CellWidth;
            createdRectTransform.anchoredPosition = new Vector2(posX, posY);
        }


        private void GoToNextColumn(ref float posY, ref float currentPoolCoverage, float cellHeight) {
#if TLP_DEBUG
            DebugLog(nameof(GoToNextColumn));
#endif
            ++m_BottomMostCellColumn;
            if (m_BottomMostCellColumn < Dimension) {
                return;
            }

            m_BottomMostCellColumn = 0;
            posY -= m_CellHeight;
            currentPoolCoverage += cellHeight;
        }


        internal static bool PoolIsNotFilled(
                int poolSize,
                int minPoolSize,
                float currentPoolCoverage,
                float requiredCoverage,
                int totalItemCount
        ) {
            return (IsMinPoolSizeNotReached(poolSize, minPoolSize)
                    || IsRequiredCoverageNotReached(currentPoolCoverage, requiredCoverage))
                   && IsTotalItemCountNotReached(poolSize, totalItemCount);
        }


        internal RectTransform AddNewCellToPool() {
#if TLP_DEBUG
            DebugLog(nameof(AddNewCellToPool));
#endif
            var createdRectTransform = CreateNewCell();
            AppendPoolCell(createdRectTransform);
            createdRectTransform.SetParent(Content, false);
            return createdRectTransform;
        }


        private RectTransform CreateNewCell() {
#if TLP_DEBUG
            DebugLog(nameof(CreateNewCell));
#endif
            var retrievedCell = UiCellPool.Get();
            if (!Utilities.IsValid(retrievedCell)) {
                Error($"{nameof(retrievedCell)} invalid");
                return null;
            }

            var createdRectTransform = retrievedCell.GetComponent<RectTransform>();
            if (!Utilities.IsValid(createdRectTransform)) {
                Error($"cells must have a {nameof(RectTransform)} component!");
                return null;
            }

            InitializeCellAnchor(createdRectTransform);

            createdRectTransform.sizeDelta = new Vector2(m_CellWidth, m_CellHeight);
            return createdRectTransform;
        }


        private static bool IsRequiredCoverageNotReached(float currentPoolCoverage, float requiredCoverage) {
            return currentPoolCoverage < requiredCoverage;
        }


        private static bool IsMinPoolSizeNotReached(int poolSize, int minPoolSize) {
            return poolSize < minPoolSize;
        }


        internal float GetRequiredCoverage() {
            return MinPoolCoverage * Viewport.rect.height;
        }


        internal int GetMinPoolSize() {
            if (Utilities.IsValid(DataSource)) {
                return Math.Min(MinPoolSize, DataSource.GetItemCount());
            }

            Error($"{nameof(DataSource)} invalid");
            return MinPoolSize;
        }


        internal void InitializeState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeState));
#endif
            #endregion

            //Reset
            m_TopMostCellColumn = 0;
            m_BottomMostCellColumn = 0;

            //set new cell size according to its aspect ratio
            m_CellWidth = Content.rect.width / Dimension;
            var prototypeCellSizeDelta = PrototypeCell.sizeDelta;
            m_CellHeight = prototypeCellSizeDelta.y / prototypeCellSizeDelta.x * m_CellWidth;
        }


        internal void InitializePrototypeCell() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializePrototypeCell));
#endif
            #endregion

            var prototypeCell = PrototypeCell;
            prototypeCell.gameObject.SetActive(false);
            InitializeCellAnchor(prototypeCell);
        }

        private void InitializeCellAnchor(RectTransform prototypeCell) {
            if (IsGrid) {
                SetTopLeftAnchor(prototypeCell);
            } else {
                SetTopAnchor(prototypeCell);
            }
        }
        #endregion


        internal void ResetCellViews() {
            int entries = CellPool.LengthSafe();
            for (int i = 0; i < entries; i++) {
                UiCellPool.Return(CellPool[i].gameObject);
            }

            CellPool = new RectTransform[0];
        }

        internal void ResetCachedCellViews() {
            int entries = CachedCellViews.LengthSafe();
            for (int i = 0; i < entries; i++) {
                UiCellPool.Return(CachedCellViews[i].gameObject);
            }

            CachedCellViews = new RecyclingScrollRectCellView[0];
        }
        #endregion

        #region Overrides
        protected override void InitializeCells() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitializeCells));
#endif
            #endregion#if TLP_DEBUG

            SetTopAnchor(Content);
            Content.anchoredPosition = Vector2.zero;

            SetRecyclingBounds();
            CreateCellPool();
        }

        internal void FinalizeInitialization() {
            m_TotalDataItems = CellPool.Length;
            m_TopMostCellIndex = 0;
            m_BottomMostCellIndex = CellPool.Length - 1;

            //Set content height according to number of rows
            int noOfRows = (int)Mathf.Ceil(CellPool.Length / (float)Dimension);
            float contentYSize = noOfRows * m_CellHeight;
            Content.sizeDelta = new Vector2(Content.sizeDelta.x, contentYSize);
            SetTopAnchor(Content);
        }
        #endregion

        #region Public API
        /// <summary>
        ///     Recycling entry point
        /// </summary>
        /// <param name="direction">scroll direction </param>
        /// <returns></returns>
        [PublicAPI]
        public override Vector2 ProcessChangeAndRecycle(Vector2 direction) {
#if TLP_DEBUG
            DebugLog(nameof(ProcessChangeAndRecycle));
#endif
            if (DoesNotNeedToRecycle()) {
                return Vector2.zero;
            }

            //Updating Recycling view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            if (IsScrollingDownBeyondVisibleElements(direction)) {
                return RecycleTopToBottom();
            }

            if (IsScrollingUpBeyondVisibleElements(direction)) {
                return RecycleBottomToTop();
            }

            return Vector2.zero;
        }
        #endregion

        #region RECYCLING
        private readonly Vector3[] m_CornerCache = new Vector3[4];


        private bool IsScrollingUpBeyondVisibleElements(Vector2 direction) {
            if (m_TopMostCellIndex < 0 || m_TopMostCellIndex >= CellPool.LengthSafe()) {
                return false;
            }

            var topMostCell = CellPool[m_TopMostCellIndex];
            topMostCell.GetWorldCorners(m_CornerCache);
            return direction.y < 0 && RectTransformExtensions.MinY(m_CornerCache) < RecyclingViewBounds.max.y;
        }


        private bool IsScrollingDownBeyondVisibleElements(Vector2 direction) {
            #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog($"{nameof(IsScrollingDownBeyondVisibleElements)}: {nameof(direction)}={direction}");
#endif
            #endregion

            if (m_BottomMostCellIndex < 0 || m_BottomMostCellIndex >= CellPool.LengthSafe()) {
                return false;
            }

            var bottomMostCell = CellPool[m_BottomMostCellIndex];
            bottomMostCell.GetWorldCorners(m_CornerCache);
            return direction.y > 0 && RectTransformExtensions.MaxY(m_CornerCache) > RecyclingViewBounds.min.y;
        }


        private bool DoesNotNeedToRecycle() {
            return m_Recycling || CellPool.LengthSafe() == 0;
        }

        /// <summary>
        ///     Recycles cells from top to bottom in the List hierarchy
        /// </summary>
        private Vector2 RecycleTopToBottom() {
#if TLP_DEBUG
            DebugLog(nameof(RecycleTopToBottom));
#endif

            if (m_BottomMostCellIndex < 0 || m_BottomMostCellIndex >= CellPool.LengthSafe()) {
                return Vector2.zero;
            }

            m_Recycling = true;

            int n = 0;
            float posY = IsGrid ? CellPool[m_BottomMostCellIndex].anchoredPosition.y : 0;

            //to determine if content size needs to be updated
            int additionalRows = 0;
            //Recycle until cell at Top is available and current item count smaller than datasource
            int totalItemCount = DataSource.GetItemCount();
            while (ViewPortBottomNotReached(totalItemCount)) {
                ReuseTopCellAtBottom(ref n, ref posY, ref additionalRows);

                ++m_TotalDataItems;
                if (!IsGrid) {
                    ++n;
                }
            }

            //Content size adjustment
            if (IsGrid) {
                Content.sizeDelta += Vector2.up * (additionalRows * m_CellHeight);
                //TODO : check if it is supposed to be done only when > 0
                if (additionalRows > 0) {
                    n -= additionalRows;
                }
            }

            m_Recycling = false;
            return RePositionContent(-n);
        }


        private Vector2 RePositionContent(int n) {
#if TLP_DEBUG
            DebugLog(nameof(RePositionContent));
#endif
            float verticalPositionChange = n * CellPool[m_TopMostCellIndex].sizeDelta.y;
            AdjustContentAnchorPosition(verticalPositionChange);
            return new Vector2(0, verticalPositionChange);
        }


        private void AdjustContentAnchorPosition(float verticalPositionChange) {
#if TLP_DEBUG
            DebugLog(nameof(AdjustContentAnchorPosition));
#endif
            var positionChange = Vector2.up * verticalPositionChange;
            foreach (var cell in CellPool) {
                cell.anchoredPosition -= positionChange;
            }

            Content.anchoredPosition += positionChange;
        }


        private bool ViewPortBottomNotReached(int totalItemCount) {
#if TLP_DEBUG
            DebugLog(nameof(ViewPortBottomNotReached));
#endif
            var topMostCell = CellPool[m_TopMostCellIndex];
            topMostCell.GetWorldCorners(m_CornerCache);

            return RectTransformExtensions.MinY(m_CornerCache) > RecyclingViewBounds.max.y
                   && IsTotalItemCountNotReached(m_TotalDataItems, totalItemCount);
        }


        private void ReuseTopCellAtBottom(ref int n, ref float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(ReuseTopCellAtBottom));
#endif
            if (IsGrid) {
                GoToNextBottomMostCellColumn(ref n, ref posY, ref additionalRows);
                MoveTopGridCellToBottom(posY, ref additionalRows);
            } else {
                posY = MoveTopCellToBottom();
            }

            ConnectCellWithData(m_TopMostCellIndex, m_TotalDataItems);

            m_BottomMostCellIndex = m_TopMostCellIndex;
            m_TopMostCellIndex = (m_TopMostCellIndex + 1) % CellPool.Length;
        }


        private void ConnectCellWithData(int cellIndex, int dataIndex) {
#if TLP_DEBUG
            DebugLog(nameof(ConnectCellWithData));
#endif
            if (cellIndex < 0 || cellIndex >= CachedCellViews.LengthSafe()) {
                return;
            }
            DataSource.SetCell(CachedCellViews[cellIndex], dataIndex);
        }


        private void GoToNextBottomMostCellColumn(ref int n, ref float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(GoToNextBottomMostCellColumn));
#endif
            ++m_BottomMostCellColumn;
            if (m_BottomMostCellColumn < Dimension) {
                return;
            }

            n++;
            m_BottomMostCellColumn = 0;
            posY = CellPool[m_BottomMostCellIndex].anchoredPosition.y - m_CellHeight;
            ++additionalRows;
        }


        private void MoveTopGridCellToBottom(float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(MoveTopGridCellToBottom));
#endif
            float posX = m_BottomMostCellColumn * m_CellWidth;
            CellPool[m_TopMostCellIndex].anchoredPosition = new Vector2(posX, posY);

            if (++m_TopMostCellColumn >= Dimension) {
                m_TopMostCellColumn = 0;
                additionalRows--;
            }
        }


        private float MoveTopCellToBottom() {
#if TLP_DEBUG
            DebugLog(nameof(MoveTopCellToBottom));
#endif
            float posY = CellPool[m_BottomMostCellIndex].anchoredPosition.y -
                         CellPool[m_BottomMostCellIndex].sizeDelta.y;
            CellPool[m_TopMostCellIndex].anchoredPosition =
                    new Vector2(CellPool[m_TopMostCellIndex].anchoredPosition.x, posY);
            return posY;
        }

        /// <summary>
        ///     Recycles cells from bottom to top in the List hierarchy
        /// </summary>
        private Vector2 RecycleBottomToTop() {
#if TLP_DEBUG
            DebugLog(nameof(RecycleBottomToTop));
#endif
            if (m_TopMostCellIndex < 0 || m_TopMostCellIndex >= CellPool.LengthSafe()) {
                return Vector2.zero;
            }

            m_Recycling = true;

            int n = 0;
            float posY = IsGrid ? CellPool[m_TopMostCellIndex].anchoredPosition.y : 0;

            //to determine if content size needs to be updated
            int additionalRows = 0;
            //Recycle until cell at bottom is available and current item count is greater than cell-pool size
            while (ViewportTopNotReached()) {
                ReuseBottomCellAtTop(ref n, ref posY, ref additionalRows);

                m_TotalDataItems--;
                ConnectCellWithData(m_BottomMostCellIndex, m_TotalDataItems - CellPool.Length);

                //set new indices
                m_TopMostCellIndex = m_BottomMostCellIndex;
                m_BottomMostCellIndex = (m_BottomMostCellIndex - 1 + CellPool.Length) % CellPool.Length;
            }

            if (IsGrid) {
                Content.sizeDelta += Vector2.up * (additionalRows * m_CellHeight);
                //TODO : check if it is supposed to be done only when > 0
                if (additionalRows > 0) {
                    n -= additionalRows;
                }
            }

            m_Recycling = false;

            return RePositionContent(n);
        }


        private bool ViewportTopNotReached() {
#if TLP_DEBUG
            DebugLog(nameof(ViewportTopNotReached));
#endif
            var bottomMostCell = CellPool[m_BottomMostCellIndex];
            bottomMostCell.GetWorldCorners(m_CornerCache);
            return RectTransformExtensions.MaxY(m_CornerCache) < RecyclingViewBounds.min.y &&
                   m_TotalDataItems > CellPool.Length;
        }


        private void ReuseBottomCellAtTop(ref int n, ref float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(ReuseBottomCellAtTop));
#endif
            if (IsGrid) {
                GoToPreviousColumn(ref n, ref posY, ref additionalRows);
                MoveBottomGridCellToTop(posY, ref additionalRows);
            } else {
                posY = MoveBottomCellToTop(ref n);
            }
        }


        private float MoveBottomCellToTop(ref int n) {
#if TLP_DEBUG
            DebugLog(nameof(MoveBottomCellToTop));
#endif
            float posY = CellPool[m_TopMostCellIndex].anchoredPosition.y + CellPool[m_TopMostCellIndex].sizeDelta.y;
            CellPool[m_BottomMostCellIndex].anchoredPosition =
                    new Vector2(CellPool[m_BottomMostCellIndex].anchoredPosition.x, posY);
            n++;
            return posY;
        }


        private void GoToPreviousColumn(ref int n, ref float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(GoToPreviousColumn));
#endif
            --m_TopMostCellColumn;
            if (m_TopMostCellColumn < 0) {
                n++;
                m_TopMostCellColumn = Dimension - 1;
                posY = CellPool[m_TopMostCellIndex].anchoredPosition.y + m_CellHeight;
                ++additionalRows;
            }
        }


        private void MoveBottomGridCellToTop(float posY, ref int additionalRows) {
#if TLP_DEBUG
            DebugLog(nameof(MoveBottomGridCellToTop));
#endif
            float posX = m_TopMostCellColumn * m_CellWidth;
            CellPool[m_BottomMostCellIndex].anchoredPosition = new Vector2(posX, posY);

            --m_BottomMostCellColumn;
            if (m_BottomMostCellColumn >= 0) {
                return;
            }

            m_BottomMostCellColumn = Dimension - 1;
            additionalRows--;
        }
        #endregion

        #region HELPERS
        /// <summary>
        ///     Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetTopAnchor(RectTransform rectTransform) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(SetTopAnchor)}: {nameof(rectTransform)}={rectTransform}");
#endif
            #endregion

            //Saving to reapply after anchoring. Width and height changes if anchoring is change.
            var rect = rectTransform.rect;
            float width = rect.width;
            float height = rect.height;

            //Setting top anchor
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
#if TLP_DEBUG
            DebugLog($"{nameof(SetTopAnchor)}: {nameof(height)}={height}");
            DebugLog($"{nameof(SetTopAnchor)}: {nameof(rectTransform.sizeDelta)}={rectTransform.sizeDelta}");
#endif
        }


        private void SetTopLeftAnchor(RectTransform rectTransform) {
#if TLP_DEBUG
            DebugLog(nameof(MoveBottomGridCellToTop));
#endif
            //Saving to reapply after anchoring. Width and height changes if anchoring is change.
            var rectTransformRect = rectTransform.rect;
            float width = rectTransformRect.width;
            float height = rectTransformRect.height;

            //Setting top anchor
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            //Re-apply size
            rectTransform.sizeDelta = new Vector2(width, height);

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(SetTopLeftAnchor)}: {nameof(height)}={height}");
            DebugLog($"{nameof(SetTopLeftAnchor)}: {nameof(rectTransform.sizeDelta)}={rectTransform.sizeDelta}");
#endif
            #endregion
        }

        #region Internal
        /// <summary>
        ///     Sets the upper and lower bounds for recycling cells.
        /// </summary>
        private void SetRecyclingBounds() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(SetRecyclingBounds));
#endif
            #endregion

            Viewport.GetWorldCorners(Corners);
            float threshHold = RecyclingThreshold * (Corners[2].y - Corners[0].y);
            // TODO use RectTransformExtensions methods
            RecyclingViewBounds.min = new Vector3(Corners[0].x, Corners[0].y - threshHold);
            RecyclingViewBounds.max = new Vector3(Corners[2].x, Corners[2].y + threshHold);
        }
        #endregion
        #endregion

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case TaskScheduler.FinishedTaskCallbackName:

                    #region TLP_DEBUG
#if TLP_DEBUG
                    DebugLog_OnEvent(eventName);
#endif
                    #endregion

                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
    }
}