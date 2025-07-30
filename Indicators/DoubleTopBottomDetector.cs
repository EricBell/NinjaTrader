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
    public class DoubleTopBottomDetector : Indicator
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
        private int arrowSize = 12;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Detects Double Top and Double Bottom Patterns";
                Name = "DoubleTopBottomDetector";
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
                ArrowSize = 12;
                
                DoubleTopColor = Brushes.Red;
                DoubleBottomColor = Brushes.Green;
                
                // Additional modern NT8 settings
                BarsRequiredToPlot = 20;
            }
            else if (State == State.Configure)
            {
                // Calculate threshold in decimal
                threshold = ThresholdPercent / 100.0;
                
                // Add plots
                AddPlot(new Stroke(DoubleTopColor, 2), PlotStyle.TriangleDown, "DoubleTop");
                AddPlot(new Stroke(DoubleBottomColor, 2), PlotStyle.TriangleUp, "DoubleBottom");
            }
        }

        protected override void OnBarUpdate()
        {
            // Wait for enough bars
            if (CurrentBar < LookbackPeriod)
                return;
                
            // Reset plot values
            Values[0][0] = double.NaN;  // Double Top
            Values[1][0] = double.NaN;  // Double Bottom
            
            // Pattern detection logic
            DetectDoublePatterns();
            
            // Display detected patterns
            if (doubleTopDetected)
            {
                Values[0][0] = High[0] + (3 * TickSize);
                
                if (ShowAnnotations)
                {
                    // FIXED: Using correct overload with isAutoScale parameter
                    Draw.Text(this, "TopText" + CurrentBar, true, "Double Top", 0, High[0] + (6 * TickSize), 0, 
                             DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 1);
                }
                
                if (AlertOnDetection)
                {
                    Alert("DoubleTopAlert" + CurrentBar, Priority.Medium, "Double Top Detected", 
                         NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 10, DoubleTopColor, Brushes.White);
                }
            }
            
            if (doubleBottomDetected)
            {
                Values[1][0] = Low[0] - (3 * TickSize);
                
                if (ShowAnnotations)
                {
                    // FIXED: Using correct overload with isAutoScale parameter
                    Draw.Text(this, "BottomText" + CurrentBar, true, "Double Bottom", 0, Low[0] - (6 * TickSize), 0, 
                             DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 1);
                }
                
                if (AlertOnDetection)
                {
                    Alert("DoubleBottomAlert" + CurrentBar, Priority.Medium, "Double Bottom Detected", 
                         NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav", 10, DoubleBottomColor, Brushes.White);
                }
            }
        }
        
        private void DetectDoublePatterns()
        {
            // Reset pattern flags
            doubleTopDetected = false;
            doubleBottomDetected = false;
            
            // Get current high and low
            double currentHigh = High[0];
            double currentLow = Low[0];
            double currentClose = Close[0];
            double prevClose = Close[1];
            
            // Find local high and low
            double localHigh = MAX(High, LookbackPeriod)[0];
            double localLow = MIN(Low, LookbackPeriod)[0];
            
            // DOUBLE TOP DETECTION
            if (!lookingForSecondPeak)
            {
                if (Math.Abs(currentHigh - localHigh) / localHigh < threshold && currentHigh > prevClose)
                {
                    firstPeak = currentHigh;
                    firstPeakBar = CurrentBar;
                    lookingForSecondPeak = true;
                    middleTrough = double.MaxValue;
                    
                    if (ShowAnnotations)
                    {
                        Draw.Dot(this, "FirstPeak" + CurrentBar, false, 0, firstPeak, DoubleTopColor);
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
                    CurrentBar > firstPeakBar + 3 &&
                    CurrentBar < firstPeakBar + LookbackPeriod)
                {
                    secondPeak = currentHigh;
                    secondPeakBar = CurrentBar;
                    
                    if (secondPeak <= firstPeak &&
                        middleTrough < Math.Min(firstPeak, secondPeak) - (Math.Min(firstPeak, secondPeak) * 0.003) &&
                        currentClose < prevClose &&
                        Volume[0] > SMA(Volume, 10)[0])
                    {
                        doubleTopDetected = true;
                        lookingForSecondPeak = false;
                        
                        if (ShowAnnotations)
                        {
                            Draw.Line(this, "TopLine" + CurrentBar, false, firstPeakBar, firstPeak, 0, secondPeak, DoubleTopColor, DashStyleHelper.Solid, 2);
                            Draw.Dot(this, "SecondPeak" + CurrentBar, false, 0, secondPeak, DoubleTopColor);
                            
                            int middleTroughBar = firstPeakBar;
                            for (int i = firstPeakBar; i <= secondPeakBar; i++)
                            {
                                if (Low[CurrentBar - i] <= middleTrough)
                                {
                                    middleTroughBar = i;
                                    break;
                                }
                            }
                            
                            Draw.Line(this, "NecklineTop" + CurrentBar, false, 
                                     CurrentBar - middleTroughBar, middleTrough, 
                                     0, middleTrough, 
                                     DoubleTopColor, DashStyleHelper.Dash, 1);
                        }
                    }
                }
                
                if (CurrentBar > firstPeakBar + LookbackPeriod)
                {
                    lookingForSecondPeak = false;
                }
            }
            
            // DOUBLE BOTTOM DETECTION
            if (!lookingForSecondTrough)
            {
                if (Math.Abs(currentLow - localLow) / localLow < threshold && currentLow < prevClose)
                {
                    firstTrough = currentLow;
                    firstTroughBar = CurrentBar;
                    lookingForSecondTrough = true;
                    middlePeak = double.MinValue;
                    
                    if (ShowAnnotations)
                    {
                        Draw.Dot(this, "FirstTrough" + CurrentBar, false, 0, firstTrough, DoubleBottomColor);
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
                    CurrentBar > firstTroughBar + 3 &&
                    CurrentBar < firstTroughBar + LookbackPeriod)
                {
                    secondTrough = currentLow;
                    secondTroughBar = CurrentBar;
                    
                    if (secondTrough >= firstTrough &&
                        middlePeak > Math.Max(firstTrough, secondTrough) + (Math.Max(firstTrough, secondTrough) * 0.003) &&
                        currentClose > prevClose &&
                        Volume[0] > SMA(Volume, 10)[0])
                    {
                        doubleBottomDetected = true;
                        lookingForSecondTrough = false;
                        
                        if (ShowAnnotations)
                        {
                            Draw.Line(this, "BottomLine" + CurrentBar, false, firstTroughBar, firstTrough, 0, secondTrough, DoubleBottomColor, DashStyleHelper.Solid, 2);
                            Draw.Dot(this, "SecondTrough" + CurrentBar, false, 0, secondTrough, DoubleBottomColor);
                            
                            int middlePeakBar = firstTroughBar;
                            for (int i = firstTroughBar; i <= secondTroughBar; i++)
                            {
                                if (High[CurrentBar - i] >= middlePeak)
                                {
                                    middlePeakBar = i;
                                    break;
                                }
                            }
                            
                            Draw.Line(this, "NecklineBottom" + CurrentBar, false, 
                                     CurrentBar - middlePeakBar, middlePeak, 
                                     0, middlePeak, 
                                     DoubleBottomColor, DashStyleHelper.Dash, 1);
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
        [Range(5, 20)]
        [Display(Name = "Arrow Size", Description = "Size of the pattern arrows", Order = 3, GroupName = "Appearance")]
        public int ArrowSize
        {
            get { return arrowSize; }
            set { arrowSize = value; }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Top Color", Description = "Color for double top patterns", Order = 4, GroupName = "Appearance")]
        public Brush DoubleTopColor { get; set; }
        
        [Browsable(false)]
        public string DoubleTopColorSerializable
        {
            get { return Serialize.BrushToString(DoubleTopColor); }
            set { DoubleTopColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Bottom Color", Description = "Color for double bottom patterns", Order = 5, GroupName = "Appearance")]
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