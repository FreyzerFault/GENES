/**

    This class demonstrates the code discussed in these two articles:

    http://devmag.org.za/2011/04/05/bzier-curves-a-tutorial/
    http://devmag.org.za/2011/06/23/bzier-path-algorithms/

    Use this code as you wish, at your own risk. If it blows up
    your computer, makes a plane crash, or otherwise cause damage,
    injury, or death, it is not my fault.

    @author Herman Tulleken, dev.mag.org.za

*/


using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
    /**
     * Class for representing a Bezier path, and methods for getting suitable points to
     * draw the curve with line segments.
     */
    public class BezierPath
    {
        private readonly List<Vector2> controlPoints;

        private int curveCount; //how many bezier curves in this path?

        // This corresponds to about 172 degrees, 8 degrees from a straight line
        public float DIVISION_THRESHOLD = -0.99f;
        public float MINIMUM_SQR_DISTANCE = 0.01f;
        public int SegmentsPerCurve = 10;

        /**
         * Constructs a new empty Bezier curve. Use one of these methods
         * to add points: SetControlPoints, Interpolate, SamplePoints.
         */
        public BezierPath() => controlPoints = new List<Vector2>();

        /**
         * Sets the control points of this Bezier path.
         * Points 0-3 forms the first Bezier curve, points
         * 3-6 forms the second curve, etc.
         */
        public void SetControlPoints(List<Vector2> newControlPoints)
        {
            controlPoints.Clear();
            controlPoints.AddRange(newControlPoints);
            curveCount = (controlPoints.Count - 1) / 3;
        }

        public void SetControlPoints(Vector2[] newControlPoints)
        {
            controlPoints.Clear();
            controlPoints.AddRange(newControlPoints);
            curveCount = (controlPoints.Count - 1) / 3;
        }

        /**
            Returns the control points for this Bezier curve.
        */
        public List<Vector2> GetControlPoints() => controlPoints;


        /**
            Calculates a Bezier interpolated path for the given points.
        */
        public void Interpolate(List<Vector2> segmentPoints, float scale)
        {
            controlPoints.Clear();

            if (segmentPoints.Count < 2) return;

            for (var i = 0; i < segmentPoints.Count; i++)
                if (i == 0) // is first
                {
                    var p1 = segmentPoints[i];
                    var p2 = segmentPoints[i + 1];

                    var tangent = p2 - p1;
                    var q1 = p1 + scale * tangent;

                    controlPoints.Add(p1);
                    controlPoints.Add(q1);
                }
                else if (i == segmentPoints.Count - 1) //last
                {
                    var p0 = segmentPoints[i - 1];
                    var p1 = segmentPoints[i];
                    var tangent = p1 - p0;
                    var q0 = p1 - scale * tangent;

                    controlPoints.Add(q0);
                    controlPoints.Add(p1);
                }
                else
                {
                    var p0 = segmentPoints[i - 1];
                    var p1 = segmentPoints[i];
                    var p2 = segmentPoints[i + 1];
                    var tangent = (p2 - p0).normalized;
                    var q0 = p1 - scale * tangent * (p1 - p0).magnitude;
                    var q1 = p1 + scale * tangent * (p2 - p1).magnitude;

                    controlPoints.Add(q0);
                    controlPoints.Add(p1);
                    controlPoints.Add(q1);
                }

            curveCount = (controlPoints.Count - 1) / 3;
        }

        /**
            Sample the given points as a Bezier path.
        */
        public void SamplePoints(List<Vector2> sourcePoints, float minSqrDistance, float maxSqrDistance, float scale)
        {
            if (sourcePoints.Count < 2) return;

            var samplePoints = new Stack<Vector2>();

            samplePoints.Push(sourcePoints[0]);

            var potentialSamplePoint = sourcePoints[1];

            var i = 2;

            for (i = 2; i < sourcePoints.Count; i++)
            {
                if (
                    (potentialSamplePoint - sourcePoints[i]).sqrMagnitude > minSqrDistance &&
                    (samplePoints.Peek() - sourcePoints[i]).sqrMagnitude > maxSqrDistance)
                    samplePoints.Push(potentialSamplePoint);

                potentialSamplePoint = sourcePoints[i];
            }

            //now handle last bit of curve
            var p1 = samplePoints.Pop(); //last sample point
            var p0 = samplePoints.Peek(); //second last sample point
            var tangent = (p0 - potentialSamplePoint).normalized;
            var d2 = (potentialSamplePoint - p1).magnitude;
            var d1 = (p1 - p0).magnitude;
            p1 = p1 + tangent * ((d1 - d2) / 2);

            samplePoints.Push(p1);
            samplePoints.Push(potentialSamplePoint);


            Interpolate(new List<Vector2>(samplePoints), scale);
        }

        /**
         * Calculates a point on the path.
         * 
         * @param curveIndex The index of the curve that the point is on. For example,
         * the second curve (index 1) is the curve with control points 3, 4, 5, and 6.
         * 
         * @param t The parameter indicating where on the curve the point is. 0 corresponds
         * to the "left" point, 1 corresponds to the "right" end point.
         */
        public Vector2 CalculateBezierPoint(int curveIndex, float t)
        {
            var nodeIndex = curveIndex * 3;

            var p0 = controlPoints[nodeIndex];
            var p1 = controlPoints[nodeIndex + 1];
            var p2 = controlPoints[nodeIndex + 2];
            var p3 = controlPoints[nodeIndex + 3];

            return CalculateBezierPoint(t, p0, p1, p2, p3);
        }

        /**
         * Gets the drawing points. This implementation simply calculates a certain number
         * of points per curve.
         */
        public List<Vector2> GetDrawingPoints0()
        {
            var drawingPoints = new List<Vector2>();

            for (var curveIndex = 0; curveIndex < curveCount; curveIndex++)
            {
                if (curveIndex == 0) //Only do this for the first end point. 
                    //When i != 0, this coincides with the 
                    //end point of the previous segment,
                    drawingPoints.Add(CalculateBezierPoint(curveIndex, 0));

                for (var j = 1; j <= SegmentsPerCurve; j++)
                {
                    var t = j / (float)SegmentsPerCurve;
                    drawingPoints.Add(CalculateBezierPoint(curveIndex, t));
                }
            }

            return drawingPoints;
        }

        /**
         * Gets the drawing points. This implementation simply calculates a certain number
         * of points per curve.
         * 
         * This is a slightly different implementation from the one above.
         */
        public List<Vector2> GetDrawingPoints1()
        {
            var drawingPoints = new List<Vector2>();

            for (var i = 0; i < controlPoints.Count - 3; i += 3)
            {
                var p0 = controlPoints[i];
                var p1 = controlPoints[i + 1];
                var p2 = controlPoints[i + 2];
                var p3 = controlPoints[i + 3];

                if (i == 0) //only do this for the first end point. When i != 0, this coincides with the end point of the previous segment,
                    drawingPoints.Add(CalculateBezierPoint(0, p0, p1, p2, p3));

                for (var j = 1; j <= SegmentsPerCurve; j++)
                {
                    var t = j / (float)SegmentsPerCurve;
                    drawingPoints.Add(CalculateBezierPoint(t, p0, p1, p2, p3));
                }
            }

            return drawingPoints;
        }

        /**
         * This gets the drawing points of a bezier curve, using recursive division,
         * which results in less points for the same accuracy as the above implementation.
         */
        public List<Vector2> GetDrawingPoints2()
        {
            var drawingPoints = new List<Vector2>();

            for (var curveIndex = 0; curveIndex < curveCount; curveIndex++)
            {
                var bezierCurveDrawingPoints = FindDrawingPoints(curveIndex);

                if (curveIndex != 0)
                    //remove the fist point, as it coincides with the last point of the previous Bezier curve.
                    bezierCurveDrawingPoints.RemoveAt(0);

                drawingPoints.AddRange(bezierCurveDrawingPoints);
            }

            return drawingPoints;
        }

        private List<Vector2> FindDrawingPoints(int curveIndex)
        {
            var pointList = new List<Vector2>();

            var left = CalculateBezierPoint(curveIndex, 0);
            var right = CalculateBezierPoint(curveIndex, 1);

            pointList.Add(left);
            pointList.Add(right);

            FindDrawingPoints(curveIndex, 0, 1, pointList, 1);

            return pointList;
        }


        /**
            @returns the number of points added.
        */
        private int FindDrawingPoints(
            int curveIndex, float t0, float t1,
            List<Vector2> pointList, int insertionIndex
        )
        {
            var left = CalculateBezierPoint(curveIndex, t0);
            var right = CalculateBezierPoint(curveIndex, t1);

            if ((left - right).sqrMagnitude < MINIMUM_SQR_DISTANCE) return 0;

            var tMid = (t0 + t1) / 2;
            var mid = CalculateBezierPoint(curveIndex, tMid);

            var leftDirection = (left - mid).normalized;
            var rightDirection = (right - mid).normalized;

            if (Vector2.Dot(leftDirection, rightDirection) > DIVISION_THRESHOLD || Mathf.Abs(tMid - 0.5f) < 0.0001f)
            {
                var pointsAddedCount = 0;

                pointsAddedCount += FindDrawingPoints(curveIndex, t0, tMid, pointList, insertionIndex);
                pointList.Insert(insertionIndex + pointsAddedCount, mid);
                pointsAddedCount++;
                pointsAddedCount += FindDrawingPoints(
                    curveIndex,
                    tMid,
                    t1,
                    pointList,
                    insertionIndex + pointsAddedCount
                );

                return pointsAddedCount;
            }

            return 0;
        }


        /**
            Calculates a point on the Bezier curve represented with the four control points given.
        */
        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;

            var p = uuu * p0; //first term

            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term

            return p;
        }
    }
}