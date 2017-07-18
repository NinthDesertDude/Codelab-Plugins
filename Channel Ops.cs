#region UICode
byte Amount1 = 0; // [1] Channel operation|Invert|Overwrite|Set|Add|Subtract|Multiply|Divide|Make less than|Make greater than|Truncate|Contrast
byte Amount2 = 0; // Channel|Red|Green|Blue|Alpha|Hue|Saturation|Value|Rgb|Hsv
byte Amount3 = 0; // Channel 2|Red|Green|Blue|Alpha|Hue|Saturation|Value
int Amount4 = 0; // [0,255] Amount
#endregion

//Simply rounds a number to the nearest 'roundTo' point.
//roundTo is an absolute value, so negative numbers are ignored.
float Round(float roundTo, float data)
{
    data = (int)Math.Round(data / roundTo) * roundTo;
    return data;
}

//This function finds the max value of a range of floats.
private float Max(params float[] numbers)
{
    float max = Single.MinValue;

    foreach (float num in numbers)
    {
        if (num > max)
        {
            max = num;
        }
    }
    
    return max;
}

//This function finds the min value of a range of floats.
private float Min(params float[] numbers)
{
    float min = Single.MaxValue;

    foreach (float num in numbers)
    {
        if (num < min)
        {
            min = num;
        }
    }
    
    return min;
}

void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
{
    float min, max, delta;

    min = Min(r, g, b);
    max = Max(r, g, b);
    v = max;

    delta = max - min;

    if (max != 0)
    {
        s = delta / max;
    }
    else
    {
        s = 0;
        h = 0;
        return;
    }

    if (r == max)
    {
        h = (g - b) / delta; // between yellow & magenta
    }
    else if (g == max)
    {
        h = 2 + (b - r) / delta; // between cyan & yellow
    }
    else
    {
        h = 4 + (r - g) / delta; // between magenta & cyan
    }
    h *= 60; // degrees
    if (h < 0)
    {
        h += 360;
    }
}

void HsvToRgb(ref byte r, ref byte g, ref byte b, float h, float s, float v)
{
    int i;
    float rtemp, gtemp, btemp;
    rtemp = (float)r;
    gtemp = (float)g;
    btemp = (float)b;
    float f, p, q, t;

    //if there is no saturation, return a shade of gray.
    if (s == 0)
    {        
        r = (byte)v;
        g = (byte)v;
        b = (byte)v;
        return;
    }

    h /= 60; // sector 0 to 5
    i = (int)Math.Floor(h);
    f = h - i; // factorial part of h
    p = v * (1 - s);
    q = v * (1 - s * f);
    t = v * (1 - s * (1 - f));

    switch (i)
    {
        case 0:
            rtemp = v;
            gtemp = t;
            btemp = p;
            break;
        case 1:
            rtemp = q;
            gtemp = v;
            btemp = p;
            break;
        case 2:
            rtemp = p;
            gtemp = v;
            btemp = t;
            break;
        case 3:
            rtemp = p;
            gtemp = q;
            btemp = v;
            break;
        case 4:
            rtemp = t;
            gtemp = p;
            btemp = v;
            break;
        default:
            rtemp = v;
            gtemp = p;
            btemp = q;
            break;
    }
    
    //Apply the temporary values to the out values.
    r = Int32Util.ClampToByte((int)rtemp);
    g = Int32Util.ClampToByte((int)gtemp);
    b = Int32Util.ClampToByte((int)btemp);
}

//This class simply stores hsv values.
public class ColorHsv
{
    public float H, S, V;
    public byte A;
    
    //The constructor allows you to set all values.
    public ColorHsv(float hue, float saturation, float value, byte alpha)
    {
        H = hue;
        S = saturation;
        V = value;
        A = alpha;
    }
}

//Clamps from 0 to the max value specified while doing operations.
private float ClampToNumber(int operation, float maxVal, params float[] numbers)
{
    float num = numbers[0];

    //Performs an arithmetic operation on the numbers (0 = add, 1 = subtract, 2 = multiply, 3 = divide).
    //The numbers are ensured to be clamped at 0 and the maximum value.
    switch (operation)
    {
        case (0): //addition.
        {
            for (int i = 1; i < numbers.Length; i++)
            {
                if (num + numbers[i] > maxVal)
                {
                    num = maxVal;
                }
                else if (num + numbers[i] < 0)
                {
                    num = 0;
                }
                else
                {
                    num += numbers[i];
                }
            }
            break;
        }
        case (1): //subtraction.
        {
            for (int i = 1; i < numbers.Length; i++)
            {
                if (num - numbers[i] > maxVal)
                {
                    num = maxVal;
                }
                else if (num - numbers[i] < 0)
                {
                    num = 0;
                }
                else
                {
                    num -= numbers[i];
                }
            }
            break;
        }
        case (2): //multiplication.
        {
            for (int i = 1; i < numbers.Length; i++)
            {
                if (num * numbers[i] > maxVal)
                {
                    num = maxVal;
                }
                else if (num * numbers[i] < 0)
                {
                    num = 0;
                }
                else
                {
                    num *= numbers[i];
                }
            }
            break;
        }
        case (3): //division.
        {
            for (int i = 1; i < numbers.Length; i++)
            {
                //Prevents division by zero.
                if (numbers[i] == 0)
                {
                    continue;
                }
                
                if (num / numbers[i] > maxVal)
                {
                    num = maxVal;
                }
                else if (num / numbers[i] < 0)
                {
                    num = 0;
                }
                else
                {
                    num /= numbers[i];
                }
            }
            break;
        }
    }
    
    return num;
}

void Render(Surface dst, Surface src, Rectangle rect)
{
    ColorBgra CurrentPixRGB;
    ColorHsv CurrentPixHSV = new ColorHsv(0, 0, 0, 0);
    for (int y = rect.Top; y < rect.Bottom; y++)
    {
        for (int x = rect.Left; x < rect.Right; x++)
        {
            CurrentPixRGB = src[x,y]; //Deals with the pixel in RGB values.
            
            //Generates the appropriate HSV values.
            RgbToHsv(CurrentPixRGB.R, CurrentPixRGB.G, CurrentPixRGB.B, out CurrentPixHSV.H, out CurrentPixHSV.S, out CurrentPixHSV.V);
            
            //Inverts the channels.
            if (Amount1 == 0)
            {
                switch (Amount2)
                {
                    case(0):
                        CurrentPixRGB.R = (byte)(255 - CurrentPixRGB.R);
                        break;
                    case(1):
                        CurrentPixRGB.G = (byte)(255 - CurrentPixRGB.G);
                        break;
                    case(2):
                        CurrentPixRGB.B = (byte)(255 - CurrentPixRGB.B);
                        break;
                    case(3):
                        CurrentPixRGB.A = (byte)(255 - CurrentPixRGB.A);
                        break;
                    case(4):
                        CurrentPixHSV.H = (360 - CurrentPixHSV.H);
                        break;
                    case(5):
                        CurrentPixHSV.S = (1 - CurrentPixHSV.S);
                        break;
                    case(6):
                        CurrentPixHSV.V = (255 - CurrentPixHSV.V);
                        break;
                    case (7):
                        CurrentPixRGB.R = (byte)(255 - CurrentPixRGB.R);
                        CurrentPixRGB.G = (byte)(255 - CurrentPixRGB.G);
                        CurrentPixRGB.B = (byte)(255 - CurrentPixRGB.B);
                        break;
                    case (8):
                        CurrentPixHSV.H = (360 - CurrentPixHSV.H);
                        CurrentPixHSV.S = (1 - CurrentPixHSV.S);
                        CurrentPixHSV.V = (255 - CurrentPixHSV.V);
                        break;
                }
            }
            //Overwrites the channels.
            else if (Amount1 == 1)
            {
                if (Amount2 == 0) //Source is red.
                {
                    switch (Amount3)
                    {
                        case(1): //green
                            CurrentPixRGB.R = CurrentPixRGB.G;
                            break;
                        case(2): //blue
                            CurrentPixRGB.R = CurrentPixRGB.B;
                            break;
                        case(3): //alpha
                            CurrentPixRGB.R = CurrentPixRGB.A;
                            break;
                        case(4): //hue
                            CurrentPixRGB.R = (byte)(CurrentPixHSV.H * 0.7083f);
                            break;
                        case(5): //saturation
                            CurrentPixRGB.R = (byte)(CurrentPixHSV.S * 255);
                            break;
                        case(6): //value
                            CurrentPixRGB.R = (byte)CurrentPixHSV.V;
                            break;
                    }
                }
                else if (Amount2 == 1) //Source is green.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixRGB.G = CurrentPixRGB.R;
                            break;
                        case(2): //blue
                            CurrentPixRGB.G = CurrentPixRGB.B;
                            break;
                        case(3): //alpha
                            CurrentPixRGB.G = CurrentPixRGB.A;
                            break;
                        case(4): //hue
                            CurrentPixRGB.G = (byte)(CurrentPixHSV.H * 0.7083f);
                            break;
                        case(5): //saturation
                            CurrentPixRGB.G = (byte)(CurrentPixHSV.S * 255);
                            break;
                        case(6): //value
                            CurrentPixRGB.G = (byte)CurrentPixHSV.V;
                            break;
                    }
                }
                else if (Amount2 == 2) //Source is blue.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixRGB.B = CurrentPixRGB.R;
                            break;
                        case(1): //green
                            CurrentPixRGB.B = CurrentPixRGB.G;
                            break;
                        case(3): //alpha
                            CurrentPixRGB.B = CurrentPixRGB.A;
                            break;
                        case(4): //hue
                            CurrentPixRGB.B = (byte)(CurrentPixHSV.H * 0.7083f);
                            break;
                        case(5): //saturation
                            CurrentPixRGB.B = (byte)(CurrentPixHSV.S * 255);
                            break;
                        case(6): //value
                            CurrentPixRGB.B = (byte)CurrentPixHSV.V;
                            break;
                    }
                }
                else if (Amount2 == 3) //Source is alpha.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixRGB.A = CurrentPixRGB.R;
                            break;
                        case(1): //green
                            CurrentPixRGB.A = CurrentPixRGB.G;
                            break;
                        case(2): //blue
                            CurrentPixRGB.A = CurrentPixRGB.B;
                            break;
                        case(4): //hue
                            CurrentPixRGB.A = (byte)(CurrentPixHSV.H * 0.7083f);
                            break;
                        case(5): //saturation
                            CurrentPixRGB.A = (byte)(CurrentPixHSV.S * 255);
                            break;
                        case(6): //value
                            CurrentPixRGB.A = (byte)CurrentPixHSV.V;
                            break;
                    }
                }
                else if (Amount2 == 4) //Source is hue.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixHSV.H = (float)CurrentPixRGB.R * 1.4117f;
                            break;
                        case(1): //green
                            CurrentPixHSV.H = (float)CurrentPixRGB.G * 1.4117f;
                            break;
                        case(2): //blue
                            CurrentPixHSV.H = (float)CurrentPixRGB.B * 1.4117f;
                            break;
                        case(3): //alpha
                            CurrentPixHSV.H = (float)CurrentPixRGB.A * 1.4117f;
                            break;
                        case(5): //saturation
                            CurrentPixHSV.H = CurrentPixHSV.S * 360;
                            break;
                        case(6): //value
                            CurrentPixHSV.H = CurrentPixHSV.V * 1.4117f;
                            break;
                    }
                }
                else if (Amount2 == 5) //Source is saturation.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixHSV.S = ((float)CurrentPixRGB.R / 255.0f);
                            break;
                        case(1): //green
                            CurrentPixHSV.S = ((float)CurrentPixRGB.G / 255.0f);
                            break;
                        case(2): //blue
                            CurrentPixHSV.S = ((float)CurrentPixRGB.B / 255.0f);
                            break;
                        case(3): //alpha
                            CurrentPixHSV.S = ((float)CurrentPixRGB.A / 255.0f);
                            break;
                        case(4): //hue
                            CurrentPixHSV.S = CurrentPixHSV.H / 360.0f;
                            break;
                        case(6): //value
                            CurrentPixHSV.S = CurrentPixHSV.V / 255.0f;
                            break;
                    }
                }
                else if (Amount2 == 6) //Source is value.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixHSV.V = ((float)CurrentPixRGB.R);
                            break;
                        case(1): //green
                            CurrentPixHSV.V = ((float)CurrentPixRGB.G);
                            break;
                        case(2): //blue
                            CurrentPixHSV.V = ((float)CurrentPixRGB.B);
                            break;
                        case(3): //alpha
                            CurrentPixHSV.V = ((float)CurrentPixRGB.A);
                            break;
                        case(4): //hue
                            CurrentPixHSV.V = CurrentPixHSV.H / 1.4117f;
                            break;
                        case(5): //saturation
                            CurrentPixHSV.V = CurrentPixHSV.S * 255;
                            break;
                    }
                }
                else if (Amount2 == 7) //Source is rgb.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixRGB.G = CurrentPixRGB.R;
                            CurrentPixRGB.B = CurrentPixRGB.R;
                            break;
                        case(1): //green
                            CurrentPixRGB.R = CurrentPixRGB.G;
                            CurrentPixRGB.B = CurrentPixRGB.G;
                            break;
                        case(2): //blue
                            CurrentPixRGB.R = CurrentPixRGB.B;
                            CurrentPixRGB.G = CurrentPixRGB.B;
                            break;
                        case(3): //alpha
                            CurrentPixRGB.R = CurrentPixRGB.A;
                            CurrentPixRGB.G = CurrentPixRGB.A;
                            CurrentPixRGB.B = CurrentPixRGB.A;
                            break;
                        case(4): //hue
                            CurrentPixRGB.R = (byte)(CurrentPixHSV.H * 0.7083f);
                            CurrentPixRGB.G = (byte)(CurrentPixHSV.H * 0.7083f);
                            CurrentPixRGB.B = (byte)(CurrentPixHSV.H * 0.7083f);
                            break;
                        case(5): //saturation
                            CurrentPixRGB.R = (byte)(CurrentPixHSV.S * 255);
                            CurrentPixRGB.G = (byte)(CurrentPixHSV.S * 255);
                            CurrentPixRGB.B = (byte)(CurrentPixHSV.S * 255); 
                            break;
                        case(6): //value
                            CurrentPixRGB.R = (byte)(CurrentPixHSV.V);
                            CurrentPixRGB.G = (byte)(CurrentPixHSV.V);
                            CurrentPixRGB.B = (byte)(CurrentPixHSV.V); 
                            break;
                    }
                }
                else if (Amount2 == 8) //Source is hsv.
                {
                    switch (Amount3)
                    {
                        case(0): //red
                            CurrentPixHSV.H = (float)CurrentPixRGB.R * 1.4117f;
                            CurrentPixHSV.S = (float)CurrentPixRGB.R / 255.0f;
                            CurrentPixHSV.V = (float)CurrentPixRGB.R;
                            break;
                        case(1): //green
                            CurrentPixHSV.H = (float)CurrentPixRGB.G * 1.4117f;
                            CurrentPixHSV.S = (float)CurrentPixRGB.G / 255.0f;
                            CurrentPixHSV.V = (float)CurrentPixRGB.G;
                            break;
                        case(2): //blue
                            CurrentPixHSV.H = (float)CurrentPixRGB.B * 1.4117f;
                            CurrentPixHSV.S = (float)CurrentPixRGB.B / 255.0f;
                            CurrentPixHSV.V = (float)CurrentPixRGB.B;
                            break;
                        case(3): //alpha
                            CurrentPixHSV.H = (float)CurrentPixRGB.A * 1.4117f;
                            CurrentPixHSV.S = (float)CurrentPixRGB.A / 255.0f;
                            CurrentPixHSV.V = (float)CurrentPixRGB.A;
                            break;
                        case(4): //hue
                            CurrentPixHSV.S = CurrentPixHSV.H / 360.0f;
                            CurrentPixHSV.V = CurrentPixHSV.H / 1.4117f;
                            break;
                        case(5): //saturation
                            CurrentPixHSV.H = CurrentPixHSV.S * 360;
                            CurrentPixHSV.V = CurrentPixHSV.S * 255;
                            break;
                        case(6): //value
                            CurrentPixHSV.H = CurrentPixHSV.V * 1.4117f;
                            CurrentPixHSV.S = CurrentPixHSV.V / 255.0f;
                            break;
                    }
                }
            }
            //Sets the value of the selected channel.
            else if (Amount1 == 2)
            {
                switch (Amount2)
                {
                    case(0): //red
                        CurrentPixRGB.R = (byte)Amount4;
                        break;
                    case(1): //green
                        CurrentPixRGB.G = (byte)Amount4;
                        break;
                    case(2): //blue
                        CurrentPixRGB.B = (byte)Amount4;
                        break;
                    case(3): //alpha
                        CurrentPixRGB.A = (byte)Amount4;
                        break;
                    case(4): //hue
                        CurrentPixHSV.H = Amount4 * 1.4117f;
                        break;
                    case(5): //saturation
                        CurrentPixHSV.S = (float)Amount4 / 255.0f;
                        break;
                    case(6): //value
                        CurrentPixHSV.V = (float)Amount4;
                        break;
                    case(7): //rgb
                        CurrentPixRGB.R = (byte)Amount4;
                        CurrentPixRGB.G = (byte)Amount4;
                        CurrentPixRGB.B = (byte)Amount4;
                        break;
                    case(8): //hsv
                        CurrentPixHSV.H = Amount4 * 1.4117f;
                        CurrentPixHSV.S = Amount4 / 255.0f;
                        CurrentPixHSV.V = Amount4;
                        break;
                        
                }
            }
            //Adds, Subtracts, Multiplies, and Divides the value of the selected channel.
            //The difference is in which values Amount1 are. See ClampToNumber() for how.
            else if (Amount1 >= 3 && Amount1 <= 6)
            {
                switch (Amount2)
                {                
                    case(0): //red
                        CurrentPixRGB.R = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.R, (float)Amount4);
                        break;
                    case(1): //green
                        CurrentPixRGB.G = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.G, (float)Amount4);
                        break;
                    case(2): //blue
                        CurrentPixRGB.B = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.B, (float)Amount4);
                        break;
                    case(3): //alpha
                        CurrentPixRGB.A = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.A, (float)Amount4);
                        break;
                    case(4): //hue
                        CurrentPixHSV.H = ClampToNumber(Amount1 - 3, 360, CurrentPixHSV.H, (float)Amount4 * 1.4117f);
                        break;
                    case(5): //saturation
                        CurrentPixHSV.S = ClampToNumber(Amount1 - 3, 1, CurrentPixHSV.S, (float)Amount4 / 255.0f);
                        break;
                    case(6): //value
                        CurrentPixHSV.V = ClampToNumber(Amount1 - 3, 255, CurrentPixHSV.V, (float)Amount4);
                        break;
                    case(7): //rgb
                        CurrentPixRGB.R = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.R, (float)Amount4);
                        CurrentPixRGB.G = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.G, (float)Amount4);
                        CurrentPixRGB.B = (byte)ClampToNumber(Amount1 - 3, 255, CurrentPixRGB.B, (float)Amount4);
                        break;
                    case(8): //hsv
                        CurrentPixHSV.H = ClampToNumber(Amount1 - 3, 360, CurrentPixHSV.H, (float)Amount4 * 1.4117f);
                        CurrentPixHSV.S = ClampToNumber(Amount1 - 3, 1, CurrentPixHSV.S, (float)Amount4 / 255.0f);
                        CurrentPixHSV.V = ClampToNumber(Amount1 - 3, 255, CurrentPixHSV.V, (float)Amount4);                        
                        break;
                }
            }
            //Forces all pixels to be no greater than the specified number for a specified channel.
            else if (Amount1 == 7)
            {
                switch (Amount2)
                {
                    case(0): //red
                        if (CurrentPixRGB.R > (byte)Amount4)
                        {
                            CurrentPixRGB.R = (byte)Amount4;
                        }
                        break;
                    case(1): //green
                        if (CurrentPixRGB.G > (byte)Amount4)
                        {
                            CurrentPixRGB.G = (byte)Amount4;
                        }
                        break;
                    case(2): //blue
                        if (CurrentPixRGB.B > (byte)Amount4)
                        {
                            CurrentPixRGB.B = (byte)Amount4;
                        }
                        break;
                    case(3): //alpha
                        if (CurrentPixRGB.A > (byte)Amount4)
                        {
                            CurrentPixRGB.A = (byte)Amount4;
                        }
                        break;
                    case(4): //hue
                        if (CurrentPixHSV.H > (float)Amount4 * 1.4117f)
                        {
                            CurrentPixHSV.H = (float)Amount4 * 1.4117f;
                        }
                        break;
                    case(5): //saturation
                        if (CurrentPixHSV.S > (float)Amount4 / 255.0f)
                        {
                            CurrentPixHSV.S = (float)Amount4 / 255.0f;
                        }
                        break;
                    case(6): //value
                        if (CurrentPixHSV.V > (float)Amount4)
                        {
                            CurrentPixHSV.V = (float)Amount4;
                        }
                        break;
                    case(7): //rgb
                        if (CurrentPixRGB.R > (byte)Amount4)
                        {
                            CurrentPixRGB.R = (byte)Amount4;
                        }
                        if (CurrentPixRGB.G > (byte)Amount4)
                        {
                            CurrentPixRGB.G = (byte)Amount4;
                        }
                        if (CurrentPixRGB.B > (byte)Amount4)
                        {
                            CurrentPixRGB.B = (byte)Amount4;
                        }
                        break;
                    case(8): //hsv
                        if (CurrentPixHSV.H > (float)Amount4 * 1.4117f)
                        {
                            CurrentPixHSV.H = (float)Amount4 * 1.4117f;
                        }
                        if (CurrentPixHSV.S > (float)Amount4 / 255.0f)
                        {
                            CurrentPixHSV.S = (float)Amount4 / 255.0f;
                        }
                        if (CurrentPixHSV.V > (float)Amount4)
                        {
                            CurrentPixHSV.V = (float)Amount4;
                        }
                        break;
                }
            }
            //Forces all pixels to be no less than the specified number for a specified channel.
            else if (Amount1 == 8)
            {
                switch (Amount2)
                {
                    case(0): //red
                        if (CurrentPixRGB.R < (byte)Amount4)
                        {
                            CurrentPixRGB.R = (byte)Amount4;
                        }
                        break;
                    case(1): //green
                        if (CurrentPixRGB.G < (byte)Amount4)
                        {
                            CurrentPixRGB.G = (byte)Amount4;
                        }
                        break;
                    case(2): //blue
                        if (CurrentPixRGB.B < (byte)Amount4)
                        {
                            CurrentPixRGB.B = (byte)Amount4;
                        }
                        break;
                    case(3): //alpha
                        if (CurrentPixRGB.A < (byte)Amount4)
                        {
                            CurrentPixRGB.A = (byte)Amount4;
                        }
                        break;
                    case(4): //hue
                        if (CurrentPixHSV.H < Amount4 * 1.4117f)
                        {
                            CurrentPixHSV.H = Amount4 * 1.4117f;
                        }
                        break;
                    case(5): //saturation
                        if (CurrentPixHSV.S < (float)Amount4 / 255.0f)
                        {
                            CurrentPixHSV.S = (float)Amount4 / 255.0f;
                        }
                        break;
                    case(6): //value
                        if (CurrentPixHSV.V < (float)Amount4)
                        {
                            CurrentPixHSV.V = (float)Amount4;
                        }
                        break;
                    case(7): //rgb
                        if (CurrentPixRGB.R < (byte)Amount4)
                        {
                            CurrentPixRGB.R = (byte)Amount4;
                        }
                        if (CurrentPixRGB.G < (byte)Amount4)
                        {
                            CurrentPixRGB.G = (byte)Amount4;
                        }
                        if (CurrentPixRGB.B < (byte)Amount4)
                        {
                            CurrentPixRGB.B = (byte)Amount4;
                        }
                        break;
                    case(8): //hsv
                        if (CurrentPixHSV.H < (float)Amount4 * 1.4117f)
                        {
                            CurrentPixHSV.H = (float)Amount4 * 1.4117f;
                        }
                        if (CurrentPixHSV.S < (float)Amount4 / 255.0f)
                        {
                            CurrentPixHSV.S = (float)Amount4 / 255.0f;
                        }
                        if (CurrentPixHSV.V < (float)Amount4)
                        {
                            CurrentPixHSV.V = (float)Amount4;
                        }
                        break;
                }
            }
            //Makes all values multiples of Amount4 (truncation).
            else if (Amount1 == 9)
            {
                if (Amount4 != 0) //Prevents division by zero.
                {
                    switch (Amount2)
                    {
                        case(0): //red
                            CurrentPixRGB.R = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.R));
                            break;
                        case(1): //green
                            CurrentPixRGB.G = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.G));
                            break;
                        case(2): //blue
                            CurrentPixRGB.B = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.B));
                            break;
                        case(3): //alpha
                            CurrentPixRGB.A = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.A));
                            break;
                        case(4): //hue
                            CurrentPixHSV.H = Int32Util.ClampToByte((int)Round((float)Amount4 * 1.4117f, (float)CurrentPixHSV.H));
                            break;
                        case(5): //saturation
                            CurrentPixHSV.S = Int32Util.ClampToByte((int)Round((float)Amount4 / 255.0f, (float)CurrentPixHSV.S));
                            break;
                        case(6): //value
                            CurrentPixHSV.V = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixHSV.V));
                            break;
                        case(7): //rgb
                            CurrentPixRGB.R = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.R));
                            CurrentPixRGB.G = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.G));
                            CurrentPixRGB.B = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixRGB.B));
                            break;
                        case(8): //hsv
                            CurrentPixHSV.H = Int32Util.ClampToByte((int)Round((float)Amount4 * 1.4117f, (float)CurrentPixHSV.H));
                            CurrentPixHSV.S = Int32Util.ClampToByte((int)Round((float)Amount4 / 255.0f, (float)CurrentPixHSV.S));
                            CurrentPixHSV.V = Int32Util.ClampToByte((int)Round((float)Amount4, (float)CurrentPixHSV.V));
                            break;
                    }
                }
            }
            //Increases the contrast.
            /*A margin is removed from the low and high end of the channel.
            The low end is shifted down by that margin to become 0.
            The high end is scaled up by the margin*2.
            */
            else if (Amount1 == 10)
            {
                //Prevents division by zero.
                if (Amount4 != 0)
                {
                    float tempVal = 0;
                
                    //Creates a temporary value to avoid byte conversions.
                    switch (Amount2)
                    {
                        case(0):
                            tempVal = (float)CurrentPixRGB.R;
                            break;
                        case(1):
                            tempVal = (float)CurrentPixRGB.G;
                            break;
                        case(2):
                            tempVal = (float)CurrentPixRGB.B;
                            break;
                        case(3):
                            tempVal = (float)CurrentPixRGB.A;
                            break;
                        case(4):
                            tempVal = CurrentPixHSV.H;
                            break;
                        case(5):
                            tempVal = CurrentPixHSV.S;
                            break;
                        case(6):
                            tempVal = CurrentPixHSV.V;
                            break;
                    }

                    //Multiplies everything by a fraction based on Amount4.
                    if (tempVal > 128)
                    {
                        tempVal *= (float)(255.0f / (255.0f - Amount4));
                    }
                    else if (tempVal < 128)
                    {
                        tempVal *= (float)(1 - (Math.Abs(255.0f / (255.0f - Amount4)) - 1));
                    }
                    
                    //Makes sure that nothing is less than 0 or greater than 255.
                    if (tempVal < 0)
                    {
                        tempVal = 0;
                    }
                    else if (tempVal > 255)
                    {
                        tempVal = 255;
                    }
                    
                    //Assigns the temporary value to the selected channel.
                    switch (Amount2)
                    {
                        case(0):
                            CurrentPixRGB.R = (byte)tempVal;
                            break;
                        case(1):
                            CurrentPixRGB.G = (byte)tempVal;
                            break;
                        case(2):
                            CurrentPixRGB.B = (byte)tempVal;
                            break;
                        case(3):
                            CurrentPixRGB.A = (byte)tempVal;
                            break;
                        case(4):
                            CurrentPixHSV.H = tempVal;
                            break;
                        case(5):
                            CurrentPixHSV.S = tempVal;
                            break;
                        case(6):
                            CurrentPixHSV.V = tempVal;
                            break;
                    }
                }                
            }            

            if (Amount2 <= 3 || Amount2 == 7) //Applies the altered pixel if it affected the rgb channels.
            {
                dst[x,y] = CurrentPixRGB;
            }
            else //applies the altered pixel if it affects the hsv channels.
            {
                HsvToRgb(ref CurrentPixRGB.R, ref CurrentPixRGB.G, ref CurrentPixRGB.B, CurrentPixHSV.H, CurrentPixHSV.S, CurrentPixHSV.V);
                dst[x,y] = CurrentPixRGB;
            }
        }
    }
}