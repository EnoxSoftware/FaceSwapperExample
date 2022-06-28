using OpenCVForUnity.CoreModule;
using System;

namespace OpenCVForUnity.FaceChange
{
    public class RectUtils
    {
        /// <summary>
        /// Creates and returns an inflated copy of the specified CvRect structure.
        /// </summary>
        /// <param name="rect">The Rectangle with which to start. This rectangle is not modified. </param>
        /// <param name="x">The amount to inflate this Rectangle horizontally. </param>
        /// <param name="y">The amount to inflate this Rectangle vertically. </param>
        /// <returns></returns>
        public static Rect Inflate(Rect rect, int x, int y)
        {
            rect.x -= x;
            rect.y -= y;
            rect.width += (2 * x);
            rect.height += (2 * y);
            return rect;
        }

        /// <summary>
        /// Determines the CvRect structure that represents the intersection of two rectangles. 
        /// </summary>
        /// <param name="a">A rectangle to intersect. </param>
        /// <param name="b">A rectangle to intersect. </param>
        /// <returns></returns>
        public static Rect Intersect(Rect a, Rect b)
        {
            int x1 = Math.Max(a.x, b.x);
            int x2 = Math.Min(a.x + a.width, b.x + b.width);
            int y1 = Math.Max(a.y, b.y);
            int y2 = Math.Min(a.y + a.height, b.y + b.height);

            if (x2 >= x1 && y2 >= y1)
                return new Rect(x1, y1, x2 - x1, y2 - y1);
            else
                return new Rect();
        }

        /// <summary>
        /// Gets a CvRect structure that contains the union of two CvRect structures. 
        /// </summary>
        /// <param name="a">A rectangle to union. </param>
        /// <param name="b">A rectangle to union. </param>
        /// <returns></returns>
        public static Rect Union(Rect a, Rect b)
        {
            int x1 = Math.Min(a.x, b.x);
            int x2 = Math.Max(a.x + a.width, b.x + b.width);
            int y1 = Math.Min(a.y, b.y);
            int y2 = Math.Max(a.y + a.height, b.y + b.height);

            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }
    }
}