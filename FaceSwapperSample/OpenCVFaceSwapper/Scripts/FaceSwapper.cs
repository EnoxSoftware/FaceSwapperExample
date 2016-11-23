using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenCVForUnity.FaceSwap
{
    /// <summary>
    /// FaceSwaper.
    /// Code is the rewrite of https://github.com/mc-jesus/FaceSwap using the “OpenCV for Unity” and "Dlib FaceLandmark Detector". 
    /// </summary>
    public class FaceSwapper : IDisposable
    {

        protected Rect rect_ann, rect_bob;
        protected float maskAlpha;

        protected Point[] points_ann = new Point[9];
        protected Point[] points_bob = new Point[9];
        protected Point[] affine_transform_keypoints_ann = new Point[3];
        protected Point[] affine_transform_keypoints_bob = new Point[3];
        protected Size feather_amount = new Size();

        protected Mat small_frame;
        protected Size small_frame_size;
        protected Rect small_frame_rect;
        protected Mat frame_c3;

        //full size Mat
        protected Mat refined_ann_and_bob_warpped_full;
        protected Mat refined_bob_and_ann_warpped_full;
        protected Mat warpped_face_ann_full;
        protected Mat warpped_face_bob_full;
        protected Mat mask_ann_full;
        protected Mat mask_bob_full;
        protected Mat warpped_mask_ann_full;
        protected Mat warpped_mask_bob_full;
        protected Mat refined_masks_full;
        protected Mat face_ann_full;
        protected Mat face_bob_full;
        protected Mat warpped_faces_full;

        //ROI Mat
        protected Mat refined_ann_and_bob_warpped;
        protected Mat refined_bob_and_ann_warpped;
        protected Mat warpped_face_ann;
        protected Mat warpped_face_bob;
        protected Mat mask_ann;
        protected Mat mask_bob;
        protected Mat warpped_mask_ann;
        protected Mat warpped_mask_bob;
        protected Mat refined_masks;
        protected Mat face_ann;
        protected Mat face_bob;
        protected Mat warpped_faces;

        //affineTransforms
        protected Mat trans_ann_to_bob;
        protected Mat trans_bob_to_ann;


        protected byte[,] LUT = new byte[3, 256];
        protected int[,] source_hist_int = new int[3, 256];
        protected int[,] target_hist_int = new int[3, 256];
        protected float[,] source_histogram = new float[3, 256];
        protected float[,] target_histogram = new float[3, 256];


        public bool useSeamlessCloneForPasteFaces;
        public bool isShowingDebugFacePoints;


        // Initialize face swapped with landmarks
        public FaceSwapper()
        {
			trans_ann_to_bob = new Mat();
			trans_bob_to_ann = new Mat();
        }

        //Swaps faces in points on frame
        public void SwapFaces(Mat frame, Point[] landmark_points_ann, Point[] landmark_points_bob, float alpha)
        {
            alpha = alpha > 0 ? alpha : 0;
            alpha = alpha < 1 ? alpha : 1; 
            maskAlpha =  alpha;


            getFacePoints(landmark_points_ann, points_ann, affine_transform_keypoints_ann);
            getFacePoints(landmark_points_bob, points_bob, affine_transform_keypoints_bob);
            getFeather_amount(points_ann, feather_amount);

            rect_ann = Imgproc.boundingRect(new MatOfPoint(points_ann));
            rect_bob = Imgproc.boundingRect(new MatOfPoint(points_bob));


            Mat original_frame = frame;
            if (useSeamlessCloneForPasteFaces && (CvType.channels(original_frame.type()) == 4))
            {
                if (frame_c3 == null
                || (frame_c3.width() != warpped_faces_full.width() || frame_c3.height() != warpped_faces_full.height())
                || frame_c3.type() != warpped_faces_full.type())
                {
                    if (frame_c3 != null)
                    {
                        frame_c3.Dispose();
                        frame_c3 = null;
                    }
                    frame_c3 = new Mat();
                }
                Imgproc.cvtColor(frame, frame_c3, Imgproc.COLOR_RGBA2RGB);
                frame = frame_c3;
            }
            
            if (small_frame != null)
            {
                small_frame.Dispose();
            }
            small_frame_rect = getMinFrameRect(frame, rect_ann, rect_bob);
            small_frame = new Mat(frame, small_frame_rect);
            small_frame_size = new Size(small_frame.cols(), small_frame.rows());


            if (warpped_faces_full == null
                || (frame.width() != warpped_faces_full.width() || frame.height() != warpped_faces_full.height())
                || frame.type() != warpped_faces_full.type())
            {
                createMat(frame);
            }
            createMatROI(small_frame_rect);


            rect_ann = getRectInFrame(small_frame, rect_ann);
            rect_bob = getRectInFrame(small_frame, rect_bob);
            points_ann = getPointsInFrame(small_frame, points_ann);
            points_bob = getPointsInFrame(small_frame, points_bob);
            affine_transform_keypoints_ann = getPointsInFrame(small_frame, affine_transform_keypoints_ann);
            affine_transform_keypoints_bob = getPointsInFrame(small_frame, affine_transform_keypoints_bob);


            getTransformationMatrices();
            getMasks();
            getWarppedMasks();
            refined_masks = getRefinedMasks();
            extractFaces();
            warpped_faces = getWarppedFaces();


            if (useSeamlessCloneForPasteFaces)
            {
                pasteFacesOnFrameUsingSeamlessClone(frame);
                
                if (CvType.channels(original_frame.type()) == 4)
                {
                    Imgproc.cvtColor(frame, original_frame, Imgproc.COLOR_RGB2RGBA);

                    frame = original_frame;
                    if (small_frame != null)
                    {
                        small_frame.Dispose();
                        small_frame = null;
                    }
                    small_frame = new Mat(frame, small_frame_rect);
                }
            }
            else
            {
                colorCorrectFaces();
                featherMasks();
                pasteFacesOnFrame();
            }


            if (isShowingDebugFacePoints)
            {
                drawDebugFacePoints();
            }
        }

        public void SwapFaces(Mat frame, List<Point> landmark_points_ann, List<Point> landmark_points_bob, float alpha)
        {
            SwapFaces(frame, landmark_points_ann.ToArray(), landmark_points_bob.ToArray(), alpha);
        }

        public void SwapFaces(Mat frame, Vector2[] landmark_points_ann, Vector2[] landmark_points_bob, float alpha)
        {
            Point[] landmark_points_ann_arr = new Point[landmark_points_ann.Length];
            for (int i = 0; i < landmark_points_ann_arr.Length; i++)
            {
                landmark_points_ann_arr[i] = new Point(landmark_points_ann[i].x, landmark_points_ann[i].y);
            }

            Point[] landmark_points_bob_arr = new Point[landmark_points_bob.Length];
            for (int i = 0; i < landmark_points_bob_arr.Length; i++)
            {
                landmark_points_bob_arr[i] = new Point(landmark_points_bob[i].x, landmark_points_bob[i].y);
            }

            SwapFaces(frame, landmark_points_ann_arr, landmark_points_bob_arr, alpha);
        }

        public void SwapFaces(Mat frame, List<Vector2> landmark_points_ann, List<Vector2> landmark_points_bob, float alpha)
        {
            Point[] landmark_points_ann_arr = new Point[landmark_points_ann.Count];
            for (int i = 0; i < landmark_points_ann_arr.Length; i++)
            {
                landmark_points_ann_arr[i] = new Point(landmark_points_ann[i].x, landmark_points_ann[i].y);
            }

            Point[] landmark_points_bob_arr = new Point[landmark_points_bob.Count];
            for (int i = 0; i < landmark_points_bob_arr.Length; i++)
            {
                landmark_points_bob_arr[i] = new Point(landmark_points_bob[i].x, landmark_points_bob[i].y);
            }

            SwapFaces(frame, landmark_points_ann_arr, landmark_points_bob_arr, alpha);
        }

        private void createMat(Mat frame)
        {
            disposeMat();

            Size size = frame.size();
            int type = frame.type();

            refined_ann_and_bob_warpped_full = new Mat(size, CvType.CV_8UC1);
            refined_bob_and_ann_warpped_full = new Mat(size, CvType.CV_8UC1);

            warpped_face_ann_full = new Mat(size, type);
            warpped_face_bob_full = new Mat(size, type);

            mask_ann_full = new Mat(size, CvType.CV_8UC1);
            mask_bob_full = new Mat(size, CvType.CV_8UC1);
            warpped_mask_ann_full = new Mat(size, CvType.CV_8UC1);
            warpped_mask_bob_full = new Mat(size, CvType.CV_8UC1);
            refined_masks_full = new Mat(size, CvType.CV_8UC1, new Scalar(0));

            face_ann_full = new Mat(size, type);
            face_bob_full = new Mat(size, type);
            warpped_faces_full = new Mat(size, type, Scalar.all(0));
        }

        private void disposeMat()
        {
            if (refined_ann_and_bob_warpped_full != null)
            {
                refined_ann_and_bob_warpped_full.Dispose();
                refined_ann_and_bob_warpped_full = null;
            }
            if (refined_bob_and_ann_warpped_full != null)
            {
                refined_bob_and_ann_warpped_full.Dispose();
                refined_bob_and_ann_warpped_full = null;
            }

            if (warpped_face_ann_full != null)
            {
                warpped_face_ann_full.Dispose();
                warpped_face_ann_full = null;
            }
            if (warpped_face_bob_full != null)
            {
                warpped_face_bob_full.Dispose();
                warpped_face_bob_full = null;
            }

            if (mask_ann_full != null)
            {
                mask_ann_full.Dispose();
                mask_ann_full = null;
            }
            if (mask_bob_full != null)
            {
                mask_bob_full.Dispose();
                mask_bob_full = null;
            }
            if (warpped_mask_ann_full != null)
            {
                warpped_mask_ann_full.Dispose();
                warpped_mask_ann_full = null;
            }
            if (warpped_mask_bob_full != null)
            {
                warpped_mask_bob_full.Dispose();
                warpped_mask_bob_full = null;
            }
            if (refined_masks_full != null)
            {
                refined_masks_full.Dispose();
                refined_masks_full = null;
            }

            if (face_ann_full != null)
            {
                face_ann_full.Dispose();
                face_ann_full = null;
            }
            if (face_bob_full != null)
            {
                face_bob_full.Dispose();
                face_bob_full = null;
            }
            if (warpped_faces_full != null)
            {
                warpped_faces_full.Dispose();
                warpped_faces_full = null;
            }
        }

        private void createMatROI(Rect roi)
        {
            disposeMatROI();

            refined_ann_and_bob_warpped = new Mat(refined_ann_and_bob_warpped_full, roi);
            refined_bob_and_ann_warpped = new Mat(refined_bob_and_ann_warpped_full, roi);

            warpped_face_ann = new Mat(warpped_face_ann_full, roi);
            warpped_face_bob = new Mat(warpped_face_bob_full, roi);

            mask_ann = new Mat(mask_ann_full, roi);
            mask_bob = new Mat(mask_bob_full, roi);
            warpped_mask_ann = new Mat(warpped_mask_ann_full, roi);
            warpped_mask_bob = new Mat(warpped_mask_bob_full, roi);
            refined_masks = new Mat(refined_masks_full, roi);

            face_ann = new Mat(face_ann_full, roi);
            face_bob = new Mat(face_bob_full, roi);
            warpped_faces = new Mat(warpped_faces_full, roi);
        }

        private void disposeMatROI()
        {
            if (refined_ann_and_bob_warpped != null)
            {
                refined_ann_and_bob_warpped.Dispose();
                refined_ann_and_bob_warpped = null;
            }
            if (refined_bob_and_ann_warpped != null)
            {
                refined_bob_and_ann_warpped.Dispose();
                refined_bob_and_ann_warpped = null;
            }

            if (warpped_face_ann != null)
            {
                warpped_face_ann.Dispose();
                warpped_face_ann = null;
            }
            if (warpped_face_bob != null)
            {
                warpped_face_bob.Dispose();
                warpped_face_bob = null;
            }

            if (mask_ann != null)
            {
                mask_ann.Dispose();
                mask_ann = null;
            }
            if (mask_bob != null)
            {
                mask_bob.Dispose();
                mask_bob = null;
            }
            if (warpped_mask_ann != null)
            {
                warpped_mask_ann.Dispose();
                warpped_mask_ann = null;
            }
            if (warpped_mask_bob != null)
            {
                warpped_mask_bob.Dispose();
                warpped_mask_bob = null;
            }
            if (refined_masks != null)
            {
                refined_masks.Dispose();
                refined_masks = null;
            }

            if (face_ann != null)
            {
                face_ann.Dispose();
                face_ann = null;
            }
            if (face_bob != null)
            {
                face_bob.Dispose();
                face_bob = null;
            }
            if (warpped_faces != null)
            {
                warpped_faces.Dispose();
                warpped_faces = null;
            }
        }

        // Returns minimal Mat containing both faces
        private Rect getMinFrameRect(Mat frame, Rect rect_ann, Rect rect_bob)
        {
            Rect bounding_rect = RectUtils.Union(rect_ann, rect_bob);
            bounding_rect = RectUtils.Intersect(bounding_rect, new Rect(0, 0, frame.cols(), frame.rows()));

            return bounding_rect;
        }

        private Rect getRectInFrame(Mat frame, Rect r)
        {
            Size wholesize = new Size();
            Point ofs = new Point();
            frame.locateROI(wholesize, ofs);

            Rect rect = new Rect(r.x - (int)ofs.x, r.y - (int)ofs.y, r.width, r.height);
            rect = RectUtils.Intersect(rect, new Rect(0, 0, frame.cols(), frame.rows()));

            return rect;
        }

        // Finds facial landmarks on faces and extracts the useful points
        protected virtual void getFacePoints(Point[] landmark_points, Point[] points, Point[] affine_transform_keypoints)
        {
            if (landmark_points.Length != 9)
                throw new ArgumentNullException("Invalid landmark_points.");

            //points(facial contour)
            points[0] = landmark_points[0];
            points[1] = landmark_points[1];
            points[2] = landmark_points[2];
            points[3] = landmark_points[3];
            points[4] = landmark_points[4];
            points[5] = landmark_points[5];
            points[6] = landmark_points[6];
            points[7] = landmark_points[7];
            points[8] = landmark_points[8];

            //affine_transform_keypoints(eyes and chin)
            affine_transform_keypoints[0] = points[3];
            affine_transform_keypoints[1] = new Point(points[8].x, points[0].y);
            affine_transform_keypoints[2] = new Point(points[7].x, points[6].y);
        }

        private void getFeather_amount(Point[] points, Size feather_amount)
        {
            feather_amount.width = feather_amount.height = Core.norm(new MatOfPoint(new Point(points[0].x - points[6].x, points[0].y - points[6].y))) / 8;
        }

        private Point[] getPointsInFrame(Mat frame, Point[] p)
        {
            Size wholesize = new Size();
            Point ofs = new Point();
            frame.locateROI(wholesize, ofs);

            Point[] points = new Point[p.Length];

            for (int i = 0; i < p.Length; i++)
            {
                points[i] = new Point(p[i].x - ofs.x, p[i].y - ofs.y);
            }

            return points;
        }

        // Calculates transformation matrices based on points extracted by getFacePoints
        private void getTransformationMatrices()
        {
            trans_ann_to_bob = Imgproc.getAffineTransform(new MatOfPoint2f(affine_transform_keypoints_ann), new MatOfPoint2f(affine_transform_keypoints_bob));
            Imgproc.invertAffineTransform(trans_ann_to_bob, trans_bob_to_ann);
        }

        // Creates masks for faces based on the points extracted in getFacePoints
        private void getMasks()
        {
            mask_ann.setTo(Scalar.all(0));
            mask_bob.setTo(Scalar.all(0));
            Imgproc.fillConvexPoly(mask_ann, new MatOfPoint(points_ann), new Scalar(255 * maskAlpha));
            Imgproc.fillConvexPoly(mask_bob, new MatOfPoint(points_bob), new Scalar(255 * maskAlpha));
        }

        // Creates warpped masks out of masks created in getMasks to switch places
        private void getWarppedMasks()
        {
            Imgproc.warpAffine(mask_ann, warpped_mask_ann, trans_ann_to_bob, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0));
            Imgproc.warpAffine(mask_bob, warpped_mask_bob, trans_bob_to_ann, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0));
        }

        // Returns Mat of refined mask such that warpped mask isn't bigger than original mask
        private Mat getRefinedMasks()
        {
            Core.bitwise_and(mask_ann, warpped_mask_bob, refined_ann_and_bob_warpped);
            Core.bitwise_and(mask_bob, warpped_mask_ann, refined_bob_and_ann_warpped);

            refined_masks.setTo(Scalar.all(0));
            refined_ann_and_bob_warpped.copyTo(refined_masks, refined_ann_and_bob_warpped);
            refined_bob_and_ann_warpped.copyTo(refined_masks, refined_bob_and_ann_warpped);

            return refined_masks;
        }

        // Extracts faces from images based on masks created in getMasks
        private void extractFaces()
        {
            small_frame.copyTo(face_ann, mask_ann);
            small_frame.copyTo(face_bob, mask_bob);
        }

        // Creates warpped faces out of faces extracted in extractFaces
        private Mat getWarppedFaces()
        {  
            Imgproc.warpAffine(face_ann, warpped_face_ann, trans_ann_to_bob, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0, 0, 0));
            Imgproc.warpAffine(face_bob, warpped_face_bob, trans_bob_to_ann, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0, 0, 0));

            warpped_face_ann.copyTo(warpped_faces, warpped_mask_ann);
            warpped_face_bob.copyTo(warpped_faces, warpped_mask_bob);

            return warpped_faces;
        }

        // Matches Ann face color to Bob face color and vice versa
        private void colorCorrectFaces()
        {
            using (Mat rect_ann_small_frame = new Mat(small_frame, rect_ann))
            using (Mat rect_ann_warpped_faces = new Mat(warpped_faces, rect_ann))
            using (Mat rect_ann_warpped_mask_bob = new Mat(warpped_mask_bob, rect_ann))
            {
                specifiyHistogram(rect_ann_small_frame, rect_ann_warpped_faces, rect_ann_warpped_mask_bob);
            }
            using (Mat rect_bob_small_frame = new Mat(small_frame, rect_bob))
            using (Mat rect_bob_warpped_faces = new Mat(warpped_faces, rect_bob))
            using (Mat rect_bob_warpped_mask_ann = new Mat(warpped_mask_ann, rect_bob))
            {
                specifiyHistogram(rect_bob_small_frame, rect_bob_warpped_faces, rect_bob_warpped_mask_ann);
            }
        }

        // Blurs edges of masks
        private void featherMasks()
        {
            using (Mat rect_ann_refined_masks = new Mat(refined_masks, rect_ann))
            {
                featherMask(rect_ann_refined_masks);
            }
            using (Mat rect_bob_refined_masks = new Mat(refined_masks, rect_bob))
            {
                featherMask(rect_bob_refined_masks);
            }
        }

        // Blurs edges of mask
        private void featherMask(Mat refined_masks)
        {
            Imgproc.erode(refined_masks, refined_masks, Imgproc.getStructuringElement(Imgproc.MORPH_RECT, feather_amount), new Point(-1, -1), 1, Core.BORDER_CONSTANT, new Scalar(0));
            Imgproc.blur(refined_masks, refined_masks, feather_amount, new Point(-1, -1), Core.BORDER_CONSTANT);
        }

        // Pastes faces on original frame
        private void pasteFacesOnFrame()
        {
            byte[] masks_byte = new byte[refined_masks.total() * refined_masks.elemSize()];
            Utils.copyFromMat<byte>(refined_masks, masks_byte);
            byte[] faces_byte = new byte[warpped_faces.total() * warpped_faces.elemSize()];
            Utils.copyFromMat<byte>(warpped_faces, faces_byte);
            byte[] frame_byte = new byte[small_frame.total() * small_frame.elemSize()];
            Utils.copyFromMat<byte>(small_frame, frame_byte);
            
            int pixel_i = 0;
            int elemSize = (int)small_frame.elemSize();
            int total = (int)small_frame.total();
            
            for (int i = 0; i < total; i++)
            {
                if (masks_byte[i] != 0)
                {
                    frame_byte[pixel_i] = (byte)(((255 - masks_byte[i]) * frame_byte[pixel_i] + masks_byte[i] * faces_byte[pixel_i]) >> 8);
                    frame_byte[pixel_i + 1] = (byte)(((255 - masks_byte[i]) * frame_byte[pixel_i + 1] + masks_byte[i] * faces_byte[pixel_i + 1]) >> 8);
                    frame_byte[pixel_i + 2] = (byte)(((255 - masks_byte[i]) * frame_byte[pixel_i + 2] + masks_byte[i] * faces_byte[pixel_i + 2]) >> 8);
                }
                pixel_i += elemSize;
            }

            Utils.copyToMat(frame_byte, small_frame);
        }

        // Calculates source image histogram and changes target_image to match source hist
        private void specifiyHistogram(Mat source_image, Mat target_image, Mat mask)
        {
            System.Array.Clear(source_hist_int, 0, source_hist_int.Length);
            System.Array.Clear(target_hist_int, 0, target_hist_int.Length);

            byte[] mask_byte = new byte[mask.total() * mask.elemSize()];
            Utils.copyFromMat<byte>(mask, mask_byte);
            byte[] source_image_byte = new byte[source_image.total() * source_image.elemSize()];
            Utils.copyFromMat<byte>(source_image, source_image_byte);
            byte[] target_image_byte = new byte[target_image.total() * target_image.elemSize()];
            Utils.copyFromMat<byte>(target_image, target_image_byte);

            int pixel_i = 0;
			int elemSize = (int)source_image.elemSize();
            int total = (int)mask.total();
            for (int i = 0; i < total; i++)
            {
                if (mask_byte[i] != 0)
                {
                    source_hist_int[0, source_image_byte[pixel_i]]++;
                    source_hist_int[1, source_image_byte[pixel_i + 1]]++;
                    source_hist_int[2, source_image_byte[pixel_i + 2]]++;

                    target_hist_int[0, target_image_byte[pixel_i]]++;
                    target_hist_int[1, target_image_byte[pixel_i + 1]]++;
                    target_hist_int[2, target_image_byte[pixel_i + 2]]++;
                }
                // Advance to next pixel
                pixel_i += elemSize;
            }

            // Calc CDF
            for (int i = 1; i < 256; i++)
            {
                source_hist_int[0, i] += source_hist_int[0, i - 1];
                source_hist_int[1, i] += source_hist_int[1, i - 1];
                source_hist_int[2, i] += source_hist_int[2, i - 1];

                target_hist_int[0, i] += target_hist_int[0, i - 1];
                target_hist_int[1, i] += target_hist_int[1, i - 1];
                target_hist_int[2, i] += target_hist_int[2, i - 1];
            }

            // Normalize CDF
            for (int i = 0; i < 256; i++)
            {
                source_histogram[0, i] = (source_hist_int[0, i] != 0) ? (float)source_hist_int[0, i] / source_hist_int[0, 255] : 0;
                source_histogram[1, i] = (source_hist_int[1, i] != 0) ? (float)source_hist_int[1, i] / source_hist_int[1, 255] : 0;
                source_histogram[2, i] = (source_hist_int[2, i] != 0) ? (float)source_hist_int[2, i] / source_hist_int[2, 255] : 0;

                target_histogram[0, i] = (target_hist_int[0, i] != 0) ? (float)target_hist_int[0, i] / target_hist_int[0, 255] : 0;
                target_histogram[1, i] = (target_hist_int[1, i] != 0) ? (float)target_hist_int[1, i] / target_hist_int[1, 255] : 0;
                target_histogram[2, i] = (target_hist_int[2, i] != 0) ? (float)target_hist_int[2, i] / target_hist_int[2, 255] : 0;
            }

            // Create lookup table
            for (int i = 0; i < 256; i++)
            {
                LUT[0, i] = binary_search(target_histogram[0, i], source_histogram, 0);
                LUT[1, i] = binary_search(target_histogram[1, i], source_histogram, 1);
                LUT[2, i] = binary_search(target_histogram[2, i], source_histogram, 2);
            }

            // repaint pixels
            pixel_i = 0;
            for (int i = 0; i < total; i++)
            {
                if (mask_byte[i] != 0)
                {
                    target_image_byte[pixel_i] = LUT[0, target_image_byte[pixel_i]];
                    target_image_byte[pixel_i + 1] = LUT[1, target_image_byte[pixel_i + 1]];
                    target_image_byte[pixel_i + 2] = LUT[2, target_image_byte[pixel_i + 2]];
                }
                // Advance to next pixel
                pixel_i += elemSize;
            }

            Utils.copyToMat(target_image_byte, target_image);
        }

        private byte binary_search(float needle, float[,] haystack, int haystack_index)
        {
            byte l = 0, r = 255, m = 0;
            while (l < r)
            {
                m = (byte)((l + r) / 2);
                if (needle > haystack[haystack_index, m])
                    l = (byte)(m + 1);
                else
                    r = (byte)(m - 1);
            }
            // TODO check closest value
            return m;
        }


        // Pastes faces on original frame using SeamlessClone
        private void pasteFacesOnFrameUsingSeamlessClone(Mat full_frame)
        {
            //Processing to avoid the problem that synthesis is shifted.
            Imgproc.rectangle(refined_masks, new Point(1, 1), new Point(small_frame_size.width - 1, small_frame_size.height - 1), new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_8, 0);

            using (Mat full_dst_frame = new Mat()){
                full_frame.copyTo(full_dst_frame);
                using (Mat small_dest_frame = new Mat(full_dst_frame, small_frame_rect))
                {
                    //Utils.setDebugMode(true);
                    Photo.seamlessClone(warpped_faces, small_frame, refined_masks, new Point(small_frame_size.width / 2, small_frame_size.height / 2), small_dest_frame, Photo.NORMAL_CLONE);
                    //Utils.setDebugMode(false);
                }
                full_dst_frame.copyTo(full_frame);      
            }
        }


        private void drawDebugFacePoints()
        {
            for (int i = 0; i < points_ann.Length; i++)
            {
                Imgproc.circle(small_frame, points_ann[i], 1, new Scalar(255, 0, 0, 255), 2, Core.LINE_AA, 0);
            }
            for (int i = 0; i < affine_transform_keypoints_ann.Length; i++)
            {
                Imgproc.circle(small_frame, affine_transform_keypoints_ann[i], 1, new Scalar(0, 255, 0, 255), 2, Core.LINE_AA, 0);
            }
            for (int i = 0; i < points_bob.Length; i++)
            {
                Imgproc.circle(small_frame, points_bob[i], 1, new Scalar(255, 0, 0, 255), 2, Core.LINE_AA, 0);
            }
            for (int i = 0; i < affine_transform_keypoints_bob.Length; i++)
            {
                Imgproc.circle(small_frame, affine_transform_keypoints_bob[i], 1, new Scalar(0, 255, 0, 255), 2, Core.LINE_AA, 0);
            }
            //
            Imgproc.rectangle(small_frame, new Point(1, 1), new Point(small_frame_size.width - 2, small_frame_size.height - 2), new Scalar(255, 0, 0, 255), 2, Imgproc.LINE_8, 0);
            Imgproc.rectangle(small_frame, this.rect_ann.tl(), this.rect_ann.br(), new Scalar(255, 0, 0, 255), 1, Imgproc.LINE_8, 0);
            Imgproc.rectangle(small_frame, this.rect_bob.tl(), this.rect_bob.br(), new Scalar(255, 0, 0, 255), 1, Imgproc.LINE_8, 0);
            //
        }


        public void Dispose()
        {
            if (small_frame != null)
            {
                small_frame.Dispose();
                small_frame = null;
            }
            disposeMatROI();
            disposeMat();

            if (frame_c3 != null)
            {
                frame_c3.Dispose();
                frame_c3 = null;
            }
        }
    }
}
