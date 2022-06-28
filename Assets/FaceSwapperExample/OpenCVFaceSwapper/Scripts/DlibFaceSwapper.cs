using OpenCVForUnity.CoreModule;
using System;

namespace OpenCVForUnity.FaceSwap
{
    public class DlibFaceSwapper : FaceSwapper
    {
        // Finds facial landmarks on faces and extracts the useful points
        protected override void getFacePoints(Point[] landmark_points, Point[] points, Point[] affine_transform_keypoints)
        {
            if (landmark_points.Length != 68)
                throw new ArgumentNullException("Invalid landmark_points.");

            //points(facial contour)
            points[0] = landmark_points[0];
            points[1] = landmark_points[3];
            points[2] = landmark_points[5];
            points[3] = landmark_points[8];
            points[4] = landmark_points[11];
            points[5] = landmark_points[13];
            points[6] = landmark_points[16];
            Point nose_length = new Point(landmark_points[27].x - landmark_points[30].x, landmark_points[27].y - landmark_points[30].y);
            points[7] = new Point(landmark_points[26].x + nose_length.x, landmark_points[26].y + nose_length.y);
            points[8] = new Point(landmark_points[17].x + nose_length.x, landmark_points[17].y + nose_length.y);

            //affine_transform_keypoints(eyes and chin)
            affine_transform_keypoints[0] = points[3];
            affine_transform_keypoints[1] = landmark_points[36];
            affine_transform_keypoints[2] = landmark_points[45];
        }
    }
}