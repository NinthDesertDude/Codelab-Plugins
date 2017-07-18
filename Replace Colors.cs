#region UICode
ColorBgra Amount1 = ColorBgra.FromBgr(255, 255, 255); //Affected color
ColorBgra Amount2 = ColorBgra.FromBgr(0, 0, 0); //New color
int Amount3 = 0; //[0, 255] Affected color transparency
int Amount4 = 0; //[0, 255] New color transparency
int Amount5 = 10; //[0, 255] Tolerance
int Amount6 = 360; //[0, 360] Max hue difference (color)
int Amount7 = 100; //[0, 100] Max saturation difference (grayness)
int Amount8 = 100; //[0, 100] Max value difference (lightness)
int Amount9 = 0; //[0, 255] Color mixing hardness
#endregion

void Render(Surface dst, Surface src, Rectangle rect)
{
    //Sets up color objects to contain values.
    ColorBgra CurrentPixel;
    HsvColor CurrentPixelHSV, AffectedColorHSV;
    
    for (int y = rect.Top; y < rect.Bottom; y++)
    {
        //Exits if the user cancels.
        if (IsCancelRequested) return;
        
        //The tolerance hardness must be less than the tolerance.
        if (Amount9 < Amount5)
            Amount9 = Amount5;
        
        for (int x = rect.Left; x < rect.Right; x++)
        {
            CurrentPixel = src[x,y];
            CurrentPixelHSV = HsvColor.FromColor(CurrentPixel);
            AffectedColorHSV = HsvColor.FromColor(Amount1);
            
            //Calculates differences between the current pixel and color to replace.
            //These are used to determine if the pixel should be replaced or not.
            //The smaller the difference, the more likely the pixel will be replaced.
            double redDiff = Math.Abs(CurrentPixel.R - Amount1.R);
            double greenDiff = Math.Abs(CurrentPixel.G - Amount1.G);
            double blueDiff = Math.Abs(CurrentPixel.B - Amount1.B);
            double alphaDiff = Math.Abs(CurrentPixel.A - (255 - Amount3));
            double totalDiff = (redDiff + greenDiff + blueDiff + alphaDiff) / 4;
            double hueDiff = Math.Abs(CurrentPixelHSV.Hue - AffectedColorHSV.Hue);
            double satDiff = Math.Abs(CurrentPixelHSV.Saturation - AffectedColorHSV.Saturation);
            double valDiff = Math.Abs(CurrentPixelHSV.Value - AffectedColorHSV.Value);
            
            //If the pixel color differences are small enough, replace it.
            if ((totalDiff <= Amount5) &&
                (hueDiff <= Amount6) &&
                (satDiff <= Amount7) &&
                (valDiff <= Amount8))
            {
                //Use no interpolation if it's a perfect match or color
                //mixing hardness is max.
                if (totalDiff == 0 || Amount9 == 255)
                {
                    //Copies the pixel and transparency.
                    CurrentPixel = Amount2;
                    CurrentPixel.A = (byte)(255 - Amount4);
                }
                
                //Interpolates each channel with a bias based on the tolerance.
                else
                {
                    //The color change intensity = how closely the pixel matched the original color.
                    CurrentPixel.R = (byte)(CurrentPixel.R + ((Amount2.R - CurrentPixel.R) * (1 - (totalDiff / Amount9))));
                    CurrentPixel.G = (byte)(CurrentPixel.G + ((Amount2.G - CurrentPixel.G) * (1 - (totalDiff / Amount9))));
                    CurrentPixel.B = (byte)(CurrentPixel.B + ((Amount2.B - CurrentPixel.B) * (1 - (totalDiff / Amount9))));
                    CurrentPixel.A = (byte)(CurrentPixel.A + (((255 - Amount4) - CurrentPixel.A) * (1 - (totalDiff / Amount9))));
                }
            }
            
            dst[x,y] = CurrentPixel;
        }
    }
}