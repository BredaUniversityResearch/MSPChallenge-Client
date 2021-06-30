using UnityEngine;
using System.Collections;

public static class BlurUtil
{
    // source: https://forum.unity3d.com/threads/contribution-texture2d-blur-in-c.185694/
    // modified to work on Alpha8 format

    private static float avgA = 0;
    private static float blurPixelCount = 0;

    public static Texture2D FastBlur(Texture2D image, int radius, int iterations)
    {
        Texture2D tex = image;

        for (var i = 0; i < iterations; i++)
        {
            tex = BlurImage(tex, radius, true);
            tex = BlurImage(tex, radius, false);
        }

        return tex;
    }

    static Texture2D BlurImage(Texture2D image, int blurSize, bool horizontal)
    {
        Texture2D blurred = new Texture2D(image.width, image.height, TextureFormat.Alpha8, false);
        int _W = image.width;
        int _H = image.height;
        int xx, yy, x, y;

        if (horizontal)
        {
            for (yy = 0; yy < _H; yy++)
            {
                for (xx = 0; xx < _W; xx++)
                {
                    ResetPixel();

                    //Right side of pixel
                    for (x = xx; (x < xx + blurSize && x < _W); x++)
                    {
                        AddPixel(image.GetPixel(x, yy));
                    }

                    //Left side of pixel
                    for (x = xx; (x > xx - blurSize && x > 0); x--)
                    {
                        AddPixel(image.GetPixel(x, yy));
                    }

                    CalcPixel();

                    for (x = xx; x < xx + blurSize && x < _W; x++)
                    {
                        blurred.SetPixel(x, yy, new Color(0, 0, 0, avgA));
                    }
                }
            }
        }

        else
        {
            for (xx = 0; xx < _W; xx++)
            {
                for (yy = 0; yy < _H; yy++)
                {
                    ResetPixel();

                    //Over pixel
                    for (y = yy; (y < yy + blurSize && y < _H); y++)
                    {
                        AddPixel(image.GetPixel(xx, y));
                    }

                    //Under pixel
                    for (y = yy; (y > yy - blurSize && y > 0); y--)
                    {
                        AddPixel(image.GetPixel(xx, y));
                    }

                    CalcPixel();

                    for (y = yy; y < yy + blurSize && y < _H; y++)
                    {
                        blurred.SetPixel(xx, y, new Color(0, 0, 0, avgA));
                    }
                }
            }
        }

        blurred.Apply();
        return blurred;
    }

    static void AddPixel(Color pixel)
    {
        avgA += pixel.a;
        blurPixelCount++;
    }

    static void ResetPixel()
    {
        avgA = 0.0f;
        blurPixelCount = 0;
    }

    static void CalcPixel()
    {
        avgA = avgA / blurPixelCount;
    }
}
