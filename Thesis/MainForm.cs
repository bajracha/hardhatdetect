using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//Library Files for EmguCV for image processing.
//It is an open source library developed by OpenCV community for C#
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
//EmguCV library definition ends

//Library for threads
using System.Threading;

//Creater: Dinesh Bajracharya
//Created On: Feb 9, 2013
//This form consists of the image processing techniques for the hard hat detection.
//

//namespace Thesis starts
namespace Thesis
{
    //form MainForm Starts
    public partial class MainForm : Form
    {
        private Image<Bgr, Byte> myImage; //Object to store image to be processed. Will store the colored image
        private Image<Gray, Byte> grayScale_Image; //Object to store the grayscale image converted from main image.
        private String myImageFileName; //String to store the name of file opened.

        private Capture camera; //Object to store the webcam object
        Thread webcam; //Thread to start capturing image from webcam
        Boolean camcapture = false; //Flag to get image from camera or not

        //Constructor
        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
             
        }
        //Constructor Ends

        //Function to open the image with an openFileDialog
        //The result is stored into the myImage object for further processing
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog Openfile = new OpenFileDialog();
            //Open Dialog to select the processing image
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                //Load the Image 
                myImageFileName = Openfile.FileName;
                myImage = new Image<Bgr, Byte>(Openfile.FileName);
                //Convert to bitmap and show in pictureBox
                showImage(myImage.ToBitmap());
        
            }
        }
        //openToolStripMenuItem_Click ends

        //Close and Exit the application
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopCapture();
            Application.Exit();
            this.Close();
            
        }
        //exitToolStripMenuItem_Click ends

        //Menu to convert image to grayScale
        private void grayScaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImage != null){
                grayScale_Image = convertToGrayScale(myImage);
                showImage(grayScale_Image.ToBitmap());
            }
            else {
                MessageBox.Show(MainForm.ActiveForm,"Image Not Loaded. Please Open an image");
            }
        }
        //grayScaleToolStripMenuItem_Click ends

        //Menu to convert image to find canny edges
        private void cannyEdgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImage != null)
            {
                grayScale_Image = cannyEdgeDetect(myImage,125, 100);
                showImage(grayScale_Image.ToBitmap());
            }
            else
            {
                MessageBox.Show(MainForm.ActiveForm, "Image Not Loaded. Please Open an image");
            }
        }
        //cannyEdgeToolStripMenuItem_Click ends

        //Menu to detect face in the image
        private void faceDetectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImage != null)
            {
                try
                {
                    MCvAvgComp[] faces = faceDetection(myImage);
                    Image<Bgr, Byte> image = myImage.Copy();
                    foreach (MCvAvgComp face in faces)
                    {
                        //Draw rectangle in each face detected
                        image.Draw(face.rect, new Bgr(0, 255, 0), 2);
                    }
                    showImage(image.ToBitmap());
                }
                catch (Exception ex) {
                    Console.Out.WriteLine("Error in face detection: "+ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MainForm.ActiveForm, "Image Not Loaded. Please Open an image");
            }
        }
        //faceDetectionToolStripMenuItem_Click ends

        //Menu to detect the hardHat in image
        private void hardHatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImage != null)
            {
                try
                {
                    Image<Bgr, Byte> image = detectHardHat(myImage);
                    showImage(image.ToBitmap());
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("Error in hard hat detection: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MainForm.ActiveForm, "Image Not Loaded. Please Open an image");
            }
        }
        //hardHatToolStripMenuItem_Click ends

        //To start capturing from webcam
        private void startBtn_Click(object sender, EventArgs e)
        {
            Console.Out.WriteLine("Start Webcam...");
            //Initialize camera to the webcam capture

            try
            {
                camera = new Capture(0);
                //camera.FlipHorizontal = true;
               
                //Start the webcam
                camera.Start();
                camcapture = true;
                //Start capturing
                //Create new thread for capturing images
                webcam = new Thread(captureImageFromWebcam);
                webcam.IsBackground = true;
                webcam.Start();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error in camera initialization: " + ex.Message);
            }
            
        }
        //startBtn_Click ends

        //To stop webcam
        private void stopBtn_Click(object sender, EventArgs e)
        {
            Console.Out.WriteLine("Stop Webcam...");
            stopCapture();
        }
        //stopBtn_Click
        
        
        //Show the image in the imageBox pictureBox in the form
        private void showImage(Bitmap image)
        {
            Console.Out.WriteLine("Displaying Image in the imageBox...");
            imageBox.Image = image;
        }
        //showImage ends

        //Converts the image to grayScale
        private Image<Gray, Byte> convertToGrayScale(Image<Bgr,Byte> image)
        {
            Console.Out.WriteLine("Converting Image to GrayScale...");
            return image.Convert<Gray, Byte>();
        }
        //convertToGrayScale ends

        //Canny Edge Detection
        private Image<Gray, Byte> cannyEdgeDetect(Image<Bgr,Byte> image,double threshold, double thresholdLinking) {
            Console.Out.WriteLine("Finding Edges using Canny...");
            grayScale_Image = convertToGrayScale(image);
            return grayScale_Image.Canny(new Gray(threshold), new Gray(thresholdLinking));
        }
        //cannyEdgeDetect ends

        //Face Detection in image
        private MCvAvgComp[] faceDetection(Image<Bgr, Byte> image)
        {
            Console.Out.WriteLine("Finding faces in the images...");
            grayScale_Image = convertToGrayScale(image);
            //HaarCascade for face detection. 
            HaarCascade facehaar; 
            try
            {
                //Face is detected by using haar-like structure training. The trained data set is already defined in the XML file
                facehaar = new HaarCascade(@"haarcascade_frontalface_default.xml");
                //Use Canny edge to filter the images.
                return facehaar.Detect(grayScale_Image, 1.1, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(grayScale_Image.Width / 12, grayScale_Image.Height / 12), new Size(grayScale_Image.Width / 2, grayScale_Image.Height / 2));
            }
            catch (Exception ex) {
                Console.Out.WriteLine("File Not Found for FaceHaarCascade: "+ex.Message);
            }
            
            return null;
        }
        //faceDetection ends

        //Detect Hard Hat in the image
        private Image<Bgr, Byte> detectHardHat(Image<Bgr,Byte> image) {
            Console.Out.WriteLine("Detecting Hard Hat...");
            try
            {
                MCvAvgComp[] faces = faceDetection(image);
                Image<Bgr, Byte> newImage = image.CopyBlank();
                foreach (MCvAvgComp face in faces)
                {
                    //Counter for the cirles in the processed image
                    int counter = 0;

                    //Object to store the region of the head above face for analysis
                    var headRegion = face;
                    headRegion.rect.Height = (face.rect.Height) ;
                    headRegion.rect.Y = face.rect.Y - (face.rect.Height * 8) / 10;
                    headRegion.rect.Width = face.rect.Width + face.rect.Width;
                    headRegion.rect.X = face.rect.X - (face.rect.Width * 2) / 4;

                    //Set the region of the image to headRegion for analysis
                    image.ROI = headRegion.rect;

                    //Object to store the region of the hard hat for display
                    var hardHat = face;
                    hardHat.rect.Height = face.rect.Height;
                    hardHat.rect.Y = face.rect.Y - (face.rect.Height * 8) / 10;
                    hardHat.rect.Width = face.rect.Width;
                    hardHat.rect.X = face.rect.X;

                    //Convert Image to HSV format for filtering
                    Image<Hsv, Byte> hsv = image.Convert<Hsv, Byte>();
                    //Apply the red color filter in the HSV model
                    grayScale_Image = hsv.InRange(new Hsv(0, 80, 80), new Hsv(20, 200, 200));

                    //List to store the contour region
                    List<Contour<Point>> list = new List<Contour<Point>>();

                    //Find the canny of the filtered image
                    grayScale_Image = cannyEdgeDetect(grayScale_Image.Convert<Bgr, Byte>(), 100, 50);

                    Console.Out.WriteLine("Head>" + headRegion.rect.Height + ":::" + headRegion.rect.Width);
                    //return grayScale_Image.Convert<Bgr, Byte>();
                    //Find the contours
                    Contour<Point> ctrs = grayScale_Image.FindContours();

                    while (ctrs != null)
                    {
                        Contour<Point> ctr = ctrs.ApproxPoly(2);
                        if (ctr.Area > 1) { list.Add(ctr); }
                        ctrs = ctrs.HNext;
                    }
                    foreach (Contour<Point> lis in list)
                    {
                        newImage.Draw(lis, new Bgr(Color.White), 1);
                    }
                    //newImage.ROI = headRegion.rect;

                    CircleF[] circles = grayScale_Image.HoughCircles(new Gray(220), new Gray(160), 5, 40, (headRegion.rect.Height *7)/20, headRegion.rect.Height/2)[0];
                    foreach (CircleF circle in circles)
                    {
                        counter++;
                        //image.Draw(circle, new Bgr(0, 255, 0), 2);
                    }
                    //return newImage.Convert<Bgr, Byte>();

                    //Reset the region of image back to whole image
                    image.ROI = Rectangle.Empty;

                    //Draw rectangle for each face detected
                    image.Draw(face.rect, new Bgr(0, 0, 0), 1);

                    if (counter > 0)
                    {
                        image.Draw(hardHat.rect, new Bgr(255, 0, 0), 2);
                    }
                }
                return image;
            }
            catch(Exception ex){
                Console.Out.WriteLine("Error in detectHardHar(): " + ex.Message);
            }
            return myImage;
        }
        //detectHardHat ends

        private void captureImageFromWebcam() 
        {
            Console.Out.WriteLine("Start Capturing from Webcam...");
            try
            {
                while (camcapture)
                {
                    Image<Bgr, Byte> currentFrame = camera.QueryFrame();
                    if (currentFrame != null)
                    {
                        myImage = currentFrame;
                        Image<Bgr, Byte> image = detectHardHat(myImage);
                        showImage(image.ToBitmap());
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error in capturing image from Webcam: "+ ex.Message);
                webcam.Abort();
            }
            finally
            {
                webcam.Abort();
            }
        }
        //captureImageFromWebcam ends


        public void MainForm_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            stopCapture();
            Application.Exit();
        }

        public void stopCapture()
        {
            if (camcapture)
            {
                camcapture = false;
                Thread.Sleep(1000);
                webcam.Join();
                camera.Stop();
                Console.Out.WriteLine("State->" + webcam.ThreadState);
                
            }
        }
        
    }
    //Form MainForm Ends
}
//namespace Thesis ends