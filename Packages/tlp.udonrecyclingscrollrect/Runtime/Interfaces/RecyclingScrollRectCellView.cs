//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

/// <summary>
/// Interface for creating a Cell.
/// Prototype Cell must have a monobeviour inheriting from ICell
/// </summary>

using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UnityEngine;

namespace TLP.UdonRecyclingScrollRect.Runtime.Interfaces
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingScrollRectCellView), ExecutionOrder)]
    public abstract class RecyclingScrollRectCellView : View
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder + 20;
        #endregion

        public int Index;
    }
}