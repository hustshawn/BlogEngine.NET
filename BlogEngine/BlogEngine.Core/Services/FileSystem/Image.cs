using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BlogEngine.Core.Providers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BlogEngine.Core.FileSystem
{
    /// <summary>
    /// Image specific class, will contain the extra methods and properties of an image.
    /// Uses SixLabors.ImageSharp for cross-platform image processing.
    /// </summary>
    public partial class Image : File
    {
        #region Fields, Constants & Enums

        /// <summary>
        /// The current image. Call the accessor directly.
        /// </summary>
        private Image<Rgba32> _image;

        /// <summary>
        /// The detected format of the current image.
        /// </summary>
        private IImageFormat _detectedFormat;

        /// <summary>
        /// Enum for the file modification methods.
        /// </summary>
        public enum ModificationType
        {
            /// <summary>Flip the image horizontally</summary>
            FlipHorizontal,
            /// <summary>Flip the image vertically</summary>
            FlipVertical,
            /// <summary>270 degree clockwise rotation without flipping</summary>
            RotateLeft,
            /// <summary>90 degree clockwise rotation without flipping</summary>
            RotateRight
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor, copies all the properties from the file object into the new object.
        /// </summary>
        public Image(File obj)
        {
            if (obj != null)
            {
                var qry = typeof(File).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite);
                foreach (var pi in qry)
                    this.GetType().GetProperty(pi.Name).SetValue(this, pi.GetValue(obj, null), null);
            }
            if (!this.IsImage)
                throw new Exception("File not of an image type");
        }

        /// <summary>
        /// Ctor, property free constructor.
        /// </summary>
        public Image()
            : this(null) { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ImageSharp image of the current image item.
        /// </summary>
        public Image<Rgba32> ImageData
        {
            get
            {
                if (_image == null)
                    LoadImageFromContents();
                return _image;
            }
            internal set
            {
                _image = value;
            }
        }

        /// <summary>
        /// Gets the full download path to the file, using the file handler.
        /// </summary>
        public string ImageUrl
        {
            get
            {
                return $"{Utils.RelativeWebRoot}image.axd?picture={SafeFilePath}";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the image and detected format from FileContents.
        /// </summary>
        private void LoadImageFromContents()
        {
            _detectedFormat = SixLabors.ImageSharp.Image.DetectFormat(this.FileContents);
            _image = SixLabors.ImageSharp.Image.Load<Rgba32>(this.FileContents);
        }

        /// <summary>
        /// Gets an appropriate encoder for the detected format.
        /// </summary>
        private IImageEncoder GetEncoder()
        {
            if (_detectedFormat == null)
                return new PngEncoder();

            var name = _detectedFormat.Name;
            return name switch
            {
                "JPEG" => new JpegEncoder { Quality = 90 },
                "BMP" => new BmpEncoder(),
                "GIF" => new GifEncoder(),
                "TIFF" => new TiffEncoder(),
                _ => new PngEncoder()
            };
        }

        /// <summary>
        /// Converts the current ImageSharp image to a byte array using the detected format.
        /// </summary>
        private byte[] ImageToArray(Image<Rgba32> img)
        {
            using var ms = new MemoryStream();
            img.Save(ms, GetEncoder());
            return ms.ToArray();
        }

        /// <summary>
        /// Resizes the image to a new size. The image will be replaced.
        /// </summary>
        /// <param name="newSize">The new size for the image.</param>
        /// <returns>The resized image.</returns>
        public Image ResizeImage(Size newSize)
        {
            var img = ImageData;
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = newSize,
                Mode = ResizeMode.Max
            }));
            var nfile = BlogService.UploadFile(ImageToArray(img), this.Name, this.ParentDirectory, true);
            this.FileContents = nfile.FileContents;
            nfile.Dispose();
            _image = null; // force reload
            return this;
        }

        /// <summary>
        /// Creates a thumbnail from the original image.
        /// </summary>
        /// <param name="MaxSize">The max size in pixels of the thumbnail to generate.</param>
        /// <param name="originalContents">Byte array containing the original image.</param>
        /// <returns>Byte array containing the newly generated thumbnail image.</returns>
        public static byte[] ResizeImageThumbnail(int MaxSize, byte[] originalContents)
        {
            IImageFormat detectedFormat = SixLabors.ImageSharp.Image.DetectFormat(originalContents); using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(originalContents);
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxSize, MaxSize),
                Mode = ResizeMode.Max
            }));

            var encoder = GetEncoderForFormat(detectedFormat);
            using var ms = new MemoryStream();
            img.Save(ms, encoder);
            return ms.ToArray();
        }

        /// <summary>
        /// Resizes the image to a new size based on a percentage.
        /// </summary>
        /// <param name="Percent">The percentage to resize.</param>
        /// <returns>The resized image.</returns>
        public Image ResizeImage(decimal Percent)
        {
            var img = ImageData;
            int sizeX = (int)Math.Ceiling(img.Width * (Percent / 100));
            int sizeY = (int)Math.Ceiling(img.Height * (Percent / 100));
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(sizeX, sizeY),
                Mode = ResizeMode.Max
            }));
            var nfile = BlogService.UploadFile(ImageToArray(img), this.Name, this.ParentDirectory, true);
            this.FileContents = nfile.FileContents;
            nfile.Dispose();
            _image = null;
            return this;
        }

        /// <summary>
        /// Accepts change requests for an image for rotate &amp; flip.
        /// </summary>
        /// <param name="modifications">A list of changes, will be executed in the order received.</param>
        /// <returns>The original image.</returns>
        public Image ModifyImage(params ModificationType[] modifications)
        {
            var img = ImageData;
            foreach (var change in modifications)
            {
                switch (change)
                {
                    case ModificationType.RotateLeft:
                        img.Mutate(x => x.Rotate(RotateMode.Rotate270));
                        break;
                    case ModificationType.RotateRight:
                        img.Mutate(x => x.Rotate(RotateMode.Rotate90));
                        break;
                    case ModificationType.FlipHorizontal:
                        img.Mutate(x => x.Flip(FlipMode.Horizontal));
                        break;
                    case ModificationType.FlipVertical:
                        img.Mutate(x => x.Flip(FlipMode.Vertical));
                        break;
                }
            }
            var nfile = BlogService.UploadFile(ImageToArray(img), this.Name, this.ParentDirectory, true);
            this.FileContents = nfile.FileContents;
            nfile.Dispose();
            _image = null;
            return this;
        }

        /// <summary>
        /// Crops an image by giving the crop start x &amp; y co-ordinates and then the height &amp; width to crop to.
        /// </summary>
        /// <param name="x">The x co-ordinate.</param>
        /// <param name="y">The y co-ordinate.</param>
        /// <param name="width">The width to crop.</param>
        /// <param name="height">The height to crop.</param>
        /// <returns>The cropped image, on exception the image will not be modified.</returns>
        public Image CropImage(int x, int y, int width, int height)
        {
            try
            {
                var img = ImageData;
                img.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
                var nfile = BlogService.UploadFile(ImageToArray(img), this.Name, this.ParentDirectory, true);
                this.FileContents = nfile.FileContents;
                _image?.Dispose();
                _image = null;
                return this;
            }
            catch
            {
                return this;
            }
        }

        /// <summary>
        /// Gets an appropriate encoder for the given format.
        /// </summary>
        private static IImageEncoder GetEncoderForFormat(IImageFormat format)
        {
            if (format == null)
                return new PngEncoder();

            return format.Name switch
            {
                "JPEG" => new JpegEncoder { Quality = 90 },
                "BMP" => new BmpEncoder(),
                "GIF" => new GifEncoder(),
                "TIFF" => new TiffEncoder(),
                _ => new PngEncoder()
            };
        }

        #endregion
    }
}
