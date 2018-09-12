// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Common.Contract;
using FaceAPI = Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision.Contract;
using System.Windows.Controls;
using System.Windows.Documents;

namespace LiveCameraSample
{
    public class Visualization
    {
        private static string _latestEmail = "";
        private static List<ExchangeManipulator.Availability> list = new List<ExchangeManipulator.Availability>();

        private static SolidColorBrush s_lineBrush = new SolidColorBrush(new System.Windows.Media.Color { R = 255, G = 0, B = 0, A = 255 });
        private static Typeface s_typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private static BitmapSource DrawOverlay(BitmapSource baseImage, Action<DrawingContext, double> drawAction)
        {
            double annotationScale = baseImage.PixelHeight / 320;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();

            drawingContext.DrawImage(baseImage, new Rect(0, 0, baseImage.Width, baseImage.Height));

            drawAction(drawingContext, annotationScale);

            drawingContext.Close();

            RenderTargetBitmap outputBitmap = new RenderTargetBitmap(
                baseImage.PixelWidth, baseImage.PixelHeight,
                baseImage.DpiX, baseImage.DpiY, PixelFormats.Pbgra32);

            outputBitmap.Render(visual);

            return outputBitmap;
        }

        public static BitmapSource DrawTags(BitmapSource baseImage, Tag[] tags)
        {
            if (tags == null)
            {
                return baseImage;
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                double y = 0;
                foreach (var tag in tags)
                {
                    // Create formatted text--in a particular font at a particular size
                    FormattedText ft = new FormattedText(tag.Name,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                        42 * annotationScale, Brushes.YellowGreen);
                    // Instead of calling DrawText (which can only draw the text in a solid colour), we
                    // convert to geometry and use DrawGeometry, which allows us to add an outline. 
                    drawingContext.DrawText(ft, new System.Windows.Point(10 * annotationScale, y));
                    //var geom = ft.BuildGeometry(new System.Windows.Point(10 * annotationScale, y));
                    //drawingContext.DrawGeometry(s_lineBrush, new Pen(Brushes.Black, 2 * annotationScale), geom);
                    // Move line down
                    y += 42 * annotationScale;
                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        public static BitmapSource DrawFaces(BitmapSource baseImage, FaceAPI.Face[] faces, EmotionScores[] emotionScores, AtosEmployee[] celebName)
        {
            if (faces == null)
            {
                return baseImage;
            }

            Action<DrawingContext, double> drawAction = (drawingContext, annotationScale) =>
            {
                for (int i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    if (face.FaceRectangle == null) { continue; }
                    var magnifier = 60;

                    Rect faceRect = new Rect(
                        face.FaceRectangle.Left - magnifier/2, face.FaceRectangle.Top - magnifier/2,
                        face.FaceRectangle.Width + magnifier, face.FaceRectangle.Height + magnifier);
                    string text = "";

                    if (celebName != null && celebName.Length > i && celebName?[i] != null)
                    {
                        text += celebName[i].FirstName + " - ";
                    }

                    if (face.FaceAttributes != null)
                    {
                        text += Aggregation.SummarizeFaceAttributes(face.FaceAttributes);
                    }

                    //if (emotionScores?[i] != null)
                    //{
                    //    text += Aggregation.SummarizeEmotion(emotionScores[i]);
                    //}



                    faceRect.Inflate(6 * annotationScale, 6 * annotationScale);

                    double lineThickness = 4 * annotationScale;

                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(s_lineBrush, lineThickness),
                        faceRect);

                    if (text != "")
                    {
                        FormattedText ft = new FormattedText(text,
                            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, s_typeface,
                            16 * annotationScale, Brushes.Black);

                        var pad = 3 * annotationScale;

                        var ypad = pad;
                        var xpad = pad + 4 * annotationScale;
                        var origin = new System.Windows.Point(
                            faceRect.Left + xpad - lineThickness / 2,
                            faceRect.Top - ft.Height - ypad + lineThickness / 2);
                        var rect = ft.BuildHighlightGeometry(origin).GetRenderBounds(null);
                        rect.Inflate(xpad, ypad);

                        drawingContext.DrawRectangle(s_lineBrush, null, rect);
                        drawingContext.DrawText(ft, origin);
                    }
                }
            };

            return DrawOverlay(baseImage, drawAction);
        }

        internal static TextBlock SummarizeAllAttributes(TextBlock tb, FaceAPI.Face[] faces, AtosEmployee[] celebrityNames, Tag[] tags, EmotionScores[] emotionScores)
        {
            tb.Inlines.Clear();
            var header = "Hi!";
            if (celebrityNames != null && celebrityNames.Length > 0)
            {
                header = "Hi, " + celebrityNames[0].FirstName + " " + celebrityNames[0].LastName;

                tb.Inlines.Add(new Run(header) { FontSize = 24 });

                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(String.Format("Your email is {0}", celebrityNames[0].Email));

                if (_latestEmail != celebrityNames[0].Email)
                {
                    _latestEmail = celebrityNames[0].Email;
                    list = ExchangeManipulator.ExchangeHandler.GetCalendarInfo(celebrityNames[0].Email);
                }
                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(String.Format("You next meetings are:"));
                foreach (var item in list)
                {
                    tb.Inlines.Add(new LineBreak());
                    tb.Inlines.Add(String.Format("{0} from {1} to {2}", item.Status, item.StartDate.ToShortTimeString(), item.EndDate.ToShortTimeString()));

                }                          
            }
            else
            {
                header = "Who the duck are you?";
                tb.Inlines.Add(new Run(header) { FontSize = 24 });
            }

            if (faces != null && faces.Length > 0)
            {
               

                var face = faces[0];
                if (face.FaceAttributes != null)
                {

                    tb.Inlines.Add(new LineBreak());
                    var gender = face.FaceAttributes.Gender;
                    tb.Inlines.Add(String.Format("You are {0}", gender));

                    tb.Inlines.Add(new LineBreak());
                    var age = face.FaceAttributes.Age;
                    tb.Inlines.Add(String.Format("I think you are {0} years old", age));

                    tb.Inlines.Add(new LineBreak());
                    var facialHair = face.FaceAttributes.FacialHair;
                    if (facialHair.Beard > 0.5)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I think I see a beard ({0}%)", facialHair.Beard * 100));
                    }
                    if (facialHair.Moustache > 0.5)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I think I see a moustache ({0}%)", facialHair.Moustache * 100));
                    }
                    if (facialHair.Sideburns > 0.5)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("Do I see sideburns? ({0}%)", facialHair.Sideburns * 100));
                    }

                    var makeUp = face.FaceAttributes.Makeup;
                    if (makeUp.EyeMakeup)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I think I see some eye makeup"));
                    }
                    if (makeUp.LipMakeup)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I think I see a lipstick"));
                    }

                    tb.Inlines.Add(new LineBreak());
                    var hair = face.FaceAttributes.Hair;
                    if (hair.Bald > 0.5)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I think you're bald ({0}%)", hair.Bald * 100));
                    }
                    else
                    {
                        if (hair.HairColor.Count() > 0)
                        {
                            tb.Inlines.Add(new LineBreak());
                            tb.Inlines.Add(String.Format("Your hair color is {0}", hair.HairColor[0].Color.ToString()));
                        }
                    }

                    var glasses = face.FaceAttributes.Glasses;
                    var glassesText = "";
                    switch (glasses)
                    {
                        case FaceAPI.Glasses.NoGlasses: glassesText = "no glasses"; break;
                        case FaceAPI.Glasses.ReadingGlasses: glassesText = "reading glasses"; break;
                        case FaceAPI.Glasses.Sunglasses: glassesText = "sunglasses"; break;
                        case FaceAPI.Glasses.SwimmingGoggles: glassesText = "swimming goggles"; break;
                    }
                    tb.Inlines.Add(new LineBreak());
                    tb.Inlines.Add(String.Format("Your are wearing {0}", glassesText));

                    var smile = face.FaceAttributes.Smile;
                    if (smile > 0.5)
                    {
                        tb.Inlines.Add(new LineBreak());
                        tb.Inlines.Add(String.Format("I see a smile! ({0}%)", smile * 100));
                    }


                    if (face.FaceAttributes.Emotion != null)
                    {
                        tb.Inlines.Add(new LineBreak());
                        var emotion = Aggregation.SummarizeEmotion(face.FaceAttributes.Emotion);
                        var emotionPair = Aggregation.GetDominantEmotion(face.FaceAttributes.Emotion);
                        tb.Inlines.Add(String.Format("You major emotion is "));
                        switch (emotionPair.Item1)
                        {
                            case "Anger": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Red }); break;
                            case "Happiness": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Green }); break;
                            case "Sadness": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Blue }); break;
                            case "Surprise": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Orange }); break;
                            case "Disgust": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Brown }); break;
                            case "Fear": tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Gray }); break;
                            default: tb.Inlines.Add(new Run(emotion) { Foreground = Brushes.Black }); break;
                        }
                    }
                }
            }
            //tb.Inlines.Add(new Run("the TextBlock control ") { FontWeight = FontWeights.Bold });
            //tb.Inlines.Add("using ");
            //tb.Inlines.Add(new Run("inline ") { FontStyle = FontStyles.Italic });
            //tb.Inlines.Add(new Run("text formatting ") { Foreground = Brushes.Blue });
            //tb.Inlines.Add("from ");
            //tb.Inlines.Add(new Run("Code-Behind") { TextDecorations = TextDecorations.Underline });
            //tb.Inlines.Add(".");
            //List<string> attrs = new List<string>();
            //if (attr.Gender != null) attrs.Add(attr.Gender);
            //if (attr.Age > 0) attrs.Add(attr.Age.ToString());
            //if (attr.HeadPose != null)
            //{
            //    // Simple rule to estimate whether person is facing camera. 
            //    bool facing = Math.Abs(attr.HeadPose.Yaw) < 25;
            //    attrs.Add(facing ? "facing camera" : "not facing camera");
            //}
            ////if (attr.FacialHair != null) attrs.Add("Your beard score: " + attr.FacialHair.Beard);
            return tb;
        }

    }
}
