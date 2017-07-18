#region UICode
double Amount1 = 1; // [0,10] Sampling range
int Amount2 = 5; // [1,10] Quality
byte Amount3 = 2; // Background Color|Black|Transparent, no image|Transparent, with image
double Amount4 = 0.1; // [0,1] Intensity threshold
double Amount5 = 255; // [0,255] Red threshold
double Amount6 = 255; // [0,255] Green threshold
double Amount7 = 255; // [0,255] Blue threshold
double Amount8 = 255; // [0,255] Alpha threshold
#endregion

void Render(Surface dst, Surface src, Rectangle rect)
{  
    //Creates variables to store the colors, then sets the current one.
    ColorBgra CurrentPixel, Col1, Col2;
    CurrentPixel = ColorBgra.White;
    
    //Random number gen used for random angles. Since sin and cos have a range
    //of -1 to 1, the random angle formula is (2 * randomNum - 1) * amplitude
    //to avoid expensive computations.
    Random rng = new Random();
    
    //Stores a list of all edges detected
    List<int> edgePixelsX = new List<int>();
    List<int> edgePixelsY = new List<int>();
    
    //The for loops iterate through all pixels.
    for (float y = rect.Top; y < rect.Bottom; y++)
    {
        //Cancels by user request.
        if (IsCancelRequested)
        {
            return;
        }
        for (float x = rect.Left; x < rect.Right; x++)
        {
            //The number of samplings taken.        
            for (int i = 0; i < Amount2; i++)
            {
                //Sets the value of the current pixel to be transparent or black.
                //The pixel color will later become white if an edge is detected.
                CurrentPixel = src[(int)x,(int)y];
                    
                double magX = (rng.NextDouble() * 2 - 1) * Amount1;
                double magY = (rng.NextDouble() * 2 - 1) * Amount1;
                
                //Gets the intensities of the pixels.
                Col1 = src.GetBilinearSampleClamped(x, y);
                Col2 = src.GetBilinearSampleClamped(x + (float)magX, y + (float)magY);
                
                if (Amount4 < 1)
                {                
                    //The intensity threshold has been reached.
                    if (Math.Abs(Col1.GetIntensity()- Col2.GetIntensity()) > Amount4)
                    {
                        edgePixelsX.Add((int)x);
                        edgePixelsY.Add((int)y);
                    }
                }
                //If red is being calculated.
                if (Amount5 < 255)
                {
                    //The redness threshold has been reached.
                    if (Math.Abs(Col1.R - Col2.R) > Amount5)
                    {
                        edgePixelsX.Add((int)x);
                        edgePixelsY.Add((int)y);
                    }
                }
                //If green is being calculated.
                if (Amount6 < 255)
                {
                    //The green-ness threshold has been reached.
                    if (Math.Abs(Col1.G - Col2.G) > Amount6)
                    {
                        edgePixelsX.Add((int)x);
                        edgePixelsY.Add((int)y);
                    }
                }
                //If blue is being calculated.
                if (Amount7 < 255)
                {
                    //The blueness threshold has been reached.
                    if (Math.Abs(Col1.B - Col2.B) > Amount7)
                    {
                        edgePixelsX.Add((int)x);
                        edgePixelsY.Add((int)y);
                    }
                }
                //If alpha is being calculated.
                if (Amount8 < 255)
                {
                    //The alpha threshold has been reached.
                    if (Math.Abs(Col1.A - Col2.A) > Amount8)
                    {
                        edgePixelsX.Add((int)x);
                        edgePixelsY.Add((int)y);
                    }
                }
            
                //Applies all changes to the destination surface.
                if (Amount3 == 0) //black
                {
                    dst[(int)x, (int)y] = ColorBgra.Black;
                }
                else if (Amount3 == 1) //transparent, no image
                {
                    dst[(int)x, (int)y] = ColorBgra.Transparent;
                }
                else if (Amount3 == 2) //transparent, with image
                {
                    dst[(int)x, (int)y] = CurrentPixel;
                }
            }
        }
    }
    
    //Iterates through each edge pixel and makes it white.
    for (int i = 0; i < edgePixelsX.Count; i++)
    {
        //Uses white pixels for black backgrounds.
        if (Amount3 == 0)
        {
            dst[edgePixelsX[i], edgePixelsY[i]] = ColorBgra.White;
        }
        //Uses black pixels for other backgrounds.
        else
        {
            dst[edgePixelsX[i], edgePixelsY[i]] = ColorBgra.Black;
        }
    }
}