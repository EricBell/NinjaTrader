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

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class DoubleTopBottomDetector : Indicator
    {
        #region Variables
        // Pattern detection parameters
        private int lookbackPeriod = 20;            // Lookback period for pattern detection
        private double thresholdPercent = 0.15;     // Threshold for similarity (0.15%)
        private double threshold;                   // Calculated threshold (decimal)
        
        // Pattern detection variables
        private double firstPeak = 0;               // First peak for double top
        private double secondPeak = 0;              // Second peak for double top
        private double firstTrough = 0;             // First trough for double bottom
        private double secondTrough = 0;            // Second trough for double bottom
        private double middleTrough = 0;            // Middle trough for double top
        private double middlePeak = 0;              // Middle peak for double bottom
        private int firstPeakBar = 0;               // Bar index of first peak
        private int secondPeakBar = 0;              // Bar index of second peak
        private int firstTroughBar = 0;             // Bar index of first trough
        private int secondTroughBar = 0;            // Bar index of second trough
        
        // Pattern state tracking
        private bool lookingForSecondPeak = false;   // Flag for finding second peak
        private bool lookingForSecondTrough = false; // Flag for finding second trough
        private bool doubleTopDetected = false;      // Flag for double top pattern
        private bool doubleBottomDetected = false;   // Flag for double bottom pattern
        
        // Plotting
        private bool showAnnotations = true;         // Flag to show text annotations
        private bool alertOnDetection = true;        // Flag to alert on detection
        private int arrowSize = 12;                  // Size of the arrows
        #endregion
        
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
                
                // Indicator parameters
                LookbackPeriod = 20;
                ThresholdPercent = 0.15;
                ShowAnnotations = true;
                AlertOnDetection = true;
                ArrowSize = 12;
                
                // Colors
                DoubleTopColor = Brushes.Red;
                DoubleBottomColor = Brushes.Green;
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
            if (CurrentBar < lookbackPeriod)
                return;
                
            // Reset plot values
            Values[0][0] = double.NaN;  // Double Top
            Values[1][0] = double.NaN;  // Double Bottom
            
            // Pattern detection logic
            DetectDoublePatterns();
            
            // Display detected patterns
            if (doubleTopDetected)
            {
                Values[0][0] = High[0] + (3 * TickSize);  // Position arrow above the bar
                
                if (showAnnotations)
                {
                    Draw.Text(this, "TopText" + CurrentBar, "Double Top", 0, High[0] + (6 * TickSize), 0, DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 1);
                }
                
                if (alertOnDetection)
                {
                    Alert("DoubleTopAlert" + CurrentBar, Priority.Medium, "Double Top Detected", NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 10, Brushes.Red, Brushes.White);
                }
            }
            
            if (doubleBottomDetected)
            {
                Values[1][0] = Low[0] - (3 * TickSize);  // Position arrow below the bar
                
                if (showAnnotations)
                {
                    Draw.Text(this, "BottomText" + CurrentBar, "Double Bottom", 0, Low[0] - (6 * TickSize), 0, DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 1);
                }
                
                if (alertOnDetection)
                {
                    Alert("DoubleBottomAlert" + CurrentBar, Priority.Medium, "Double Bottom Detected", NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav", 10, Brushes.Green, Brushes.White);
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
            double localHigh = MAX(High, lookbackPeriod)[0];
            double localLow = MIN(Low, lookbackPeriod)[0];
            
            // DOUBLE TOP DETECTION
            // If we're not looking for a second peak yet
            if (!lookingForSecondPeak)
            {
                // Check if current bar made a significant high
                if (Math.Abs(currentHigh - localHigh) / localHigh < threshold && currentHigh > prevClose)
                {
                    // Potential first peak found
                    firstPeak = currentHigh;
                    firstPeakBar = CurrentBar;
                    lookingForSecondPeak = true;
                    middleTrough = double.MaxValue;
                    
                    if (showAnnotations)
                    {
                        Draw.Dot(this, "FirstPeak" + CurrentBar, false, 0, firstPeak, DoubleTopColor);
                    }
                }
            }
            // If we're looking for a second peak
            else
            {
                // Update middle trough if needed
                if (currentLow < middleTrough)
                {
                    middleTrough = currentLow;
                }
                
                // Check if we've found a second peak (near first peak level)
                if (Math.Abs(currentHigh - firstPeak) / firstPeak < threshold && 
                    CurrentBar > firstPeakBar + 3 &&  // At least 3 bars after first peak
                    CurrentBar < firstPeakBar + lookbackPeriod) // Within lookback window
                {
                    secondPeak = currentHigh;
                    secondPeakBar = CurrentBar;
                    
                    // Validate double top pattern
                    if (secondPeak <= firstPeak && // Second peak failed to exceed first
                        middleTrough < Math.Min(firstPeak, secondPeak) - (Math.Min(firstPeak, secondPeak) * 0.003) && // Significant middle trough
                        currentClose < prevClose && // Price starting to decline
                        Volume[0] > SMA(Volume, 10)[0]) // Higher than average volume
                    {
                        doubleTopDetected = true;
                        lookingForSecondPeak = false; // Reset flag
                        
                        if (showAnnotations)
                        {
                            // Draw pattern visualization
                            Draw.Line(this, "TopLine" + CurrentBar, false, firstPeakBar, firstPeak, secondPeakBar, secondPeak, DoubleTopColor, DashStyleHelper.Solid, 2);
                            Draw.Dot(this, "SecondPeak" + CurrentBar, false, 0, secondPeak, DoubleTopColor);
                            
                            // Draw neckline
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
                
                // Reset if we've gone too far without finding a match
                if (CurrentBar > firstPeakBar + lookbackPeriod)
                {
                    lookingForSecondPeak = false;
                }
            }
            
            // DOUBLE BOTTOM DETECTION
            // If we're not looking for a second trough yet
            if (!lookingForSecondTrough)
            {
                // Check if current bar made a significant low
                if (Math.Abs(currentLow - localLow) / localLow < threshold && currentLow < prevClose)
                {
                    // Potential first trough found
                    firstTrough = currentLow;
                    firstTroughBar = CurrentBar;
                    lookingForSecondTrough = true;
                    middlePeak = double.MinValue;
                    
                    if (showAnnotations)
                    {
                        Draw.Dot(this, "FirstTrough" + CurrentBar, false, 0, firstTrough, DoubleBottomColor);
                    }
                }
            }
            // If we're looking for a second trough
            else
            {
                // Update middle peak if needed
                if (currentHigh > middlePeak)
                {
                    middlePeak = currentHigh;
                }
                
                // Check if we've found a second trough (near first trough level)
                if (Math.Abs(currentLow - firstTrough) / firstTrough < threshold && 
                    CurrentBar > firstTroughBar + 3 &&  // At least 3 bars after first trough
                    CurrentBar < firstTroughBar + lookbackPeriod) // Within lookback window
                {
                    secondTrough = currentLow;
                    secondTroughBar = CurrentBar;
                    
                    // Validate double bottom pattern
                    if (secondTrough >= firstTrough && // Second trough held above first
                        middlePeak > Math.Max(firstTrough, secondTrough) + (Math.Max(firstTrough, secondTrough) * 0.003) && // Significant middle peak
                        currentClose > prevClose && // Price starting to rise
                        Volume[0] > SMA(Volume, 10)[0]) // Higher than average volume
                    {
                        doubleBottomDetected = true;
                        lookingForSecondTrough = false; // Reset flag
                        
                        if (showAnnotations)
                        {
                            // Draw pattern visualization
                            Draw.Line(this, "BottomLine" + CurrentBar, false, firstTroughBar, firstTrough, secondTroughBar, secondTrough, DoubleBottomColor, DashStyleHelper.Solid, 2);
                            Draw.Dot(this, "SecondTrough" + CurrentBar, false, 0, secondTrough, DoubleBottomColor);
                            
                            // Draw neckline
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
                
                // Reset if we've gone too far without finding a match
                if (CurrentBar > firstTroughBar + lookbackPeriod)
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