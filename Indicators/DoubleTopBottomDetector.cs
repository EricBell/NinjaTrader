#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class EnhancedDoubleTopBottomDetector : Indicator
    {
        private int lookbackPeriod = 20;
        private double thresholdPercent = 0.15;
        private double threshold;
        
        private double firstPeak = 0;
        private double secondPeak = 0;
        private double firstTrough = 0;
        private double secondTrough = 0;
        private double middleTrough = 0;
        private double middlePeak = 0;
        private int firstPeakBar = 0;
        private int secondPeakBar = 0;
        private int firstTroughBar = 0;
        private int secondTroughBar = 0;
        
        private bool lookingForSecondPeak = false;
        private bool lookingForSecondTrough = false;
        private bool doubleTopDetected = false;
        private bool doubleBottomDetected = false;
        
        private bool showAnnotations = true;
        private bool alertOnDetection = true;
        private int arrowSize = 14;
        private bool highlightPattern = true;
        private int lineThickness = 3;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enhanced version of Double Top/Bottom Pattern Detector with improved visibility";
                Name = "EnhancedDoubleTopBottomDetector";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                
                LookbackPeriod = 20;
                ThresholdPercent = 0.15;
                ShowAnnotations = true;
                AlertOnDetection = true;
                ArrowSize = 14;
                HighlightPattern = true;
                LineThickness = 3;
                
                DoubleTopColor = Brushes.Red;
                DoubleBottomColor = Brushes.Green;
                
                BarsRequiredToPlot = 20;
            }
            else if (State == State.Configure)
            {
                threshold = ThresholdPercent / 100.0;
                
                // Add plots with larger size for better visibility
                AddPlot(new Stroke(DoubleTopColor, LineThickness), PlotStyle.TriangleDown, "DoubleTop");
                AddPlot(new Stroke(DoubleBottomColor, LineThickness), PlotStyle.TriangleUp, "DoubleBottom");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < LookbackPeriod)
                return;
                
            Values[0][0] = double.NaN;
            Values[1][0] = double.NaN;
            
            DetectDoublePatterns();
            
            if (doubleTopDetected)
            {
                // Make arrow larger and position it more prominently
                Values[0][0] = High[0] + (5 * TickSize);
                
                if (ShowAnnotations)
                {
                    // Larger, more visible text with background
                    Draw.Text(this, "TopText" + CurrentBar, true, "DOUBLE TOP", 0, High[0] + (10 * TickSize), 0, 
                             Brushes.White, new SimpleFont("Arial", 12, FontWeight.Bold), TextAlignment.Center, DoubleTopColor, Brushes.Red, 80);
                             
                    // Draw a rectangle highlighting the pattern area
                    if (HighlightPattern)
                    {
                        Draw.Rectangle(this, "TopRect" + CurrentBar, true, 
                                      firstPeakBar, Math.Min(firstPeak, secondPeak) - (Math.Min(firstPeak, secondPeak) * 0.002), 
                                      0, Math.Max(firstPeak, secondPeak) + (Math.Max(firstPeak, secondPeak) * 0.002), 
                                      DoubleTopColor, Brushes.Transparent, 15);
                    }
                }
                
                if (AlertOnDetection)
                {
                    Alert("DoubleTopAlert" + CurrentBar, Priority.High, "Double Top Detected", 
                         NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 15, DoubleTopColor, Brushes.White);
                }
            }
            
            if (doubleBottomDetected)
            {
                // Make arrow larger and position it more prominently
                Values[1][0] = Low[0] - (5 * TickSize);
                
                if (ShowAnnotations)
                {
                    // Larger, more visible text with background
                    Draw.Text(this, "BottomText" + CurrentBar, true, "DOUBLE BOTTOM", 0, Low[0] - (10 * TickSize), 0, 
                             Brushes.White, new SimpleFont("Arial", 12, FontWeight.Bold), TextAlignment.Center, DoubleBottomColor, Brushes.Green, 80);
                             
                    // Draw a rectangle highlighting the pattern area
                    if (HighlightPattern)
                    {
                        Draw.Rectangle(this, "BottomRect" + CurrentBar, true, 
                                      firstTroughBar, Math.Min(firstTrough, secondTrough) - (Math.Min(firstTrough, secondTrough) * 0.002), 
                                      0, Math.Max(firstTrough, secondTrough) + (Math.Max(firstTrough, secondTrough) * 0.002), 
                                      DoubleBottomColor, Brushes.Transparent, 15);
                    }
                }
                
                if (AlertOnDetection)
                {
                    Alert("DoubleBottomAlert" + CurrentBar, Priority.High, "Double Bottom Detected", 
                         NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav", 15, DoubleBottomColor, Brushes.White);
                }
            }
        }
        
        private void DetectDoublePatterns()
        {
            doubleTopDetected = false;
            doubleBottomDetected = false;
            
            double currentHigh = High[0];
            double currentLow = Low[0];
            double currentClose = Close[0];
            double prevClose = Close[1];
            
            double localHigh = MAX(High, LookbackPeriod)[0];
            double localLow = MIN(Low, LookbackPeriod)[0];
            
            // DOUBLE TOP DETECTION
            if (!lookingForSecondPeak)
            {
                if (Math.Abs(currentHigh - localHigh) / localHigh < threshold)
                {
                    firstPeak = currentHigh;
                    firstPeakBar = CurrentBar;
                    lookingForSecondPeak = true;
                    middleTrough = double.MaxValue;
                    
                    if (ShowAnnotations)
                    {
                        Draw.Diamond(this, "FirstPeak" + CurrentBar, true, 0, firstPeak + (2 * TickSize), DoubleTopColor);
                        Draw.Text(this, "Peak1" + CurrentBar, true, "P1", 0, firstPeak + (6 * TickSize), 0, 
                                DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
                    }
                }
            }
            else
            {
                if (currentLow < middleTrough)
                {
                    middleTrough = currentLow;
                }
                
                if (Math.Abs(currentHigh - firstPeak) / firstPeak < threshold && 
                    CurrentBar > firstPeakBar + 2 &&
                    CurrentBar < firstPeakBar + LookbackPeriod)
                {
                    secondPeak = currentHigh;
                    secondPeakBar = CurrentBar;
                    
                    // MODIFIED: Less strict requirements for pattern confirmation
                    if (secondPeak <= firstPeak * 1.001 && // Allow slight higher second peak
                        middleTrough < Math.Min(firstPeak, secondPeak) - (Math.Min(firstPeak, secondPeak) * 0.001)) // Reduced retracement
                    {
                        doubleTopDetected = true;
                        lookingForSecondPeak = false;
                        
                        if (ShowAnnotations)
                        {
                            // Draw pattern with thicker, more visible lines
                            Draw.Line(this, "TopLine" + CurrentBar, true, firstPeakBar, firstPeak, 0, secondPeak, 
                                    DoubleTopColor, DashStyleHelper.Solid, LineThickness);
                            
                            Draw.Diamond(this, "SecondPeak" + CurrentBar, true, 0, secondPeak + (2 * TickSize), DoubleTopColor);
                            Draw.Text(this, "Peak2" + CurrentBar, true, "P2", 0, secondPeak + (6 * TickSize), 0, 
                                    DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
                            
                            // Draw neckline with thick, dashed line
                            int middleTroughBar = Math.Max(firstPeakBar - 10, 0);
                            for (int i = 0; i < 20; i++)
                            {
                                if (i < CurrentBar && Math.Abs(Low[i] - middleTrough) < TickSize)
                                {
                                    middleTroughBar = i;
                                    break;
                                }
                            }
                            
                            Draw.Line(this, "NecklineTop" + CurrentBar, true, 
                                    middleTroughBar, middleTrough, 
                                    0, middleTrough, 
                                    DoubleTopColor, DashStyleHelper.Dash, LineThickness);
                                    
                            Draw.Text(this, "NeckTop" + CurrentBar, true, "NECKLINE", Math.Max(middleTroughBar - 3, 0), 
                                    middleTrough - (4 * TickSize), 0, 
                                    DoubleTopColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
                            
                            Draw.ArrowDown(this, "EntryTop" + CurrentBar, true, 0, middleTrough + (10 * TickSize), DoubleTopColor);
                            Draw.Text(this, "EntrySellText" + CurrentBar, true, "SELL", 0, middleTrough + (15 * TickSize), 0, 
                                    Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleTopColor, null, 0);
                            
                            // Add rectangle highlighting
                            if (HighlightPattern)
                            {
                                Draw.Rectangle(this, "TopRect" + CurrentBar, true, 
                                            firstPeakBar, middleTrough - (0.5 * TickSize), 
                                            0, Math.Max(firstPeak, secondPeak) + (TickSize), 
                                            DoubleTopColor, Brushes.Transparent, 10);
                            }
                        }
                    }
                }
                
                if (CurrentBar > firstPeakBar + LookbackPeriod)
                {
                    lookingForSecondPeak = false;
                }
            }
            
            // DOUBLE BOTTOM DETECTION - Similar modifications
            if (!lookingForSecondTrough)
            {
                if (Math.Abs(currentLow - localLow) / localLow < threshold)
                {
                    firstTrough = currentLow;
                    firstTroughBar = CurrentBar;
                    lookingForSecondTrough = true;
                    middlePeak = double.MinValue;
                    
                    if (ShowAnnotations)
                    {
                        Draw.Diamond(this, "FirstTrough" + CurrentBar, true, 0, firstTrough - (2 * TickSize), DoubleBottomColor);
                        Draw.Text(this, "Trough1" + CurrentBar, true, "T1", 0, firstTrough - (6 * TickSize), 0, 
                                DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
                    }
                }
            }
            else
            {
                if (currentHigh > middlePeak)
                {
                    middlePeak = currentHigh;
                }
                
                if (Math.Abs(currentLow - firstTrough) / firstTrough < threshold && 
                    CurrentBar > firstTroughBar + 2 &&
                    CurrentBar < firstTroughBar + LookbackPeriod)
                {
                    secondTrough = currentLow;
                    secondTroughBar = CurrentBar;
                    
                    // MODIFIED: Less strict requirements for pattern confirmation
                    if (secondTrough >= firstTrough * 0.999 && // Allow slightly lower second trough
                        middlePeak > Math.Max(firstTrough, secondTrough) + (Math.Max(firstTrough, secondTrough) * 0.001)) // Reduced retracement
                    {
                        doubleBottomDetected = true;
                        lookingForSecondTrough = false;
                        
                        if (ShowAnnotations)
                        {
                            // Similar drawing code for double bottom
                            Draw.Line(this, "BottomLine" + CurrentBar, true, firstTroughBar, firstTrough, 0, secondTrough, 
                                    DoubleBottomColor, DashStyleHelper.Solid, LineThickness);
                            
                            Draw.Diamond(this, "SecondTrough" + CurrentBar, true, 0, secondTrough - (2 * TickSize), DoubleBottomColor);
                            Draw.Text(this, "Trough2" + CurrentBar, true, "T2", 0, secondTrough - (6 * TickSize), 0, 
                                    DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
                            
                            // Draw neckline with thick, dashed line
                            int middlePeakBar = Math.Max(firstTroughBar - 10, 0);
                            for (int i = 0; i < 20; i++)
                            {
                                if (i < CurrentBar && Math.Abs(High[i] - middlePeak) < TickSize)
                                {
                                    middlePeakBar = i;
                                    break;
                                }
                            }
                            
                            Draw.Line(this, "NecklineBottom" + CurrentBar, true, 
                                    middlePeakBar, middlePeak, 
                                    0, middlePeak, 
                                    DoubleBottomColor, DashStyleHelper.Dash, LineThickness);
                                    
                            Draw.Text(this, "NeckBottom" + CurrentBar, true, "NECKLINE", Math.Max(middlePeakBar - 3, 0), 
                                    middlePeak + (4 * TickSize), 0, 
                                    DoubleBottomColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
                            
                            Draw.ArrowUp(this, "EntryBottom" + CurrentBar, true, 0, middlePeak - (10 * TickSize), DoubleBottomColor);
                            Draw.Text(this, "EntryBuyText" + CurrentBar, true, "BUY", 0, middlePeak - (15 * TickSize), 0, 
                                    Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleBottomColor, null, 0);
                            
                            // Add rectangle highlighting
                            if (HighlightPattern)
                            {
                                Draw.Rectangle(this, "BottomRect" + CurrentBar, true, 
                                            firstTroughBar, Math.Min(firstTrough, secondTrough) - (TickSize), 
                                            0, middlePeak + (0.5 * TickSize), 
                                            DoubleBottomColor, Brushes.Transparent, 10);
                            }
                        }
                    }
                }
                
                if (CurrentBar > firstTroughBar + LookbackPeriod)
                {
                    lookingForSecondTrough = false;
                }
            }
        }
        
        #region Properties
        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "Lookback Period", Description = "Number of bars to look back for pattern formation", Order = 1, GroupName = "Pattern Parameters")]
        public int LookbackPeriod
        {
            get { return lookbackPeriod; }
            set { lookbackPeriod = value; }
        }
        
        [NinjaScriptProperty]
        [Range(0.05, 1.0)]
        [Display(Name = "Threshold %", Description = "Threshold percentage for similarity", Order = 2, GroupName = "Pattern Parameters")]
        public double ThresholdPercent
        {
            get { return thresholdPercent; }
            set { thresholdPercent = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Show Annotations", Description = "Show text annotations on the chart", Order = 1, GroupName = "Appearance")]
        public bool ShowAnnotations
        {
            get { return showAnnotations; }
            set { showAnnotations = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Alert On Detection", Description = "Play sound alert when pattern detected", Order = 2, GroupName = "Appearance")]
        public bool AlertOnDetection
        {
            get { return alertOnDetection; }
            set { alertOnDetection = value; }
        }
        
        [NinjaScriptProperty]
        [Range(5, 30)]
        [Display(Name = "Arrow Size", Description = "Size of the pattern arrows", Order = 3, GroupName = "Appearance")]
        public int ArrowSize
        {
            get { return arrowSize; }
            set { arrowSize = value; }
        }
        
        [NinjaScriptProperty]
        [Display(Name = "Highlight Pattern", Description = "Highlight the pattern area with semi-transparent background", Order = 4, GroupName = "Appearance")]
        public bool HighlightPattern
        {
            get { return highlightPattern; }
            set { highlightPattern = value; }
        }
        
        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name = "Line Thickness", Description = "Thickness of pattern lines", Order = 5, GroupName = "Appearance")]
        public int LineThickness
        {
            get { return lineThickness; }
            set { lineThickness = value; }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Top Color", Description = "Color for double top patterns", Order = 6, GroupName = "Appearance")]
        public Brush DoubleTopColor { get; set; }
        
        [Browsable(false)]
        public string DoubleTopColorSerializable
        {
            get { return Serialize.BrushToString(DoubleTopColor); }
            set { DoubleTopColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Bottom Color", Description = "Color for double bottom patterns", Order = 7, GroupName = "Appearance")]
        public Brush DoubleBottomColor { get; set; }
        
        [Browsable(false)]
        public string DoubleBottomColorSerializable
        {
            get { return Serialize.BrushToString(DoubleBottomColor); }
            set { DoubleBottomColor = Serialize.StringToBrush(value); }
        }
        #endregion
    }
}