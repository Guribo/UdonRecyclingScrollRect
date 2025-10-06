//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using System;
using JetBrains.Annotations;
using TLP.RecyclingScrollRect.Runtime.Utils;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using TLP.UdonUtils.Runtime.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLP.UdonRecyclingScrollRect.Runtime.Recycling_System
{
    /// <summary>
    ///     Recycling system for horizontal type.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(HorizontalRecyclingScrollView), ExecutionOrder)]
    public class HorizontalRecyclingScrollView : RecyclingScrollView
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RecyclingScrollView.ExecutionOrder + 1;
        #endregion

        public override void OnModelChanged() {
        }

        //Cell dimensions
        private float _cellWidth, _cellHeight;
        private int _leftMostCellRow, _rightMostCellRow; // used for recyling in Grid layout. leftmost and rightmost row
        private Bounds _RecyclingViewBounds;
        private bool _recycling;

        //Trackers
        private int _currentItemCount; //item count corresponding to the datasource.
        private int _leftMostCellIndex, _rightMostCellIndex; //Topmost and bottommost cell in the List

        //Cached zero vector
        private readonly Vector2 _zeroVector = Vector2.zero;

        #region HELPERS
        /// <summary>
        ///     Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetLeftAnchor(RectTransform rectTransform) {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change.
            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;

            var pos = IsGrid ? new Vector2(0, 1) : new Vector2(0, 0.5f);

            //Setting top anchor
            rectTransform.anchorMin = pos;
            rectTransform.anchorMax = pos;
            rectTransform.pivot = pos;

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        #endregion

        #region TESTING
        public void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                    _RecyclingViewBounds.min - new Vector3(0, 2000),
                    _RecyclingViewBounds.min + new Vector3(0, 2000));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                    _RecyclingViewBounds.max - new Vector3(0, 2000),
                    _RecyclingViewBounds.max + new Vector3(0, 2000));
        }
        #endregion

        #region INIT
        /// <summary>
        ///     Corotuine for initiazation.
        ///     Using coroutine for init because few UI stuff requires a frame to update
        /// </summary>
        /// <param name="onInitialized">callback when init done</param>
        /// <returns></returns>
        protected override void InitializeCells() {
#if TLP_DEBUG
            DebugLog(nameof(InitializeCells));
#endif
            //Setting up container and bounds
            SetLeftAnchor(Content);
            Content.anchoredPosition = Vector3.zero;
            SetRecyclingBounds();

            CreateCellPool();
            _currentItemCount = CellPool.Length;
            _leftMostCellIndex = 0;
            _rightMostCellIndex = CellPool.Length - 1;

            //Set content width according to no of columns
            var columns = Mathf.CeilToInt((float)CellPool.Length / Dimension);
            var contentXSize = columns * _cellWidth;
            Content.sizeDelta = new Vector2(contentXSize, Content.sizeDelta.y);
            SetLeftAnchor(Content);
        }


        /// <summary>
        ///     Sets the upper and lower bounds for recycling cells.
        /// </summary>
        /// <param name="balance"></param>
        private void SetRecyclingBounds() {
            Viewport.GetWorldCorners(Corners);
            var threshHold = RecyclingThreshold * (Corners[2].x - Corners[0].x);
            _RecyclingViewBounds.min = new Vector3(Corners[0].x - threshHold, Corners[0].y);
            _RecyclingViewBounds.max = new Vector3(Corners[2].x + threshHold, Corners[2].y);
        }

        /// <summary>
        ///     Creates cell Pool for recycling, Caches ICells
        /// </summary>
        private void CreateCellPool() {
            //Set the prototype cell active and set cell anchor as top
            PrototypeCell.gameObject.SetActive(true);
            SetLeftAnchor(PrototypeCell);

            //set new cell size according to its aspect ratio
            _cellHeight = Content.rect.height / Dimension;
            var prototypeCellSizeDelta = PrototypeCell.sizeDelta;
            _cellWidth = prototypeCellSizeDelta.x / prototypeCellSizeDelta.y * _cellHeight;

            //Reset
            _leftMostCellRow = _rightMostCellRow = 0;

            //Temps
            float currentPoolCoverage = 0;
            var poolSize = 0;
            float posX = 0;
            float posY = 0;

            //Get the required pool coverage and minimum size for the Cell pool
            var requiredCoverage = MinPoolCoverage * Viewport.rect.width;
            var minPoolSize = Math.Min(MinPoolSize, DataSource.GetItemCount());

            //Resetting Pool
            if (CellPool != null) {
                foreach (var item in CachedCellViews) {
                    Destroy(item.gameObject);
                }

                foreach (var item in CellPool) {
                    Destroy(item.gameObject);
                }
            }

            CachedCellViews = new RecyclingScrollRectCellView[0];
            CellPool = new RectTransform[0];

            //create cells until the Pool area is covered and pool size is the minimum required
            while ((poolSize < minPoolSize || currentPoolCoverage < requiredCoverage) &&
                   poolSize < DataSource.GetItemCount()) {
                //Instantiate and add to Pool
                var item = Object.Instantiate(PrototypeCell.gameObject).GetComponent<RectTransform>();
                item.name = "Cell";
                item.sizeDelta = new Vector2(_cellWidth, _cellHeight);
                AppendPoolCell(item);
                item.SetParent(Content, false);

                var itemRect = item.rect;
                if (IsGrid) {
                    posY = -_rightMostCellRow * _cellHeight;
                    item.anchoredPosition = new Vector2(posX, posY);
                    if (++_rightMostCellRow >= Dimension) {
                        _rightMostCellRow = 0;
                        posX += _cellWidth;
                        currentPoolCoverage += itemRect.width;
                    }
                } else {
                    item.anchoredPosition = new Vector2(posX, 0);
                    posX = item.anchoredPosition.x + itemRect.width;
                    currentPoolCoverage += itemRect.width;
                }

                //Setting data for Cell
                var cell = item.GetComponent<RecyclingScrollRectCellView>();

                AppendCachedCell(cell);
                DataSource.SetCell(cell, poolSize);

                //Update the Pool size
                poolSize++;
            }

            if (IsGrid) {
                _rightMostCellRow = (_rightMostCellRow - 1 + Dimension) % Dimension;
            }

            //Deactivate prototype cell if it is not a prefab(i.e it's present in scene)
            PrototypeCell.gameObject.SetActive(false);
        }
        #endregion

        #region RECYCLING
        private readonly Vector3[] _cornerCache = new Vector3[4];

        /// <summary>
        ///     Recyling entry point
        /// </summary>
        /// <param name="direction">scroll direction </param>
        /// <returns></returns>
        public override Vector2 ProcessChangeAndRecycle(Vector2 direction) {
            if (_recycling || CellPool == null || CellPool.Length == 0) {
                return _zeroVector;
            }

            //Updating Recycling view bounds since it can change with resolution changes.
            SetRecyclingBounds();

            if (IsScrollingRightBeyondVisibleElements(direction)) {
                return RecycleLeftToRight();
            }

            if (IsScrollingLeftBeyondVisbibleElements(direction)) {
                return RecycleRightToleft();
            }

            return _zeroVector;
        }

        private bool IsScrollingLeftBeyondVisbibleElements(Vector2 direction) {
            var leftMostCell = CellPool[_leftMostCellIndex];
            leftMostCell.GetWorldCorners(_cornerCache);
            return direction.x > 0 && RectTransformExtensions.MaxX(_cornerCache) > _RecyclingViewBounds.min.x;
        }

        private bool IsScrollingRightBeyondVisibleElements(Vector2 direction) {
            var rightMostCell = CellPool[_rightMostCellIndex];
            rightMostCell.GetWorldCorners(_cornerCache);
            return direction.x < 0 && RectTransformExtensions.MinX(_cornerCache) < _RecyclingViewBounds.max.x;
        }

        /// <summary>
        ///     Recycles cells from Left to Right in the List heirarchy
        /// </summary>
        private Vector2 RecycleLeftToRight() {
            _recycling = true;

            var n = 0;
            var posX = IsGrid ? CellPool[_rightMostCellIndex].anchoredPosition.x : 0;
            float posY = 0;

            //to determine if content size needs to be updated
            var additionalColoums = 0;

            //Recycle until cell at left is available and current item count smaller than datasource
            var totalItemCount = DataSource.GetItemCount();
            while (ViewportRightNotReached(totalItemCount)) {
                if (IsGrid) {
                    if (++_rightMostCellRow >= Dimension) {
                        n++;
                        _rightMostCellRow = 0;
                        posX = CellPool[_rightMostCellIndex].anchoredPosition.x + _cellWidth;
                        additionalColoums++;
                    }

                    //Move Left most cell to right
                    posY = -_rightMostCellRow * _cellHeight;
                    CellPool[_leftMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (++_leftMostCellRow >= Dimension) {
                        _leftMostCellRow = 0;
                        additionalColoums--;
                    }
                } else {
                    //Move Left most cell to right
                    posX = CellPool[_rightMostCellIndex].anchoredPosition.x +
                           CellPool[_rightMostCellIndex].sizeDelta.x;
                    CellPool[_leftMostCellIndex].anchoredPosition =
                            new Vector2(posX, CellPool[_leftMostCellIndex].anchoredPosition.y);
                }

                //Cell for row at
                DataSource.SetCell(CachedCellViews[_leftMostCellIndex], _currentItemCount);

                //set new indices
                _rightMostCellIndex = _leftMostCellIndex;
                _leftMostCellIndex = (_leftMostCellIndex + 1) % CellPool.Length;

                _currentItemCount++;
                if (!IsGrid) {
                    n++;
                }
            }

            //Content size adjustment
            if (IsGrid) {
                Content.sizeDelta += Vector2.right * (additionalColoums * _cellWidth);
                if (additionalColoums > 0) {
                    n -= additionalColoums;
                }
            }

            //Content anchor position adjustment.
            foreach (var cell in CellPool) {
                cell.anchoredPosition -= Vector2.right * (n * CellPool[_leftMostCellIndex].sizeDelta.x);
            }

            Content.anchoredPosition += Vector2.right * (n * CellPool[_leftMostCellIndex].sizeDelta.x);
            _recycling = false;
            return Vector2.right * (n * CellPool[_leftMostCellIndex].sizeDelta.x);
        }

        private bool ViewportRightNotReached(int totalItemCount) {
            var leftMostCell = CellPool[_leftMostCellIndex];
            leftMostCell.GetWorldCorners(_cornerCache);
            return RectTransformExtensions.MaxX(_cornerCache) < _RecyclingViewBounds.min.x &&
                   IsTotalItemCountNotReached(_currentItemCount, totalItemCount);
        }

        /// <summary>
        ///     Recycles cells from Right to Left in the List heirarchy
        /// </summary>
        private Vector2 RecycleRightToleft() {
            _recycling = true;

            var n = 0;
            var posX = IsGrid ? CellPool[_leftMostCellIndex].anchoredPosition.x : 0;
            float posY = 0;

            //to determine if content size needs to be updated
            var additionalColoums = 0;
            //Recycle until cell at Right end is avaiable and current item count is greater than cellpool size
            while (ViewPortLeftNotReached()) {
                if (IsGrid) {
                    if (--_leftMostCellRow < 0) {
                        n++;
                        _leftMostCellRow = Dimension - 1;
                        posX = CellPool[_leftMostCellIndex].anchoredPosition.x - _cellWidth;
                        additionalColoums++;
                    }

                    //Move Right most cell to left
                    posY = -_leftMostCellRow * _cellHeight;
                    CellPool[_rightMostCellIndex].anchoredPosition = new Vector2(posX, posY);

                    if (--_rightMostCellRow < 0) {
                        _rightMostCellRow = Dimension - 1;
                        additionalColoums--;
                    }
                } else {
                    //Move Right most cell to left
                    posX = CellPool[_leftMostCellIndex].anchoredPosition.x - CellPool[_leftMostCellIndex].sizeDelta.x;
                    CellPool[_rightMostCellIndex].anchoredPosition =
                            new Vector2(posX, CellPool[_rightMostCellIndex].anchoredPosition.y);
                    n++;
                }

                _currentItemCount--;
                //Cell for row at
                DataSource.SetCell(CachedCellViews[_rightMostCellIndex], _currentItemCount - CellPool.Length);

                //set new indices
                _leftMostCellIndex = _rightMostCellIndex;
                _rightMostCellIndex = (_rightMostCellIndex - 1 + CellPool.Length) % CellPool.Length;
            }

            //Content size adjustment
            if (IsGrid) {
                Content.sizeDelta += Vector2.right * (additionalColoums * _cellWidth);
                if (additionalColoums > 0) {
                    n -= additionalColoums;
                }
            }

            //Content anchor position adjustment.
            foreach (var cell in CellPool) {
                cell.anchoredPosition += Vector2.right * (n * CellPool[_leftMostCellIndex].sizeDelta.x);
            }

            Content.anchoredPosition -= Vector2.right * (n * CellPool[_leftMostCellIndex].sizeDelta.x);
            _recycling = false;
            return Vector2.right * (-n * CellPool[_leftMostCellIndex].sizeDelta.x);
        }

        private bool ViewPortLeftNotReached() {
            var rightMostCell = CellPool[_rightMostCellIndex];
            rightMostCell.GetWorldCorners(_cornerCache);
            return RectTransformExtensions.MinX(_cornerCache) > _RecyclingViewBounds.max.x &&
                   _currentItemCount > CellPool.Length;
        }
        #endregion
    }
}