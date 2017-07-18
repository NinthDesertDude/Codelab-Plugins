// Name: Joshua Lamusga
// Submenu: 
// Author:
// Title:
// Version:
// Desc:
// Keywords:
// URL:
// Help:
#region UICode
#endregion

void Render(Surface dst, Surface src, Rectangle rect)
{
    for (int y = rect.Top; y < rect.Bottom; y++)
    {
        if (IsCancelRequested)
        {
            return;
        }
        
        for (int x = rect.Left; x < rect.Right; x++)
        {
            dst[x,y] = ColorBgra.FromBgra(0, 0, 0, 0);
        }
    }
}
