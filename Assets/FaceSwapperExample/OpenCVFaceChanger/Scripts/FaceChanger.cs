using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnity.FaceChange
{
    /// <summary>
    /// FaceChanger.
    /// This code is a rewrite of https://github.com/mc-jesus/FaceSwap using "OpenCV for Unity".
    /// </summary>
    public class FaceChanger : IDisposable
    {
        protected List<FaceChangeData> faceChangeData = new List<FaceChangeData>();

        protected Rect target_rect, source_rect;
        protected float maskAlpha;

        protected Point[] target_points = new Point[9];
        protected Point[] source_points = new Point[9];
        protected Point[] target_affine_transform_keypoints = new Point[3];
        protected Point[] source_affine_transform_keypoints = new Point[3];
        protected Size feather_amount = new Size();

        protected Mat target_frame_full;
        protected Mat target_frame;
        protected Size target_frame_size;
        protected Rect target_frame_rect;

        //full size Mat
        protected Mat target_mask_full;
        protected Mat source_mask_full;
        protected Mat warpped_source_mask_full;
        protected Mat refined_target_and_warpped_source_mask_full;
        protected Mat refined_mask_full;
        protected Mat source_face_full;
        protected Mat warpped_source_face_full;
        protected Mat warpped_face_full;

        //ROI Mat
        protected Mat target_mask;
        protected Mat source_mask;
        protected Mat warpped_source_mask;
        protected Mat refined_target_and_warpped_source_mask;
        protected Mat refined_mask;
        protected Mat source_face;
        protected Mat warpped_source_face;
        protected Mat warpped_face;

        //affineTransforms
        protected Mat trans_source_to_target = new Mat();


        protected byte[,] LUT = new byte[3, 256];
        protected int[,] source_hist_int = new int[3, 256];
        protected int[,] target_hist_int = new int[3, 256];
        protected float[,] source_histogram = new float[3, 256];
        protected float[,] target_histogram = new float[3, 256];

        public bool enableColorCorrectFace = true;
        public bool isShowingDebugFacePoints = false;


        // Initialize face changed with landmarks
        public FaceChanger()
        {

        }

        public void SetTargetImage(Mat frame)
        {
            target_frame_full = frame;

            if (warpped_face_full == null
                || (target_frame_full.width() != warpped_face_full.width() || target_frame_full.height() != warpped_face_full.height())
                || target_frame_full.type() != warpped_face_full.type())
            {
                createMat(target_frame_full);
            }
        }


        //Change faces in points on frame
        public void AddFaceChangeData(Mat source_frame, Point[] source_landmark_points, Point[] target_landmark_points, float alpha)
        {

            faceChangeData.Add(new FaceChangeData(source_frame, source_landmark_points, target_landmark_points, alpha));

        }

        public void AddFaceChangeData(Mat source_frame, List<Point> source_landmark_points, List<Point> target_landmark_points, float alpha)
        {

            faceChangeData.Add(new FaceChangeData(source_frame, source_landmark_points.ToArray(), target_landmark_points.ToArray(), alpha));

        }

        public void AddFaceChangeData(Mat source_frame, Vector2[] source_landmark_points, Vector2[] target_landmark_points, float alpha)
        {
            Point[] landmark_points_source_arr = new Point[source_landmark_points.Length];
            for (int i = 0; i < landmark_points_source_arr.Length; i++)
            {
                landmark_points_source_arr[i] = new Point(source_landmark_points[i].x, source_landmark_points[i].y);
            }

            Point[] landmark_points_target_arr = new Point[target_landmark_points.Length];
            for (int i = 0; i < landmark_points_target_arr.Length; i++)
            {
                landmark_points_target_arr[i] = new Point(target_landmark_points[i].x, target_landmark_points[i].y);
            }

            AddFaceChangeData(source_frame, landmark_points_source_arr, landmark_points_target_arr, alpha);
        }

        public void AddFaceChangeData(Mat source_frame, List<Vector2> source_landmark_points, List<Vector2> target_landmark_points, float alpha)
        {
            Point[] landmark_points_source_arr = new Point[source_landmark_points.Count];
            for (int i = 0; i < landmark_points_source_arr.Length; i++)
            {
                landmark_points_source_arr[i] = new Point(source_landmark_points[i].x, source_landmark_points[i].y);
            }

            Point[] landmark_points_target_arr = new Point[target_landmark_points.Count];
            for (int i = 0; i < landmark_points_target_arr.Length; i++)
            {
                landmark_points_target_arr[i] = new Point(target_landmark_points[i].x, target_landmark_points[i].y);
            }

            AddFaceChangeData(source_frame, landmark_points_source_arr, landmark_points_target_arr, alpha);
        }

        public void ClearFaceChangeData()
        {
            faceChangeData.Clear();
        }

        public void ChangeFace()
        {
            foreach (FaceChangeData data in faceChangeData)
            {
                changeFace(data);
            }

            // Pastes source face on original frame
            AlphaBlend_pixel(warpped_face_full, target_frame_full, refined_mask_full, target_frame_full);
            //            AlphaBlend_mat (warpped_face_full, target_frame_full, refined_mask_full, target_frame_full);


            if (isShowingDebugFacePoints)
            {
                drawDebugFacePoints();
            }

            faceChangeData.Clear();

            //reset mask
            refined_mask_full.setTo(Scalar.all(0));
        }

        private void changeFace(FaceChangeData data)
        {
            maskAlpha = data.alpha;

            getFacePoints(data.target_landmark_points, target_points, target_affine_transform_keypoints);
            getFacePoints(data.source_landmark_points, source_points, source_affine_transform_keypoints);
            getFeather_amount(target_points, feather_amount);

            target_rect = Imgproc.boundingRect(new MatOfPoint(target_points));
            source_rect = Imgproc.boundingRect(new MatOfPoint(source_points));

            Rect rect_in_source_frame = getRectInFrame(data.source_frame, source_rect);

            //            Debug.Log(source_rect.x + " " + source_rect.y);

            if (source_rect.width > target_frame_full.width() || source_rect.height > target_frame_full.height())
                throw new ArgumentNullException("The size of the face area in source image is too large.");


            int shift_x = 0;
            int shift_y = 0;
            //shift rect
            if (target_rect.x + source_rect.width <= target_frame_full.width())
            {
                shift_x = target_rect.x - source_rect.x;
            }
            else
            {
                shift_x = target_frame_full.width() - source_rect.width - source_rect.x;
            }
            if (target_rect.y + source_rect.height <= target_frame_full.height())
            {
                shift_y = target_rect.y - source_rect.y;
            }
            else
            {
                shift_y = target_frame_full.height() - source_rect.height - source_rect.y;
            }
            source_rect.x = source_rect.x + shift_x;
            source_rect.y = source_rect.y + shift_y;

            //shift points
            for (int i = 0; i < source_points.Length; i++)
            {
                source_points[i].x = source_points[i].x + shift_x;
                source_points[i].y = source_points[i].y + shift_y;
            }

            for (int i = 0; i < source_affine_transform_keypoints.Length; i++)
            {
                source_affine_transform_keypoints[i].x = source_affine_transform_keypoints[i].x + shift_x;
                source_affine_transform_keypoints[i].y = source_affine_transform_keypoints[i].y + shift_y;
            }


            if (target_frame != null)
            {
                target_frame.Dispose();
            }
            target_frame_rect = getMinFrameRect(target_frame_full, target_rect, source_rect);
            target_frame = new Mat(target_frame_full, target_frame_rect);
            target_frame_size = new Size(target_frame.cols(), target_frame.rows());

            createMatROI(target_frame_rect);



            target_rect = getRectInFrame(target_frame, target_rect);
            source_rect = getRectInFrame(target_frame, source_rect);
            target_points = getPointsInFrame(target_frame, target_points);
            source_points = getPointsInFrame(target_frame, source_points);
            target_affine_transform_keypoints = getPointsInFrame(target_frame, target_affine_transform_keypoints);
            source_affine_transform_keypoints = getPointsInFrame(target_frame, source_affine_transform_keypoints);


            getTransformationMatrice();
            getMasks();
            getWarppedMask();
            refined_mask = getRefinedMask();
            extractFace(data.source_frame, rect_in_source_frame);
            warpped_face = getWarppedFace();

            featherMask();

            // Matches Target face color to Source face color and vice versa
            if (enableColorCorrectFace)
            {
                using (Mat target_frame_target_rect = new Mat(target_frame, target_rect))
                using (Mat warpped_face_target_rect = new Mat(warpped_face, target_rect))
                using (Mat warpped_source_mask_target_rect = new Mat(warpped_source_mask, target_rect))
                {
                    specifyHistogram(target_frame_target_rect, warpped_face_target_rect, warpped_source_mask_target_rect);
                    //                    TransferColor_Lab (target_frame_target_rect, warpped_face_target_rect, warpped_source_mask_target_rect);
                    //                    TransferColor_YCrCb (target_frame_target_rect, warpped_face_target_rect, warpped_source_mask_target_rect);
                }
            }
        }

        private void createMat(Mat frame)
        {
            disposeMat();

            Size size = frame.size();
            int type = frame.type();

            target_mask_full = new Mat(size, CvType.CV_8UC1);
            source_mask_full = new Mat(size, CvType.CV_8UC1);
            warpped_source_mask_full = new Mat(size, CvType.CV_8UC1);
            refined_target_and_warpped_source_mask_full = new Mat(size, CvType.CV_8UC1);
            refined_mask_full = new Mat(size, CvType.CV_8UC1, new Scalar(0));

            source_face_full = new Mat(size, type);
            warpped_source_face_full = new Mat(size, type);
            warpped_face_full = new Mat(size, type, Scalar.all(0));
        }

        private void disposeMat()
        {

            if (target_mask_full != null)
            {
                target_mask_full.Dispose();
                target_mask_full = null;
            }
            if (source_mask_full != null)
            {
                source_mask_full.Dispose();
                source_mask_full = null;
            }
            if (warpped_source_mask_full != null)
            {
                warpped_source_mask_full.Dispose();
                warpped_source_mask_full = null;
            }
            if (refined_target_and_warpped_source_mask_full != null)
            {
                refined_target_and_warpped_source_mask_full.Dispose();
                refined_target_and_warpped_source_mask_full = null;
            }

            if (refined_mask_full != null)
            {
                refined_mask_full.Dispose();
                refined_mask_full = null;
            }

            if (source_face_full != null)
            {
                source_face_full.Dispose();
                source_face_full = null;
            }
            if (warpped_source_face_full != null)
            {
                warpped_source_face_full.Dispose();
                warpped_source_face_full = null;
            }
            if (warpped_face_full != null)
            {
                warpped_face_full.Dispose();
                warpped_face_full = null;
            }
        }

        private void createMatROI(Rect roi)
        {
            disposeMatROI();

            target_mask = new Mat(target_mask_full, roi);
            source_mask = new Mat(source_mask_full, roi);
            warpped_source_mask = new Mat(warpped_source_mask_full, roi);
            refined_target_and_warpped_source_mask = new Mat(refined_target_and_warpped_source_mask_full, roi);
            refined_mask = new Mat(refined_mask_full, roi);

            source_face = new Mat(source_face_full, roi);
            warpped_source_face = new Mat(warpped_source_face_full, roi);
            warpped_face = new Mat(warpped_face_full, roi);
        }

        private void disposeMatROI()
        {

            if (target_mask != null)
            {
                target_mask.Dispose();
                target_mask = null;
            }
            if (source_mask != null)
            {
                source_mask.Dispose();
                source_mask = null;
            }
            if (warpped_source_mask != null)
            {
                warpped_source_mask.Dispose();
                warpped_source_mask = null;
            }
            if (refined_target_and_warpped_source_mask != null)
            {
                refined_target_and_warpped_source_mask.Dispose();
                refined_target_and_warpped_source_mask = null;
            }
            if (refined_mask != null)
            {
                refined_mask.Dispose();
                refined_mask = null;
            }

            if (source_face != null)
            {
                source_face.Dispose();
                source_face = null;
            }
            if (warpped_source_face != null)
            {
                warpped_source_face.Dispose();
                warpped_source_face = null;
            }
            if (warpped_face != null)
            {
                warpped_face.Dispose();
                warpped_face = null;
            }
        }

        // Returns minimal Mat containing both faces
        private Rect getMinFrameRect(Mat frame, Rect target_rect, Rect source_rect)
        {
            Rect bounding_rect = RectUtils.Union(target_rect, source_rect);
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

        // Finds facial landmarks on face and extracts the useful points
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

        // Calculates transformation matrice based on points extracted by getFacePoints
        private void getTransformationMatrice()
        {
            trans_source_to_target = Imgproc.getAffineTransform(new MatOfPoint2f(source_affine_transform_keypoints), new MatOfPoint2f(target_affine_transform_keypoints));
        }

        // Creates masks for faces based on the points extracted in getFacePoints
        private void getMasks()
        {
            target_mask.setTo(Scalar.all(0));
            source_mask.setTo(Scalar.all(0));
            Imgproc.fillConvexPoly(target_mask, new MatOfPoint(target_points), new Scalar(255 * maskAlpha));
            Imgproc.fillConvexPoly(source_mask, new MatOfPoint(source_points), new Scalar(255 * maskAlpha));
        }

        // Creates warpped mask out of mask created in getMasks to switch places
        private void getWarppedMask()
        {
            Imgproc.warpAffine(source_mask, warpped_source_mask, trans_source_to_target, target_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0));
        }

        // Returns Mat of refined mask such that warpped mask isn't bigger than original mask
        private Mat getRefinedMask()
        {

            Core.bitwise_and(target_mask, warpped_source_mask, refined_target_and_warpped_source_mask);
            refined_target_and_warpped_source_mask.copyTo(refined_mask, refined_target_and_warpped_source_mask);

            return refined_mask;
        }

        // Extracts face from source image based on mask created in getMask
        private void extractFace(Mat frame, Rect rect)
        {
            Rect new_source_rect = source_rect.clone();

            if (rect.width != new_source_rect.width)
            {
                int shift_x = 0;
                if (rect.x == 0 || new_source_rect.x == 0)
                {
                    if (rect.width < new_source_rect.width)
                    {
                        shift_x = new_source_rect.width - rect.width;
                        new_source_rect.x += shift_x;
                        new_source_rect.width -= shift_x;
                    }
                    else
                    {
                        shift_x = rect.width - new_source_rect.width;
                        rect.x += shift_x;
                        rect.width -= shift_x;
                    }
                }

                if (rect.x + rect.width == frame.width() || source_rect.x + new_source_rect.width == target_frame_full.width())
                {
                    if (rect.width < new_source_rect.width)
                    {
                        shift_x = new_source_rect.width - rect.width;
                        new_source_rect.width -= shift_x;
                    }
                    else
                    {
                        shift_x = rect.width - new_source_rect.width;
                        rect.width -= shift_x;
                    }
                }
            }

            if (rect.height != new_source_rect.height)
            {
                int shift_y = 0;
                if (rect.y == 0 || new_source_rect.y == 0)
                {
                    if (rect.height < new_source_rect.height)
                    {
                        shift_y = new_source_rect.height - rect.height;
                        new_source_rect.y += shift_y;
                        new_source_rect.height -= shift_y;
                    }
                    else
                    {
                        shift_y = rect.height - new_source_rect.height;
                        rect.y += shift_y;
                        rect.height -= shift_y;
                    }
                }

                if (rect.y + rect.height == frame.height() || source_rect.y + new_source_rect.height == target_frame_full.height())
                {
                    if (rect.height < new_source_rect.height)
                    {
                        shift_y = new_source_rect.height - rect.height;
                        new_source_rect.height -= shift_y;
                    }
                    else
                    {
                        shift_y = rect.height - new_source_rect.height;
                        rect.height -= shift_y;
                    }
                }
            }

            using (Mat source_frame_source_rect = new Mat(frame, rect))
            using (Mat source_face_source_rect = new Mat(source_face, new_source_rect))
            using (Mat source_mask_source_rect = new Mat(source_mask, new_source_rect))
            {
                source_frame_source_rect.copyTo(source_face_source_rect, source_mask_source_rect);
            }
        }

        // Creates warpped face out of face extracted in extractFace
        private Mat getWarppedFace()
        {
            Imgproc.warpAffine(source_face, warpped_source_face, trans_source_to_target, target_frame_size, Imgproc.INTER_NEAREST, Core.BORDER_CONSTANT, new Scalar(0, 0, 0));
            warpped_source_face.copyTo(warpped_face, warpped_source_mask);

            return warpped_face;
        }

        // Blurs edges of mask
        private void featherMask()
        {
            using (Mat refined_mask_target_rect = new Mat(refined_mask, target_rect))
            {
                Imgproc.erode(refined_mask, refined_mask, Imgproc.getStructuringElement(Imgproc.MORPH_RECT, feather_amount), new Point(-1, -1), 1, Core.BORDER_CONSTANT, new Scalar(0));
                Imgproc.blur(refined_mask, refined_mask, feather_amount, new Point(-1, -1), Core.BORDER_CONSTANT);
            }
        }

        // Pastes faces on original frame.
        private void AlphaBlend_pixel(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            byte[] fg_byte = new byte[fg.total() * fg.channels()];
            MatUtils.copyFromMat<byte>(fg, fg_byte);
            byte[] bg_byte = new byte[bg.total() * bg.channels()];
            MatUtils.copyFromMat<byte>(bg, bg_byte);
            byte[] alpha_byte = new byte[alpha.total() * alpha.channels()];
            MatUtils.copyFromMat<byte>(alpha, alpha_byte);

            int pixel_i = 0;
            int channels = (int)bg.channels();
            int total = (int)bg.total();

            for (int i = 0; i < total; i++)
            {
                if (alpha_byte[i] == 0)
                {
                }
                else if (alpha_byte[i] == 255)
                {
                    bg_byte[pixel_i] = fg_byte[pixel_i];
                    bg_byte[pixel_i + 1] = fg_byte[pixel_i + 1];
                    bg_byte[pixel_i + 2] = fg_byte[pixel_i + 2];
                }
                else
                {
                    bg_byte[pixel_i] = (byte)(((255 - alpha_byte[i]) * bg_byte[pixel_i] + alpha_byte[i] * fg_byte[pixel_i]) >> 8);
                    bg_byte[pixel_i + 1] = (byte)(((255 - alpha_byte[i]) * bg_byte[pixel_i + 1] + alpha_byte[i] * fg_byte[pixel_i + 1]) >> 8);
                    bg_byte[pixel_i + 2] = (byte)(((255 - alpha_byte[i]) * bg_byte[pixel_i + 2] + alpha_byte[i] * fg_byte[pixel_i + 2]) >> 8);
                }
                pixel_i += channels;
            }

            MatUtils.copyToMat(bg_byte, dst);
        }

        List<Mat> channels = new List<Mat>();
        Scalar scalar255 = new Scalar(255);
        // Pastes faces on original frame.
        private void AlphaBlend_mat(Mat fg, Mat bg, Mat alpha, Mat dst)
        {
            using (Mat _alpha = scalar255 - alpha)
            using (Mat _bg = new Mat())
            {
                Core.split(bg, channels);
                Core.multiply(_alpha, channels[0], channels[0], 1.0 / 255);
                Core.multiply(_alpha, channels[1], channels[1], 1.0 / 255);
                Core.multiply(_alpha, channels[2], channels[2], 1.0 / 255);
                Core.merge(channels, _bg);

                using (Mat _fg = new Mat())
                {
                    Core.split(fg, channels);
                    Core.multiply(alpha, channels[0], channels[0], 1.0 / 255);
                    Core.multiply(alpha, channels[1], channels[1], 1.0 / 255);
                    Core.multiply(alpha, channels[2], channels[2], 1.0 / 255);
                    Core.merge(channels, _fg);

                    Core.add(_fg, _bg, dst);
                }
            }
        }


        Mat sourceMat_c3;
        Mat targetMat_c3;
        Mat sourceMatLab;
        Mat targetMatLab;
        // Super fast color transfer between images.
        private void TransferColor_Lab(Mat source, Mat target, Mat mask)
        {
            bool is4chanelColor = false;
            if (source.channels() == 4)
            {

                if (sourceMat_c3 == null)
                    sourceMat_c3 = new Mat();
                if (targetMat_c3 == null)
                    targetMat_c3 = new Mat();

                is4chanelColor = true;
                Imgproc.cvtColor(source, sourceMat_c3, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(target, targetMat_c3, Imgproc.COLOR_RGBA2RGB);
            }
            else
            {

                sourceMat_c3 = source;
                targetMat_c3 = target;
            }

            if (sourceMatLab == null)
                sourceMatLab = new Mat();
            if (targetMatLab == null)
                targetMatLab = new Mat();

            Imgproc.cvtColor(sourceMat_c3, sourceMatLab, Imgproc.COLOR_RGB2Lab);
            Imgproc.cvtColor(targetMat_c3, targetMatLab, Imgproc.COLOR_RGB2Lab);

            //            sourceMatLab.convertTo (sourceMatLab, CvType.CV_32FC3);
            //            targetMatLab.convertTo (targetMatLab, CvType.CV_32FC3);

            MatOfDouble labMeanSrc = new MatOfDouble();
            MatOfDouble labStdSrc = new MatOfDouble();
            Core.meanStdDev(sourceMatLab, labMeanSrc, labStdSrc, mask);

            MatOfDouble labMeanTar = new MatOfDouble();
            MatOfDouble labStdTar = new MatOfDouble();
            Core.meanStdDev(targetMatLab, labMeanTar, labStdTar, mask);

            targetMatLab.convertTo(targetMatLab, CvType.CV_32FC3);

            // subtract the means from the target image
            double[] labMeanTarArr = labMeanTar.toArray();
            Core.subtract(targetMatLab, new Scalar(labMeanTarArr[0], labMeanTarArr[1], labMeanTarArr[2]), targetMatLab);

            // scale by the standard deviations
            double[] labStdTarArr = labStdTar.toArray();
            double[] labStdSrcArr = labStdSrc.toArray();
            Scalar scalar = new Scalar(labStdTarArr[0] / labStdSrcArr[0], labStdTarArr[1] / labStdSrcArr[1], labStdTarArr[2] / labStdSrcArr[2]);
            Core.multiply(targetMatLab, scalar, targetMatLab);

            // add in the source mean
            double[] labMeanSrcArr = labMeanSrc.toArray();
            Core.add(targetMatLab, new Scalar(labMeanSrcArr[0], labMeanSrcArr[1], labMeanSrcArr[2]), targetMatLab);

            // clip the pixel intensities to [0, 255] if they fall outside this range.
            //Imgproc.threshold (targetMatLab, targetMatLab, 0, 0, Imgproc.THRESH_TOZERO);
            //Imgproc.threshold (targetMatLab, targetMatLab, 255, 255, Imgproc.THRESH_TRUNC);

            targetMatLab.convertTo(targetMatLab, CvType.CV_8UC3);
            Imgproc.cvtColor(targetMatLab, targetMat_c3, Imgproc.COLOR_Lab2RGB);

            if (is4chanelColor)
            {
                Imgproc.cvtColor(sourceMat_c3, source, Imgproc.COLOR_RGB2RGBA);
                Imgproc.cvtColor(targetMat_c3, target, Imgproc.COLOR_RGB2RGBA);
            }
        }

        Mat sourceMatYCrCb;
        Mat targetMatYCrCb;
        // Super fast color transfer between images. (Slightly faster than processing in Lab color format)
        private void TransferColor_YCrCb(Mat source, Mat target, Mat mask)
        {
            bool is4chanelColor = false;
            if (source.channels() == 4)
            {

                if (sourceMat_c3 == null)
                    sourceMat_c3 = new Mat();
                if (targetMat_c3 == null)
                    targetMat_c3 = new Mat();

                is4chanelColor = true;
                Imgproc.cvtColor(source, sourceMat_c3, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(target, targetMat_c3, Imgproc.COLOR_RGBA2RGB);
            }
            else
            {

                sourceMat_c3 = source;
                targetMat_c3 = target;
            }

            if (sourceMatYCrCb == null)
                sourceMatYCrCb = new Mat();
            if (targetMatYCrCb == null)
                targetMatYCrCb = new Mat();

            Imgproc.cvtColor(sourceMat_c3, sourceMatYCrCb, Imgproc.COLOR_RGB2YCrCb);
            Imgproc.cvtColor(targetMat_c3, targetMatYCrCb, Imgproc.COLOR_RGB2YCrCb);

            MatOfDouble labMeanSrc = new MatOfDouble();
            MatOfDouble labStdSrc = new MatOfDouble();
            Core.meanStdDev(sourceMatYCrCb, labMeanSrc, labStdSrc, mask);

            MatOfDouble labMeanTar = new MatOfDouble();
            MatOfDouble labStdTar = new MatOfDouble();
            Core.meanStdDev(targetMatYCrCb, labMeanTar, labStdTar, mask);

            targetMatYCrCb.convertTo(targetMatYCrCb, CvType.CV_32FC3);

            // subtract the means from the target image
            double[] labMeanTarArr = labMeanTar.toArray();
            Core.subtract(targetMatYCrCb, new Scalar(labMeanTarArr[0], labMeanTarArr[1], labMeanTarArr[2]), targetMatYCrCb);

            // scale by the standard deviations
            double[] labStdTarArr = labStdTar.toArray();
            double[] labStdSrcArr = labStdSrc.toArray();
            Scalar scalar = new Scalar(labStdTarArr[0] / labStdSrcArr[0], labStdTarArr[1] / labStdSrcArr[1], labStdTarArr[2] / labStdSrcArr[2]);
            Core.multiply(targetMatYCrCb, scalar, targetMatYCrCb);

            // add in the source mean
            double[] labMeanSrcArr = labMeanSrc.toArray();
            Core.add(targetMatYCrCb, new Scalar(labMeanSrcArr[0], labMeanSrcArr[1], labMeanSrcArr[2]), targetMatYCrCb);

            // clip the pixel intensities to [0, 255] if they fall outside this range.
            //Imgproc.threshold (targetMatYCrCb, targetMatYCrCb, 0, 0, Imgproc.THRESH_TOZERO);
            //Imgproc.threshold (targetMatYCrCb, targetMatYCrCb, 255, 255, Imgproc.THRESH_TRUNC);

            targetMatYCrCb.convertTo(targetMatYCrCb, CvType.CV_8UC3);
            Imgproc.cvtColor(targetMatYCrCb, targetMat_c3, Imgproc.COLOR_YCrCb2RGB);

            if (is4chanelColor)
            {
                Imgproc.cvtColor(targetMat_c3, target, Imgproc.COLOR_RGB2RGBA);
            }
        }


        // Calculates source image histogram and changes target_image to match source hist.
        private void specifyHistogram(Mat source_image, Mat target_image, Mat mask)
        {
            byte[][] LUT = new byte[3][];
            for (int i = 0; i < LUT.Length; i++)
            {
                LUT[i] = new byte[256];
            }
            double[][] source_hist = new double[3][];
            for (int i = 0; i < source_hist.Length; i++)
            {
                source_hist[i] = new double[256];
            }
            double[][] target_hist = new double[3][];
            for (int i = 0; i < target_hist.Length; i++)
            {
                target_hist[i] = new double[256];
            }
            double[][] source_cdf = new double[3][];
            for (int i = 0; i < source_cdf.Length; i++)
            {
                source_cdf[i] = new double[256];
            }
            double[][] target_cdf = new double[3][];
            for (int i = 0; i < target_cdf.Length; i++)
            {
                target_cdf[i] = new double[256];
            }

            double[] source_histMax = new double[3];
            double[] target_histMax = new double[3];

            byte[] mask_byte = new byte[mask.total() * mask.channels()];
            MatUtils.copyFromMat<byte>(mask, mask_byte);
            byte[] source_image_byte = new byte[source_image.total() * source_image.channels()];
            MatUtils.copyFromMat<byte>(source_image, source_image_byte);
            byte[] target_image_byte = new byte[target_image.total() * target_image.channels()];
            MatUtils.copyFromMat<byte>(target_image, target_image_byte);

            int pixel_i = 0;
            int channels = (int)source_image.channels();
            int total = (int)source_image.total();
            for (int i = 0; i < total; i++)
            {
                if (mask_byte[i] != 0)
                {
                    byte c = source_image_byte[pixel_i];
                    source_hist[0][c]++;
                    if (source_hist[0][c] > source_histMax[0])
                        source_histMax[0] = source_hist[0][c];

                    c = source_image_byte[pixel_i + 1];
                    source_hist[1][c]++;
                    if (source_hist[1][c] > source_histMax[1])
                        source_histMax[1] = source_hist[1][c];

                    c = source_image_byte[pixel_i + 2];
                    source_hist[2][c]++;
                    if (source_hist[2][c] > source_histMax[2])
                        source_histMax[2] = source_hist[2][c];

                    c = target_image_byte[pixel_i];
                    target_hist[0][c]++;
                    if (target_hist[0][c] > target_histMax[0])
                        target_histMax[0] = target_hist[0][c];

                    c = target_image_byte[pixel_i + 1];
                    target_hist[1][c]++;
                    if (target_hist[1][c] > target_histMax[1])
                        target_histMax[1] = target_hist[1][c];

                    c = target_image_byte[pixel_i + 2];
                    target_hist[2][c]++;
                    if (target_hist[2][c] > target_histMax[2])
                        target_histMax[2] = target_hist[2][c];
                }
                // Advance to next pixel
                pixel_i += channels;
            }

            // Normalize hist
            for (int i = 0; i < 256; i++)
            {
                source_hist[0][i] /= source_histMax[0];
                source_hist[1][i] /= source_histMax[1];
                source_hist[2][i] /= source_histMax[2];

                target_hist[0][i] /= target_histMax[0];
                target_hist[1][i] /= target_histMax[1];
                target_hist[2][i] /= target_histMax[2];
            }

            // Calc cumulative distribution function (CDF) 
            source_cdf[0][0] = source_hist[0][0];
            source_cdf[1][0] = source_hist[1][0];
            source_cdf[2][0] = source_hist[2][0];
            target_cdf[0][0] = target_hist[0][0];
            target_cdf[1][0] = target_hist[1][0];
            target_cdf[2][0] = target_hist[2][0];
            for (int i = 1; i < 256; i++)
            {
                source_cdf[0][i] = source_cdf[0][i - 1] + source_hist[0][i];
                source_cdf[1][i] = source_cdf[1][i - 1] + source_hist[1][i];
                source_cdf[2][i] = source_cdf[2][i - 1] + source_hist[2][i];

                target_cdf[0][i] = target_cdf[0][i - 1] + target_hist[0][i];
                target_cdf[1][i] = target_cdf[1][i - 1] + target_hist[1][i];
                target_cdf[2][i] = target_cdf[2][i - 1] + target_hist[2][i];
            }

            // Normalize CDF
            for (int i = 0; i < 256; i++)
            {
                source_cdf[0][i] /= source_cdf[0][255];
                source_cdf[1][i] /= source_cdf[1][255];
                source_cdf[2][i] /= source_cdf[2][255];

                target_cdf[0][i] /= target_cdf[0][255];
                target_cdf[1][i] /= target_cdf[1][255];
                target_cdf[2][i] /= target_cdf[2][255];
            }

            // Create lookup table (LUT)
            const double HISTMATCH_EPSILON = 0.000001f;
            for (int i = 0; i < 3; i++)
            {
                int last = 0;
                for (int j = 0; j < 256; j++)
                {
                    double F1j = target_cdf[i][j];

                    for (int k = last; k < 256; k++)
                    {
                        double F2k = source_cdf[i][k];
                        if (Math.Abs(F2k - F1j) < HISTMATCH_EPSILON || F2k > F1j)
                        {
                            LUT[i][j] = (byte)k;
                            last = k;
                            break;
                        }
                    }
                }
            }

            // Repaint pixels
            pixel_i = 0;
            for (int i = 0; i < total; i++)
            {
                if (mask_byte[i] != 0)
                {
                    target_image_byte[pixel_i] = LUT[0][target_image_byte[pixel_i]];
                    target_image_byte[pixel_i + 1] = LUT[1][target_image_byte[pixel_i + 1]];
                    target_image_byte[pixel_i + 2] = LUT[2][target_image_byte[pixel_i + 2]];
                }
                // Advance to next pixel
                pixel_i += channels;
            }

            MatUtils.copyToMat(target_image_byte, target_image);
        }


        private void drawDebugFacePoints()
        {
            int index = faceChangeData.Count;
            foreach (FaceChangeData data in faceChangeData)
            {
                getFacePoints(data.target_landmark_points, target_points, target_affine_transform_keypoints);
                for (int i = 0; i < target_points.Length; i++)
                {
                    Imgproc.circle(target_frame_full, target_points[i], 1, new Scalar(255, 0, 0, 255), 2, Imgproc.LINE_AA, 0);
                }
                for (int i = 0; i < target_affine_transform_keypoints.Length; i++)
                {
                    Imgproc.circle(target_frame_full, target_affine_transform_keypoints[i], 1, new Scalar(0, 255, 0, 255), 2, Imgproc.LINE_AA, 0);
                }

                getFacePoints(data.source_landmark_points, source_points, source_affine_transform_keypoints);
                for (int i = 0; i < source_points.Length; i++)
                {
                    Imgproc.circle(data.source_frame, source_points[i], 1, new Scalar(255, 0, 0, 255), 2, Imgproc.LINE_AA, 0);
                }
                for (int i = 0; i < source_affine_transform_keypoints.Length; i++)
                {
                    Imgproc.circle(data.source_frame, source_affine_transform_keypoints[i], 1, new Scalar(0, 255, 0, 255), 2, Imgproc.LINE_AA, 0);
                }

                //
                target_rect = Imgproc.boundingRect(new MatOfPoint(target_points));
                source_rect = Imgproc.boundingRect(new MatOfPoint(source_points));
                Scalar color = new Scalar(127, 127, (int)(255 / faceChangeData.Count) * index, 255);
                Imgproc.rectangle(target_frame_full, this.target_rect.tl(), this.target_rect.br(), color, 1, Imgproc.LINE_8, 0);
                Imgproc.rectangle(data.source_frame, this.source_rect.tl(), this.source_rect.br(), color, 1, Imgproc.LINE_8, 0);
                //

                index--;
            }
        }


        public void Dispose()
        {
            if (target_frame != null)
            {
                target_frame.Dispose();
                target_frame = null;
            }
            disposeMatROI();
            disposeMat();

            foreach (var c in channels)
            {
                c.Dispose();
            }
            channels.Clear();
            if (sourceMat_c3 != null)
            {
                sourceMat_c3.Dispose();
                sourceMat_c3 = null;
            }
            if (targetMat_c3 != null)
            {
                targetMat_c3.Dispose();
                targetMat_c3 = null;
            }
            if (sourceMatLab != null)
            {
                sourceMatLab.Dispose();
                sourceMatLab = null;
            }
            if (targetMatLab != null)
            {
                targetMatLab.Dispose();
                targetMatLab = null;
            }
            if (sourceMatYCrCb != null)
            {
                sourceMatYCrCb.Dispose();
                sourceMatYCrCb = null;
            }
            if (targetMatYCrCb != null)
            {
                targetMatYCrCb.Dispose();
                targetMatYCrCb = null;
            }
        }

        public class FaceChangeData
        {
            public Mat source_frame;
            public Point[] source_landmark_points;
            public Point[] target_landmark_points;

            public float alpha
            {
                set
                { this._alpha = Mathf.Clamp01(value); }
                get { return this._alpha; }
            }

            private float _alpha = 1.0f;

            public FaceChangeData(Mat source_frame, Point[] source_landmark_points, Point[] target_landmark_points, float alpha)
            {
                this.source_frame = source_frame;
                this.source_landmark_points = source_landmark_points;
                this.target_landmark_points = target_landmark_points;
                this.alpha = alpha;
            }
        }
    }
}
