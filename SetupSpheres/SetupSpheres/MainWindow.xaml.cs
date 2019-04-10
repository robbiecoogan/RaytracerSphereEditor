using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.IO;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace SetupSpheres
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        struct Keyframe
        {
            public int TimelineLoc;
            public double xPos, yPos, zPos, scale;
            public Color kColor;
            

            public Keyframe(double xPos, double yPos, double zPos, double scale, int TimelineLoc, Color kColor)
            {
                this.xPos = xPos;
                this.yPos = yPos;
                this.zPos = zPos;
                this.scale = scale;
                this.kColor = kColor;

                this.TimelineLoc = TimelineLoc;
            }
        }
        struct ObjectAnimationInfo
        {
            public List<Keyframe> Keyframes;
        }

        struct JsonObject
        {
            public int minFrame;
            public int maxFrame;
            public Color defaultCol;
            public List<Keyframe> keyframes;
            public JsonObject(int minFrame, int maxFrame, List<Keyframe> keyframes, Color defaultCol)
            {
                this.minFrame = minFrame;
                this.maxFrame = maxFrame;
                this.keyframes = keyframes;
                this.defaultCol = defaultCol;
            }
        }

        int numOfSpheres = 1;
        PerspectiveCamera mainCam = new PerspectiveCamera();
        Viewport3D mainViewPort = new Viewport3D();
        Model3DGroup modelList = new Model3DGroup();
        Model3DGroup extraList = new Model3DGroup();
        ModelVisual3D modelVis3D = new ModelVisual3D();
        ModelVisual3D modelVis3D2 = new ModelVisual3D();
        DoubleCollection scaleContainer = new DoubleCollection();
        MeshGeometry3D defaultMesh = new MeshGeometry3D();
        List<Color> objectColorList = new List<Color>();
        List<ObjectAnimationInfo> objectAnimDetails = new List<ObjectAnimationInfo>();
        int numFrames = 100;
        //timer
        System.Windows.Threading.DispatcherTimer dTimer = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            dTimer.Tick += dTimer_Tick;
            dTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dTimer.Start();

            ((DockPanel)TimelinePanel.Children[1]).Background = new SolidColorBrush(Colors.White);

            mainCam.Position = new Point3D(mainCam.Position.X, mainCam.Position.Y, 10);
            mainCam.LookDirection = new Vector3D(mainCam.LookDirection.X, mainCam.LookDirection.Y, -10);

            mainViewPort.Camera = mainCam;

            DirectionalLight mainLight = new DirectionalLight();
            mainLight.Color = Colors.White;
            mainLight.Direction = new Vector3D(-1, -1, -1);

            extraList.Children.Add(mainLight);


            DiffuseMaterial objMat = new DiffuseMaterial(Brushes.Green);

            loadOBJ(defaultMesh, "sphere.obj", 0);

            for (int i = 0; i < numOfSpheres; i++)
            {
                GeometryModel3D newSphere = new GeometryModel3D();
                newSphere.Geometry = defaultMesh;
                newSphere.Material = objMat;

                SolidColorBrush temp = (SolidColorBrush)objMat.Brush;
                objectColorList.Add(temp.Color);

                TranslateTransform3D movement = new TranslateTransform3D(new Vector3D(i - 2, 0, 0));
                newSphere.Transform = movement;

                objectAnimDetails.Add(new ObjectAnimationInfo {Keyframes = new List<Keyframe>()});//create a space for the element

                modelList.Children.Add(newSphere);
            }
            modelVis3D.Content = modelList;
            mainViewPort.Children.Add(modelVis3D);
            modelVis3D2.Content = extraList;
            mainViewPort.Children.Add(modelVis3D2);
            MainPanel.Children.Add(mainViewPort);

            for (int i = 0; i < numOfSpheres; i++)
            {
                SphereList.Items.Add("Sphere " + i);
                scaleContainer.Add(1.0000);
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            //Create all the lines to be used on the timeline
            for (int i = 0; i < (numFrames*2) + 1; i++)
            {
                DockPanel padding = new DockPanel();
                DockPanel line = new DockPanel();
                
                double leftmargin = i * (TimelinePanel.Width / numFrames);

                line.Margin = new Thickness(0, 0, 0, 0);
                padding.Margin = new Thickness(0, 0, 0, 0);

                padding.Width = TimelinePanel.Width / numFrames - 1;
                padding.Height = TimelinePanel.Height;
                padding.Background = TimelinePanel.Background;

                line.Width = 1;
                line.Height = TimelinePanel.Height;
                line.Background = new SolidColorBrush(Colors.Black);

                TimelinePanel.Children.Add(padding);
                TimelinePanel.Children.Add(line);
            }
        }

        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (BtnTimelinePlay.Content.ToString() == "Pause")
            {
                if (TimelineSlider.Value == 100) TimelineSlider.Value = 1;
                TimelineSlider.Value += 1;
            }
            
        }

        private void UpdateUI()
        {
            int selectedI = SphereList.SelectedIndex;
            if (selectedI >= 0)
            {
                double xPos = modelList.Children.ElementAt(selectedI).Transform.Value.OffsetX;
                double yPos = modelList.Children.ElementAt(selectedI).Transform.Value.OffsetY;
                double zPos = modelList.Children.ElementAt(selectedI).Transform.Value.OffsetZ;
                double scale = scaleContainer[selectedI];
                XSlider.Value = xPos;
                YSlider.Value = yPos;
                ZSlider.Value = zPos;
                ScaleSlider.Value = scale;
                ColorShowPanel.Background = new SolidColorBrush(objectColorList[selectedI]);
            }
        }

        private void UpdateSpheres()
        {
            int selectedI = -1;
            if (SphereList != null) selectedI = SphereList.SelectedIndex;
            if (selectedI >= 0)
            {
                TranslateTransform3D newPos = new TranslateTransform3D(new Vector3D(XSlider.Value, YSlider.Value, ZSlider.Value));
                ScaleTransform3D newscale = new ScaleTransform3D(ScaleSlider.Value, ScaleSlider.Value, ScaleSlider.Value);
                Transform3DGroup transGroup = new Transform3DGroup();
                transGroup.Children.Add(newscale);
                transGroup.Children.Add(newPos);
                scaleContainer[selectedI] = ScaleSlider.Value;
                modelList.Children.ElementAt(selectedI).Transform = transGroup;

                for (int i = 0; i < objectAnimDetails[selectedI].Keyframes.Count; i++)
                {
                    if (objectAnimDetails[selectedI].Keyframes[i].TimelineLoc == TimelineSlider.Value)
                    {
                        Color thisCol = ((SolidColorBrush)ColorShowPanel.Background).Color;
                        objectAnimDetails[selectedI].Keyframes[i] = new Keyframe(newPos.OffsetX, newPos.OffsetY, newPos.OffsetZ, scaleContainer[selectedI], (int)TimelineSlider.Value, thisCol);
                    }
                }
                
            }
        }

        private void DeleteSphere(int i)
        {
            if (i >= 0)
            {
                numOfSpheres--;
                SphereList.Items.RemoveAt(i);
                modelList.Children.RemoveAt(i);
                scaleContainer.RemoveAt(i);
                objectColorList.RemoveAt(i);
                objectAnimDetails.RemoveAt(i);
                SphereList.SelectedValue = SphereList.Items.Count - 1;
            }
        }

        private void AddSphere()
        {
            numOfSpheres++;
            SphereList.Items.Add("Sphere " + (SphereList.Items.Count));
            scaleContainer.Add(1.0000);

            DiffuseMaterial objMat = new DiffuseMaterial(ColorShowPanel.Background);
            GeometryModel3D newSphere = new GeometryModel3D();
            newSphere.Geometry = defaultMesh;
            newSphere.Material = objMat;
            SolidColorBrush temp = (SolidColorBrush)objMat.Brush;
            objectColorList.Add(temp.Color);

            objectAnimDetails.Add(new ObjectAnimationInfo { Keyframes = new List<Keyframe>() });//create a space for the element

            modelList.Children.Add(newSphere);
        }

        private int getnumCharSize(int valFound)
        {
            if (valFound < 10) return 1;
            else if (valFound < 100) return 2;
            else if (valFound < 1000) return 3;
            else if (valFound < 10000) return 4;
            else if (valFound < 100000) return 5;
            else if (valFound < 1000000) return 6;

            return -1;
        }

        private double Distance(Point3D vec1, Point3D vec2)
        {
            double returnVal = 0;

            double xDiff, yDiff, zDiff;

            xDiff = Math.Abs(vec1.X - vec2.X);
            xDiff *= xDiff;

            yDiff = Math.Abs(vec1.Y - vec2.Y);
            yDiff *= yDiff;

            zDiff = Math.Abs(vec1.Z - vec2.Z);
            zDiff *= zDiff;

            double total = xDiff + yDiff + zDiff;
            returnVal = Math.Sqrt(total);

            return returnVal;
        }

        bool dontUpdateSpheres = false;//this will be set to true by other functions to prevent sideeffects of changing the slider value through code
        private void XSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dontUpdateSpheres)
            {
                float val = (float)Math.Round(XSlider.Value, 2);
                LblXPos.Content = "X Pos: " + val;
                UpdateSpheres();
            }
            else dontUpdateSpheres = false;
        }

        private void YSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dontUpdateSpheres)
            {
                float val = (float)Math.Round(YSlider.Value, 2);
                LblYPos.Content = "Y Pos: " + val;
                UpdateSpheres();
            }
            else dontUpdateSpheres = false;
        }

        private void ZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dontUpdateSpheres)
            {
                float val = (float)Math.Round(ZSlider.Value, 2);
                LblZPos.Content = "Z Pos: " + val;
                UpdateSpheres();
            }
            else dontUpdateSpheres = false;
        }

        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dontUpdateSpheres)
            {
                if (ScaleSlider.Value >= 0 && scaleContainer.Count >= 0 && SphereList != null && SphereList.SelectedIndex >= 0)
                {
                    float val = (float)Math.Round(ScaleSlider.Value, 2);
                    LblScale.Content = "Scale: " + val;
                    scaleContainer[SphereList.SelectedIndex] = ScaleSlider.Value;
                    UpdateSpheres();
                }
            }
            else dontUpdateSpheres = false;
        }


        private void loadOBJ(MeshGeometry3D inputObject, string fileLoc, int xOffset)
        {
            //open sphere file
            try
            {
                StreamReader sr = new StreamReader(fileLoc);

                string lineInfo = "";
                bool foundFirst = false;
                int objectNum = 0;
                int lastWorkedWith = 0;
                int totalVerts = 0;

                Point3DCollection vertices = new Point3DCollection();
                MeshGeometry3D loadMesh = new MeshGeometry3D();
                Int32Collection indices = new Int32Collection();

                while (!sr.EndOfStream)//check each line 
                {
                    lineInfo = sr.ReadLine();
                    if (lineInfo.Count() > 0 && lineInfo[0] == 'v' && lineInfo[1] != 'n' && lineInfo[1] != 't')
                    {
                        if (lastWorkedWith == 2)
                        {
                            lastWorkedWith = 1;
                            objectNum++;
                        }


                        foundFirst = true;
                        //read in 3 vertices and add them to the Point3DCollection
                        int lastIndex = 2;//used to grab from between 1st num to 2nd num, and 2nd num to 3rd num.
                        Point3D position = new Point3D(0, 0, 0);
                        double x = -1.2345; double y = -1.2345; double z = -1.2345;
                        for (int i = 0; i < lineInfo.Length; i++)
                        {
                            if (i >= 3 && lineInfo[i] == ' ' || i == lineInfo.Length - 1)//if a space is detected, grab the number and add it
                            {
                                if (x == -1.2345) x = Convert.ToDouble(lineInfo.Substring(lastIndex, i - lastIndex)) + xOffset;
                                else if (y == -1.2345) y = Convert.ToDouble(lineInfo.Substring(lastIndex, i - lastIndex));
                                else z = Convert.ToDouble(lineInfo.Substring(lastIndex, i - lastIndex));
                                lastIndex = i;


                            }

                        }
                        position = new Point3D(x, y, z);
                        vertices.Add(position);
                    }

                    int counter = 0;

                    if (lineInfo.Count() > 0 && lineInfo[0] == 'f')//if this line is an index list
                    {
                        if (lastWorkedWith == 1 || lastWorkedWith == 0)
                        {
                            lastWorkedWith = 2;
                        }
                        counter++;
                        int lastIndex = 2;
                        int numCharSize = 0;//size to ignore when grabbing data, as the line number is printed in the OBJ

                        int numElements = 0;
                        int[] subIndices = new int[4]; ;//enough room for 4 indices, but there may be 3.
                        for (int i = 0; i < lineInfo.Length; i++)
                        {
                            numCharSize = getnumCharSize(counter);

                            //find slash
                            if (i >= 3 && lineInfo[i] == '/')
                            {
                                int newCounter = 0;
                                int numLength = i - lastIndex;
                                while (lineInfo[i + newCounter] != ' ')//find the next space
                                {
                                    if (i + newCounter == lineInfo.Length - 1) break;
                                    newCounter++;
                                }
                                if (lineInfo[i + newCounter] == ' ')
                                {
                                    numCharSize = newCounter;
                                    i = i + newCounter;
                                }
                                else if (i + newCounter == lineInfo.Length - 1)
                                {
                                    numCharSize = newCounter;
                                    i = lineInfo.Length - 1;
                                }

                                if (i < lineInfo.Length - 1)
                                    subIndices[numElements] = Convert.ToInt32(lineInfo.Substring(lastIndex, numLength));
                                else
                                    subIndices[numElements] = Convert.ToInt32(lineInfo.Substring(lastIndex, numLength));
                                numElements++;
                                lastIndex = i;

                            }

                            if (i == lineInfo.Length - 1)//if the last element has been checked
                            {
                                //Count how many elements have been found, if it's 4, we need to make 2 triangles, if 3, it'll be a normal
                                if (numElements == 4)
                                {
                                    int modVal = (objectNum != 0) ? 0 : 0;
                                    modVal--;

                                    indices.Add(subIndices[0] + modVal);
                                    indices.Add(subIndices[1] + modVal);
                                    indices.Add(subIndices[3] + modVal);
                                    indices.Add(subIndices[1] + modVal);
                                    indices.Add(subIndices[2] + modVal);
                                    indices.Add(subIndices[3] + modVal);
                                }
                                else//if there are 3 elements
                                {
                                    int modVal = (objectNum != 0) ? 0 : 0;
                                    modVal--;
                                    indices.Add(subIndices[0] + modVal);
                                    indices.Add(subIndices[1] + modVal);
                                    indices.Add(subIndices[2] + modVal);
                                }


                            }
                        }
                    }
                }

                inputObject.Positions = vertices;
                inputObject.TriangleIndices = indices;

            }
            catch
            {
                SphereList.Items.Add("ERROR: COULDNT LOAD OBJECT FILE");
            }

        }

        int selectionBoxPrevI = -1;
        private void SphereList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectionBoxPrevI == -1) selectionBoxPrevI = SphereList.SelectedIndex;

            int selectedI = SphereList.SelectedIndex;
            if (selectedI >= 0)
            {
                TimelineSlider.Value = TimelineSlider.Value+1;
                TimelineSlider.Value = TimelineSlider.Value-1;
                for (int i = 0; i < numOfSpheres; i++)
                {
                    DiffuseMaterial objMaterial = new DiffuseMaterial(new SolidColorBrush(objectColorList[i]));
                    //set everything to standard green
                    //((GeometryModel3D)modelList.Children.ElementAt(i)).Material = objMaterial;
                }
                //now set the selected item to bright
                //byte brightnessVal = 35;
                //byte rVal = (objectColorList[selectedI].R + brightnessVal < 255) ? (byte)(objectColorList[selectedI].R /*+ brightnessVal*/) : (byte)(objectColorList[selectedI].R /*- brightnessVal*/);
                //byte gVal = (objectColorList[selectedI].G + brightnessVal < 255) ? (byte)(objectColorList[selectedI].G /*+ brightnessVal*/) : (byte)(objectColorList[selectedI].G /*- brightnessVal*/);
                //byte bVal = (objectColorList[selectedI].B + brightnessVal < 255) ? (byte)(objectColorList[selectedI].B /*+ brightnessVal*/) : (byte)(objectColorList[selectedI].B /*- brightnessVal*/);
                //DiffuseMaterial objMat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(rVal, gVal, bVal)));
                //if (selectedI >= 0) ((GeometryModel3D)modelList.Children.ElementAt(selectedI)).Material = objMat;
                UpdateUI();

                //also, we now have to go through the timeline and clear any old yellow lines, then draw the current object's yellow lines
                if (selectionBoxPrevI < modelList.Children.Count)
                {
                    for (int i = 0; i < objectAnimDetails[selectionBoxPrevI].Keyframes.Count; i++)
                    {
                        int timelineIndex = objectAnimDetails[selectionBoxPrevI].Keyframes[i].TimelineLoc * 2; timelineIndex--;
                        ((DockPanel)TimelinePanel.Children[timelineIndex]).Background = new SolidColorBrush(Colors.Black);
                    }
                }
                //iterate through current object and draw keyframes as gold
                for (int i = 0; i < objectAnimDetails[selectedI].Keyframes.Count; i++)
                {
                    int timelineIndex = objectAnimDetails[selectedI].Keyframes[i].TimelineLoc * 2; timelineIndex--;
                    ((DockPanel)TimelinePanel.Children[timelineIndex]).Background = new SolidColorBrush(Colors.Gold);
                }
                selectionBoxPrevI = SphereList.SelectedIndex;
            }
        }

        private void BtnDeleteSphere_Click(object sender, RoutedEventArgs e)
        {
            DeleteSphere(SphereList.SelectedIndex);
        }

        private void BtnAddSphere_Click(object sender, RoutedEventArgs e)
        {
            AddSphere();
        }

        private void BtnChangeColour_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog dialog = new ColorDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && SphereList.SelectedIndex >= 0)
            {
                ColorShowPanel.Background = new SolidColorBrush(Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B));
                int selectedI = SphereList.SelectedIndex;
                objectColorList[selectedI] = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
                ((GeometryModel3D)modelList.Children[selectedI]).Material = new DiffuseMaterial(new SolidColorBrush(objectColorList[selectedI]));
                UpdateSpheres();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TimelinePanel.Width = mainGrid.ColumnDefinitions[0].ActualWidth - 14;

            if (NumLabel != null)
            {
                NumLabel.Margin = new Thickness(5 + ((TimelineSlider.ActualWidth - 5) / TimelineSlider.Maximum * TimelineSlider.Value), NumLabel.Margin.Top, NumLabel.Margin.Right, NumLabel.Margin.Bottom);
                NumLabel.Text = TimelineSlider.Value.ToString();
            }

            for (int i = 0; i < TimelinePanel.Children.Count; i++)
            {
                if (TimelinePanel.Children[i].GetType() == typeof(DockPanel))
                {
                    DockPanel childCopy = (DockPanel)TimelinePanel.Children[i];
                    if (childCopy.Width > 1 && (TimelinePanel.Width / (numFrames+1)) - 1 > 1) ((DockPanel)TimelinePanel.Children[i]).Width = (TimelinePanel.Width / numFrames) - 1;
                }
            }
        }

        int prevSliderI = -1;
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (prevSliderI < 0) prevSliderI = (int)(TimelineSlider.Value * 2) - 1;

            if (NumLabel != null)
            {
                NumLabel.Margin = new Thickness(5 + ((TimelineSlider.ActualWidth - 10) / TimelineSlider.Maximum * TimelineSlider.Value), NumLabel.Margin.Top, NumLabel.Margin.Right, NumLabel.Margin.Bottom);
                NumLabel.Text = TimelineSlider.Value.ToString();
            }

            TimelineSlider.Value = (int)TimelineSlider.Value;
            if (TimelineSlider.Value > 0 && TimelinePanel.Children.Count >= ((int)(TimelineSlider.Value * 2) - 1))
            {
                DockPanel prevPoint = ((DockPanel)TimelinePanel.Children[prevSliderI]);
                var prevCol = prevPoint.Background as SolidColorBrush;
                var curCol = ((DockPanel)TimelinePanel.Children[(int)(TimelineSlider.Value * 2) - 1]).Background as SolidColorBrush;

                if (!Colors.Gold.Equals(prevCol.Color)) prevPoint.Background = new SolidColorBrush(Colors.Black);
                if (!Colors.Gold.Equals(curCol.Color)) ((DockPanel)TimelinePanel.Children[(int)(TimelineSlider.Value * 2) - 1]).Background = new SolidColorBrush(Colors.White);
            }

            prevSliderI = (int)(TimelineSlider.Value * 2) - 1;


            //Search through every object keyframe, and if it is equal to the current selected keyframe, set everything to that info
            for (int i = 0; i < objectAnimDetails.Count; i++)
            {

                //sort keyframes
                /////////////////////////////////////////////////////
                List<Keyframe> sortedFrames = new List<Keyframe>();
                for (int k = 0; k < objectAnimDetails[i].Keyframes.Count; k++)
                {
                    int TimelineLoc = objectAnimDetails[i].Keyframes[k].TimelineLoc;
                    double xPos = objectAnimDetails[i].Keyframes[k].xPos;
                    double yPos = objectAnimDetails[i].Keyframes[k].yPos;
                    double zPos = objectAnimDetails[i].Keyframes[k].zPos;
                    double scale = objectAnimDetails[i].Keyframes[k].scale;

                    Color thisCol = objectAnimDetails[i].Keyframes[k].kColor;
                    sortedFrames.Add(new Keyframe(xPos, yPos, zPos, scale, TimelineLoc, thisCol));
                }

                for (int k = 1; k < sortedFrames.Count; k++)
                {
                    if (k >= 1)
                    {
                        int copyTL = sortedFrames[k].TimelineLoc;
                        double copyX, copyY, copyZ;
                        copyX = sortedFrames[k].xPos; copyY = sortedFrames[k].yPos; copyZ = sortedFrames[k].zPos;
                        double copyScale = sortedFrames[k].scale;
                        int redCol = sortedFrames[k].kColor.R;
                        int greenCol = sortedFrames[k].kColor.G;
                        int blueCol = sortedFrames[k].kColor.B;
                        Color copyCol = Color.FromRgb((byte)redCol, (byte)greenCol, (byte)blueCol);
                        Keyframe copy = new Keyframe(copyX, copyY, copyZ, copyScale, copyTL, copyCol);

                        if (sortedFrames[k].TimelineLoc < sortedFrames[k - 1].TimelineLoc)
                        {
                            //swap elements
                            sortedFrames[k] = sortedFrames[k - 1];
                            sortedFrames[k - 1] = new Keyframe(copyX, copyY, copyZ, copyScale, copyTL, copyCol);
                            k -= 2;
                        }
                    }
                }

                for (int k = 0; k < objectAnimDetails[i].Keyframes.Count(); k++)
                {
                    objectAnimDetails[i].Keyframes[k] = sortedFrames[k];
                }
                /////////////////////////////////////////////////////

            }


            for (int i = 0; i < objectAnimDetails.Count; i++)
            {
                for (int k = 0; k < objectAnimDetails[i].Keyframes.Count; k++)
                {
                    if (k == 0 && TimelineSlider.Value <= objectAnimDetails[i].Keyframes[k].TimelineLoc)//if selected location is earlier than earliest keyframe
                    {
                        TranslateTransform3D pos = new TranslateTransform3D(new Vector3D(objectAnimDetails[i].Keyframes[k].xPos, objectAnimDetails[i].Keyframes[k].yPos, objectAnimDetails[i].Keyframes[k].zPos));
                        ScaleTransform3D scale = new ScaleTransform3D(new Vector3D(objectAnimDetails[i].Keyframes[k].scale, objectAnimDetails[i].Keyframes[k].scale, objectAnimDetails[i].Keyframes[k].scale));
                        Transform3DGroup transGroup = new Transform3DGroup();
                        transGroup.Children.Add(scale);
                        transGroup.Children.Add(pos);
                        scaleContainer[i] = objectAnimDetails[i].Keyframes[k].scale;
                        modelList.Children[i].Transform = transGroup;
                        ((GeometryModel3D)modelList.Children[i]).Material = new DiffuseMaterial(new SolidColorBrush(objectAnimDetails[i].Keyframes[k].kColor));
                        ColorShowPanel.Background = new SolidColorBrush(objectAnimDetails[i].Keyframes[k].kColor);

                    }
                    else if (k + 1 < objectAnimDetails[i].Keyframes.Count && TimelineSlider.Value >= objectAnimDetails[i].Keyframes[k].TimelineLoc && TimelineSlider.Value <= objectAnimDetails[i].Keyframes[k + 1].TimelineLoc)//if we are between 2 keyframes, animate to the appropriate frame
                    {
                        double xDiff = objectAnimDetails[i].Keyframes[k + 1].xPos - objectAnimDetails[i].Keyframes[k].xPos;
                        double yDiff = objectAnimDetails[i].Keyframes[k + 1].yPos - objectAnimDetails[i].Keyframes[k].yPos;
                        double zDiff = objectAnimDetails[i].Keyframes[k + 1].zPos - objectAnimDetails[i].Keyframes[k].zPos;
                        double scaleDiff = objectAnimDetails[i].Keyframes[k + 1].scale - objectAnimDetails[i].Keyframes[k].scale;
                        double redDiff = objectAnimDetails[i].Keyframes[k+1].kColor.R - objectAnimDetails[i].Keyframes[k].kColor.R;
                        double greenDiff = objectAnimDetails[i].Keyframes[k + 1].kColor.G - objectAnimDetails[i].Keyframes[k].kColor.G;
                        double blueDiff = objectAnimDetails[i].Keyframes[k + 1].kColor.B - objectAnimDetails[i].Keyframes[k].kColor.B;
                        Vector3D difference = new Vector3D(xDiff, yDiff, zDiff);
                        int totalFrames = 0;//the amount of frames between the two keyframes
                        totalFrames = (((objectAnimDetails[i].Keyframes[k + 1].TimelineLoc * 2) - 1) - ((objectAnimDetails[i].Keyframes[k].TimelineLoc * 2) - 1));
                        int currentFrame = 0;//the amount of frames that we are currently into the animation
                        currentFrame = ((((int)TimelineSlider.Value * 2) - 1) - ((objectAnimDetails[i].Keyframes[k].TimelineLoc * 2) - 1));
                        difference /= totalFrames;//the difference can now be multiplied by the current frame to get where the object should be
                        Vector3D newPos = difference * currentFrame;
                        newPos.X += objectAnimDetails[i].Keyframes[k].xPos;
                        newPos.Y += objectAnimDetails[i].Keyframes[k].yPos;
                        newPos.Z += objectAnimDetails[i].Keyframes[k].zPos;
                        scaleDiff /= totalFrames;
                        scaleDiff *= currentFrame;
                        scaleDiff += objectAnimDetails[i].Keyframes[k].scale;
                        redDiff /= totalFrames;
                        greenDiff /= totalFrames;
                        blueDiff /= totalFrames;
                        redDiff *= currentFrame;
                        greenDiff *= currentFrame;
                        blueDiff *= currentFrame;
                        Color keyframeCol = objectAnimDetails[i].Keyframes[k].kColor;
                        redDiff += objectAnimDetails[i].Keyframes[k].kColor.R;
                        greenDiff += objectAnimDetails[i].Keyframes[k].kColor.G;
                        blueDiff += objectAnimDetails[i].Keyframes[k].kColor.B;

                        TranslateTransform3D move = new TranslateTransform3D(newPos);
                        ScaleTransform3D scale = new ScaleTransform3D(new Vector3D(scaleDiff, scaleDiff, scaleDiff));
                        Transform3DGroup transGroup = new Transform3DGroup();
                        //transGroup.Children.Add(defaultScale);
                        transGroup.Children.Add(scale);
                        transGroup.Children.Add(move);

                        scaleContainer[i] = scaleDiff;
                        modelList.Children[i].Transform = transGroup;
                        ((GeometryModel3D)modelList.Children[i]).Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb((byte)redDiff, (byte)greenDiff, (byte)blueDiff)));
                        ColorShowPanel.Background = new SolidColorBrush(Color.FromRgb((byte)redDiff, (byte)greenDiff, (byte)blueDiff));
                    }
                    else if (k == objectAnimDetails[i].Keyframes.Count - 2 && TimelineSlider.Value > objectAnimDetails[i].Keyframes[k + 1].TimelineLoc)
                    {
                        TranslateTransform3D pos = new TranslateTransform3D(new Vector3D(objectAnimDetails[i].Keyframes[k + 1].xPos, objectAnimDetails[i].Keyframes[k + 1].yPos, objectAnimDetails[i].Keyframes[k + 1].zPos));
                        ScaleTransform3D scale = new ScaleTransform3D(new Vector3D(objectAnimDetails[i].Keyframes[k + 1].scale, objectAnimDetails[i].Keyframes[k + 1].scale, objectAnimDetails[i].Keyframes[k + 1].scale));
                        Transform3DGroup transGroup = new Transform3DGroup();
                        transGroup.Children.Add(scale);
                        transGroup.Children.Add(pos);
                        scaleContainer[i] = objectAnimDetails[i].Keyframes[k + 1].scale;
                        modelList.Children[i].Transform = transGroup;
                        ((GeometryModel3D)modelList.Children[i]).Material = new DiffuseMaterial(new SolidColorBrush(objectAnimDetails[i].Keyframes[k+1].kColor));
                        ColorShowPanel.Background = new SolidColorBrush(objectAnimDetails[i].Keyframes[k+1].kColor);
                    }



                }
            }
            ////now update the UI
            int selectedI = SphereList.SelectedIndex;
            if (selectedI >= 0)
            {
                dontUpdateSpheres = true;
                XSlider.Value = modelList.Children[selectedI].Transform.Value.OffsetX;
                dontUpdateSpheres = true;
                YSlider.Value = modelList.Children[selectedI].Transform.Value.OffsetY;
                dontUpdateSpheres = true;
                ZSlider.Value = modelList.Children[selectedI].Transform.Value.OffsetZ;
                dontUpdateSpheres = true;
                ScaleSlider.Value = scaleContainer[selectedI];
            }

        }

        private void BtnKeyframeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (SphereList.SelectedIndex >= 0)
            {
                if (objectAnimDetails.ElementAt(SphereList.SelectedIndex).Keyframes.Count >= 0)
                {
                    bool foundVal = false;
                    for (int j = 0; j < objectAnimDetails.ElementAt(SphereList.SelectedIndex).Keyframes.Count; j++)
                    {
                        Keyframe currObj = objectAnimDetails.ElementAt(SphereList.SelectedIndex).Keyframes[j];;
                        if (currObj.TimelineLoc == TimelineSlider.Value)//if user has clicked this button, and we have found a keyframe that matches the index of the selected keyframe
                        {
                            objectAnimDetails.ElementAt(SphereList.SelectedIndex).Keyframes.RemoveAt(j);
                            foundVal = true;
                            ((DockPanel)TimelinePanel.Children[(currObj.TimelineLoc * 2) - 1]).Background = new SolidColorBrush(Colors.Black);
                            break;
                        }
                    }
                    if (!foundVal)//if the whole list has been searched, and nothing has been found, add a new element
                    {
                        double objectXpos = modelList.Children[SphereList.SelectedIndex].Transform.Value.OffsetX;
                        double objectYpos = modelList.Children[SphereList.SelectedIndex].Transform.Value.OffsetY;
                        double objectZpos = modelList.Children[SphereList.SelectedIndex].Transform.Value.OffsetZ;
                        double objectScale = scaleContainer[SphereList.SelectedIndex];
                        int timelineI = (int)TimelineSlider.Value;
                        Color thisCol = ((SolidColorBrush)ColorShowPanel.Background).Color;
                        objectAnimDetails.ElementAt(SphereList.SelectedIndex).Keyframes.Add(new Keyframe(objectXpos, objectYpos, objectZpos, objectScale, timelineI, thisCol));
                        ((DockPanel)TimelinePanel.Children[(timelineI * 2) - 1]).Background = new SolidColorBrush(Colors.Gold);
                    }
                }
            }
            else//if nothing is selected
            {
                System.Windows.Forms.MessageBox.Show("Please select an Object to add a Keyframe", "No Object Selected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.RightAlign);
            }

            for (int i = 1; i < TimelinePanel.Children.Count/2; i++)
            {
                if (i == TimelineSlider.Value)
                {
                    ((DockPanel)TimelinePanel.Children[(i * 2) - 1]).Background = new SolidColorBrush(Colors.White);
                }
                else
                {
                    ((DockPanel)TimelinePanel.Children[(i * 2) - 1]).Background = new SolidColorBrush(Colors.Black);
                }
            }
            if (SphereList.SelectedIndex >= 0 && objectAnimDetails[SphereList.SelectedIndex].Keyframes.Count > 0)
            {
                for (int i = 0; i < objectAnimDetails[SphereList.SelectedIndex].Keyframes.Count; i++)
                {
                    ((DockPanel)TimelinePanel.Children[(objectAnimDetails[SphereList.SelectedIndex].Keyframes[i].TimelineLoc * 2) - 1]).Background = new SolidColorBrush(Colors.Gold);
                }
            }
        }

        private void BtnTimelinePlay_Click(object sender, RoutedEventArgs e)
        {
            if ((e.Source as System.Windows.Controls.Button).Content == "Play")
            (e.Source as System.Windows.Controls.Button).Content = "Pause";
            else
                (e.Source as System.Windows.Controls.Button).Content = "Play";
        }

        private void TimelinePanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            //////////////////////export to JSON file//////////////////////

            //change the value of the timeline slider forward by 1 then back by 1 to trigger the timelineslider changed function (as this orders the keyframes by timeline value)
            TimelineSlider.Value++;
            TimelineSlider.Value--;

            string saveDir = "";

            //open file dialog and get file directory
            
            SaveFileDialog _fileDialog = new SaveFileDialog();
            if (_fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                saveDir = _fileDialog.FileName;
                //check if the filename that the user has entered contains a dot, and remove it if so
                for (int i = saveDir.Length - 1; i > 0; i--)
                {
                    //iterate until a slash is found, then we've reached the end of the filename
                    if (saveDir[i] == '\\') { break; }
                    if (saveDir[i] == '.')
                    {
                        saveDir = saveDir.Substring(0, i);//trim the text after the dot
                        break;
                    }
                }
                saveDir += ".JSON";//file name should be E.g: 'C:\\users\\foo\\filename.JSON' regardless of whether the user entered their own file extension

                //USING Newtonsoft's JSON.net (https://www.newtonsoft.com/json)
                StreamWriter mywriter = new StreamWriter(saveDir);
                List<JsonObject> objects = new List<JsonObject>();
                for (int i = 0; i < SphereList.Items.Count; i++)
                {
                    JsonObject IObject = new JsonObject((int)TimelineSlider.Minimum, (int)TimelineSlider.Maximum, objectAnimDetails[i].Keyframes, objectColorList[i]);
                    objects.Add(IObject);
                }
                mywriter.WriteLine(JsonConvert.SerializeObject(objects, Formatting.Indented));
                mywriter.Flush();
            }



            ////now actually save the info as JSON
            //List<string> objects = new List<string>();
            //for (int i = 0; i < SphereList.Items.Count; i++)
            //{
            //    string textLine = "";
            //    textLine += "{\"object\": ";
            //    textLine += "{" +
            //                "\"name\": " + //("name": )
            //                "\"sphere " + i + "\",";//e.g.("sphere 1",)
            //                for (int k = 0; k < objectAnimDetails[i].Keyframes.Count; k++)
            //                {
            //                    Keyframe currKeyframe = objectAnimDetails[i].Keyframes[k];
            //                    textLine += "\"keyframe\": {" +
            //                    "\"firstFrame\": \"" + TimelineSlider.Minimum + "\"," +
            //                    "\"lastFrame\": \"" + TimelineSlider.Maximum + "\"," +
            //                    "\"keyframeLoc\": \"" + currKeyframe.TimelineLoc + "\"," +
            //                    "\"xPos\": \"" + currKeyframe.xPos + "\"," +
            //                    "\"yPos\": \"" + currKeyframe.yPos + "\"," +
            //                    "\"zPos\": \"" + currKeyframe.zPos + "\"," +
            //                    "\"scale\": \"" + currKeyframe.scale + "\"," +
            //                    "\"color\": \"" + currKeyframe.kColor +
            //                    "\"},";
            //                }
            //                textLine += "}";
            //                if (i < SphereList.Items.Count - 1) textLine += ",";
            //                else textLine += "}";

            //    objects.Add(textLine);
            //}
            //string[] totalText = new string[SphereList.Items.Count+1];
            //for (int i = 0; i < objects.Count; i++)
            //{
            //    totalText[i] += objects[i];
            //}
            //totalText[SphereList.Items.Count] = "}";
            //System.IO.File.WriteAllLines(saveDir, totalText);
        }
    }
}
