using System;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.PhotoModule;
using Rect = OpenCVForUnity.CoreModule.Rect;
using OpenCVForUnity.UtilsModule;

namespace OpenCVForUnity.FaceSwap
{
    /// <summary>
    /// FaceSwaper.
    /// This code is a rewrite of https://github.com/mc-jesus/FaceSwap using "OpenCV for Unity" and "Dlib FaceLandmark Detector".
    /// </summary>
    public class FaceSwapper : IDisposable
    {
        protected Rect rect_ann, rect_bob;
        protected float maskAlpha;

        protected Point[] points_ann = new Point[9];
        protected Point[] points_bob = new Point[9];
        protected Point[] affine_transform_keypoints_ann = new Point[3];
        protected Point[] affine_transform_keypoints_bob = new Point[3];
        protected Size feather_amount = new Size ();

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


        // Initialize face swapped with landmarks.
        public FaceSwapper ()
        {
            trans_ann_to_bob = new Mat ();
            trans_bob_to_ann = new Mat ();
        }

        // Swaps faces in points on frame.
        public void SwapFaces (Mat frame, Point[] landmark_points_ann, Point[] landmark_points_bob, float alpha)
        {
            alpha = alpha > 0 ? alpha : 0;
            alpha = alpha < 1 ? alpha : 1; 
            maskAlpha = alpha;


            getFacePoints (landmark_points_ann, points_ann, affine_transform_keypoints_ann);
            getFacePoints (landmark_points_bob, points_bob, affine_transform_keypoints_bob);
            getFeather_amount (points_ann, feather_amount);

            rect_ann = Imgproc.boundingRect (new MatOfPoint (points_ann));
            rect_bob = Imgproc.boundingRect (new MatOfPoint (points_bob));


            Mat original_frame = frame;
            if (useSeamlessCloneForPasteFaces && (CvType.channels (original_frame.type ()) == 4)) {
                if (frame_c3 == null
                    || (frame_c3.width () != warpped_faces_full.width () || frame_c3.height () != warpped_faces_full.height ())
                    || frame_c3.type () != warpped_faces_full.type ()) {
                    if (frame_c3 != null) {
                        frame_c3.Dispose ();
                        frame_c3 = null;
                    }
                    frame_c3 = new Mat ();
                }
                Imgproc.cvtColor (frame, frame_c3, Imgproc.COLOR_RGBA2RGB);
                frame = frame_c3;
            }
            
            if (small_frame != null) {
                small_frame.Dispose ();
            }
            small_frame_rect = getMinFrameRect (frame, rect_ann, rect_bob);
            small_frame = new Mat (frame, small_frame_rect);
            small_frame_size = new Size (small_frame.cols (), small_frame.rows ());


            if (warpped_faces_full == null
                || (frame.width () != warpped_faces_full.width () || frame.height () != warpped_faces_full.height ())
                || frame.type () != warpped_faces_full.type ()) {
                createMat (frame);
            }
            createMatROI (small_frame_rect);


            rect_ann = getRectInFrame (small_frame, rect_ann);
            rect_bob = getRectInFrame (small_frame, rect_bob);
            points_ann = getPointsInFrame (small_frame, points_ann);
            points_bob = getPointsInFrame (small_frame, points_bob);
            affine_transform_keypoints_ann = getPointsInFrame (small_frame, affine_transform_keypoints_ann);
            affine_transform_keypoints_bob = getPointsInFrame (small_frame, affine_transform_keypoints_bob);


            getTransformationMatrices ();
            getMasks ();
            getWarppedMasks ();
            refined_masks = getRefinedMasks ();
            extractFaces ();
            warpped_faces = getWarppedFaces ();


            if (useSeamlessCloneForPasteFaces) {
                pasteFacesOnFrameUsingSeamlessClone (frame);
                
                if (CvType.channels (original_frame.type ()) == 4) {
                    Imgproc.cvtColor (frame, original_frame, Imgproc.COLOR_RGB2RGBA);

                    frame = original_frame;
                    if (small_frame != null) {
                        small_frame.Dispose ();
                        small_frame = null;
                    }
                    small_frame = new Mat (frame, small_frame_rect);
                }
            } else {
                featherMasks ();

                // Matches Ann face color to Bob face color and vice versa.
                using (Mat rect_ann_small_frame = new Mat (small_frame, rect_ann))
                using (Mat rect_ann_warpped_faces = new Mat (warpped_faces, rect_ann))
                using (Mat rect_ann_warpped_mask_bob = new Mat (warpped_mask_bob, rect_ann))
                using (Mat rect_ann_refined_masks = new Mat (refined_masks, rect_ann)) {
                    specifyHistogram (rect_ann_small_frame, rect_ann_warpped_faces, rect_ann_warpped_mask_bob);
//                    TransferColor_Lab (rect_ann_small_frame, rect_ann_warpped_faces, rect_ann_warpped_mask_bob);
//                    TransferColor_YCrCb (rect_ann_small_frame, rect_ann_warpped_faces, rect_ann_warpped_mask_bob);

                    // Pastes source face on original frame
                    AlphaBlend_pixel (rect_ann_warpped_faces, rect_ann_small_frame, rect_ann_refined_masks, rect_ann_small_frame);
//                    AlphaBlend_mat (rect_ann_warpped_faces, rect_ann_small_frame, rect_ann_refined_masks, rect_ann_small_frame);
                }
                using (Mat rect_bob_small_frame = new Mat (small_frame, rect_bob))
                using (Mat rect_bob_warpped_faces = new Mat (warpped_faces, rect_bob))
                using (Mat rect_bob_warpped_mask_ann = new Mat (warpped_mask_ann, rect_bob))
                using (Mat rect_bob_refined_masks = new Mat (refined_masks, rect_bob)) {
                    specifyHistogram (rect_bob_small_frame, rect_bob_warpped_faces, rect_bob_warpped_mask_ann);
//                    TransferColor_Lab (rect_bob_small_frame, rect_bob_warpped_faces, rect_bob_warpped_mask_ann);
//                    TransferColor_YCrCb (rect_bob_small_frame, rect_bob_warpped_faces, rect_bob_warpped_mask_ann);

                    // Pastes source face on original frame
                    AlphaBlend_pixel (rect_bob_warpped_faces, rect_bob_small_frame, rect_bob_refined_masks, rect_bob_small_frame);
//                    AlphaBlend_mat (rect_bob_warpped_faces, rect_bob_small_frame, rect_bob_refined_masks, rect_bob_small_frame);
                }
            }


            if (isShowingDebugFacePoints) {
                drawDebugFacePoints ();
            }
        }

        public void SwapFaces (Mat frame, List<Point> landmark_points_ann, List<Point> landmark_points_bob, float alpha)
        {
            SwapFaces (frame, landmark_points_ann.ToArray (), landmark_points_bob.ToArray (), alpha);
        }

        public void SwapFaces (Mat frame, Vector2[] landmark_points_ann, Vector2[] landmark_points_bob, float alpha)
        {
            Point[] landmark_points_ann_arr = new Point[landmark_points_ann.Length];
            for (int i = 0; i < landmark_points_ann_arr.Length; i++) {
                landmark_points_ann_arr [i] = new Point (landmark_points_ann [i].x, landmark_points_ann [i].y);
            }

            Point[] landmark_points_bob_arr = new Point[landmark_points_bob.Length];
            for (int i = 0; i < landmark_points_bob_arr.Length; i++) {
                landmark_points_bob_arr [i] = new Point (landmark_points_bob [i].x, landmark_points_bob [i].y);
            }

            SwapFaces (frame, landmark_points_ann_arr, landmark_points_bob_arr, alpha);
        }

        public void SwapFaces (Mat frame, List<Vector2> landmark_points_ann, List<Vector2> landmark_points_bob, float alpha)
        {
            Point[] landmark_points_ann_arr = new Point[landmark_points_ann.Count];
            for (int i = 0; i < landmark_points_ann_arr.Length; i++) {
                landmark_points_ann_arr [i] = new Point (landmark_points_ann [i].x, landmark_points_ann [i].y);
            }

            Point[] landmark_points_bob_arr = new Point[landmark_points_bob.Count];
            for (int i = 0; i < landmark_points_bob_arr.Length; i++) {
                landmark_points_bob_arr [i] = new Point (landmark_points_bob [i].x, landmark_points_bob [i].y);
            }

            SwapFaces (frame, landmark_points_ann_arr, landmark_points_bob_arr, alpha);
        }

        private void createMat (Mat frame)
        {
            disposeMat ();

            Size size = frame.size ();
            int type = frame.type ();

            refined_ann_and_bob_warpped_full = new Mat (size, CvType.CV_8UC1);
            refined_bob_and_ann_warpped_full = new Mat (size, CvType.CV_8UC1);

            warpped_face_ann_full = new Mat (size, type);
            warpped_face_bob_full = new Mat (size, type);

            mask_ann_full = new Mat (size, CvType.CV_8UC1);
            mask_bob_full = new Mat (size, CvType.CV_8UC1);
            warpped_mask_ann_full = new Mat (size, CvType.CV_8UC1);
            warpped_mask_bob_full = new Mat (size, CvType.CV_8UC1);
            refined_masks_full = new Mat (size, CvType.CV_8UC1, new Scalar (0));

            face_ann_full = new Mat (size, type);
            face_bob_full = new Mat (size, type);
            warpped_faces_full = new Mat (size, type, Scalar.all (0));
        }

        private void disposeMat ()
        {
            if (refined_ann_and_bob_warpped_full != null) {
                refined_ann_and_bob_warpped_full.Dispose ();
                refined_ann_and_bob_warpped_full = null;
            }
            if (refined_bob_and_ann_warpped_full != null) {
                refined_bob_and_ann_warpped_full.Dispose ();
                refined_bob_and_ann_warpped_full = null;
            }

            if (warpped_face_ann_full != null) {
                warpped_face_ann_full.Dispose ();
                warpped_face_ann_full = null;
            }
            if (warpped_face_bob_full != null) {
                warpped_face_bob_full.Dispose ();
                warpped_face_bob_full = null;
            }

            if (mask_ann_full != null) {
                mask_ann_full.Dispose ();
                mask_ann_full = null;
            }
            if (mask_bob_full != null) {
                mask_bob_full.Dispose ();
                mask_bob_full = null;
            }
            if (warpped_mask_ann_full != null) {
                warpped_mask_ann_full.Dispose ();
                warpped_mask_ann_full = null;
            }
            if (warpped_mask_bob_full != null) {
                warpped_mask_bob_full.Dispose ();
                warpped_mask_bob_full = null;
            }
            if (refined_masks_full != null) {
                refined_masks_full.Dispose ();
                refined_masks_full = null;
            }

            if (face_ann_full != null) {
                face_ann_full.Dispose ();
                face_ann_full = null;
            }
            if (face_bob_full != null) {
                face_bob_full.Dispose ();
                face_bob_full = null;
            }
            if (warpped_faces_full != null) {
                warpped_faces_full.Dispose ();
                warpped_faces_full = null;
            }
        }

        private void createMatROI (Rect roi)
        {
            disposeMatROI ();

            refined_ann_and_bob_warpped = new Mat (refined_ann_and_bob_warpped_full, roi);
            refined_bob_and_ann_warpped = new Mat (refined_bob_and_ann_warpped_full, roi);

            warpped_face_ann = new Mat (warpped_face_ann_full, roi);
            warpped_face_bob = new Mat (warpped_face_bob_full, roi);

            mask_ann = new Mat (mask_ann_full, roi);
            mask_bob = new Mat (mask_bob_full, roi);
            warpped_mask_ann = new Mat (warpped_mask_ann_full, roi);
            warpped_mask_bob = new Mat (warpped_mask_bob_full, roi);
            refined_masks = new Mat (refined_masks_full, roi);

            face_ann = new Mat (face_ann_full, roi);
            face_bob = new Mat (face_bob_full, roi);
            warpped_faces = new Mat (warpped_faces_full, roi);
        }

        private void disposeMatROI ()
        {
            if (refined_ann_and_bob_warpped != null) {
                refined_ann_and_bob_warpped.Dispose ();
                refined_ann_and_bob_warpped = null;
            }
            if (refined_bob_and_ann_warpped != null) {
                refined_bob_and_ann_warpped.Dispose ();
                refined_bob_and_ann_warpped = null;
            }

            if (warpped_face_ann != null) {
                warpped_face_ann.Dispose ();
                warpped_face_ann = null;
            }
            if (warpped_face_bob != null) {
                warpped_face_bob.Dispose ();
                warpped_face_bob = null;
            }

            if (mask_ann != null) {
                mask_ann.Dispose ();
                mask_ann = null;
            }
            if (mask_bob != null) {
                mask_bob.Dispose ();
                mask_bob = null;
            }
            if (warpped_mask_ann != null) {
                warpped_mask_ann.Dispose ();
                warpped_mask_ann = null;
            }
            if (warpped_mask_bob != null) {
                warpped_mask_bob.Dispose ();
                warpped_mask_bob = null;
            }
            if (refined_masks != null) {
                refined_masks.Dispose ();
                refined_masks = null;
            }

            if (face_ann != null) {
                face_ann.Dispose ();
                face_ann = null;
            }
            if (face_bob != null) {
                face_bob.Dispose ();
                face_bob = null;
            }
            if (warpped_faces != null) {
                warpped_faces.Dispose ();
                warpped_faces = null;
            }
        }

        // Returns minimal Mat containing both faces.
        private Rect getMinFrameRect (Mat frame, Rect rect_ann, Rect rect_bob)
        {
            Rect bounding_rect = RectUtils.Union (rect_ann, rect_bob);
            bounding_rect = RectUtils.Intersect (bounding_rect, new Rect (0, 0, frame.cols (), frame.rows ()));

            return bounding_rect;
        }

        private Rect getRectInFrame (Mat frame, Rect r)
        {
            Size wholesize = new Size ();
            Point ofs = new Point ();
            frame.locateROI (wholesize, ofs);

            Rect rect = new Rect (r.x - (int)ofs.x, r.y - (int)ofs.y, r.width, r.height);
            rect = RectUtils.Intersect (rect, new Rect (0, 0, frame.cols (), frame.rows ()));

            return rect;
        }

        // Finds facial landmarks on faces and extracts the useful points.
        protected virtual void getFacePoints (Point[] landmark_points, Point[] points, Point[] affine_transform_keypoints)
        {
            if (landmark_points.Length != 9)
                throw new ArgumentNullException ("Invalid landmark_points.");

            //points(facial contour)
            points [0] = landmark_points [0];
            points [1] = landmark_points [1];
            points [2] = landmark_points [2];
            points [3] = landmark_points [3];
            points [4] = landmark_points [4];
            points [5] = landmark_points [5];
            points [6] = landmark_points [6];
            points [7] = landmark_points [7];
            points [8] = landmark_points [8];

            //affine_transform_keypoints(eyes and chin)
            affine_transform_keypoints [0] = points [3];
            affine_transform_keypoints [1] = new Point (points [8].x, points [0].y);
            affine_transform_keypoints [2] = new Point (points [7].x, points [6].y);
        }

        private void getFeather_amount (Point[] points, Size feather_amount)
        {
            feather_amount.width = feather_amount.height = Core.norm (new MatOfPoint (new Point (points [0].x - points [6].x, points [0].y - points [6].y))) / 8;
        }

        private Point[] getPointsInFrame (Mat frame, Point[] p)
        {
            Size wholesize = new Size ();
            Point ofs = new Point ();
            frame.locateROI (wholesize, ofs);

            Point[] points = new Point[p.Length];

            for (int i = 0; i < p.Length; i++) {
                points [i] = new Point (p [i].x - ofs.x, p [i].y - ofs.y);
            }

            return points;
        }

        // Calculates transformation matrices based on points extracted by getFacePoints.
        private void getTransformationMatrices ()
        {
            trans_ann_to_bob = Imgproc.getAffineTransform (new MatOfPoint2f (affine_transform_keypoints_ann), new MatOfPoint2f (affine_transform_keypoints_bob));
            Imgproc.invertAffineTransform (trans_ann_to_bob, trans_bob_to_ann);
        }

        // Creates masks for faces based on the points extracted in getFacePoints.
        private void getMasks ()
        {
            mask_ann.setTo (Scalar.all (0));
            mask_bob.setTo (Scalar.all (0));
            Imgproc.fillConvexPoly (mask_ann, new MatOfPoint (points_ann), new Scalar (255 * maskAlpha));
            Imgproc.fillConvexPoly (mask_bob, new MatOfPoint (points_bob), new Scalar (255 * maskAlpha));
        }

        // Creates warpped masks out of masks created in getMasks to switch places.
        private void getWarppedMasks ()
        {
            Imgproc.warpAffine (mask_ann, warpped_mask_ann, trans_ann_to_bob, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar (0));
            Imgproc.warpAffine (mask_bob, warpped_mask_bob, trans_bob_to_ann, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar (0));
        }

        // Returns Mat of refined mask such that warpped mask isn't bigger than original mask.
        private Mat getRefinedMasks ()
        {
            Core.bitwise_and (mask_ann, warpped_mask_bob, refined_ann_and_bob_warpped);
            Core.bitwise_and (mask_bob, warpped_mask_ann, refined_bob_and_ann_warpped);

            refined_masks.setTo (Scalar.all (0));
            refined_ann_and_bob_warpped.copyTo (refined_masks, refined_ann_and_bob_warpped);
            refined_bob_and_ann_warpped.copyTo (refined_masks, refined_bob_and_ann_warpped);

            return refined_masks;
        }

        // Extracts faces from images based on masks created in getMasks.
        private void extractFaces ()
        {
            small_frame.copyTo (face_ann, mask_ann);
            small_frame.copyTo (face_bob, mask_bob);
        }

        // Creates warpped faces out of faces extracted in extractFaces.
        private Mat getWarppedFaces ()
        {  
            Imgproc.warpAffine (face_ann, warpped_face_ann, trans_ann_to_bob, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar (0, 0, 0));
            Imgproc.warpAffine (face_bob, warpped_face_bob, trans_bob_to_ann, small_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar (0, 0, 0));

            warpped_face_ann.copyTo (warpped_faces, warpped_mask_ann);
            warpped_face_bob.copyTo (warpped_faces, warpped_mask_bob);

            return warpped_faces;
        }
            
        // Blurs edges of masks.
        private void featherMasks ()
        {
            using (Mat rect_ann_refined_masks = new Mat (refined_masks, rect_ann)) {
                featherMask (rect_ann_refined_masks);
            }
            using (Mat rect_bob_refined_masks = new Mat (refined_masks, rect_bob)) {
                featherMask (rect_bob_refined_masks);
            }
        }

        // Blurs edges of mask.
        private void featherMask (Mat refined_masks)
        {
            Imgproc.erode (refined_masks, refined_masks, Imgproc.getStructuringElement (Imgproc.MORPH_RECT, feather_amount), new Point (-1, -1), 1, Core.BORDER_CONSTANT, new Scalar (0));
            Imgproc.blur (refined_masks, refined_masks, feather_amount, new Point (-1, -1), Core.BORDER_CONSTANT);
        }

        // Pastes faces on original frame.
        private void AlphaBlend_pixel (Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total () * fg.channels ()];
            MatUtils.copyFromMat<byte> (fg, fg_byte);
            byte[] bg_byte = new byte[bg.total () * bg.channels ()];
            MatUtils.copyFromMat<byte> (bg, bg_byte);
            byte[] alpha_byte = new byte[alpha.total () * alpha.channels ()];
            MatUtils.copyFromMat<byte> (alpha, alpha_byte);

            int pixel_i = 0;
            int channels = (int)bg.channels ();
            int total = (int)bg.total ();

            for (int i = 0; i < total; i++) {
                if (alpha_byte [i] == 0) {
                } else if (alpha_byte [i] == 255) {
                    bg_byte [pixel_i] = fg_byte [pixel_i];
                    bg_byte [pixel_i + 1] = fg_byte [pixel_i + 1];
                    bg_byte [pixel_i + 2] = fg_byte [pixel_i + 2];
                } else {
                    bg_byte [pixel_i] = (byte)(((255 - alpha_byte [i]) * bg_byte [pixel_i] + alpha_byte [i] * fg_byte [pixel_i]) >> 8);
                    bg_byte [pixel_i + 1] = (byte)(((255 - alpha_byte [i]) * bg_byte [pixel_i + 1] + alpha_byte [i] * fg_byte [pixel_i + 1]) >> 8);
                    bg_byte [pixel_i + 2] = (byte)(((255 - alpha_byte [i]) * bg_byte [pixel_i + 2] + alpha_byte [i] * fg_byte [pixel_i + 2]) >> 8);
                }
                pixel_i += channels;
            }

            MatUtils.copyToMat (bg_byte, dst);
        }

        List<Mat> channels = new List<Mat> ();
        Scalar scalar255 = new Scalar (255);
        // Pastes faces on original frame.
        private void AlphaBlend_mat (Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            using (Mat _alpha = scalar255 - alpha)
            using (Mat _bg = new Mat ()) {
                Core.split (bg, channels);
                Core.multiply (_alpha, channels [0], channels [0], 1.0 / 255);
                Core.multiply (_alpha, channels [1], channels [1], 1.0 / 255);
                Core.multiply (_alpha, channels [2], channels [2], 1.0 / 255);
                Core.merge (channels, _bg);

                using (Mat _fg = new Mat ()) {
                    Core.split (fg, channels);
                    Core.multiply (alpha, channels [0], channels [0], 1.0 / 255);
                    Core.multiply (alpha, channels [1], channels [1], 1.0 / 255);
                    Core.multiply (alpha, channels [2], channels [2], 1.0 / 255);
                    Core.merge (channels, _fg);

                    Core.add (_fg, _bg, dst);
                }
            }
        }


        Mat sourceMat_c3;
        Mat targetMat_c3;
        Mat sourceMatLab;
        Mat targetMatLab;
        // Super fast color transfer between images.
        private void TransferColor_Lab (Mat source, Mat target, Mat mask)
        {            
            bool is4chanelColor = false;
            if (source.channels () == 4) {

                if (sourceMat_c3 == null)
                    sourceMat_c3 = new Mat ();
                if (targetMat_c3 == null)
                    targetMat_c3 = new Mat ();

                is4chanelColor = true;
                Imgproc.cvtColor (source, sourceMat_c3, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor (target, targetMat_c3, Imgproc.COLOR_RGBA2RGB);
            } else {

                sourceMat_c3 = source;
                targetMat_c3 = target;
            }

            if (sourceMatLab == null)
                sourceMatLab = new Mat ();
            if (targetMatLab == null)
                targetMatLab = new Mat ();

            Imgproc.cvtColor (sourceMat_c3, sourceMatLab, Imgproc.COLOR_RGB2Lab);
            Imgproc.cvtColor (targetMat_c3, targetMatLab, Imgproc.COLOR_RGB2Lab);

            //            sourceMatLab.convertTo (sourceMatLab, CvType.CV_32FC3);
            //            targetMatLab.convertTo (targetMatLab, CvType.CV_32FC3);

            MatOfDouble labMeanSrc = new MatOfDouble ();
            MatOfDouble labStdSrc = new MatOfDouble ();
            Core.meanStdDev (sourceMatLab, labMeanSrc, labStdSrc, mask);

            MatOfDouble labMeanTar = new MatOfDouble ();
            MatOfDouble labStdTar = new MatOfDouble ();
            Core.meanStdDev (targetMatLab, labMeanTar, labStdTar, mask);

            targetMatLab.convertTo (targetMatLab, CvType.CV_32FC3);

            // subtract the means from the target image
            double[] labMeanTarArr = labMeanTar.toArray ();
            Core.subtract (targetMatLab, new Scalar (labMeanTarArr [0], labMeanTarArr [1], labMeanTarArr [2]), targetMatLab);

            // scale by the standard deviations
            double[] labStdTarArr = labStdTar.toArray ();
            double[] labStdSrcArr = labStdSrc.toArray ();
            Scalar scalar = new Scalar (labStdTarArr [0] / labStdSrcArr [0], labStdTarArr [1] / labStdSrcArr [1], labStdTarArr [2] / labStdSrcArr [2]);
            Core.multiply (targetMatLab, scalar, targetMatLab);

            // add in the source mean
            double[] labMeanSrcArr = labMeanSrc.toArray ();
            Core.add (targetMatLab, new Scalar (labMeanSrcArr [0], labMeanSrcArr [1], labMeanSrcArr [2]), targetMatLab);

            // clip the pixel intensities to [0, 255] if they fall outside this range.
            //Imgproc.threshold (targetMatLab, targetMatLab, 0, 0, Imgproc.THRESH_TOZERO);
            //Imgproc.threshold (targetMatLab, targetMatLab, 255, 255, Imgproc.THRESH_TRUNC);

            targetMatLab.convertTo (targetMatLab, CvType.CV_8UC3);
            Imgproc.cvtColor (targetMatLab, targetMat_c3, Imgproc.COLOR_Lab2RGB);

            if (is4chanelColor) {
                Imgproc.cvtColor (sourceMat_c3, source, Imgproc.COLOR_RGB2RGBA);
                Imgproc.cvtColor (targetMat_c3, target, Imgproc.COLOR_RGB2RGBA);
            }
        }

        Mat sourceMatYCrCb;
        Mat targetMatYCrCb;
        // Super fast color transfer between images. (Slightly faster than processing in Lab color format)
        private void TransferColor_YCrCb (Mat source, Mat target, Mat mask)
        {
            bool is4chanelColor = false;
            if (source.channels () == 4) {

                if (sourceMat_c3 == null)
                    sourceMat_c3 = new Mat ();
                if (targetMat_c3 == null)
                    targetMat_c3 = new Mat ();

                is4chanelColor = true;
                Imgproc.cvtColor (source, sourceMat_c3, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor (target, targetMat_c3, Imgproc.COLOR_RGBA2RGB);
            } else {

                sourceMat_c3 = source;
                targetMat_c3 = target;
            }

            if (sourceMatYCrCb == null)
                sourceMatYCrCb = new Mat ();
            if (targetMatYCrCb == null)
                targetMatYCrCb = new Mat ();

            Imgproc.cvtColor (sourceMat_c3, sourceMatYCrCb, Imgproc.COLOR_RGB2YCrCb);
            Imgproc.cvtColor (targetMat_c3, targetMatYCrCb, Imgproc.COLOR_RGB2YCrCb);

            MatOfDouble labMeanSrc = new MatOfDouble ();
            MatOfDouble labStdSrc = new MatOfDouble ();
            Core.meanStdDev (sourceMatYCrCb, labMeanSrc, labStdSrc, mask);

            MatOfDouble labMeanTar = new MatOfDouble ();
            MatOfDouble labStdTar = new MatOfDouble ();
            Core.meanStdDev (targetMatYCrCb, labMeanTar, labStdTar, mask);

            targetMatYCrCb.convertTo (targetMatYCrCb, CvType.CV_32FC3);

            // subtract the means from the target image
            double[] labMeanTarArr = labMeanTar.toArray ();
            Core.subtract (targetMatYCrCb, new Scalar (labMeanTarArr [0], labMeanTarArr [1], labMeanTarArr [2]), targetMatYCrCb);

            // scale by the standard deviations
            double[] labStdTarArr = labStdTar.toArray ();
            double[] labStdSrcArr = labStdSrc.toArray ();
            Scalar scalar = new Scalar (labStdTarArr [0] / labStdSrcArr [0], labStdTarArr [1] / labStdSrcArr [1], labStdTarArr [2] / labStdSrcArr [2]);
            Core.multiply (targetMatYCrCb, scalar, targetMatYCrCb);

            // add in the source mean
            double[] labMeanSrcArr = labMeanSrc.toArray ();
            Core.add (targetMatYCrCb, new Scalar (labMeanSrcArr [0], labMeanSrcArr [1], labMeanSrcArr [2]), targetMatYCrCb);

            // clip the pixel intensities to [0, 255] if they fall outside this range.
            //Imgproc.threshold (targetMatYCrCb, targetMatYCrCb, 0, 0, Imgproc.THRESH_TOZERO);
            //Imgproc.threshold (targetMatYCrCb, targetMatYCrCb, 255, 255, Imgproc.THRESH_TRUNC);

            targetMatYCrCb.convertTo (targetMatYCrCb, CvType.CV_8UC3);
            Imgproc.cvtColor (targetMatYCrCb, targetMat_c3, Imgproc.COLOR_YCrCb2RGB);

            if (is4chanelColor) {
                Imgproc.cvtColor (targetMat_c3, target, Imgproc.COLOR_RGB2RGBA);
            }
        }

        // Calculates source image histogram and changes target_image to match source hist.
        private void specifyHistogram (Mat source_image, Mat target_image, Mat mask)
        {
            byte[][] LUT = new byte[3][];
            for (int i = 0; i < LUT.Length; i++) {
                LUT [i] = new byte[256];
            }
            double[][] source_hist = new double[3][];
            for (int i = 0; i < source_hist.Length; i++) {
                source_hist [i] = new double[256];
            }
            double[][] target_hist = new double[3][];
            for (int i = 0; i < target_hist.Length; i++) {
                target_hist [i] = new double[256];
            }
            double[][] source_cdf = new double[3][];
            for (int i = 0; i < source_cdf.Length; i++) {
                source_cdf [i] = new double[256];
            }
            double[][] target_cdf = new double[3][];
            for (int i = 0; i < target_cdf.Length; i++) {
                target_cdf [i] = new double[256];
            }

            double[] source_histMax = new double[3];
            double[] target_histMax = new double[3];

            byte[] mask_byte = new byte[mask.total () * mask.channels ()];
            MatUtils.copyFromMat<byte> (mask, mask_byte);
            byte[] source_image_byte = new byte[source_image.total () * source_image.channels ()];
            MatUtils.copyFromMat<byte> (source_image, source_image_byte);
            byte[] target_image_byte = new byte[target_image.total () * target_image.channels ()];
            MatUtils.copyFromMat<byte> (target_image, target_image_byte);

            int pixel_i = 0;
            int channels = (int)source_image.channels ();
            int total = (int)source_image.total ();
            for (int i = 0; i < total; i++) {
                if (mask_byte [i] != 0) {
                    byte c = source_image_byte [pixel_i];
                    source_hist [0] [c]++;
                    if (source_hist [0] [c] > source_histMax [0])
                        source_histMax [0] = source_hist [0] [c];

                    c = source_image_byte [pixel_i + 1];
                    source_hist [1] [c]++;
                    if (source_hist [1] [c] > source_histMax [1])
                        source_histMax [1] = source_hist [1] [c];

                    c = source_image_byte [pixel_i + 2];
                    source_hist [2] [c]++;
                    if (source_hist [2] [c] > source_histMax [2])
                        source_histMax [2] = source_hist [2] [c];

                    c = target_image_byte [pixel_i];
                    target_hist [0] [c]++;
                    if (target_hist [0] [c] > target_histMax [0])
                        target_histMax [0] = target_hist [0] [c];

                    c = target_image_byte [pixel_i + 1];
                    target_hist [1] [c]++;
                    if (target_hist [1] [c] > target_histMax [1])
                        target_histMax [1] = target_hist [1] [c];

                    c = target_image_byte [pixel_i + 2];
                    target_hist [2] [c]++;
                    if (target_hist [2] [c] > target_histMax [2])
                        target_histMax [2] = target_hist [2] [c];
                }
                // Advance to next pixel
                pixel_i += channels;
            }

            // Normalize hist
            for (int i = 0; i < 256; i++) {
                source_hist [0] [i] /= source_histMax [0];
                source_hist [1] [i] /= source_histMax [1];
                source_hist [2] [i] /= source_histMax [2];

                target_hist [0] [i] /= target_histMax [0];
                target_hist [1] [i] /= target_histMax [1];
                target_hist [2] [i] /= target_histMax [2];
            }

            // Calc cumulative distribution function (CDF) 
            source_cdf [0] [0] = source_hist [0] [0];
            source_cdf [1] [0] = source_hist [1] [0];
            source_cdf [2] [0] = source_hist [2] [0];
            target_cdf [0] [0] = target_hist [0] [0];
            target_cdf [1] [0] = target_hist [1] [0];
            target_cdf [2] [0] = target_hist [2] [0];
            for (int i = 1; i < 256; i++) {
                source_cdf [0] [i] = source_cdf [0] [i - 1] + source_hist [0] [i];
                source_cdf [1] [i] = source_cdf [1] [i - 1] + source_hist [1] [i];
                source_cdf [2] [i] = source_cdf [2] [i - 1] + source_hist [2] [i];

                target_cdf [0] [i] = target_cdf [0] [i - 1] + target_hist [0] [i];
                target_cdf [1] [i] = target_cdf [1] [i - 1] + target_hist [1] [i];
                target_cdf [2] [i] = target_cdf [2] [i - 1] + target_hist [2] [i];
            }

            // Normalize CDF
            for (int i = 0; i < 256; i++) {
                source_cdf [0] [i] /= source_cdf [0] [255];
                source_cdf [1] [i] /= source_cdf [1] [255];
                source_cdf [2] [i] /= source_cdf [2] [255];

                target_cdf [0] [i] /= target_cdf [0] [255];
                target_cdf [1] [i] /= target_cdf [1] [255];
                target_cdf [2] [i] /= target_cdf [2] [255];
            }

            // Create lookup table (LUT)
            const double HISTMATCH_EPSILON = 0.000001f;
            for (int i = 0; i < 3; i++) {
                int last = 0;
                for (int j = 0; j < 256; j++) {
                    double F1j = target_cdf [i] [j];

                    for (int k = last; k < 256; k++) {
                        double F2k = source_cdf [i] [k];
                        if (Math.Abs (F2k - F1j) < HISTMATCH_EPSILON || F2k > F1j) {
                            LUT [i] [j] = (byte)k;
                            last = k;
                            break;
                        }
                    }
                }
            }

            // Repaint pixels
            pixel_i = 0;
            for (int i = 0; i < total; i++) {
                if (mask_byte [i] != 0) {
                    target_image_byte [pixel_i] = LUT [0] [target_image_byte [pixel_i]];
                    target_image_byte [pixel_i + 1] = LUT [1] [target_image_byte [pixel_i + 1]];
                    target_image_byte [pixel_i + 2] = LUT [2] [target_image_byte [pixel_i + 2]];
                }
                // Advance to next pixel
                pixel_i += channels;
            }

            MatUtils.copyToMat (target_image_byte, target_image);
        }


        // Pastes faces on original frame using SeamlessClone.
        private void pasteFacesOnFrameUsingSeamlessClone (Mat full_frame)
        {
            //Processing to avoid the problem that synthesis is shifted.
            Imgproc.rectangle (refined_masks, new Point (1, 1), new Point (small_frame_size.width - 1, small_frame_size.height - 1), new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_8, 0);

            using (Mat full_dst_frame = new Mat ()) {
                full_frame.copyTo (full_dst_frame);
                using (Mat small_dest_frame = new Mat (full_dst_frame, small_frame_rect)) {
                    //Utils.setDebugMode(true);
                    Photo.seamlessClone (warpped_faces, small_frame, refined_masks, new Point (small_frame_size.width / 2, small_frame_size.height / 2), small_dest_frame, Photo.NORMAL_CLONE);
                    //Utils.setDebugMode(false);
                }
                full_dst_frame.copyTo (full_frame);      
            }
        }


        private void drawDebugFacePoints ()
        {
            for (int i = 0; i < points_ann.Length; i++) {
                Imgproc.circle (small_frame, points_ann [i], 1, new Scalar (255, 0, 0, 255), 2, Imgproc.LINE_AA, 0);
            }
            for (int i = 0; i < affine_transform_keypoints_ann.Length; i++) {
                Imgproc.circle (small_frame, affine_transform_keypoints_ann [i], 1, new Scalar (0, 255, 0, 255), 2, Imgproc.LINE_AA, 0);
            }
            for (int i = 0; i < points_bob.Length; i++) {
                Imgproc.circle (small_frame, points_bob [i], 1, new Scalar (255, 0, 0, 255), 2, Imgproc.LINE_AA, 0);
            }
            for (int i = 0; i < affine_transform_keypoints_bob.Length; i++) {
                Imgproc.circle (small_frame, affine_transform_keypoints_bob [i], 1, new Scalar (0, 255, 0, 255), 2, Imgproc.LINE_AA, 0);
            }
            //
            Imgproc.rectangle (small_frame, new Point (1, 1), new Point (small_frame_size.width - 2, small_frame_size.height - 2), new Scalar (255, 0, 0, 255), 2, Imgproc.LINE_8, 0);
            Imgproc.rectangle (small_frame, this.rect_ann.tl (), this.rect_ann.br (), new Scalar (255, 0, 0, 255), 1, Imgproc.LINE_8, 0);
            Imgproc.rectangle (small_frame, this.rect_bob.tl (), this.rect_bob.br (), new Scalar (255, 0, 0, 255), 1, Imgproc.LINE_8, 0);
            //
        }


        public void Dispose ()
        {
            if (small_frame != null) {
                small_frame.Dispose ();
                small_frame = null;
            }
            disposeMatROI ();
            disposeMat ();

            if (frame_c3 != null) {
                frame_c3.Dispose ();
                frame_c3 = null;
            }

            foreach (var c in channels) {
                c.Dispose ();
            }
            channels.Clear ();
            if (sourceMat_c3 != null) {
                sourceMat_c3.Dispose ();
                sourceMat_c3 = null;
            }
            if (targetMat_c3 != null) {
                targetMat_c3.Dispose ();
                targetMat_c3 = null;
            }
            if (sourceMatLab != null) {
                sourceMatLab.Dispose ();
                sourceMatLab = null;
            }
            if (targetMatLab != null) {
                targetMatLab.Dispose ();
                targetMatLab = null;
            }
            if (sourceMatYCrCb != null) {
                sourceMatYCrCb.Dispose ();
                sourceMatYCrCb = null;
            }
            if (targetMatYCrCb != null) {
                targetMatYCrCb.Dispose ();
                targetMatYCrCb = null;
            }
        }
    }
}
