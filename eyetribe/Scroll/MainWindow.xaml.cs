/*
 * Copyright (c) 2013-present, The Eye Tribe. 
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree. 
 *
 * 
 * Original scroll sample has been modified for research purposes 
 * by Prairie Rose Goodwin at North Carolina State University.  July 2015
 * 
 */
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows7.Multitouch;
using Windows7.Multitouch.WPF;
using TETCSharpClient;
using TETCSharpClient.Data;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Scroll
{
    public partial class MainWindow : IGazeListener
    {
        #region Variables
        List<List<UIElement>> elements;
        int set = 0;
        String fileName;
        Random rnd;
        List<Brush> colors = new List<Brush>();
        private const float DPI_DEFAULT = 96f; // default system DIP setting
        private const double SPEED_BOOST = 20.0;
        private const double ACTIVE_SCROLL_AREA = 0.25; // 25% top and bottom
        private const int MAX_IMAGE_WIDTH = 1600;
        private readonly double dpiScale;
        private Matrix transfrm;
        enum Direction { Up = -1, Down = 1 }
        enum Input { Touch = 0, Mouse = 1 }
        public enum Target { Circle = 0, Crosshair = 1 };
       // Input input = Input.Touch;
        Input input = Input.Mouse;
       Boolean VISUALIZE = false;
        //Boolean VISUALIZE = true;
        
        #endregion

        #region Get/Set

        private bool IsTouchEnabled { get; set; }

        #endregion

        #region Enums

        public enum DeviceCap
        {
            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90
        }

        #endregion

        #region Methods I'm writing

        public MainWindow()
        {




            #region eyeTribe
            var connectedOk = true;
            GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);

            if (!GazeManager.Instance.IsActivated)
            {
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("EyeTribe Server has not been started")));
                connectedOk = false;
            }
            if (!connectedOk)
            {
                Close();
                return;
            }

            InitializeComponent();

            // Check if multi-touch capability is available
            IsTouchEnabled = TouchHandler.DigitizerCapabilities.IsMultiTouchReady;

            // Get the current DIP scale
            dpiScale = CalcDpiScale();





            Loaded += (sender, args) =>
                {
                    // if (Screen.PrimaryScreen.Bounds.Width > MAX_IMAGE_WIDTH)
                    // WebImage.Width = MAX_IMAGE_WIDTH * dpiScale;
                    // else
                    //WebImage.Width = Screen.PrimaryScreen.Bounds.Width * dpiScale;

                    // Transformation matrix that accomodate for the DPI settings
                    var presentationSource = PresentationSource.FromVisual(this);
                    transfrm = presentationSource.CompositionTarget.TransformFromDevice;

                    // enable stylus (touch) events
                    if (IsTouchEnabled)
                        Factory.EnableStylusEvents(this);


                };
            #endregion


            fileName = DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + "-" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_Trial.txt";
            rnd = new Random();

            if (VISUALIZE)
            {
                showData();
            }
            else
            {
                elements = getUIElements(set);
                Console.WriteLine("elements: " + elements.Count);
                Console.WriteLine("First element: " + elements[0].Count);
                foreach (UIElement e in elements[0])
                {
                    Ovals.Children.Add(e);
                }




                Target t;

                //type of target
                if (set % 2 == 0)
                {
                    t = Target.Circle;
                }
                else
                {
                    t = Target.Crosshair;
                }

                //size and location of target

                Shape cur = (Shape)elements[0].Last();
                double curX = Canvas.GetLeft(cur) + .5 * cur.Width;
                double curY = Canvas.GetTop(cur) + .5 * cur.Height;
                double curRadius = Math.Max(cur.Width, cur.Height) / 2;
                Log("APPEARED", t, curRadius, curX, curY);


                //So that the cursor does not appear for the touch portion
                this.Cursor = System.Windows.Input.Cursors.None;
                if (input == Input.Touch)
                {
                    setTouchInput();
                }
                else {
                    setMouseInput();
                }
            }
        }

        void showData()
        {

            try
            {
                int numLines = 0;
                string[] fileEntries = Directory.GetFiles("../../../../Trials/");

                using (StreamWriter sw = new StreamWriter("../../../../Trials/statistics.xls", true))
                {
                    sw.WriteLine("sourceFile" + "\t" + "target" + "\t" + "inputType" + "\t" + "radius" + "\t" + "target position x" + "\t" + "target position y" + "\t" + "inputX" + "\t" + "inputY" + "\t" + "gazeX" + "\t" + "gazeY" + "\t" + "targetInputOffset" + "\t" + "targetGazeOffset" + "\t" + "inputGazeOffset\tangle");
                }

                foreach (string sourceFile in fileEntries)
                {
                    string[] readText = File.ReadAllLines(sourceFile);
                    foreach (string line in readText)
                    {
                        string[] tokens = line.Split(':');
                        //                    Console.WriteLine(line);
                      
                        if (tokens.Length >= 7 && tokens[6].Equals("ACTIVATED"))
                        {
                            try
                            {
                                double r = Double.Parse(tokens[8]);
                                double x = Double.Parse(tokens[9]);
                                double y = Double.Parse(tokens[10]);
                                String inputType = tokens[11];
                                double touchX = Double.Parse(tokens[12]);
                                double touchY = Double.Parse(tokens[13]);
                                double gazeX = Double.Parse(tokens[14]);
                                double gazeY = Double.Parse(tokens[15]);
                                double targetInputOffset = Math.Sqrt((x - touchX) * (x - touchX) + (y - touchY) * (y - touchY));
                                double targetGazeOffset = Math.Sqrt((x - gazeX) * (x - gazeX) + (y - gazeY) * (y - gazeY));
                                double inputGazeOffset = Math.Sqrt((touchX - gazeX) * (touchX - gazeX) + (touchY - gazeY) * (touchY - gazeY));

                                using (StreamWriter sw = new StreamWriter("../../../../Trials/statistics.xls", true))
                                {
                                    sw.WriteLine(sourceFile.Substring(sourceFile.LastIndexOf("/") + 1) + "\t" + tokens[7] + "\t" + inputType + "\t" + r + "\t" + x + "\t" + y + "\t" + touchX + "\t" + touchY + "\t" + gazeX + "\t" + gazeY + "\t" + targetInputOffset + "\t" + targetGazeOffset + "\t" + inputGazeOffset + "\t" + Math.Atan((gazeY-y)/(gazeX-x))*180/Math.PI);
                                    numLines++;
                                }
                                if (tokens[7].Equals("Circle"))
                                {
                                    Brush color = PickBrush();
                                    Ellipse target = new Ellipse() { Width = r * 2, Height = r * 2, Fill = color };
                                    Canvas.SetLeft(target, x);
                                    Canvas.SetTop(target, y);
                                    Ovals.Children.Add(target);

                                    Ellipse gaze = new Ellipse() { Width = 10, Height = 10, Fill = color };
                                    Canvas.SetLeft(gaze, gazeX);
                                    Canvas.SetTop(gaze, gazeY);
                                    Ovals.Children.Add(gaze);

                                    Ellipse touch = new Ellipse() { Width = 10, Height = 10, Fill = color };
                                    Canvas.SetLeft(touch, touchX);
                                    Canvas.SetTop(touch, touchY);
                                    Ovals.Children.Add(touch);

                                }
                                if (tokens[7].Equals("Crosshair"))
                                {

                                    Brush color = PickBrush();
                                    Ellipse target = new Ellipse() { Width = r * 2, Height = r * 2, Fill = color, Stroke = Brushes.Black };
                                    Canvas.SetLeft(target, x);
                                    Canvas.SetTop(target, y);
                                    Ovals.Children.Add(target);

                                    Ellipse gaze = new Ellipse() { Width = 10, Height = 10, Fill = color };
                                    Canvas.SetLeft(gaze, gazeX);
                                    Canvas.SetTop(gaze, gazeY);
                                    Ovals.Children.Add(gaze);

                                    Ellipse touch = new Ellipse() { Width = 10, Height = 10, Fill = color };
                                    Canvas.SetLeft(touch, touchX);
                                    Canvas.SetTop(touch, touchY);
                                    Ovals.Children.Add(touch);

                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                Console.WriteLine(line);
                                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!");
                            }
                        }
                    }
                }
                using (StreamWriter sw = new StreamWriter("../../../../Trials/statistics.xls", true))
                {
                    //	Min(L+numlines/2+1:L+3*numlines/4)	Min(L3*numlines/4+1:L+numLines)		


                    String allRange = "L2:L" + (numLines + 1);
                    String mouseRange = "L2:L" + (numLines / 2 + 1);
                    String touchRange = "L" + (numLines / 2 + 2) + ":L" + (numLines + 1);
                    String mouseCircleRange = "L2:L" + (numLines / 4 + 1);
                    String mouseCrosshairRange = "L" + (numLines / 4 + 2) + ":L" + (numLines / 2 + 1);
                    String touchCircleRange = "L" + (numLines / 2 + 2) + ":L" + (3 * numLines / 4 + 1);
                    String touchCrosshairRange = "L" + (3 * numLines / 4 + 2) + ":L" + (numLines + 1);

                    sw.WriteLine("\tAll\tMouse\tTouch\tMouse Circle\tmouse crosshair\ttouch circle\ttouch crosshair\t" + numLines);
                    sw.WriteLine("min\t=MIN(" + allRange + ")\t=MIN(" + mouseRange + ")\t=MIN(" + touchRange + ")\t=MIN(" + mouseCircleRange + ")\t=MIN(" + mouseCrosshairRange + ")\t=MIN(" + touchCircleRange + ")\t=MIN(" + touchCrosshairRange + ")\t");
                    sw.WriteLine("median\t=MEDIAN(" + allRange + ")\t=MEDIAN(" + mouseRange + ")\t=MEDIAN(" + touchRange + ")\t=MEDIAN(" + mouseCircleRange + ")\t=MEDIAN(" + mouseCrosshairRange + ")\t=MEDIAN(" + touchCircleRange + ")\t=MEDIAN(" + touchCrosshairRange + ")\t");
                    sw.WriteLine("average\t=AVERAGE(" + allRange + ")\t=AVERAGE(" + mouseRange + ")\t=AVERAGE(" + touchRange + ")\t=AVERAGE(" + mouseCircleRange + ")\t=AVERAGE(" + mouseCrosshairRange + ")\t=AVERAGE(" + touchCircleRange + ")\t=AVERAGE(" + touchCrosshairRange + ")\t");
                    sw.WriteLine("stddev\t=STDEV.P(" + allRange + ")\t=STDEV.P(" + mouseRange + ")\t=STDEV.P(" + touchRange + ")\t=STDEV.P(" + mouseCircleRange + ")\t=STDEV.P(" + mouseCrosshairRange + ")\t=STDEV.P(" + touchCircleRange + ")\t=STDEV.P(" + touchCrosshairRange + ")\t");
                    sw.WriteLine("stddev low range");
                    sw.WriteLine("stddev high range");
                    sw.WriteLine("2stddev\t=2*STDEV.P(" + allRange + ")\t=2*STDEV.P(" + mouseRange + ")\t=2*STDEV.P(" + touchRange + ")\t=2*STDEV.P(" + mouseCircleRange + ")\t=2*STDEV.P(" + mouseCrosshairRange + ")\t=2*STDEV.P(" + touchCircleRange + ")\t=2*STDEV.P(" + touchCrosshairRange + ")\t");
                    sw.WriteLine("2stddev low range");
                    sw.WriteLine("2stddev high range");
                    sw.WriteLine("max\t=MAX(" + allRange + ")\t=MAX(" + mouseRange + ")\t=MAX(" + touchRange + ")\t=MAX(" + mouseCircleRange + ")\t=MAX(" + mouseCrosshairRange + ")\t=MAX(" + touchCircleRange + ")\t=MAX(" + touchCrosshairRange + ")\t");


                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            catch (Exception e) {

                Console.WriteLine("what is this?");
                Console.WriteLine(e.Message);
            }
        }

        private Brush PickBrush()
        {
            Brush result = Brushes.Transparent;


            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            int random = rnd.Next(properties.Length);
            result = (Brush)properties[random].GetValue(null, null);

            return result;
        }

        void myTarget_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MyWindow);
            ActivateTouchPoint(pos);
        }

        private void ActivateTouchPoint(Point pos)
        {
            Target t;

            //type of target
            if (set % 2 == 0)
            {
                t = Target.Circle;
            }
            else
            {
                t = Target.Crosshair;
            }

            //size and location of target
            Shape cur = (Shape)elements[0].Last();
            double curX = Canvas.GetLeft(cur) + .5 * cur.Width;
            double curY = Canvas.GetTop(cur) + .5 * cur.Height;
            double curRadius = Math.Max(cur.Width, cur.Height) / 2;

            //location of gaze
            double gazeX = Canvas.GetLeft(GazePointer) + GazePointer.Width / 2;
            double gazeY = Canvas.GetTop(GazePointer) + GazePointer.Height / 2;

            String inputString = input.ToString();


            Log("ACTIVATED", t, curRadius, curX, curY, inputString, pos.X, pos.Y, gazeX, gazeY);
            Log("DISAPPEARED", t, curRadius, curX, curY);

            foreach (UIElement el in elements[0])
            {
                Ovals.Children.Remove(el);
            }
            elements.Remove(elements[0]);


            System.Timers.Timer loading = new System.Timers.Timer(1500);
            loading.Elapsed += loading_Elapsed;
            loading.AutoReset = false;
            loading.Enabled = true;
        }

        void loading_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {


            this.Dispatcher.Invoke((Action)(() =>
            {
                if (elements.Count > 0)
                {
                    foreach (UIElement el in elements[0])
                    {
                        Ovals.Children.Add(el);
                    }

                    Target t;

                    //type of target
                    if (set % 2 == 0)
                    {
                        t = Target.Circle;
                    }
                    else
                    {
                        t = Target.Crosshair;
                    }

                    //size and location of target

                    Shape cur = (Shape)elements[0].Last();
                    double curX = Canvas.GetLeft(cur) + .5 * cur.Width;
                    double curY = Canvas.GetTop(cur) + .5 * cur.Height;
                    double curRadius = Math.Max(cur.Width, cur.Height) / 2;

                    Log("APPEARED", t, curRadius, curX, curY);


                }
                else if (elements.Count == 0 && set < 3)
                {
                    set++;
                    if (set == 2)
                    {
                        switchInput();
                    }

                    elements = getUIElements(set);
                    foreach (UIElement el in elements[0])
                    {
                        Ovals.Children.Add(el);
                    }

                    Target t;

                    //type of target
                    if (set % 2 == 0)
                    {
                        t = Target.Circle;
                    }
                    else
                    {
                        t = Target.Crosshair;
                    }

                    //size and location of target

                    Shape cur = (Shape)elements[0].Last();
                    double curX = Canvas.GetLeft(cur) + .5 * cur.Width;
                    double curY = Canvas.GetTop(cur) + .5 * cur.Height;
                    double curRadius = Math.Max(cur.Width, cur.Height) / 2;
                    Log("APPEARED", t, curRadius, curX, curY);

                }
                else
                {
                    Console.WriteLine("FINISHED.  File: " + fileName);
                    System.Windows.Application.Current.Shutdown();

                }

            }));
        }

        private void switchInput()
        {
            if (input == Input.Mouse)
            {
                setTouchInput();
            }
            else
            {
                setMouseInput();
            }
        }

        private void setMouseInput()
        {
            AreaCursor.Visibility = Visibility.Visible;
            input = Input.Mouse;
        }

        private void setTouchInput()
        {
            AreaCursor.Visibility = Visibility.Collapsed;
            input = Input.Touch;
        }

        void Log(String action, double x, double y)
        {
            Log(action + ":" + x + ":" + y);
        }

        void Log(String action, Target target, double size, double objX, double objY)
        {
            Log(action + ":" + target.ToString() + ":" + size + ":" + objX + ":" + objY);
        }

        void Log(String action, Target target, double size, double objX, double objY, String inputType, double touchX, double touchY, double gazeX, double gazeY)
        {
            Log(action + ":" + target.ToString() + ":" + size + ":" + objX + ":" + objY + ":" + inputType + ":" + touchX + ":" + touchY + ":" + gazeX + ":" + gazeY);
        }

        void Log(String events)
        {
            if (VISUALIZE) return;
            Console.WriteLine("writing to file: " + fileName);


            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
                DateTime now = DateTime.Now;
                sw.WriteLine(now.Year + ":" + now.Month + ":" + now.Day + ":" + now.Hour + ":" + now.Minute + ":" + now.Second + ":" + now.Millisecond +":" + events);
            }

        }

        public List<List<UIElement>> getUIElements(int trial)
        {
           // Set set = new Set();

            int setSize = 25;
            List<List<UIElement>> elements = new List<List<UIElement>>();
            int windowWidth = 1200;//(int)System.Windows.Application.Current.MainWindow.Width;
            int windowHeight = 600;// (int)System.Windows.Application.Current.MainWindow.Height;

            Console.WriteLine("PossibleDimensions: " + windowWidth + ", " + windowHeight);
            switch (trial)
            {
                case 0:
                    for (int i = 0; i < setSize; i++)
                    {
                        Ellipse myTarget = new Ellipse() { Width = 50, Height = 50 };
                        myTarget.Fill = Brushes.DarkGray;
                        myTarget.MouseDown += myTarget_MouseDown;

                        Canvas.SetLeft(myTarget, rnd.Next(windowWidth - (int)(myTarget.Width)));
                        Canvas.SetTop(myTarget, rnd.Next(windowHeight - (int)(myTarget.Height)));
                        List<UIElement> listy = new List<UIElement>();
                        listy.Add(myTarget);
                        elements.Add(listy);
                      //  set.target = Target.Circle;
                    }

                    break;
                case 1:
                    for (int i = 0; i < setSize; i++)
                    {
                        Ellipse myTarget = new Ellipse() { Width = 50, Height = 50 };
                        myTarget.Fill = Brushes.Transparent;
                        myTarget.MouseDown += myTarget_MouseDown;
                        myTarget.Tag = "Crosshair";

                        Canvas.SetLeft(myTarget, rnd.Next(windowWidth - (int)(myTarget.Width)));
                        Canvas.SetTop(myTarget, rnd.Next(windowHeight - (int)(myTarget.Height)));

                        Rectangle v = new Rectangle();
                        v.Tag = "Crosshair";
                        v.Height = myTarget.Height;
                        v.Width = 2;
                        v.Fill = Brushes.Red;
                        Canvas.SetTop(v, Canvas.GetTop(myTarget));
                        Canvas.SetLeft(v, Canvas.GetLeft(myTarget) + .5 * myTarget.Width - v.Width * .5);

                        Rectangle h = new Rectangle();
                        h.Tag = "Crosshair";
                        h.Height = 2;
                        h.Width = myTarget.Width;
                        h.Fill = Brushes.Red;
                        Canvas.SetLeft(h, Canvas.GetLeft(myTarget));
                        Canvas.SetTop(h, Canvas.GetTop(myTarget) + .5 * myTarget.Height - h.Height * .5);

                        List<UIElement> listy = new List<UIElement>();
                        listy.Add(h);
                        listy.Add(v);
                        listy.Add(myTarget);
                        elements.Add(listy);
                     //   set.target = Target.Crosshair;


                    }
                    break;
                case 2:
                    for (int i = 0; i < setSize; i++)
                    {
                        Ellipse myTarget = new Ellipse() { Width = 50, Height = 50 };
                        myTarget.Fill = Brushes.DarkGray;
                        myTarget.MouseDown += myTarget_MouseDown;

                        Canvas.SetLeft(myTarget, rnd.Next(windowWidth - (int)(myTarget.Width)));
                        Canvas.SetTop(myTarget, rnd.Next(windowHeight - (int)(myTarget.Height)));
                        List<UIElement> listy = new List<UIElement>();
                        listy.Add(myTarget);
                        elements.Add(listy);
                        //set.target = Target.Circle;

                    }

                    break;
                case 3:
                    for (int i = 0; i < setSize; i++)
                    {
                        Ellipse myTarget = new Ellipse() { Width = 50, Height = 50 };
                        myTarget.Fill = Brushes.Transparent;
                        myTarget.MouseDown += myTarget_MouseDown;
                        myTarget.Tag = "Crosshair";

                        Canvas.SetLeft(myTarget, rnd.Next(windowWidth - (int)(myTarget.Width)));
                        Canvas.SetTop(myTarget, rnd.Next(windowHeight - (int)(myTarget.Height)));

                        Rectangle v = new Rectangle();
                        v.Tag = "Crosshair";
                        v.Height = myTarget.Height;
                        v.Width = 2;
                        v.Fill = Brushes.Red;
                        Canvas.SetTop(v, Canvas.GetTop(myTarget));
                        Canvas.SetLeft(v, Canvas.GetLeft(myTarget) + .5 * myTarget.Width - v.Width * .5);

                        Rectangle h = new Rectangle();
                        h.Tag = "Crosshair";
                        h.Height = 2;
                        h.Width = myTarget.Width;
                        h.Fill = Brushes.Red;
                        Canvas.SetLeft(h, Canvas.GetLeft(myTarget));
                        Canvas.SetTop(h, Canvas.GetTop(myTarget) + .5 * myTarget.Height - h.Height * .5);

                        List<UIElement> listy = new List<UIElement>();
                        listy.Add(h);
                        listy.Add(v);
                        listy.Add(myTarget);
                        elements.Add(listy);
                       // set.target = Target.Crosshair;

                    }
                    break;
            }


         //   set.elements = elements;
          //  set.id = trial;
            return elements;
        }



        #endregion

        #region Public methods

        public void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            // Invoke thread
            Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));
        }

        #endregion

        #region Private methods



        private void UpdateUI(int x, int y)
        {
            // Unhide the GazePointer if you want to see your gaze point
            // if (GazePointer.Visibility == Visibility.Visible)
            // {
            var relativePt = new Point(x, y);
            relativePt = transfrm.Transform(relativePt);
            Canvas.SetLeft(GazePointer, relativePt.X - GazePointer.Width / 2);
            Canvas.SetTop(GazePointer, relativePt.Y - GazePointer.Height / 2);
            Log("Gaze", (relativePt.X - GazePointer.Width / 2), (relativePt.Y - GazePointer.Height / 2));
            // }


        }






        private static void CleanUp()
        {
            GazeManager.Instance.Deactivate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();
            base.OnClosing(e);
        }

        private static double CalcDpiScale()
        {
            return DPI_DEFAULT / GetSystemDpi().X;
        }

        #endregion

        #region Native methods

        public static Point GetSystemDpi()
        {
            Point result = new Point();
            IntPtr hDc = GetDC(IntPtr.Zero);
            result.X = GetDeviceCaps(hDc, (int)DeviceCap.LOGPIXELSX);
            result.Y = GetDeviceCaps(hDc, (int)DeviceCap.LOGPIXELSY);
            ReleaseDC(IntPtr.Zero, hDc);
            return result;
        }

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        #endregion

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //Console.WriteLine("Mouse Pos: " + e.GetPosition(MyWindow).X + ", " + e.GetPosition(MyWindow).Y);
            Log("MouseMoved", e.GetPosition(MyWindow).X, e.GetPosition(MyWindow).Y);
            Canvas.SetLeft(AreaCursor, e.GetPosition(MyWindow).X - AreaCursor.Width / 2);
            Canvas.SetTop(AreaCursor, e.GetPosition(MyWindow).Y - AreaCursor.Height / 2);
        }

        private void AreaCursor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Shape cur = (Shape)elements[0].Last();
            double curX = Canvas.GetLeft(cur) + .5 * cur.Width;
            double curY = Canvas.GetTop(cur) + .5 * cur.Height;
            double curRadius = Math.Max(cur.Width, cur.Height) / 2;



            double cursorRadius = AreaCursor.Width / 2;
            double cursorX = Canvas.GetLeft(AreaCursor) + .5 * AreaCursor.Width;
            double cursorY = Canvas.GetTop(AreaCursor) + .5 * AreaCursor.Height;


            double distance = Math.Sqrt((curX - cursorX) * (curX - cursorX) + (curY - cursorY) * (curY - cursorY));
            Console.WriteLine("Widths: " + curRadius + " + " + cursorRadius + " = " + (curRadius + cursorRadius));
            Console.WriteLine("Distance: " + distance);
            if (distance < curRadius + cursorRadius)
            {
                ActivateTouchPoint(new Point(cursorX, cursorY));
            }
        }

        private void MyWindow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}