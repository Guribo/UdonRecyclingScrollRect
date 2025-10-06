//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

/// <summary>
/// Interface for creating DataSource
/// Recycling Scroll Rect must be provided a Data source which must inherit from this.
/// </summary>

using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UnityEngine;
using TLP.UdonUtils;

namespace TLP.UdonRecyclingScrollRect.Runtime.Interfaces
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingScrollRectDataSource), ExecutionOrder)]
    public abstract class RecyclingScrollRectDataSource : Controller
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RecyclingScrollRectCellView.ExecutionOrder + 100;
        #endregion

        [PublicAPI]
        public abstract int GetItemCount();

        [PublicAPI]
        public abstract void SetCell(RecyclingScrollRectCellView recyclingScrollRectCellView, int index);
    }
}