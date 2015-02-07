/*

 Copyright (c) 2005-2011 Devsense.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Permissions;
using System.Security;
using System.Net;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Reflection;

using PHP.Core;

namespace PHP.Library.Gd2
{
    /// <summary>
    /// Implements PHP functions provided by Gd2 extension.
    /// </summary>
    [ImplementsExtension("gd")]
    public static class PhpGd
    {
        #region GDVersionConstants

        /// <summary>
        /// The GD version PHP was compiled against.
        /// </summary>
        [ImplementsConstant("GD_VERSION")]
        public const string version = "2.0.35";

        /// <summary>
        /// The GD major version PHP was compiled against.
        /// </summary>
        [ImplementsConstant("GD_MAJOR_VERSION")]
        public const int major_version = 2;

        /// <summary>
        /// The GD minor version PHP was compiled against.
        /// </summary>
        [ImplementsConstant("GD_MINOR_VERSION")]
        public const int minor_version = 0;

        /// <summary>
        /// The GD release version PHP was compiled against.
        /// </summary>
        [ImplementsConstant("GD_RELEASE_VERSION")]
        public const int release_version = 35;

        /// <summary>
        /// The GD "extra" version (beta/rc..) PHP was compiled against.
        /// </summary>
        [ImplementsConstant("GD_EXTRA_VERSION")]
        public const string extra_version = ""; //"beta";
        
        /// <summary>
        /// When the bundled version of GD is used this is 1 otherwise its set to 0.
        /// </summary>
        [ImplementsConstant("GD_BUNDLED")]
        public const int bundled = 1;

        #endregion

        #region ImgType

        /// <summary>
        /// Image types enumeration, corresponds to IMGTYPE_ PHP constants.
        /// </summary>
        [Flags]
        public enum ImgType
        {
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_GIF")]
            GIF = 1,
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_JPG")]
            JPG = JPEG,
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_JPEG")]
            JPEG = 2,
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_PNG")]
            PNG = 4,
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_WBMP")]
            WBMP = 8,
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            [ImplementsConstant("IMG_XPM")]
            XPM = 16,

            /// <summary>
            /// A combinanation of IMG_ constants that are supported.
            /// </summary>
            Supported = GIF | JPEG | PNG,

            /// <summary>
            /// UNknown image type.
            /// </summary>
            Unknown = -1
        }

        #endregion

        #region FilledArcStyles

        /// <summary>
        /// Filled Arc Style types enumeration
        /// </summary>
        [Flags]
        public enum FilledArcStyles
        {
            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// This constant has the same value as IMG_ARC_PIE.
            /// </summary>
            [ImplementsConstant("IMG_ARC_ROUNDED")]
            ROUNDED = PIE,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            [ImplementsConstant("IMG_ARC_PIE")]
            PIE = 0,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            [ImplementsConstant("IMG_ARC_CHORD")]
            CHORD = 1,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            [ImplementsConstant("IMG_ARC_NOFILL")]
            NOFILL = 2,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            [ImplementsConstant("IMG_ARC_EDGED")]
            EDGED = 4,
        }

        #endregion

        #region ColorValues

        /// <summary>
        /// Special Image Color values enumeration.
        /// </summary>
        public enum ColorValues
        {
            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            [ImplementsConstant("IMG_COLOR_STYLED")]
            STYLED = -2,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            [ImplementsConstant("IMG_COLOR_BRUSHED")]
            BRUSHED = -3,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            [ImplementsConstant("IMG_COLOR_STYLEDBRUSHED")]
            STYLEDBRUSHED = -4,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            [ImplementsConstant("IMG_COLOR_TILED")]
            TILED = -5,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            [ImplementsConstant("IMG_COLOR_TRANSPARENT")]
            TRANSPARENT = -6
        }

        #endregion

        #region FilterTypes

        /// <summary>
        /// Filled Arc Style types enumeration
        /// </summary>
        public enum FilterTypes
        {
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_NEGATE")]
            NEGATE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_GRAYSCALE")]
            GRAYSCALE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_BRIGHTNESS")]
            BRIGHTNESS,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_CONTRAST")]
            CONTRAST,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_COLORIZE")]
            COLORIZE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_EDGEDETECT")]
            EDGEDETECT,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_EMBOSS")]
            EMBOSS,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_GAUSSIAN_BLUR")]
            GAUSSIAN_BLUR,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_SELECTIVE_BLUR")]
            SELECTIVE_BLUR,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_MEAN_REMOVAL")]
            MEAN_REMOVAL,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_SMOOTH")]
            SMOOTH,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            [ImplementsConstant("IMG_FILTER_PIXELATE")]
            PIXELATE,
        }

        #endregion

        #region gd_info

        /// <summary>
        /// Retrieve information about the currently installed GD library
        /// </summary>
        /// <returns></returns>
        [ImplementsFunction("gd_info")]
        public static PhpArray gd_info()
        {
            PhpArray array = new PhpArray();

            array.Add("GD Version", "bundled (2.0 compatible)");
            array.Add("FreeType Support", true);
            array.Add("FreeType Linkage", "with TTF library");
            array.Add("T1Lib Support", false);
            array.Add("GIF Read Support", true);
            array.Add("GIF Create Support", true);
            array.Add("JPEG Support", true);
            array.Add("JPG Support", true);
            array.Add("PNG Support", true);
            array.Add("WBMP Support", false);
            array.Add("XPM Support", false);
            array.Add("XBM Support", false);
            array.Add("JIS-mapped Japanese Font Support", false); // Maybe is true because of .net unicode strings?

            return array;
        }

        #endregion

        #region image2wbmp

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("image2wbmp", FunctionImplOptions.NotSupported)]
        public static bool image2wbmp(PhpResource im)
        {
            return image2wbmp(im, null, 0);
        }

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("image2wbmp", FunctionImplOptions.NotSupported)]
        public static bool image2wbmp(PhpResource im, string filename)
        {
            return image2wbmp(im, filename, 0);
        }

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("image2wbmp", FunctionImplOptions.NotSupported)]
        public static bool image2wbmp(PhpResource im, string filename, int threshold)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagealphablending

        /// <summary>
        /// Turn alpha blending mode on or off for the given image
        /// </summary> 
        [ImplementsFunction("imagealphablending")]
        public static bool imagealphablending(PhpResource im, bool on)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            // In PHP AlphaBlending is supported only in True color images
            if (!img.IsIndexed)
            {
                img.AlphaBlending = on;
            }

            return false;
        }

        #endregion

        #region imageantialias

        /// <summary>
        /// Should antialiased functions used or not
        /// </summary> 
        [ImplementsFunction("imageantialias")]
        public static bool imageantialias(PhpResource im, bool on)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            img.AntiAlias = on;

            return true;
        }

        #endregion

        #region imagearc

        /// <summary>
        /// Draw a partial ellipse
        /// </summary> 
        [ImplementsFunction("imagearc")]
        public static bool imagearc(PhpResource im, int cx, int cy, int w, int h, int s, int e, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                Pen pen = CreatePen(col, img, false);

                int range = 0;
                AdjustAnglesAndSize(ref w, ref h, ref s, ref e, ref range);

                g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);

                pen.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Adjust angles and size for same behavior as in PHP
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="range"></param>
        private static void AdjustAnglesAndSize(ref int w, ref int h, ref int s, ref int e, ref int range)
        {
            if (w < 0) w = 0;
            if (h < 0) h = 0;

            if (w > 1 && w <= 4) w -= 1;
            if (h > 1 && h <= 4) h -= 1;
            if (w > 4) w -= 2;
            if (h > 4) h -= 2;

            range = e - s;
            if (range < 360) range = range + (range / 360) * 360;
            if (range > 360) range = range - (range / 360) * 360;

            if (s < 360) s = s + (s / 360) * 360;
            if (e < 360) e = e + (e / 360) * 360;

            if (s < 0) s = 360 + s;
            if (e < 0) e = 360 + e;

            if (e > 360) e = e - (e / 360) * 360;
            if (s > 360) e = e - (e / 360) * 360;
        }

        #endregion

        #region imagechar

        /// <summary>
        /// Draw a character
        /// </summary> 
        [ImplementsFunction("imagechar")]
        public static bool imagechar(PhpResource im, int font, int x, int y, string c, int col)
        {
            return imagestring(im, font, x, y, c[0].ToString(), col);
        }

        #endregion

        #region imagecharup

        /// <summary>
        /// Draw a character rotated 90 degrees counter-clockwise
        /// </summary> 
        [ImplementsFunction("imagecharup", FunctionImplOptions.NotSupported)]
        public static bool imagecharup(PhpResource im, int font, int x, int y, string c, int col)
        {
            //return imagestringup(im, font, x, y, c[0].ToString(), col);
            return false;
        }

        #endregion

        #region imagecolorallocate

        /// <summary>
        /// Allocate a color for an image
        /// </summary> 
        [ImplementsFunction("imagecolorallocate")]
        public static int imagecolorallocate(PhpResource im, int red, int green, int blue)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return 0;   // TODO: (maros) check if it is 0 in PHP
            
            //TODO: (Maros) In non-truecolor images (palette images) have little more function.
            int color = RGBToPHPColor(red, green, blue);

            return color;
        }

        /// <summary>
        /// RGB values to PHP Color format
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        private static int RGBToPHPColor(int red, int green, int blue)
        {
            int color = 0; // = 0x00 << 24;

            color = color | blue & 0x0000FF;
            color = color | ((green & 0x0000FF) << 8);
            color = color | ((red & 0x0000FF) << 16);
            return color;
        }

        /// <summary>
        /// Converts PHP Color format to .NET Color format (different alpha meaning)
        /// </summary>
        /// <param name="color">PHP Color</param>
        /// <returns>.NET Color</returns>
        private static Color PHPColorToNETColor(int color)
        {
            if (color == (int)ColorValues.TRANSPARENT)
            {
                return Color.Transparent;
            }

            Color col;

            int alpha = PHPColorToPHPAlpha(color);
            int red = PHPColorToRed(color);
            int green = PHPColorToGreen(color);
            int blue = PHPColorToBlue(color);

            // PHP Alpha format to .NET Alpha format
            alpha = (byte)((1.0f - ((float)alpha / 127.0f)) * 255.0f);

            col = Color.FromArgb(alpha, red, green, blue);

            return col;
        }

        #endregion

        #region imagecolorallocatealpha

        /// <summary>
        /// Allocate a color with an alpha level.  Works for true color and palette based images.
        /// </summary> 
        [ImplementsFunction("imagecolorallocatealpha")]
        public static int imagecolorallocatealpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return 0;   // TODO: (maros) chekc if it is 0 in PHP
            
            //TODO: (Maros) In non-truecolor images (palette images) have little more function.

            return RGBAToPHPColor(red, green, blue, alpha);
        }

        /// <summary>
        /// RGBA values to PHP Color format.
        /// </summary>
        private static int RGBAToPHPColor(int red, int green, int blue, int alpha)
        {
            int color = alpha << 24;

            color = color | blue & 0x0000FF;
            color = color | ((green & 0x0000FF) << 8);
            color = color | ((red & 0x0000FF) << 16);
            return color;
        }

        #endregion

        #region imagecolorat

        /// <summary>
        /// Get the index of the color of a pixel
        /// </summary> 
        [ImplementsFunction("imagecolorat")]
        public static int imagecolorat(PhpResource im, int x, int y)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;  // TODO: (maros) check if it is -1 in PHP
            
            return NETColorToPHPColor(img.Image.GetPixel(x, y));
        }

        /// <summary>
        /// Converts .NET Color format to PHP Color format
        /// </summary>
        /// <param name="col">.NET Color</param>
        /// <returns>PHP Color</returns>
        private static int NETColorToPHPColor(Color col)
        {
            int alpha = col.A;

            int color = RGBToPHPColor(col.R, col.G, col.B);

            // PHP Alpha format to .NET Alpha format
            alpha = (byte)((1.0f - ((float)alpha / 255.0f)) * 127.0f);
            alpha = alpha << 24;

            return color | alpha;
        }

        #endregion

        #region imagecolorclosest

        /// <summary>
        /// Get the index of the closest color to the specified color
        /// </summary> 
        [ImplementsFunction("imagecolorclosest", FunctionImplOptions.NotSupported)]
        public static int imagecolorclosest(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolorclosestalpha

        /// <summary>
        /// Find the closest matching colour with alpha transparency
        /// </summary> 
        [ImplementsFunction("imagecolorclosestalpha", FunctionImplOptions.NotSupported)]
        public static int imagecolorclosestalpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolorclosesthwb

        /// <summary>
        /// Get the index of the color which has the hue, white and blackness nearest to the given color
        /// </summary> 
        [ImplementsFunction("imagecolorclosesthwb", FunctionImplOptions.NotSupported)]
        public static int imagecolorclosesthwb(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolordeallocate

        /// <summary>
        /// De-allocate a color for an image
        /// </summary> 
        [ImplementsFunction("imagecolordeallocate", FunctionImplOptions.NotSupported)]
        public static bool imagecolordeallocate(PhpResource im, int index)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagecolorexact

        /// <summary>
        /// Get the index of the specified color
        /// </summary> 
        [ImplementsFunction("imagecolorexact", FunctionImplOptions.NotSupported)]
        public static int imagecolorexact(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolorexactalpha

        /// <summary>
        /// Find exact match for colour with transparency
        /// </summary> 
        [ImplementsFunction("imagecolorexactalpha", FunctionImplOptions.NotSupported)]
        public static int imagecolorexactalpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolormatch

        /// <summary>
        /// Makes the colors of the palette version of an image more closely match the true color version
        /// </summary> 
        [ImplementsFunction("imagecolormatch", FunctionImplOptions.NotSupported)]
        public static bool imagecolormatch(PhpResource im1, PhpResource im2)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagecolorresolve

        /// <summary>
        /// Get the index of the specified color or its closest possible alternative
        /// </summary> 
        [ImplementsFunction("imagecolorresolve", FunctionImplOptions.NotSupported)]
        public static int imagecolorresolve(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolorresolvealpha

        /// <summary>
        /// Resolve/Allocate a colour with an alpha level.  Works for true colour and palette based images
        /// </summary> 
        [ImplementsFunction("imagecolorresolvealpha", FunctionImplOptions.NotSupported)]
        public static int imagecolorresolvealpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagecolorset

        /// <summary>
        /// Set the color for the specified palette index
        /// </summary> 
        [ImplementsFunction("imagecolorset", FunctionImplOptions.NotSupported)]
        public static void imagecolorset(PhpResource im, int col, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
        }

        #endregion

        #region imagecolorsforindex

        /// <summary>
        /// Get the colors for an index
        /// </summary> 
        [ImplementsFunction("imagecolorsforindex", FunctionImplOptions.NotSupported)]
        public static PhpArray imagecolorsforindex(PhpResource im, int col)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecolorstotal

        /// <summary>
        /// Find out the number of colors in an image's palette
        /// </summary> 
        [ImplementsFunction("imagecolorstotal")]
        public static int imagecolorstotal(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return 0;

            var format = img.Image.PixelFormat;

            if ((format & PixelFormat.Format1bppIndexed) != 0)
                return 2;
            if ((format & PixelFormat.Format4bppIndexed) != 0)
                return 16;
            if ((format & PixelFormat.Format8bppIndexed) != 0)
                return 256;

            if ((format & PixelFormat.Indexed) != 0)
            {
                // count the palette
                try
                {
                    // TODO: optimize, cache ?
                    return img.Image.Palette.Entries.Length;
                }
                catch
                {
                    // ignored, some error during SafeNativeMethods.Gdip.GdipGetImagePalette
                }
            }

            // non indexed image
            return 0;
        }

        #endregion

        #region imagecolortransparent

        /// <summary>
        /// Define a color as transparent
        /// </summary> 
        [ImplementsFunction("imagecolortransparent")]
        public static int imagecolortransparent(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1; // TODO: (maros) check if it is -1 in PHP
            

            if (img.IsTransparentColSet == false)
            {
                return -1;
            }

            return NETColorToPHPColor(img.transparentColor);
        }

        /// <summary>
        /// Define a color as transparent
        /// </summary> 
        [ImplementsFunction("imagecolortransparent")]
        public static int imagecolortransparent(PhpResource im, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1; // TODO: (maros) chekc if it is -1 in PHP
            

            img.transparentColor = PHPColorToNETColor(col);
            img.IsTransparentColSet = true;

            return col;
        }

        #endregion

        #region imageconvolution

        /// <summary>
        /// Apply a 3x3 convolution matrix, using coefficient div and offset
        /// </summary> 
        [ImplementsFunction("imageconvolution", FunctionImplOptions.NotSupported)]
        public static PhpResource imageconvolution(PhpResource src_im, PhpArray matrix3x3, double div, double offset)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecopy

        /// <summary>
        /// Copy part of an image
        /// </summary> 
        [ImplementsFunction("imagecopy")]
        public static bool imagecopy(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h)
        {
            return CopyImageTransparent(dst_im, src_im, dst_x, dst_y, src_x, src_y, src_w, src_h, 100);
        }

        #endregion

        #region imagecopymerge

        /// <summary>
        /// Merge one part of an image with another
        /// </summary> 
        [ImplementsFunction("imagecopymerge")]
        public static bool imagecopymerge(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct)
        {
            return CopyImageTransparent(dst_im, src_im, dst_x, dst_y, src_x, src_y, src_w, src_h, pct);
        }

        private static bool CopyImageTransparent(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct)
        {
            PhpGdImageResource dst_img = PhpGdImageResource.ValidImage(dst_im);
            if (dst_img == null)
                return false;

            PhpGdImageResource src_img = PhpGdImageResource.ValidImage(src_im);
            if (src_img == null)
                return false;            

            if (src_w < 0) src_w = 0;
            if (src_h < 0) src_h = 0;

            if (src_w == 0 && src_h == 0)
            {
                return true;
            }

            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;

            try
            {

                if (pct == 100)
                {
                    Graphics g = Graphics.FromImage(dst_img.Image);
                    g.DrawImage(src_img.Image, dst_x, dst_y, new Rectangle(src_x, src_y, src_w, src_h),
                        GraphicsUnit.Pixel);
                    g.Dispose();
                }
                else
                {
                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix00 = cm.Matrix11 = cm.Matrix22 = cm.Matrix44 = ((float)pct / 100.0f);
                    cm.Matrix33 = 1.0f;

                    ImageAttributes ia = new ImageAttributes();
                    ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    Graphics g = Graphics.FromImage(dst_img.Image);
                    g.DrawImage(src_img.Image, new Rectangle(src_x, src_y, src_w, src_h), dst_x, dst_y,
                        src_w, src_h, GraphicsUnit.Pixel, ia);

                    ia.Dispose();
                    g.Dispose();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion

        #region imagecopymergegray

        /// <summary>
        /// Merge one part of an image with another
        /// </summary> 
        [ImplementsFunction("imagecopymergegray", FunctionImplOptions.NotSupported)]
        public static bool imagecopymergegray(PhpResource src_im, PhpResource dst_im,
            int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagecopyresampled

        /// <summary>
        /// Copy and resize part of an image using resampling to help ensure clarity
        /// </summary> 
        [ImplementsFunction("imagecopyresampled")]
        public static bool imagecopyresampled(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h)
        {
            return ImageCopyAndResize(dst_im, src_im, dst_x, dst_y, src_x, src_y, dst_w, dst_h,
                src_w, src_h, InterpolationMode.HighQualityBicubic);
        }

        #endregion

        #region imagecopyresized

        /// <summary>
        /// Copy and resize part of an image
        /// </summary> 
        [ImplementsFunction("imagecopyresized")]
        public static bool imagecopyresized(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h)
        {
            return ImageCopyAndResize(dst_im, src_im, dst_x, dst_y, src_x, src_y, dst_w, dst_h,
                src_w, src_h, InterpolationMode.NearestNeighbor);
        }

        private static bool ImageCopyAndResize(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h,
            int src_w, int src_h, InterpolationMode mode)
        {
            PhpGdImageResource dst_img = PhpGdImageResource.ValidImage(dst_im);
            if (dst_img == null)
                return false;

            PhpGdImageResource src_img = PhpGdImageResource.ValidImage(src_im);
            if (src_img == null)
                return false;            

            //if (src_w == 0 && src_h == 0)
            //    return true;

            if (dst_w == 0 || dst_h == 0)
                return true;
            
            //if (dst_w < 0) dst_w = 0;
            //if (dst_h < 0) dst_h = 0;
            
            using (Graphics g = Graphics.FromImage(dst_img.Image))
            {
                g.InterpolationMode = mode;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(src_img.Image, new Rectangle(dst_x, dst_y, dst_w, dst_h),
                    new Rectangle(src_x, src_y, src_w, src_h), GraphicsUnit.Pixel);
            }

            return true;
        }

        #endregion

        #region imagecreate

        /// <summary>
        /// Create a new image
        /// </summary> 
        [ImplementsFunction("imagecreate")]
        [return: CastToFalse]
        public static PhpResource imagecreate(int x_size, int y_size)
        {
            if (x_size <= 0 || y_size <= 0)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Utils.Resources.GetString("invalid_image_dimensions")));
                return null;
            }

            PhpGdImageResource img = new PhpGdImageResource(x_size, y_size);
            if (img == null) return null;

            // Draw white background
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                SolidBrush brush = new SolidBrush(Color.White);
                g.FillRectangle(brush, 0, 0, img.Image.Width, img.Image.Height);
                brush.Dispose();
            }

            //TODO: (Maros) This function should create palette based image.
            // NOTE: (J) indexed image is created in Bitmap constructor, by providing PixelFormat.Indexed
            //img.IsTrueColor = false;

            return img;
        }

        #endregion

        #region imagecreatefromgd

        /// <summary>
        /// Create a new image from GD file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromgd", FunctionImplOptions.NotSupported)]
        public static PhpResource imagecreatefromgd(string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecreatefromgd2

        /// <summary>
        /// Create a new image from GD2 file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromgd2", FunctionImplOptions.NotSupported)]
        public static PhpResource imagecreatefromgd2(string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecreatefromgd2part

        /// <summary>
        /// Create a new image from a given part of GD2 file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromgd2part", FunctionImplOptions.NotSupported)]
        public static PhpResource imagecreatefromgd2part(string filename, int srcX, int srcY, int width, int height)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecreatefromgif

        /// <summary>
        /// Create a new image from GIF file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromgif")]
        [return: CastToFalse]
        public static PhpResource imagecreatefromgif(string filename)
        {
            return CreateGdImageFrom(filename, ImageFormat.Gif);
        }

        #endregion

        #region imagecreatefromjpeg

        /// <summary>
        /// Create a new image from JPEG file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromjpeg")]
        [return: CastToFalse]
        public static PhpResource imagecreatefromjpeg(string filename)
        {
            return CreateGdImageFrom(filename, ImageFormat.Jpeg);
        }

        #endregion

        #region imagecreatefrompng

        /// <summary>
        /// Create a new image from PNG file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefrompng")]
        [return: CastToFalse]
        public static PhpResource imagecreatefrompng(string filename)
        {
            return CreateGdImageFrom(filename, ImageFormat.Png);
        }

        #endregion

        #region imagecreatefromstring

        /// <summary>
        /// Create a new image from the image stream in the string
        /// </summary> 
        [ImplementsFunction("imagecreatefromstring")]
        [return: CastToFalse]
        public static PhpResource imagecreatefromstring(PhpBytes image)
        {
            if (image == null)
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("empty_string_or_invalid_image"));
                return null;
            }

            PhpGdImageResource res;

            try
            {
                MemoryStream stream = new MemoryStream(image.ReadonlyData);
                Image img = Image.FromStream(stream, true, false);

                res = new PhpGdImageResource(img);
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("empty_string_or_invalid_image"));
                return null;
            }

            return res;
        }

        #endregion

        #region imagecreatefromwbmp

        /// <summary>
        /// Create a new image from WBMP file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromwbmp")]
        public static PhpResource imagecreatefromwbmp(string filename)
        {
            PhpResource resource = CreateGdImageFrom(filename, null);
            return null;
        }

        #endregion

        #region imagecreatefromxbm

        /// <summary>
        /// Create a new image from XBM file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromxbm", FunctionImplOptions.NotSupported)]
        public static PhpResource imagecreatefromxbm(string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecreatefromxpm

        /// <summary>
        /// Create a new image from XPM file or URL
        /// </summary> 
        [ImplementsFunction("imagecreatefromxpm", FunctionImplOptions.NotSupported)]
        public static PhpResource imagecreatefromxpm(string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagecreatetruecolor

        /// <summary>
        /// Create a new true color image
        /// </summary> 
        [ImplementsFunction("imagecreatetruecolor")]
        [return: CastToFalse]
        public static PhpResource imagecreatetruecolor(int x_size, int y_size)
        {
            if (x_size <= 0 || y_size <= 0)
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("invalid_image_dimensions"));
                return null;
            }

            PhpGdImageResource img = new PhpGdImageResource(x_size, y_size);
            if (img == null) return null;

            // Draw black background
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                SolidBrush brush = new SolidBrush(Color.Black);
                g.FillRectangle(brush, 0, 0, img.Image.Width, img.Image.Height);
                brush.Dispose();
            }

            img.AlphaBlending = true;

            return img;
        }

        #endregion

        #region imagedashedline

        /// <summary>
        /// Draw a dashed line (DEPRECATED in PHP)
        /// </summary> 
        [ImplementsFunction("imagedashedline")]
        public static bool imagedashedline(PhpResource im, int x1, int y1, int x2, int y2, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;
                Pen pen = CreatePen(col, img, false);
                pen.DashStyle = DashStyle.Dash;
                g.DrawLine(pen, x1, y1, x2, y2);
                pen.Dispose();
            }

            return true;
        }

        #endregion

        #region imagedestroy

        /// <summary>
        /// Destroy an image
        /// </summary> 
        [ImplementsFunction("imagedestroy")]
        public static bool imagedestroy(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            img.Dispose();

            return true;
        }

        #endregion

        #region imageellipse

        /// <summary>
        /// Draw an ellipse
        /// </summary> 
        [ImplementsFunction("imageellipse")]
        public static bool imageellipse(PhpResource im, int cx, int cy, int w, int h, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                SetAntiAlias(img, g);
                Pen pen = CreatePen(col, img, false);
                pen.Width = 1;
                g.DrawEllipse(pen, cx - (w / 2), cy - (h / 2), w, h);
                pen.Dispose();
            }

            return true;
        }

        #endregion

        #region imagefill

        /// <summary>
        /// Flood fill
        /// </summary> 
        [ImplementsFunction("imagefill")]
        public static bool imagefill(PhpResource im, int x, int y, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (x < 0 || y < 0) return true;
            if (x > img.Image.Width || x > img.Image.Height) return true;

            //TODO: (Maros) COLOR_TILED is not implemented.

            //TODO: Can be optimized.
            FloodFill(img.Image, x, y, PHPColorToNETColor(col), false, Color.Red);

            return true;
        }

        #endregion

        #region imagefilledarc

        /// <summary>
        /// Draw a filled partial ellipse
        /// </summary> 
        [ImplementsFunction("imagefilledarc")]
        public static bool imagefilledarc(PhpResource im, int cx, int cy, int w, int h, int s, int e, int col, int style)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;
                Pen pen = CreatePen(col, img, false);

                int range = 0;
                AdjustAnglesAndSize(ref w, ref h, ref s, ref e, ref range);

                // IMG_ARC_PIE
                if (style == (int)FilledArcStyles.PIE || style == (int)FilledArcStyles.EDGED)
                {
                    g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
                    using (SolidBrush brush = new SolidBrush(GetAlphaColor(img, col)))
                    {
                        g.FillPie(brush, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
                    }
                }

                if (style == (int)FilledArcStyles.NOFILL)
                {
                    g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
                }

                if (style == ((int)FilledArcStyles.EDGED | (int)FilledArcStyles.NOFILL))
                {
                    Point[] points = { 
                                             new Point(cx+(int)(Math.Cos(s*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(s*Math.PI/180) * (h / 2.0))),
                                             new Point(cx, cy),
                                             new Point(cx+(int)(Math.Cos(e*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(e*Math.PI/180) * (h / 2.0)))
                                         };

                    g.DrawLines(pen, points);
                    g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
                }

                // IMG_ARC_CHORD
                if (style == ((int)FilledArcStyles.CHORD) || style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.EDGED))
                {
                    using (SolidBrush brush = new SolidBrush(GetAlphaColor(img, col)))
                    {
                        Point point1 = new Point(cx + (int)(Math.Cos(s * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(s * Math.PI / 180) * (h / 2.0)));
                        Point point2 = new Point(cx + (int)(Math.Cos(e * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(e * Math.PI / 180) * (h / 2.0)));

                        Point[] points = { new Point(cx, cy), point1, point2 };

                        //pen.LineJoin = LineJoin.Bevel;
                        //g.DrawPolygon(pen, points);
                        //g.DrawLine(pen, point1, point2);
                        g.FillPolygon(brush, points);
                    }
                }

                if (style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.NOFILL))
                {
                    g.DrawLine(pen,
                        new Point(cx + (int)(Math.Cos(s * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(s * Math.PI / 180) * (h / 2.0))),
                        new Point(cx + (int)(Math.Cos(e * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(e * Math.PI / 180) * (h / 2.0)))
                        );
                }

                if (style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.NOFILL | (int)FilledArcStyles.EDGED))
                {
                    Point[] points = { 
                                             new Point(cx, cy), 
                                             new Point(cx+(int)(Math.Cos(s*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(s*Math.PI/180) * (h / 2.0))), 
                                             new Point(cx+(int)(Math.Cos(e*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(e*Math.PI/180) * (h / 2.0)))
                                         };

                    //pen.LineJoin = LineJoin.Bevel;
                    g.DrawPolygon(pen, points);
                }

                pen.Dispose();
            }

            return true;
        }

        #endregion

        #region imagefilledellipse

        /// <summary>
        /// Draw an ellipse
        /// </summary> 
        [ImplementsFunction("imagefilledellipse")]
        public static bool imagefilledellipse(PhpResource im, int cx, int cy, int w, int h, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;

                if (img.tiled != null)
                {
                    g.FillEllipse(img.tiled, cx - (w / 2), cy - (h / 2), w, h);
                }
                else
                {
                    SolidBrush brush = new SolidBrush(GetAlphaColor(img, col));
                    g.FillEllipse(brush, cx - (w / 2), cy - (h / 2), w, h);
                    brush.Dispose();
                }
            }

            return true;
        }

        #endregion

        #region imagefilledpolygon

        /// <summary>
        /// Draw a filled polygon
        /// </summary> 
        [ImplementsFunction("imagefilledpolygon")]
        public static bool imagefilledpolygon(PhpResource im, PhpArray point, int num_points, int col)
        {
            return DrawPoly(im, point, num_points, col, true);
        }

        /// <summary>
        /// Draws normal or filled polygon
        /// </summary>
        /// <param name="im"></param>
        /// <param name="point"></param>
        /// <param name="num_points"></param>
        /// <param name="col"></param>
        /// <param name="filled"></param>
        /// <returns></returns>
        private static bool DrawPoly(PhpResource im, PhpArray point, int num_points, int col, bool filled)
        {
            if (im == null)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unexpected_arg_given", 1, PhpResource.PhpTypeName, PhpVariable.TypeNameNull.ToLowerInvariant()));
                return false;
            }
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (point == null)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unexpected_arg_given", 2, PhpArray.PhpTypeName, PhpVariable.TypeNameNull.ToLowerInvariant()));
                return false;
            }

            
            if (point.Count < num_points * 2)
                return false;
            
            if (num_points <= 0)
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("must_be_positive_number_of_points"));
                return false;
            }

            Point[] points = new Point[num_points];

            for (int i = 0, j = 0; i < num_points; i++, j += 2)
                points[i] = new Point(PHP.Core.Convert.ObjectToInteger(point[j]), PHP.Core.Convert.ObjectToInteger(point[j + 1]));
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;

                if (filled)
                {
                    if (col < 0)
                    {
                        if (col == (int)ColorValues.TILED)
                        {
                            if (img.tiled != null)
                            {
                                //TODO: (Maros) TILED filles little more width in PHP (pixel wider).
                                g.FillPolygon(img.tiled, points);
                            }
                            return true;
                        }
                        //TODO: (Maros) BRUSHED_STYLED missing, BRUSHED and BRUSHED_STYLED has different behavior in PHP (brush image draw for every pixel drawn)
                        //TODO: (Maros) STYLED has little different look (different angle of lines etc.)
                        else if (col == -2 && img.styled != null)
                        {
                            g.FillPolygon(img.styled, points);
                        }
                        else if (col == -3 && img.brushed != null)
                        {
                            g.FillPolygon(img.brushed, points);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        using (SolidBrush brush = new SolidBrush(PHPColorToNETColor(col)))
                        {
                            g.FillPolygon(brush, points);
                        }
                    }

                    using (Pen pen = CreatePen(col, img, false))
                    {
                        pen.LineJoin = LineJoin.Bevel;
                        g.DrawPolygon(pen, points);
                    }
                }
                else
                {
                    using (Pen pen = CreatePen(col, img, true))
                    {
                        SetAntiAlias(img, g);

                        pen.LineJoin = LineJoin.Bevel;
                        g.DrawPolygon(pen, points);
                    }
                }

            }

            return true;
        }

        #endregion

        #region imagefilledrectangle

        /// <summary>
        /// Draw a filled rectangle
        /// </summary> 
        [ImplementsFunction("imagefilledrectangle")]
        public static bool imagefilledrectangle(PhpResource im, int x1, int y1, int x2, int y2, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            // PHP adds 1 more pixel to the right bottom
            x2++;
            y2++;

            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;

                if (col == (int)ColorValues.TILED)
                {
                    if (img.tiled == null)
                        return true;

                    TextureBrush brush = img.tiled;
                    g.FillRectangle(brush, x1, y1, x2 - x1, y2 - y1);
                }
                else
                {
                    Color color = GetAlphaColor(img, col);
                    SolidBrush brush = new SolidBrush(color);
                    g.FillRectangle(brush, x1, y1, x2 - x1, y2 - y1);
                    brush.Dispose();
                }

                //g.FillRectangle(brush, x1, y1, x2 - x1, y2 - y1);
            }

            return true;
        }

        #endregion

        #region imagefilltoborder

        /// <summary>
        /// Flood fill to specific color
        /// </summary> 
        [ImplementsFunction("imagefilltoborder")]
        public static bool imagefilltoborder(PhpResource im, int x, int y, int border, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (x < 0 || y < 0) return true;
            if (x > img.Image.Width || x > img.Image.Height) return true;

            //TODO: (Maros) COLOR_TILED is not implemented.

            //TODO: (Maros) Can be optimized.
            FloodFill(img.Image, x, y, PHPColorToNETColor(col), true, PHPColorToNETColor(border));

            return true;
        }

        #endregion

        #region imagefilter

        /// <summary>
        /// Applies a filter to an image
        /// </summary> 
        [ImplementsFunction("imagefilter")]
        public static bool imagefilter(PhpResource src_im, int filtertype)
        {
            return imagefilter(src_im, filtertype, -1, -1, -1, -1);
        }

        /// <summary>
        /// Applies a filter to an image
        /// </summary> 
        [ImplementsFunction("imagefilter")]
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1)
        {
            return imagefilter(src_im, filtertype, arg1, -1, -1, -1);
        }

        /// <summary>
        /// Applies a filter to an image
        /// </summary> 
        [ImplementsFunction("imagefilter")]
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1, int arg2)
        {
            return imagefilter(src_im, filtertype, arg1, arg2, -1, -1);
        }

        /// <summary>
        /// Applies a filter to an image
        /// </summary> 
        [ImplementsFunction("imagefilter")]
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1, int arg2, int arg3)
        {
            return imagefilter(src_im, filtertype, arg1, arg2, arg3, -1);
        }

        /// <summary>
        /// Applies a filter to an image
        /// </summary> 
        [ImplementsFunction("imagefilter")]
        public static bool imagefilter(PhpResource im, int filtertype, int arg1, int arg2, int arg3, int arg4)
        {
            if (arg1 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg1", arg1);
            }

            if (arg2 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg2", arg2);
            }

            if (arg3 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg3", arg3);
            }

            if (arg4 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg4", arg4);
            }

            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            //TODO: (Maros) Not all filters are added here.

            switch (filtertype)
            {
                case (int)FilterTypes.NEGATE:
                    InvertColors(img.Image);
                    break;
                case (int)FilterTypes.GRAYSCALE:
                    MakeGrayscale(img.Image);
                    break;
                default:
                    return false;
            }

            return true;
        }

        #endregion

        #region imagefontheight

        /// <summary>
        /// Get font height
        /// </summary> 
        [ImplementsFunction("imagefontheight")]
        public static int imagefontheight(int font)
        {
            Font fontText;
            FontStyle style;
            int spacing;

            if (!GetFont(font, out fontText, out style, out spacing))
                return -1;

            return fontText.Height; // TODO
        }

        #endregion

        #region imagefontwidth

        /// <summary>
        /// Get font width
        /// </summary> 
        [ImplementsFunction("imagefontwidth")]
        public static int imagefontwidth(int font)
        {
            Font fontText;
            FontStyle style;
            int spacing;

            if (!GetFont(font, out fontText, out style, out spacing))
                return -1;

            return TextRenderer.MeasureText(" ", fontText).Width; // TODO
        }

        #endregion

        #region imageftbbox

        /// <summary>
        /// Give the bounding box of a markerName using fonts via freetype2
        /// </summary> 
        [ImplementsFunction("imageftbbox", FunctionImplOptions.NotSupported)]
        public static PhpArray imageftbbox(double size, double angle, string font_file, string text/*, PhpArray extrainfo*/)
        {
            //return imagettfbbox(size, angle, font_file, text);
            return null;
        }

        #endregion

        #region imagefttext

        /// <summary>
        /// Write text to the image using fonts via freetype2
        /// </summary> 
        [ImplementsFunction("imagefttext", FunctionImplOptions.NotSupported)]
        public static PhpArray imagefttext(PhpResource im, double size, double angle, int x, int y, int col, string font_file, string text/*, PhpArray extrainfo*/)
        {
            //return imagettftext(im, size, angle, x, y, col, font_file, text);
            return null;
        }

        #endregion

        #region imagegammacorrect

        /// <summary>
        /// Apply a gamma correction to a GD image
        /// </summary> 
        [ImplementsFunction("imagegammacorrect", FunctionImplOptions.NotSupported)]
        public static bool imagegammacorrect(PhpResource im, double inputgamma, double outputgamma)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagegd

        /// <summary>
        /// Output GD image to browser or file
        /// </summary> 
        [ImplementsFunction("imagegd", FunctionImplOptions.NotSupported)]
        public static bool imagegd(PhpResource im, string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagegd2

        /// <summary>
        /// Output GD2 image to browser or file
        /// </summary> 
        [ImplementsFunction("imagegd2", FunctionImplOptions.NotSupported)]
        public static bool imagegd2(PhpResource im, string filename, int chunk_size, int type)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagegif

        /// <summary>
        /// Output GIF image to browser or file
        /// </summary> 
        [ImplementsFunction("imagegif")]
        public static bool imagegif(PhpResource im)
        {
            return imagegif(im, null);
        }

        /// <summary>
        /// Output GIF image to browser or file
        /// </summary> 
        [ImplementsFunction("imagegif")]
        public static bool imagegif(PhpResource im, string filename)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (img.IsTransparentColSet)
            {
                ChangeColor(img.Image, img.transparentColor, Color.Transparent);
            }

            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    var context = ScriptContext.CurrentContext;
                    img.Image.Save(context.OutputStream, ImageFormat.Gif);
                }
                else
                {
                    filename = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, filename);
                    img.Image.Save(filename, ImageFormat.Gif);
                }

            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion

        #region imagegrabscreen

        /// <summary>
        /// Grab a screenshot
        /// </summary> 
        [ImplementsFunction("imagegrabscreen")]
        [return: CastToFalse]
        public static PhpResource imagegrabscreen()
        {
            Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);

            PhpGdImageResource resource = new PhpGdImageResource(bmpScreenshot);

            using (Graphics g = Graphics.FromImage(resource.Image))
            {
                try
                {
                    g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0,
                        Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                }
                catch
                {
                    g.FillRectangle(new SolidBrush(Color.Black), 0, 0,
                        resource.Image.Width, resource.Image.Height);
                }
            }

            return resource;
        }

        #endregion

        #region imagegrabwindow

        /// <summary>
        /// Grab a window or its client area using a windows handle (HWND property in COM instance)
        /// </summary> 
        [ImplementsFunction("imagegrabwindow", FunctionImplOptions.NotSupported)]
        public static PhpResource imagegrabwindow(int window_handle, int client_area)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imageinterlace

        /// <summary>
        /// Enable or disable interlace
        /// </summary> 
        [ImplementsFunction("imageinterlace", FunctionImplOptions.NotSupported)]
        public static int imageinterlace(PhpResource im, int interlace)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imageistruecolor

        /// <summary>
        /// return true if the image uses truecolor
        /// </summary> 
        [ImplementsFunction("imageistruecolor")]
        public static bool imageistruecolor(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            return !img.IsIndexed;
        }

        #endregion

        #region imagejpeg

        /// <summary>
        /// Output JPEG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagejpeg")]
        public static bool imagejpeg(PhpResource im)
        {
            return imagejpeg(im, null, 75);
        }

        /// <summary>
        /// Output JPEG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagejpeg")]
        public static bool imagejpeg(PhpResource im, string filename)
        {
            return imagejpeg(im, filename, 75);
        }

        /// <summary>
        /// Output JPEG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagejpeg")]
        public static bool imagejpeg(PhpResource im, string filename, int quality)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (quality < 0)
            {
                quality = 75;
            }

            if (quality > 100)
            {
                quality = 100;
            }

            using (EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
            {
                using (EncoderParameters encoderParams = new EncoderParameters(1))
                {
                    encoderParams.Param[0] = qualityParam;

                    // Jpeg image codec 
                    ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                        
                    try
                    {
                        if (string.IsNullOrEmpty(filename))
                        {
                            MemoryStream ms = new MemoryStream(1024);
                            img.Image.Save(ms, jpegCodec, encoderParams);

                            // saved into separated stream first, because Save method accesses stream's Position, which throws an exception when using NetworkStream
                            ms.Position = 0;
                            ms.CopyTo(ScriptContext.CurrentContext.OutputStream);
                        }
                        else
                        {
                            filename = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, filename);
                            img.Image.Save(filename, jpegCodec, encoderParams);
                        }
                    }
                    catch
                    {
                        return false;
                    }

                }
            }

            return true;
        }

        #endregion

        #region imagelayereffect

        /// <summary>
        /// Set the alpha blending flag to use the bundled libgd layering effects
        /// </summary> 
        [ImplementsFunction("imagelayereffect", FunctionImplOptions.NotSupported)]
        public static bool imagelayereffect(PhpResource im, int effect)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imageline

        /// <summary>
        /// Draw a line
        /// </summary>
        [ImplementsFunction("imageline")]
        public static bool imageline(PhpResource im, int x1, int y1, int x2, int y2, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                SetAntiAlias(img, g);
                Pen pen = CreatePen(col, img, true);
                g.DrawLine(pen, x1, y1, x2, y2);
                pen.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Tries to create Pen most compatible to PHP drawing rules
        /// </summary>
        /// <param name="col"></param>
        /// <param name="img"></param>
        /// <param name="antiAliasable"></param>
        /// <returns></returns>
        private static Pen CreatePen(int col, PhpGdImageResource img, bool antiAliasable)
        {
            Pen pen;

            if (antiAliasable && img.AntiAlias)
            {
                if (col < 0)
                {
                    pen = new Pen(Color.White);
                }
                else
                {
                    pen = new Pen(PHPColorToNETColor(col));
                }
            }
            else
            {
                if (col == (int)ColorValues.TILED)
                {
                    if (img.tiled == null)
                    {
                        return new Pen(Color.Transparent);
                    }
                    else
                    {
                        pen = new Pen(img.tiled, img.LineThickness);
                    }
                }
                else
                    // IMG_STYLED
                    if (col == -2)
                    {
                        if (img.styled == null)
                        {
                            return new Pen(Color.Transparent);
                        }
                        else
                        {
                            pen = new Pen(img.styled, img.LineThickness);
                        }
                    }

                    // TODO: (Maros) Different than in PHP. And missing IMG_STYLED_BRUSHED.
                    // IMG_BRUSHED
                    else if (col == -3)
                    {
                        if (img.brushed == null)
                        {
                            return new Pen(Color.Transparent);
                        }
                        else
                        {
                            pen = new Pen(img.brushed, img.LineThickness);
                        }
                    }
                    else
                    {
                        Color color = GetAlphaColor(img, col);

                        pen = new Pen(color, img.LineThickness);
                    }
            }

            return pen;
        }

        private static void SetAntiAlias(PhpGdImageResource img, Graphics g)
        {
            if (img.AntiAlias)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            else
            {
                g.SmoothingMode = SmoothingMode.None;
            }
        }

        #endregion

        #region imageloadfont

        /// <summary>
        /// Load a new font
        /// </summary> 
        [ImplementsFunction("imageloadfont", FunctionImplOptions.NotSupported)]
        public static int imageloadfont(string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagepalettecopy

        /// <summary>
        /// Copy the palette from the src image onto the dst image
        /// </summary> 
        [ImplementsFunction("imagepalettecopy", FunctionImplOptions.NotSupported)]
        public static void imagepalettecopy(PhpResource dst, PhpResource src)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
        }

        #endregion

        #region imagepng

        //TODO: (Maros) PHP saves little smaller PNG files.

        /// <summary>
        /// Output PNG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagepng")]
        public static bool imagepng(PhpResource im)
        {
            return imagepng(im, null);
        }

        /// <summary>
        /// Output PNG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagepng")]
        public static bool imagepng(PhpResource im, string filename)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            Bitmap saveBitmap;
            bool dispose = false;

            if ((img.IsTransparentColSet) && (img.SaveAlpha == false))
            {
                ChangeColor(img.Image, img.transparentColor, Color.Transparent);
                saveBitmap = img.Image;
            }
            else
            {
                if (img.SaveAlpha == true)
                {
                    saveBitmap = img.Image;
                }
                else
                {
                    saveBitmap = new Bitmap(img.Image.Width, img.Image.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(saveBitmap))
                    {
                        g.DrawImage(img.Image, 0, 0);
                    }
                }
            }

            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    MemoryStream ms = new MemoryStream();
                    saveBitmap.Save(ms, ImageFormat.Png);

                    ms.Position = 0;
                    ms.CopyTo(ScriptContext.CurrentContext.OutputStream);
                }
                else
                {
                    filename = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, filename);
                    saveBitmap.Save(filename, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }

            if (dispose)
            {
                saveBitmap.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Output PNG image to browser or file
        /// </summary> 
        [ImplementsFunction("imagepng")]
        public static bool imagepng(PhpResource im, string filename, int quality)
        {
            //TODO: implement quality parametr
            return imagepng(im, filename);
        }

        #endregion

        #region imagepolygon

        /// <summary>
        /// Draw a polygon
        /// </summary> 
        [ImplementsFunction("imagepolygon")]
        public static bool imagepolygon(PhpResource im, PhpArray point, int num_points, int col)
        {
            return DrawPoly(im, point, num_points, col, false);
        }

        #endregion

        #region imagepsbbox

        /// <summary>
        /// Return the bounding box needed by a string if rasterized
        /// </summary> 
        [ImplementsFunction("imagepsbbox", FunctionImplOptions.NotSupported)]
        public static PhpArray imagepsbbox(string text, PhpResource font, int size, int space, int tightness, double angle)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagepscopyfont

        /// <summary>
        /// Make a copy of a font for purposes like extending or reenconding
        /// </summary> 
        [ImplementsFunction("imagepscopyfont", FunctionImplOptions.NotSupported)]
        public static int imagepscopyfont(int font_index)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region imagepsencodefont

        /// <summary>
        /// To change a fonts character encoding vector
        /// </summary> 
        [ImplementsFunction("imagepsencodefont", FunctionImplOptions.NotSupported)]
        public static bool imagepsencodefont(PhpResource font_index, string filename)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagepsextendfont

        /// <summary>
        /// Extend or or condense (if extend &lt; 1) a font
        /// </summary> 
        [ImplementsFunction("imagepsextendfont", FunctionImplOptions.NotSupported)]
        public static bool imagepsextendfont(PhpResource font_index, double extend)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagepsfreefont

        /// <summary>
        /// Free memory used by a font
        /// </summary> 
        [ImplementsFunction("imagepsfreefont", FunctionImplOptions.NotSupported)]
        public static bool imagepsfreefont(PhpResource font_index)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagepsloadfont

        /// <summary>
        /// Load a new font from specified file
        /// </summary> 
        [ImplementsFunction("imagepsloadfont", FunctionImplOptions.NotSupported)]
        public static PhpResource imagepsloadfont(string pathname)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagepsslantfont

        /// <summary>
        /// Slant a font
        /// </summary> 
        [ImplementsFunction("imagepsslantfont", FunctionImplOptions.NotSupported)]
        public static bool imagepsslantfont(PhpResource font_index, double slant)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagepstext

        /// <summary>
        /// Rasterize a string over an image
        /// </summary> 
        [ImplementsFunction("imagepstext", FunctionImplOptions.NotSupported)]
        public static PhpArray imagepstext(PhpResource image, string text, PhpResource font, int size, int foreground,
            int background, int xcoord, int ycoord, int space, int tightness, double angle, int antialias)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion

        #region imagerectangle

        /// <summary>
        /// Draw a rectangle
        /// </summary> 
        [ImplementsFunction("imagerectangle")]
        public static bool imagerectangle(PhpResource im, int x1, int y1, int x2, int y2, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.DrawRectangle(CreatePen(col, img, false), x1, y1, x2 - x1, y2 - y1);
            }

            return true;
        }

        #endregion

        #region imagerotate

        /// <summary>
        /// Rotate an image using a custom angle
        /// </summary> 
        [ImplementsFunction("imagerotate")]
        [return: CastToFalse]
        public static PhpResource imagerotate(PhpResource src_im, double angle, int bgdcolor)
        {
            return imagerotate(src_im, angle, bgdcolor, 0);
        }

        /// <summary>
        /// Rotate an image using a custom angle
        /// </summary> 
        [ImplementsFunction("imagerotate")]
        [return: CastToFalse]
        public static PhpResource imagerotate(PhpResource im, double angle, int bgdcolor, int ignoretransparent)
        {
            if (ignoretransparent != 0)
            {
                PhpException.ArgumentValueNotSupported("ignoretransparent", ignoretransparent);
            }

            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return null;
            
            if (angle < 360) angle = angle - ((int)angle / 360) * 360;
            //if (angle < 0) angle = 360 + angle;

            PhpGdImageResource ret_im = new PhpGdImageResource(RotateImage(img.Image, -angle, PHPColorToNETColor(bgdcolor)));
            
            /*var graphics = Graphics.FromImage(ret_im.Image);

            SolidBrush brush = new SolidBrush(Color.FromArgb(bgdcolor));

            graphics.FillRectangle(brush, 0, 0, ret_im.Image.Width, ret_im.Image.Height);
            graphics.TranslateTransform((float)img.Image.Width / 2, (float)img.Image.Height / 2);
            graphics.RotateTransform(-angle);
            graphics.TranslateTransform(-(float)img.Image.Width / 2, -(float)img.Image.Height / 2);
            graphics.DrawImage(img.Image, new Point(0, 0));*/

            return ret_im;
        }

        #endregion

        #region imagesavealpha

        /// <summary>
        /// Include alpha channel to a saved image
        /// </summary> 
        [ImplementsFunction("imagesavealpha")]
        public static bool imagesavealpha(PhpResource im, bool on)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            img.SaveAlpha = on;

            return true;
        }

        #endregion

        #region imagesetbrush

        //TODO: (Maros)  When IMAGE_COLOR_BRUSHED is set in PHP, then PHP draws brush image multiple times
        // , pixel by pixel instead of brushedtexture like in here.

        /// <summary>
        /// Set the brush image to $brush when filling $image with the "IMG_COLOR_BRUSHED" color
        /// </summary> 
        [ImplementsFunction("imagesetbrush")]
        public static bool imagesetbrush(PhpResource image, PhpResource brush)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(image);
            if (img == null)
                return false;
            
            PhpGdImageResource imgBrush = PhpGdImageResource.ValidImage(brush);
            if (imgBrush == null)
                return false;
            

            img.brushed = new TextureBrush(imgBrush.Image);

            return false;
        }

        #endregion

        #region imagesetpixel

        /// <summary>
        /// Set a single pixel
        /// </summary> 
        [ImplementsFunction("imagesetpixel")]
        public static bool imagesetpixel(PhpResource im, int x, int y, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            Color color = GetAlphaColor(img, col);

            if (img.AlphaBlending == false)
            {
                img.Image.SetPixel(x, y, color);
            }
            else
            {
                Color oldColor = img.Image.GetPixel(x, y);

                double oldAlpha = (oldColor.A / 255.0);
                double newAlpha = (color.A / 255.0);

                double oldR = oldColor.R / 255.0;
                double oldG = oldColor.G / 255.0;
                double oldB = oldColor.B / 255.0;

                double newR = color.R / 255.0;
                double newG = color.G / 255.0;
                double newB = color.B / 255.0;

                double a = newAlpha + oldAlpha * (1.0 - newAlpha);

                int r = (int)(((newR * newAlpha + oldR * oldAlpha * (1.0 - newAlpha)) / a) * 255);
                int g = (int)(((newG * newAlpha + oldG * oldAlpha * (1.0 - newAlpha)) / a) * 255);
                int b = (int)(((newB * newAlpha + oldB * oldAlpha * (1.0 - newAlpha)) / a) * 255);

                int aa = (int)(a * 255);

                Color newColor = Color.FromArgb(aa, r, g, b);

                img.Image.SetPixel(x, y, newColor);
            }

            return true;
        }

        private static Color GetAlphaColor(PhpGdImageResource img, int col)
        {
            Color color;
            if (!img.AlphaBlending)
            {
                color = Color.FromArgb(255, PHPColorToRed(col), PHPColorToGreen(col), PHPColorToBlue(col));
            }
            else
            {
                color = PHPColorToNETColor(col);
            }
            return color;
        }

        private static int PHPColorToPHPAlpha(int color)
        {
            int ret = (color & 0x0000FF << 24);
            ret = (ret >> 24);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToRed(int color)
        {
            int ret = (color & 0x0000FF << 16);
            ret = (ret >> 16);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToGreen(int color)
        {
            int ret = (color & 0x0000FF << 8);
            ret = (ret >> 8);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToBlue(int color)
        {
            return (color & 0x0000FF);
        }

        #endregion

        #region imagesetstyle

        /// <summary>
        /// Set the line drawing styles for use with imageline and IMG_COLOR_STYLED.
        /// </summary> 
        [ImplementsFunction("imagesetstyle")]
        public static bool imagesetstyle(PhpResource im, PhpArray styles)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (styles == null || styles.IsEmpty())
            {
                return false;
            }

            Bitmap brushImage = new Bitmap(styles.Count, 1);

            int i = 0;
            foreach (int value in styles.Values)
            {
                brushImage.SetPixel(i, 0, PHPColorToNETColor(value));
                i++;
            }

            img.styled = new TextureBrush(brushImage);

            return true;
        }

        #endregion

        #region imagesetthickness

        /// <summary>
        /// Set line thickness for drawing lines, ellipses, rectangles, polygons etc.
        /// </summary> 
        [ImplementsFunction("imagesetthickness")]
        public static bool imagesetthickness(PhpResource im, int thickness)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (thickness < 0) thickness = 0;
            if (thickness == 0) thickness = 1;

            img.LineThickness = thickness;

            return true;
        }

        #endregion

        #region imagesettile

        /// <summary>
        /// Set the tile image to $tile when filling $image with the "IMG_COLOR_TILED" color
        /// </summary> 
        [ImplementsFunction("imagesettile")]
        public static bool imagesettile(PhpResource image, PhpResource tile)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(image);
            if (img == null)
                return false;

            PhpGdImageResource imgTile = PhpGdImageResource.ValidImage(tile);
            if (imgTile == null)
                return false;
            
            img.tiled = new TextureBrush(imgTile.Image);

            return false;
        }

        #endregion

        #region imagestring

        /// <summary>
        /// Determine <see cref="Font"/>, <see cref="FontStyle"/> and font <paramref name="spacing"/> as close as possible to given <paramref name="font"/> index.
        /// </summary>
        /// <param name="font">PHP font index.</param>
        /// <param name="fontText"></param>
        /// <param name="style"></param>
        /// <param name="spacing"></param>
        /// <returns>True iff font could be aproximated.</returns>
        private static bool GetFont(int font, out Font fontText, out FontStyle style, out int spacing)
        {
            // defaults:
            style = FontStyle.Regular;
            spacing = 5;

            switch (font)
            {
                case 1:
                    fontText = new Font(FontFamily.GenericMonospace, 9, style, GraphicsUnit.Pixel);
                    break;
                case 2:
                    fontText = new Font(FontFamily.GenericMonospace, 11, style, GraphicsUnit.Pixel);
                    spacing = 6;
                    break;
                case 3:
                    style = FontStyle.Bold;
                    fontText = new Font(FontFamily.GenericMonospace, 12, style, GraphicsUnit.Pixel);
                    spacing = 7;
                    break;
                case 4:
                    fontText = new Font(FontFamily.GenericMonospace, 14, style, GraphicsUnit.Pixel);
                    spacing = 8;
                    break;

                default:
                    //style = FontStyle.Bold;
                    fontText = new Font(FontFamily.GenericMonospace, 15, style, GraphicsUnit.Pixel);
                    spacing = 9;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Draw a string horizontally
        /// </summary> 
        [ImplementsFunction("imagestring")]
        public static bool imagestring(PhpResource im, int font, int x, int y, string str, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;
            
            if (font < 1) font = 1;

            Font fontText;
            FontStyle style;
            int spacing;

            if (!GetFont(font, out fontText, out style, out spacing))
                throw new NotImplementedException();
            
            //float descent = fontText.Size * fontText.FontFamily.GetCellDescent(style) / fontText.FontFamily.GetEmHeight(style);
            //float ascent = fontText.Size * fontText.FontFamily.GetCellAscent(style) / fontText.FontFamily.GetEmHeight(style);
            Point origin = new Point(x, y - 1);

            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
            sf.Trimming = StringTrimming.None;
            //sf.FormatFlags = StringFormatFlags.FitBlackBox;
            sf.LineAlignment = StringAlignment.Near;

            using (Graphics g = Graphics.FromImage(img.Image))
            {
                g.SmoothingMode = SmoothingMode.None;
                SizeF sizef = g.MeasureString(str, fontText, origin, sf);

                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Color color = GetAlphaColor(img, col);

                SolidBrush brush = new SolidBrush(color);
                //g.DrawString(str, fontText, brush, origin, sf);

                for (int i = 0; i < str.Length; i++)
                {
                    g.DrawString(str[i].ToString(), fontText, brush, x + i * spacing, y - 1, sf);
                }

                brush.Dispose();
            }

            return true;
        }

        #endregion

        #region imagestringup

        /// <summary>
        /// Draw a string vertically - rotated 90 degrees counter-clockwise
        /// </summary> 
        [ImplementsFunction("imagestringup", FunctionImplOptions.NotSupported)]
        public static bool imagestringup(PhpResource im, int font, int x, int y, string str, int col)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagesx

        /// <summary>
        /// Get image width
        /// </summary> 
        [ImplementsFunction("imagesx")]
        public static int imagesx(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;
            
            return img.Image.Width;
        }

        #endregion

        #region imagesy

        /// <summary>
        /// Get image height
        /// </summary> 
        [ImplementsFunction("imagesy")]
        public static int imagesy(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;  // TODO: (maros) chekc if it is -1 in PHP
            
            return img.Image.Height;
        }

        #endregion

        #region imagetruecolortopalette

        /// <summary>
        /// Convert a true colour image to a palette based image with a number of colours, optionally using dithering.
        /// </summary> 
        [ImplementsFunction("imagetruecolortopalette")]
        public static bool imagetruecolortopalette(PhpResource im, bool ditherFlag, int colorsWanted)
        {
            if (colorsWanted <= 0)
                return false;
            
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            if (img.IsIndexed)
                return true;     // already indexed

            // determine new pixel format:
            PixelFormat newformat;
            if (colorsWanted <= 2)
                newformat = PixelFormat.Format1bppIndexed;
            else if (colorsWanted <= 16)
                newformat = PixelFormat.Format4bppIndexed;
            else if (colorsWanted <= 256)
                newformat = PixelFormat.Format8bppIndexed;
            else
                newformat = PixelFormat.Indexed;
            
            // clone the image as indexed:
            var image = img.Image;
            var newimage = image.Clone(new Rectangle(0, 0, image.Width, image.Height), newformat);

            if (newimage == null)
                return false;

            img.Image = newimage;
            return true;
        }

        #endregion

        #region imagettfbbox

        /// <summary>
        /// Give the bounding box of a text using TrueType fonts
        /// </summary> 
        [ImplementsFunction("imagettfbbox")]
        [return: CastToFalse]
        public static PhpArray imagettfbbox(double size, double angle, string font_file, string text)
        {
            font_file = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, font_file);

            if (font_file == "")
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("filename_cannot_be_empty"));
                return null;
            }

            if (!File.Exists(font_file))
            {
                PhpException.Throw(PhpError.Warning, String.Format(Utils.Resources.GetString("invalid_font_filename"), font_file));
                return null;
            }

            // Font preparation
            PrivateFontCollection pfc;

            try
            {
                pfc = new PrivateFontCollection();
                pfc.AddFontFile(font_file);
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, String.Format(Utils.Resources.GetString("invalid_font_filename"), font_file));
                return null;
            }

            FontStyle style = FontStyle.Regular;

            if (!pfc.Families[0].IsStyleAvailable(FontStyle.Regular))
            {
                if (pfc.Families[0].IsStyleAvailable(FontStyle.Bold))
                {
                    style = FontStyle.Bold;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Italic))
                {
                    style = FontStyle.Italic;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Underline))
                {
                    style = FontStyle.Underline;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Strikeout))
                {
                    style = FontStyle.Strikeout;
                }
            }

            Font font = new Font(pfc.Families[0], (float)size, style, GraphicsUnit.Point);
            float descent = font.Size * font.FontFamily.GetCellDescent(style) / font.FontFamily.GetEmHeight(style);
            float ascent = font.Size * font.FontFamily.GetCellAscent(style) / font.FontFamily.GetEmHeight(style);
            Point origin = new Point(0, (int)(0 - (ascent + descent)));


            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
            SizeF sizef = MeasureString(text, font, origin, sf);


            System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();
            matrix.RotateAt(-(float)angle, new PointF(0 + (sizef.Width / 2), 0 - ((ascent + descent) / 2)));
            Point[] points = { origin };
            matrix.TransformPoints(points);

            int difX = (origin.X - points[0].X);
            int difY = (origin.Y - points[0].Y);

            origin.X = origin.X - difX;
            origin.Y = origin.Y + difY;

            Point[] points2 = new Point[4];

            if (angle != 0)
            {
                points2[0] = new Point(0 - difX, 0 + difY + 2);
                points2[1] = new Point((int)(0 - difX + sizef.Width), 0 + difY + 2);
                points2[2] = new Point((int)(0 - difX + sizef.Width), (int)(0 - (ascent + descent)) + difY + 2);
                points2[3] = new Point((int)(0 - difX), (int)(0 - (ascent + descent)) + difY + 2);

                matrix.TransformPoints(points2);
            }
            else
            {
                points2[0] = new Point(0 - difX, 0 + difY);
                points2[1] = new Point((int)(0 - difX + sizef.Width), 0 + difY);
                points2[2] = new Point((int)(0 - difX + sizef.Width), (int)(0 - (ascent + descent)) + difY + 3);
                points2[3] = new Point((int)(0 - difX), (int)(0 - (ascent + descent)) + difY + 3);
            }

            PhpArray array = new PhpArray();

            array.Add(points2[0].X);
            array.Add(points2[0].Y);
            array.Add(points2[1].X);
            array.Add(points2[1].Y);
            array.Add(points2[2].X);
            array.Add(points2[2].Y);
            array.Add(points2[3].X);
            array.Add(points2[3].Y);

            return array;
        }

        /// <summary>
        /// Measure text without graphics object
        /// </summary>
        /// <param name="s"></param>
        /// <param name="font"></param>
        /// <param name="origin"></param>
        /// <param name="sf"></param>
        /// <returns></returns>
        private static SizeF MeasureString(string s, Font font, Point origin, StringFormat sf)
        {
            SizeF result;
            using (var image = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(image))
                {
                    result = g.MeasureString(s, font, origin, sf);
                }
            }

            return result;
        }

        #endregion

        #region imagettftext

        /// <summary>
        /// Write text to the image using a TrueType font
        /// </summary> 
        [ImplementsFunction("imagettftext")]
        [return: CastToFalse]
        public static PhpArray imagettftext(PhpResource im, double size, double angle, int x, int y, int col, string font_file, string text)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return null;
            
            if (string.IsNullOrEmpty(font_file))
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("filename_cannot_be_empty"));
                return null;
            }

            font_file = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, font_file);

            if (!File.Exists(font_file))
            {
                PhpException.Throw(PhpError.Warning, String.Format(Utils.Resources.GetString("invalid_font_filename"), font_file));
                return null;
            }

            // Font preparation
            PrivateFontCollection pfc;

            try
            {
                pfc = new PrivateFontCollection();
                pfc.AddFontFile(font_file);
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, String.Format(Utils.Resources.GetString("invalid_font_filename"), font_file));
                return null;
            }

            FontStyle style = FontStyle.Regular;

            if (!pfc.Families[0].IsStyleAvailable(FontStyle.Regular))
            {
                if (pfc.Families[0].IsStyleAvailable(FontStyle.Bold))
                {
                    style = FontStyle.Bold;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Italic))
                {
                    style = FontStyle.Italic;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Underline))
                {
                    style = FontStyle.Underline;
                }
                else if (pfc.Families[0].IsStyleAvailable(FontStyle.Strikeout))
                {
                    style = FontStyle.Strikeout;
                }
            }

            Font font = new Font(pfc.Families[0], (float)size, style, GraphicsUnit.Point);
            float descent = font.Size * font.FontFamily.GetCellDescent(style) / font.FontFamily.GetEmHeight(style);
            float ascent = font.Size * font.FontFamily.GetCellAscent(style) / font.FontFamily.GetEmHeight(style);
            Point origin = new Point(x, (int)(y - (ascent + descent)));

            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);

            Graphics g = Graphics.FromImage(img.Image);
            //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            SizeF sizef = g.MeasureString(text, font, origin, sf);


            System.Drawing.Drawing2D.Matrix matrix = new System.Drawing.Drawing2D.Matrix();
            matrix.RotateAt(-(float)angle, new PointF(x + (sizef.Width / 2), y - ((ascent + descent) / 2)));
            Point[] points = { origin };
            matrix.TransformPoints(points);

            int difX = (origin.X - points[0].X);
            int difY = (origin.Y - points[0].Y);

            origin.X = origin.X - difX;
            origin.Y = origin.Y + difY;

            g.Transform = matrix;
            //g.TranslateTransform(origin.X - points[0].X, origin.Y - points[0].Y);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Color color = GetAlphaColor(img, col);

            SolidBrush brush = new SolidBrush(color);

            g.DrawString(text, font, brush, origin, sf);
            brush.Dispose();

            sf.Dispose();
            font.Dispose();
            pfc.Dispose();

            g.Dispose();

            Point[] points2 = new Point[4];

            if (angle != 0)
            {
                points2[0] = new Point(x - difX, y + difY + 2);
                points2[1] = new Point((int)(x - difX + sizef.Width), y + difY + 2);
                points2[2] = new Point((int)(x - difX + sizef.Width), (int)(y - (ascent + descent)) + difY + 2);
                points2[3] = new Point((int)(x - difX), (int)(y - (ascent + descent)) + difY + 2);

                matrix.TransformPoints(points2);
            }
            else
            {
                points2[0] = new Point(x - difX, y + difY);
                points2[1] = new Point((int)(x - difX + sizef.Width), y + difY);
                points2[2] = new Point((int)(x - difX + sizef.Width), (int)(y - (ascent + descent)) + difY + 3);
                points2[3] = new Point((int)(x - difX), (int)(y - (ascent + descent)) + difY + 3);
            }

            PhpArray array = new PhpArray();

            array.Add(points2[0].X);
            array.Add(points2[0].Y);
            array.Add(points2[1].X);
            array.Add(points2[1].Y);
            array.Add(points2[2].X);
            array.Add(points2[2].Y);
            array.Add(points2[3].X);
            array.Add(points2[3].Y);

            return array;
        }

        #endregion

        #region imagetypes

        /// <summary>
        /// Return the types of images supported in a bitfield - 1=GIF, 2=JPEG, 4=PNG, 8=WBMP, 16=XPM
        /// IMG_GIF | IMG_JPG | IMG_PNG | IMG_WBMP | IMG_XPM
        /// </summary> 
        [ImplementsFunction("imagetypes")]
        public static int imagetypes()
        {
            return (int)ImgType.Supported;
        }

        #endregion

        #region imagewbmp

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("imagewbmp", FunctionImplOptions.NotSupported)]
        public static bool imagewbmp(PhpResource im)
        {
            return imagewbmp(im, null);
        }

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("imagewbmp", FunctionImplOptions.NotSupported)]
        public static bool imagewbmp(PhpResource im, string filename)
        {
            return imagewbmp(im, filename, 0);
        }

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        [ImplementsFunction("imagewbmp", FunctionImplOptions.NotSupported)]
        public static bool imagewbmp(PhpResource im, string filename, int foreground)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region imagexbm

        /// <summary>
        /// Output XBM image to browser or file
        /// </summary> 
        [ImplementsFunction("imagexbm", FunctionImplOptions.NotSupported)]
        public static int imagexbm(int im, string filename, int foreground)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion

        #region jpeg2wbmp

        /// <summary>
        /// Convert JPEG image to WBMP image
        /// </summary> 
        [ImplementsFunction("jpeg2wbmp", FunctionImplOptions.NotSupported)]
        public static bool jpeg2wbmp(string f_org, string f_dest, int d_height, int d_width, int threshold)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region png2wbmp

        /// <summary>
        /// Convert PNG image to WBMP image
        /// </summary> 
        [ImplementsFunction("png2wbmp", FunctionImplOptions.NotSupported)]
        public static bool png2wbmp(string f_org, string f_dest, int d_height, int d_width, int threshold)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion

        #region helper functions

        /// <summary>
        /// Get the <see cref="ImgType"/> corresponding to the given <see cref="ImageFormat"/>.
        /// </summary>
        /// <param name="format"></param>
        /// <returns>Corresponding PHP <see cref="ImgType"/> or <see cref="ImgType.Unknown"/>.</returns>
        internal static ImgType GetImgType(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Gif))
                return ImgType.GIF;
            else if (format.Equals(ImageFormat.Jpeg))
                return ImgType.JPEG;
            else if (format.Equals(ImageFormat.Png))
                return ImgType.PNG;
            //else if (format.Equals(ImageFormat.Bmp))
            //    return ImgType.Unknown;
            else
                return ImgType.Unknown;
        }

        /// <summary>
        /// Tries to load image from local file or from URL and checks its format for match if specified.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static PhpResource CreateGdImageFrom(string filename, ImageFormat format)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("filename_cannot_be_empty"));
                return null;
            }

            Bitmap image = LoadBitmap(filename, format);
            if (image == null)
                return null;
            
            return new PhpGdImageResource(image);
        }

        /// <summary>
        /// Loads bitmap file from specified filename or URL and if it doesnt match specified format returns null.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static Bitmap LoadBitmap(string filename, ImageFormat format)
        {
            Bitmap image;

            PhpBytes bytes = Utils.ReadPhpBytes(filename);

            if (bytes == null)
                return null;

            try
            {
                image = (Bitmap)Image.FromStream(new MemoryStream(bytes.ReadonlyData, false));
            }
            catch
            {
                return null;
            }

            if (format != null && !image.RawFormat.Equals(format))
                return null;

            return image;
        }

        private static void FloodFill(Bitmap/*!*/bitmap, int x, int y, Color color, bool toBorder, Color border)
        {
            Debug.Assert(bitmap != null);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int[] bits = new int[data.Stride / 4 * data.Height];
            Marshal.Copy(data.Scan0, bits, 0, bits.Length);

            LinkedList<Point> check = new LinkedList<Point>();
            int floodTo = color.ToArgb();
            int floodFrom = bits[x + y * data.Stride / 4];
            bits[x + y * data.Stride / 4] = floodTo;

            int floodBorder = border.ToArgb();

            if (floodFrom != floodTo)
            {
                check.AddLast(new Point(x, y));
                while (check.Count > 0)
                {
                    Point cur = check.First.Value;
                    check.RemoveFirst();

                    foreach (Point off in new Point[]{
                        new Point(0, -1), new Point(0, 1), 
                        new Point(-1, 0), new Point(1, 0)})
                    {
                        Point next = new Point(cur.X + off.X, cur.Y + off.Y);
                        if (next.X >= 0 && next.Y >= 0 &&
                            next.X < data.Width &&
                            next.Y < data.Height)
                        {
                            if (toBorder == false)
                            {
                                if (bits[next.X + next.Y * data.Stride / 4] == floodFrom)
                                {
                                    check.AddLast(next);
                                    bits[next.X + next.Y * data.Stride / 4] = floodTo;
                                }
                            }
                            else
                            {
                                if ((bits[next.X + next.Y * data.Stride / 4] != floodBorder && bits[next.X + next.Y * data.Stride / 4] != floodTo))
                                {
                                    check.AddLast(next);
                                    bits[next.X + next.Y * data.Stride / 4] = floodTo;
                                }
                            }
                        }
                    }
                }
            }

            Marshal.Copy(bits, 0, data.Scan0, bits.Length);
            bitmap.UnlockBits(data);
        }

        private static void ChangeColor(Bitmap/*!*/bitmap, Color fromColor, Color toColor)
        {
            Debug.Assert(bitmap != null);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int[] bits = new int[data.Stride / 4 * data.Height];
            Marshal.Copy(data.Scan0, bits, 0, bits.Length);

            int fromColorInt = fromColor.ToArgb();
            int toColorInt = toColor.ToArgb();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Height; x++)
                {
                    if (bits[x + y * data.Stride / 4] == fromColorInt)
                    {
                        bits[x + y * data.Stride / 4] = toColorInt;
                    }
                }
            }

            Marshal.Copy(bits, 0, data.Scan0, bits.Length);
            bitmap.UnlockBits(data);
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        internal static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }

        /// <summary>
        /// Save image to memory stream
        /// </summary>
        private static MemoryStream ToStream(Image image, ImageFormat format)
        {
            if (image == null || format == null)
                return null;

            MemoryStream stream;

            try
            {
                stream = new MemoryStream();
                image.Save(stream, format);
                stream.Position = 0;
            }
            catch
            {
                return null;
            }

            return stream;
        }

        /// <summary>
        /// Makes specified Bitmap grayscaled
        /// </summary>
        /// <param name="bitmap">processed bitmap</param>
        /// <returns>success indication</returns>
        private static bool MakeGrayscale(Bitmap/*!*/bitmap)
        {
            Debug.Assert(bitmap != null);

            try
            {
                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][] 
                    {
                        new float[] {.3f, .3f, .3f, 0, 0},
                        new float[] {.59f, .59f, .59f, 0, 0},
                        new float[] {.11f, .11f, .11f, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                    });

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inverts colors in specified Bitmap
        /// </summary>
        /// <param name="bitmap">processed bitmap</param>
        /// <returns>success indication</returns>
        private static bool InvertColors(Bitmap/*!*/bitmap)
        {
            Debug.Assert(bitmap != null);

            try
            {
                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix();
                colorMatrix.Matrix00 = -1;
                colorMatrix.Matrix11 = -1;
                colorMatrix.Matrix22 = -1;

                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);
                
                //
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new Image containing the same image only rotated
        /// </summary>
        /// <param name="image">The <see cref="System.Drawing.Image"/> to rotate</param>
        /// <param name="angle">The amount to rotate the image, clockwise, in degrees</param>
        /// <param name="color">Color used to fill background of new image.</param>
        /// <returns>A new <see cref="System.Drawing.Bitmap"/> that is just large enough
        /// to contain the rotated image without cutting any corners off.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="image"/> is null.</exception>
        public static Bitmap RotateImage(Image image, double angle, Color color)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            const double pi2 = Math.PI / 2.0;

            // Why can't C# allow these to be const, or at least readonly
            // *sigh*  I'm starting to talk like Christian Graus :omg:
            double oldWidth = (double)image.Width;
            double oldHeight = (double)image.Height;

            // Convert degrees to radians
            double theta = ((double)angle) * Math.PI / 180.0;
            double locked_theta = theta;

            // Ensure theta is now [0, 2pi)
            while (locked_theta < 0.0)
                locked_theta += 2 * Math.PI;

            double newWidth, newHeight;
            int nWidth, nHeight; // The newWidth/newHeight expressed as ints

            #region Explaination of the calculations
            /*
			 * The trig involved in calculating the new width and height
			 * is fairly simple; the hard part was remembering that when 
			 * PI/2 <= theta <= PI and 3PI/2 <= theta < 2PI the width and 
			 * height are switched.
			 * 
			 * When you rotate a rectangle, r, the bounding box surrounding r
			 * contains for right-triangles of empty space.  Each of the 
			 * triangles hypotenuse's are a known length, either the width or
			 * the height of r.  Because we know the length of the hypotenuse
			 * and we have a known angle of rotation, we can use the trig
			 * function identities to find the length of the other two sides.
			 * 
			 * sine = opposite/hypotenuse
			 * cosine = adjacent/hypotenuse
			 * 
			 * solving for the unknown we get
			 * 
			 * opposite = sine * hypotenuse
			 * adjacent = cosine * hypotenuse
			 * 
			 * Another interesting point about these triangles is that there
			 * are only two different triangles. The proof for which is easy
			 * to see, but its been too long since I've written a proof that
			 * I can't explain it well enough to want to publish it.  
			 * 
			 * Just trust me when I say the triangles formed by the lengths 
			 * width are always the same (for a given theta) and the same 
			 * goes for the height of r.
			 * 
			 * Rather than associate the opposite/adjacent sides with the
			 * width and height of the original bitmap, I'll associate them
			 * based on their position.
			 * 
			 * adjacent/oppositeTop will refer to the triangles making up the 
			 * upper right and lower left corners
			 * 
			 * adjacent/oppositeBottom will refer to the triangles making up 
			 * the upper left and lower right corners
			 * 
			 * The names are based on the right side corners, because thats 
			 * where I did my work on paper (the right side).
			 * 
			 * Now if you draw this out, you will see that the width of the 
			 * bounding box is calculated by adding together adjacentTop and 
			 * oppositeBottom while the height is calculate by adding 
			 * together adjacentBottom and oppositeTop.
			 */
            #endregion

            double adjacentTop, oppositeTop;
            double adjacentBottom, oppositeBottom;

            // We need to calculate the sides of the triangles based
            // on how much rotation is being done to the bitmap.
            //   Refer to the first paragraph in the explaination above for 
            //   reasons why.
            if ((locked_theta >= 0.0 && locked_theta < pi2) ||
                (locked_theta >= Math.PI && locked_theta < (Math.PI + pi2)))
            {
                adjacentTop = Math.Abs(Math.Cos(locked_theta)) * oldWidth;
                oppositeTop = Math.Abs(Math.Sin(locked_theta)) * oldWidth;

                adjacentBottom = Math.Abs(Math.Cos(locked_theta)) * oldHeight;
                oppositeBottom = Math.Abs(Math.Sin(locked_theta)) * oldHeight;
            }
            else
            {
                adjacentTop = Math.Abs(Math.Sin(locked_theta)) * oldHeight;
                oppositeTop = Math.Abs(Math.Cos(locked_theta)) * oldHeight;

                adjacentBottom = Math.Abs(Math.Sin(locked_theta)) * oldWidth;
                oppositeBottom = Math.Abs(Math.Cos(locked_theta)) * oldWidth;
            }

            newWidth = adjacentTop + oppositeBottom;
            newHeight = adjacentBottom + oppositeTop;

            nWidth = (int)Math.Ceiling(newWidth);
            nHeight = (int)Math.Ceiling(newHeight);

            Bitmap rotatedBmp = new Bitmap(nWidth, nHeight);

            // This array will be used to pass in the three points that 
            // make up the rotated image
            Point[] points;

            /*
             * The values of opposite/adjacentTop/Bottom are referring to 
             * fixed locations instead of in relation to the
             * rotating image so I need to change which values are used
             * based on the how much the image is rotating.
             * 
             * For each point, one of the coordinates will always be 0, 
             * nWidth, or nHeight.  This because the Bitmap we are drawing on
             * is the bounding box for the rotated bitmap.  If both of the 
             * corrdinates for any of the given points wasn't in the set above
             * then the bitmap we are drawing on WOULDN'T be the bounding box
             * as required.
             */
            if (locked_theta >= 0.0 && locked_theta < pi2)
            {
                points = new Point[] { 
											 new Point( (int) oppositeBottom, 0 ), 
											 new Point( nWidth, (int) oppositeTop ),
											 new Point( 0, (int) adjacentBottom )
										 };

            }
            else if (locked_theta >= pi2 && locked_theta < Math.PI)
            {
                points = new Point[] { 
											 new Point( nWidth, (int) oppositeTop ),
											 new Point( (int) adjacentTop, nHeight ),
											 new Point( (int) oppositeBottom, 0 )						 
										 };
            }
            else if (locked_theta >= Math.PI && locked_theta < (Math.PI + pi2))
            {
                points = new Point[] { 
											 new Point( (int) adjacentTop, nHeight ), 
											 new Point( 0, (int) adjacentBottom ),
											 new Point( nWidth, (int) oppositeTop )
										 };
            }
            else
            {
                points = new Point[] { 
											 new Point( 0, (int) adjacentBottom ), 
											 new Point( (int) oppositeBottom, 0 ),
											 new Point( (int) adjacentTop, nHeight )		
										 };
            }

            //
            SolidBrush brush = new SolidBrush(color);

            //
            using (Graphics g = Graphics.FromImage(rotatedBmp))
            {
                g.FillRectangle(brush, 0, 0, nWidth, nHeight);
                g.DrawImage(image, points);
            }

            return rotatedBmp;
        }

        #endregion
    }
}
