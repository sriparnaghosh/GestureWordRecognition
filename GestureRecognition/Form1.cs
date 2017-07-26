using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using AForge.Imaging.Filters;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using SVM;

namespace GestureRecognition
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        String[] filenames,dirnames;
        //selecting an entire folder containing folders of raw images alphabet-wise
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    dirnames = Directory.GetDirectories(fbd.SelectedPath);
                    System.Windows.Forms.MessageBox.Show("Files found: " + dirnames.Length.ToString(), "Message");
                }
            }
        }
        
        Bitmap bmp, skinBmp;
        //preparing training data from images
        private void skinDetectToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //write features to this file
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\TrainSegmented\traindata");
            int index = 0;
            //for each directory in dirnames
            foreach (string dir in dirnames)
            {
                List<List<double>> Listofvectors = new List<List<double>>(); //to store features of all images in a directory

                //for each folder select all filenames
                filenames = Directory.GetFiles(dir);
                List<double> featureVector = new List<double>(); //to store feature of each image
                foreach (string filename in filenames)
                {
                    featureVector = processImage(filename);
                    Listofvectors.Add(featureVector);
                }
                //writing features to the file
                foreach (var vector in Listofvectors)
                {
                    String line = (index+1).ToString() + " ";
                    int featureindex = 1;
                   
                    foreach (var obj in vector)
                    {
                        line = line + featureindex.ToString() + ":" + obj.ToString() + " ";
                        featureindex++;
                       
                    }
                    file.WriteLine(line);
                }
                index++; //for each alphabet (directory)
            }//end of foreach index
            file.Close();
            MessageBox.Show("Training Data Ready! Select a new folder for testing !", "Message");
        }//end of skindetect tool strip  

        Model model;
        Problem test,train;
        private void button1_Click_1(object sender, EventArgs e)
        {
            train = Problem.Read(@"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\TrainSegmented\traindata");
            Parameter parameters = new Parameter();

            parameters.C = 32;
            parameters.Gamma = 8;
            model = Training.Train(train, parameters);
            MessageBox.Show("Training the model, done!");
        }

        //testing the model
        private void button2_Click_1(object sender, EventArgs e)
        {
                
                System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\TestSegmented\testdata");
                foreach (string dir in dirnames)
                {
                    //for each folder select all filenames
                    List<List<double>> Listofvectors = new List<List<double>>();
                    filenames = Directory.GetFiles(dir);
                    
                    List<double> featureVector = new List<double>();

                    foreach (string filename in filenames)
                    {
                        featureVector = processImage(filename);
                        Bitmap testimage = new Bitmap(filename);
                        pictureBox1.Image = testimage;
                        Listofvectors.Add(featureVector);
                    }

                    foreach (var vector in Listofvectors)
                    {
                        String line = (1).ToString() + " ";
                        int featureindex = 1;

                        foreach (var obj in vector)
                        {
                            line = line + featureindex.ToString() + ":" + obj.ToString() + " ";
                            featureindex++;

                        }
                        file.WriteLine(line);

                    }
                }
                file.Close();
                test = Problem.Read(@"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\TestSegmented\testdata");
                Prediction.Predict(test, @"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\result", model, false);
                // Read the file as one string.
                string text = System.IO.File.ReadAllText(@"C:\Users\gsrip\Documents\MyDocuments\Saarthi AI and IP\result");
                textBox1.Text = text;
          
            }

        List<double> processImage(string file)
        {
            List<double> featureVectorNorm = new List<double>();
                //load an image in a bitmap

                Bitmap bmplocal = new Bitmap(file);
                int height = 300, width = 300;
                bmp = new Bitmap(bmplocal, width, height);
                pictureBox1.Image = new Bitmap(bmp);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                using (bmp)
                using (skinBmp = new Bitmap(bmp.Width, bmp.Height))
                {
                    //skin detection
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            Color pixel = bmp.GetPixel(x, y);

                            int red = pixel.R;
                            int blue = pixel.B;
                            int green = pixel.G;
                            int max = Math.Max(red, Math.Max(green, blue));
                            int min = Math.Min(red, Math.Min(green, blue));
                            int rgdif = red - green;
                            int abs = Math.Abs(rgdif);
                            if (red > 95 && green > 40 && blue > 20 && max - min > 15 && abs > 15 && red > green && red > blue)
                                skinBmp.SetPixel(x, y, pixel);

                        }
                    }
                    //filter image
                    //grayscale filter (BT709)
                    Grayscale filter1 = new Grayscale(0.2125, 0.7154, 0.0721);
                    Bitmap newImage = new Bitmap(bmp);
                    Bitmap grayImage = filter1.Apply(newImage);

                    Threshold filter2 = new Threshold(100);
                    Bitmap bwImage = filter2.Apply(grayImage);

                    Closing filter5 = new Closing();
                    filter5.ApplyInPlace(bwImage);

                    Opening filter3 = new Opening();
                    filter3.ApplyInPlace(bwImage);

                    ExtractBiggestBlob filter4 = new ExtractBiggestBlob();
                    Bitmap biggestBlobsImage = filter4.Apply(bwImage);

                    ExtractBiggestBlob filter6 = new ExtractBiggestBlob();
                    Bitmap biggestBlobsImage1 = filter6.Apply((Bitmap)bmp);

                    Bitmap orgimage = new Bitmap(biggestBlobsImage1, 300, 300);
                    Bitmap blobimage = new Bitmap(biggestBlobsImage, 300, 300);

                    Bitmap newimage = new Bitmap(300, 300);

                    //anding the two images
                    for (int x = 0; x < 300; x++)
                    {
                        for (int y = 0; y < 300; y++)
                        {
                            Color pixel1 = orgimage.GetPixel(x, y);
                            Color pixel2 = blobimage.GetPixel(x, y);
                            int red1 = pixel1.R, red2 = pixel2.R;
                            int blue1 = pixel1.B, blue2 = pixel2.B;
                            int green1 = pixel1.G, green2 = pixel2.G;
                            int newred, newblue, newgreen;
                            newred = red1 & red2;
                            newblue = blue1 & blue2;
                            newgreen = green1 & green2;
                            Color newpixel = Color.FromArgb(newred, newgreen, newblue);

                            newimage.SetPixel(x, y, newpixel);

                        }
                    }
                    //edge detection
                    CannyEdgeDetector filter7 = new CannyEdgeDetector();
                    Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                    Bitmap edges = filter.Apply(newimage);
                    filter7.ApplyInPlace(edges);

                    //save segmented image
                    /*
                    String folderName = "C:\\Users\\gsrip\\Documents\\MyDocuments\\Saarthi AI and IP\\TestSegmented\\";
                    String pathString = System.IO.Path.Combine(folderName, alphabets[index].ToString());
                    System.IO.Directory.CreateDirectory(pathString);
                    String location = pathString + "\\image" + (n++).ToString() + ".jpg";
                    newimage.Save(@location);
                        * */

                    //feature extraction
                    List<int> featureVector = new List<int>();
                    for (int i = 0; i < 6; i++)
                        for (int j = 0; j < 6; j++)
                        {
                            int count = 0;
                            for (int x = i * 50; x < (i * 50) + 50; x++)
                                for (int y = j * 50; y < (j * 50) + 50; y++)
                                {
                                    Color pixel = edges.GetPixel(x, y);
                                    if (pixel.R != 0 && pixel.G != 0 && pixel.B != 0)
                                        count++;
                                }
                            featureVector.Add(count);

                        }
                    double sumofvector = featureVector.Sum();
                    foreach (var d in featureVector)
                     {
                         featureVectorNorm.Add((double)d / sumofvector);

                     }
                    
                } //end of using

        return featureVectorNorm;

        }//end of processimage()
    } //end of class
}// end of namespace
