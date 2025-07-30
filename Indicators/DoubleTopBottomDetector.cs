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
        private int rectangleWidth = 20; // Parameter to control rectangle width
        
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
                RectangleWidth = 20;
                
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
                             Brushes.White, new SimpleFont("Arial", 12), TextAlignment.Center, DoubleTopColor, Brushes.Red, 80);
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
                             Brushes.White, new SimpleFont("Arial", 12), TextAlignment.Center, DoubleBottomColor, Brushes.Green, 80);
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
            
            // Check for double top pattern
            DetectDoubleTop(currentHigh, currentLow, localHigh);
            
            // Check for double bottom pattern
            DetectDoubleBottom(currentLow, currentHigh, localLow);
        }

        private void DetectDoubleTop(double currentHigh, double currentLow, double localHigh)
        {
            // Looking for first peak
            if (!lookingForSecondPeak)
            {
                if (IsNewPotentialPeak(currentHigh, localHigh))
                {
                    InitializeFirstPeak(currentHigh);
                }
            }
            // Already found first peak, looking for second
            else
            {
                // Update middle trough if current low is lower
                if (currentLow < middleTrough)
                {
                    middleTrough = currentLow;
                }
                
                // Check if this could be a second peak
                if (IsPotentialSecondPeak(currentHigh))
                {
                    secondPeak = currentHigh;
                    secondPeakBar = CurrentBar;
                    
                    // Check if this completes a valid double top
                    if (IsValidDoubleTop())
                    {
                        doubleTopDetected = true;
                        lookingForSecondPeak = false;
                        
                        if (ShowAnnotations)
                        {
                            DrawDoubleTopPattern();
                        }
                    }
                }
                
                // Reset if we've gone too far without finding a second peak
                if (CurrentBar > firstPeakBar + LookbackPeriod)
                {
                    lookingForSecondPeak = false;
                }
            }
        }

        private void DetectDoubleBottom(double currentLow, double currentHigh, double localLow)
        {
            // Looking for first trough
            if (!lookingForSecondTrough)
            {
                if (IsNewPotentialTrough(currentLow, localLow))
                {
                    InitializeFirstTrough(currentLow);
                }
            }
            // Already found first trough, looking for second
            else
            {
                // Update middle peak if current high is higher
                if (currentHigh > middlePeak)
                {
                    middlePeak = currentHigh;
                }
                
                // Check if this could be a second trough
                if (IsPotentialSecondTrough(currentLow))
                {
                    secondTrough = currentLow;
                    secondTroughBar = CurrentBar;
                    
                    // Check if this completes a valid double bottom
                    if (IsValidDoubleBottom())
                    {
                        doubleBottomDetected = true;
                        lookingForSecondTrough = false;
                        
                        if (ShowAnnotations)
                        {
                            DrawDoubleBottomPattern();
                        }
                    }
                }
                
                // Reset if we've gone too far without finding a second trough
                if (CurrentBar > firstTroughBar + LookbackPeriod)
                {
                    lookingForSecondTrough = false;
                }
            }
        }

        // Helper methods for Double Top detection
        private bool IsNewPotentialPeak(double currentHigh, double localHigh)
        {
            return Math.Abs(currentHigh - localHigh) / localHigh < threshold;
        }

        private void InitializeFirstPeak(double currentHigh)
        {
            firstPeak = currentHigh;
            firstPeakBar = CurrentBar;
            lookingForSecondPeak = true;
            middleTrough = double.MaxValue;
            
            if (ShowAnnotations)
            {
                DrawFirstPeak();
            }
        }

        private bool IsPotentialSecondPeak(double currentHigh)
        {
            return Math.Abs(currentHigh - firstPeak) / firstPeak < threshold && 
                   CurrentBar > firstPeakBar + 2 &&
                   CurrentBar < firstPeakBar + LookbackPeriod;
        }

        private bool IsValidDoubleTop()
        {
            return secondPeak <= firstPeak * 1.001 && // Allow slight higher second peak
                   middleTrough < Math.Min(firstPeak, secondPeak) - (Math.Min(firstPeak, secondPeak) * 0.001); // Reduced retracement
        }

        // Helper methods for Double Bottom detection
        private bool IsNewPotentialTrough(double currentLow, double localLow)
        {
            return Math.Abs(currentLow - localLow) / localLow < threshold;
        }

        private void InitializeFirstTrough(double currentLow)
        {
            firstTrough = currentLow;
            firstTroughBar = CurrentBar;
            lookingForSecondTrough = true;
            middlePeak = double.MinValue;
            
            if (ShowAnnotations)
            {
                DrawFirstTrough();
            }
        }

        private bool IsPotentialSecondTrough(double currentLow)
        {
            return Math.Abs(currentLow - firstTrough) / firstTrough < threshold && 
                   CurrentBar > firstTroughBar + 2 &&
                   CurrentBar < firstTroughBar + LookbackPeriod;
        }

        private bool IsValidDoubleBottom()
        {
            return secondTrough >= firstTrough * 0.999 && // Allow slightly lower second trough
                   middlePeak > Math.Max(firstTrough, secondTrough) + (Math.Max(firstTrough, secondTrough) * 0.001); // Reduced retracement
        }

        // Drawing methods
        private void DrawFirstPeak()
        {
            Draw.Diamond(this, "FirstPeak" + CurrentBar, true, 0, firstPeak + (2 * TickSize), DoubleTopColor);
            Draw.Text(this, "Peak1" + CurrentBar, true, "P1", 0, firstPeak + (6 * TickSize), 0, 
                     DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
        }

        private void DrawFirstTrough()
        {
            Draw.Diamond(this, "FirstTrough" + CurrentBar, true, 0, firstTrough - (2 * TickSize), DoubleBottomColor);
            Draw.Text(this, "Trough1" + CurrentBar, true, "T1", 0, firstTrough - (6 * TickSize), 0, 
                     DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
        }

        private void DrawDoubleTopPattern()
        {
            // Calculate RELATIVE bar positions (NinjaTrader uses 0 for current bar)
            int firstPeakOffset = CurrentBar - firstPeakBar;
            int secondPeakOffset = CurrentBar - secondPeakBar;
            
            // Draw connecting line between peaks WITH CORRECT OFFSETS
            Draw.Line(this, "TopLine" + CurrentBar, true, 
                    firstPeakOffset, firstPeak, 
                    secondPeakOffset, secondPeak, 
                    DoubleTopColor, DashStyleHelper.Solid, LineThickness);
            
            // Draw second peak marker
            Draw.Diamond(this, "SecondPeak" + CurrentBar, true, 0, secondPeak + (2 * TickSize), DoubleTopColor);
            Draw.Text(this, "Peak2" + CurrentBar, true, "P2", 0, secondPeak + (6 * TickSize), 0, 
                    DoubleTopColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
            
            // Find the middle trough point and convert to RELATIVE bar index
            int middleTroughBar = FindMiddleTroughBar();
            int middleTroughOffset = CurrentBar - middleTroughBar;
            
            // Draw the neckline WITH CORRECT OFFSET
            Draw.Line(this, "NecklineTop" + CurrentBar, true, 
                    middleTroughOffset, middleTrough, 
                    0, middleTrough, 
                    DoubleTopColor, DashStyleHelper.Dash, LineThickness);
                    
            // Draw neckline label at correct position
            Draw.Text(this, "NeckTop" + CurrentBar, true, "NECKLINE", Math.Min(middleTroughOffset, RectangleWidth), 
                    middleTrough - (4 * TickSize), 0, 
                    DoubleTopColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
            
            // Draw entry signal
            Draw.ArrowDown(this, "EntryTop" + CurrentBar, true, 0, middleTrough + (10 * TickSize), DoubleTopColor);
            Draw.Text(this, "EntrySellText" + CurrentBar, true, "SELL", 0, middleTrough + (15 * TickSize), 0, 
                    Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleTopColor, null, 0);
            
            // Draw rectangle with proper width
            if (HighlightPattern)
            {
                // Use maximum of offsets limited by RectangleWidth
                int patternStartOffset = Math.Max(firstPeakOffset, secondPeakOffset);
                int limitedOffset = Math.Min(patternStartOffset, RectangleWidth);
                
                Draw.Rectangle(this, "TopRect" + CurrentBar, true, 
                            limitedOffset, middleTrough - (0.5 * TickSize), 
                            0, Math.Max(firstPeak, secondPeak) + (TickSize), 
                            DoubleTopColor, Brushes.Transparent, 10);
            }
        }

        private int FindMiddleTroughBar()
        {
            // This method should return the absolute bar index, not relative
            int barOffset = 0;
            double closestValue = double.MaxValue;
            
            // Search for the bar with the closest low to middleTrough
            for (int i = Math.Max(0, firstPeakBar - CurrentBar); i <= Math.Min(CurrentBar, secondPeakBar - CurrentBar); i++)
            {
                double diff = Math.Abs(Low[i] - middleTrough);
                if (diff < closestValue)
                {
                    closestValue = diff;
                    barOffset = i;
                }
            }
            
            // Convert from relative to absolute bar index
            return CurrentBar - barOffset;
        }

        private void DrawDoubleTopNeckline(int middleTroughBar)
        {
            Draw.Line(this, "NecklineTop" + CurrentBar, true, 
                     middleTroughBar, middleTrough, 
                     0, middleTrough, 
                     DoubleTopColor, DashStyleHelper.Dash, LineThickness);
                     
            Draw.Text(this, "NeckTop" + CurrentBar, true, "NECKLINE", Math.Max(middleTroughBar - 3, 0), 
                     middleTrough - (4 * TickSize), 0, 
                     DoubleTopColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
        }

        private void DrawDoubleTopEntrySignal()
        {
            Draw.ArrowDown(this, "EntryTop" + CurrentBar, true, 0, middleTrough + (10 * TickSize), DoubleTopColor);
            Draw.Text(this, "EntrySellText" + CurrentBar, true, "SELL", 0, middleTrough + (15 * TickSize), 0, 
                     Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleTopColor, null, 0);
        }

        private void DrawDoubleTopRectangle()
        {
            // Calculate the distance in bars between first peak and current bar
            int firstPeakOffset = CurrentBar - firstPeakBar;
            int secondPeakOffset = CurrentBar - secondPeakBar;
            
            // Use the greater of the two offsets (the earlier pattern point)
            int patternStartOffset = Math.Max(firstPeakOffset, secondPeakOffset);
            
            // Limit to a reasonable number of bars (like 30) to avoid excessive width
            int startOffset = Math.Min(patternStartOffset, 30);
            
            Draw.Rectangle(this, "TopRect" + CurrentBar, true, 
                        startOffset, middleTrough - (0.5 * TickSize), 
                        0, Math.Max(firstPeak, secondPeak) + (TickSize), 
                        DoubleTopColor, Brushes.Transparent, 10);
        }

        private void DrawDoubleBottomPattern()
        {
            // Calculate RELATIVE bar positions
            int firstTroughOffset = CurrentBar - firstTroughBar;
            int secondTroughOffset = CurrentBar - secondTroughBar;
            
            // Draw connecting line between troughs WITH CORRECT OFFSETS
            Draw.Line(this, "BottomLine" + CurrentBar, true, 
                    firstTroughOffset, firstTrough, 
                    secondTroughOffset, secondTrough, 
                    DoubleBottomColor, DashStyleHelper.Solid, LineThickness);
            
            // Draw second trough marker
            Draw.Diamond(this, "SecondTrough" + CurrentBar, true, 0, secondTrough - (2 * TickSize), DoubleBottomColor);
            Draw.Text(this, "Trough2" + CurrentBar, true, "T2", 0, secondTrough - (6 * TickSize), 0, 
                    DoubleBottomColor, new SimpleFont("Arial", 10), TextAlignment.Center, null, null, 0);
            
            // Find the middle peak point and convert to RELATIVE bar index
            int middlePeakBar = FindMiddlePeakBar();
            int middlePeakOffset = CurrentBar - middlePeakBar;
            
            // Draw the neckline WITH CORRECT OFFSET
            Draw.Line(this, "NecklineBottom" + CurrentBar, true, 
                    middlePeakOffset, middlePeak, 
                    0, middlePeak, 
                    DoubleBottomColor, DashStyleHelper.Dash, LineThickness);
                    
            // Draw neckline label at correct position
            Draw.Text(this, "NeckBottom" + CurrentBar, true, "NECKLINE", Math.Min(middlePeakOffset, RectangleWidth), 
                    middlePeak + (4 * TickSize), 0, 
                    DoubleBottomColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
            
            // Draw entry signal
            Draw.ArrowUp(this, "EntryBottom" + CurrentBar, true, 0, middlePeak - (10 * TickSize), DoubleBottomColor);
            Draw.Text(this, "EntryBuyText" + CurrentBar, true, "BUY", 0, middlePeak - (15 * TickSize), 0, 
                    Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleBottomColor, null, 0);
            
            // Draw rectangle with proper width
            if (HighlightPattern)
            {
                // Use maximum of offsets limited by RectangleWidth
                int patternStartOffset = Math.Max(firstTroughOffset, secondTroughOffset);
                int limitedOffset = Math.Min(patternStartOffset, RectangleWidth);
                
                Draw.Rectangle(this, "BottomRect" + CurrentBar, true, 
                            limitedOffset, Math.Min(firstTrough, secondTrough) - (TickSize), 
                            0, middlePeak + (0.5 * TickSize), 
                            DoubleBottomColor, Brushes.Transparent, 10);
            }          
        }

        private int FindMiddlePeakBar()
        {
           // This method should return the absolute bar index, not relative
            int barOffset = 0;
            double closestValue = double.MaxValue;
            
            // Search for the bar with the closest high to middlePeak
            for (int i = Math.Max(0, firstTroughBar - CurrentBar); i <= Math.Min(CurrentBar, secondTroughBar - CurrentBar); i++)
            {
                double diff = Math.Abs(High[i] - middlePeak);
                if (diff < closestValue)
                {
                    closestValue = diff;
                    barOffset = i;
                }
            }
            
            // Convert from relative to absolute bar index
            return CurrentBar - barOffset;
        }

        private void DrawDoubleBottomNeckline(int middlePeakBar)
        {
            Draw.Line(this, "NecklineBottom" + CurrentBar, true, 
                     middlePeakBar, middlePeak, 
                     0, middlePeak, 
                     DoubleBottomColor, DashStyleHelper.Dash, LineThickness);
                     
            Draw.Text(this, "NeckBottom" + CurrentBar, true, "NECKLINE", Math.Max(middlePeakBar - 3, 0), 
                     middlePeak + (4 * TickSize), 0, 
                     DoubleBottomColor, new SimpleFont("Arial", 9), TextAlignment.Center, null, null, 0);
        }

        private void DrawDoubleBottomEntrySignal()
        {
            Draw.ArrowUp(this, "EntryBottom" + CurrentBar, true, 0, middlePeak - (10 * TickSize), DoubleBottomColor);
            Draw.Text(this, "EntryBuyText" + CurrentBar, true, "BUY", 0, middlePeak - (15 * TickSize), 0, 
                     Brushes.White, new SimpleFont("Arial", 10), TextAlignment.Center, DoubleBottomColor, null, 0);
        }

        private void DrawDoubleBottomRectangle()
        {
            // Calculate the distance in bars between first trough and current bar
            int firstTroughOffset = CurrentBar - firstTroughBar;
            int secondTroughOffset = CurrentBar - secondTroughBar;
            
            // Use the greater of the two offsets (the earlier pattern point)
            int patternStartOffset = Math.Max(firstTroughOffset, secondTroughOffset);
            
            // Limit to a reasonable number of bars (like 30) to avoid excessive width
            int startOffset = Math.Min(patternStartOffset, 30);
            
            Draw.Rectangle(this, "BottomRect" + CurrentBar, true, 
                        startOffset, Math.Min(firstTrough, secondTrough) - (TickSize), 
                        0, middlePeak + (0.5 * TickSize), 
                        DoubleBottomColor, Brushes.Transparent, 10);
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
        
        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "Rectangle Width", Description = "Width in bars of the pattern highlight rectangle", Order = 6, GroupName = "Appearance")]
        public int RectangleWidth
        {
            get { return rectangleWidth; }
            set { rectangleWidth = value; }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Top Color", Description = "Color for double top patterns", Order = 7, GroupName = "Appearance")]
        public Brush DoubleTopColor { get; set; }
        
        [Browsable(false)]
        public string DoubleTopColorSerializable
        {
            get { return Serialize.BrushToString(DoubleTopColor); }
            set { DoubleTopColor = Serialize.StringToBrush(value); }
        }
        
        [XmlIgnore]
        [Display(Name = "Double Bottom Color", Description = "Color for double bottom patterns", Order = 8, GroupName = "Appearance")]
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