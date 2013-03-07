using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core;
using System.Drawing;
using System.Drawing.Imaging;

namespace PHP.Library.Gd2
{
    /// <summary>
    /// <see cref="PhpResource"/> representing PHP image.
    /// </summary>
    public class PhpGdImageResource : PhpResource
    {
        /// <summary>
        /// Underlaying <see cref="Bitmap"/> object. Cannot be <c>null</c> reference until it is not disposed.
        /// </summary>
        public Bitmap/*!*/Image
        {
            get
            {
                return image;
            }
            internal set
            {
                if (value == null)
                    throw new ArgumentNullException();

                image = value;
            }
        }
        private Bitmap/*!*/image;

        /// <summary>
        /// Determine if the pixel format is indexed.
        /// </summary>
        public bool IsIndexed { get { return (image.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed; } }
        
        internal bool AlphaBlending = false;
        internal bool SaveAlpha = false;
        internal bool AntiAlias = false;

        internal Color transparentColor;
        internal bool IsTransparentColSet = false;

        internal TextureBrush styled;
        internal TextureBrush brushed;
        internal TextureBrush tiled;

        internal int LineThickness = 1;

        //public bool BackgroundAllocated { get; set; }

        internal PhpGdImageResource(int x, int y)
            : base("GdImage")
        {
            image = new Bitmap(x, y);
        }

        /// <summary>
        ///  Creates PhpGdImageResource without creating internal image
        /// </summary>
        internal PhpGdImageResource(Image/*!*/img)
            : base("GdImage")
        {
            Debug.Assert(img != null);

            image = new Bitmap(img);
        }

        /// <summary>
        ///  Creates PhpGdImageResource without creating internal image
        /// </summary>
        internal PhpGdImageResource(Bitmap/*!*/img)
            : base("GdImage")
        {
            Debug.Assert(img != null);

            image = img;
        }

        /// <summary>
        /// Free resources owned by this resource.
        /// </summary>
        protected override void FreeManaged()
        {
            if (this.image != null)
            {
                this.image.Dispose();
                this.image = null;
            }

            if (this.styled != null)
            {
                this.styled.Dispose();
                this.styled = null;
            }
            if (this.brushed != null)
            {
                this.brushed.Dispose();
                this.brushed = null;
            }
            if (this.tiled != null)
            {
                this.tiled.Dispose();
                this.tiled = null;
            }

            base.FreeManaged();
        }

        /// <summary>
        /// Checks if resource is really image and if it exists
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns> 
        /// <exception cref="PhpException">Warning when resource is not valid <see cref="PhpGdImageResource"/>.</exception>
        internal static PhpGdImageResource ValidImage(PhpResource handle)
        {
            PhpGdImageResource result = handle as PhpGdImageResource;
            if (result != null && result.IsValid && result.image != null) return result;

            PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("image_resource_not_valid"));
            return null;
        }
    }
}
