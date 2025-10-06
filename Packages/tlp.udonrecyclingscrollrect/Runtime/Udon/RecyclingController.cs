using JetBrains.Annotations;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;

namespace TLP.UdonRecyclingScrollRect.Runtime.Udon
{
    /// <summary>
    /// Usage:<br/>
    /// 1. Add a ScrollRect to your scene<br/>
    /// 2. Add this script to the GameObject with the ScrollRect<br/>
    /// 3. In the value change event send a custom event this this script and provide the
    ///    method name "<see cref="Recycle"/>" without "()".<br/>
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingController), ExecutionOrder)]
    public class RecyclingController : Controller
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Controller.ExecutionOrder + 20;
        #endregion

        [FormerlySerializedAs("scrollRect")]
        [SerializeField]
        protected internal ScrollRect ScrollRect;

        #region Public API
        /// <summary>
        /// Make sure this method is called via SendCustomEvent("Recycle") by the ScrollRect component
        /// whenever the value of it changes.
        /// </summary>
        [PublicAPI]
        public void Recycle() {
#if TLP_DEBUG
            DebugLog(nameof(Recycle));
#endif
            if (!Utilities.IsValid(ScrollRect)) {
                Error($"{nameof(ScrollRect)} not set");
                return;
            }

            ScrollUpdate(ScrollRect.normalizedPosition, ScrollRect.velocity);
        }
        #endregion

        #region TODOs
        // TODO: test this
        public void SnapTo(RectTransform target) {
            Canvas.ForceUpdateCanvases();

            var scrollRectTransform = ScrollRect.transform;
            var scrollRectContent = ScrollRect.content;
            scrollRectContent.anchoredPosition =
                    GetAnchoredPosition(scrollRectTransform, scrollRectContent) - // TODO: isn't this (0,0)?
                    GetAnchoredPosition(scrollRectTransform, target);
        }
        #endregion

        #region Internal
        #region Hook Methods
        /// <summary>
        /// Hook method to be overriden.<br/>
        /// - Provides the update functionality.<br/>
        /// - Called every time the scroll view is scrolled/moving via <see cref="Recycle"/><br/>
        /// </summary>
        /// <param name="normalizedPosition">current position in range 0 to 1 (horizontal and vertical)</param>
        /// <param name="velocity">current scroll velocity (horizontal and vertical)</param>
        protected virtual void ScrollUpdate(Vector2 normalizedPosition, Vector2 velocity) {
#if TLP_DEBUG
            DebugLog($"{nameof(ScrollUpdate)}: normalized position = {normalizedPosition}, velocity = {velocity}");
#endif
        }
        #endregion

        private static Vector2 GetAnchoredPosition(Transform parent, Transform target) {
            return parent.InverseTransformPoint(target.position);
        }
        #endregion
    }
}