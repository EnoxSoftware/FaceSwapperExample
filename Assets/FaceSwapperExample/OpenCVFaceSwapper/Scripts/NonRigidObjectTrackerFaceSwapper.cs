using OpenCVForUnity.CoreModule;
using System;

namespace OpenCVForUnity.FaceSwap
{

    public class NonRigidObjectTrackerFaceSwapper : FaceSwapper
    {
        // Finds facial landmarks on faces and extracts the useful points
        protected override void getFacePoints(Point[] landmark_points, Point[] points, Point[] affine_transform_keypoints)
        {
            if (landmark_points.Length != 76)
                throw new ArgumentNullException("Invalid landmark_points.");

            //points(facial contour)
            points[0] = landmark_points[0];
            points[1] = landmark_points[2];
            points[2] = landmark_points[5];
            points[3] = landmark_points[7];
            points[4] = landmark_points[9];
            points[5] = landmark_points[12];
            points[6] = landmark_points[14];
            Point nose_line_base = new Point((landmark_points[37].x + landmark_points[45].x) / 2, (landmark_points[37].y + landmark_points[45].y) / 2);
            Point nose_length = new Point(nose_line_base.x - landmark_points[67].x, nose_line_base.y - landmark_points[67].y);
            points[7] = new Point(landmark_points[15].x + nose_length.x, landmark_points[15].y + nose_length.y);
            points[8] = new Point(landmark_points[21].x + nose_length.x, landmark_points[21].y + nose_length.y);

            //affine_transform_keypoints(eyes and chin)
            affine_transform_keypoints[0] = points[3];
            affine_transform_keypoints[1] = landmark_points[27];
            affine_transform_keypoints[2] = landmark_points[32];
        }
    }
}
