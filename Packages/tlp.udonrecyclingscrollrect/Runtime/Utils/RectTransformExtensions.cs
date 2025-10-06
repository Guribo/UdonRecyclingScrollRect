//MIT License
//Copyright (c) 2020 Mohammed Iqubal Hussain
//Website : Polyandcode.com 

using TLP.UdonUtils;
using UnityEngine;

namespace TLP.RecyclingScrollRect.Runtime.Utils
{
    /// <summary>
    ///     Extension methods for Rect Transform
    /// </summary>
    public static class RectTransformExtensions
    {
        public static float MaxY(Vector3[] fourCorners) {
            return fourCorners[1].y;
        }

        public static float MinY(Vector3[] fourCorners) {
            return fourCorners[0].y;
        }

        public static float MaxX(Vector3[] fourCorners) {
            return fourCorners[2].x;
        }

        public static float MinX(Vector3[] fourCorners) {
            return fourCorners[0].x;
        }
    }
}