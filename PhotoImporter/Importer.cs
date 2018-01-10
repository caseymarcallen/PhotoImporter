using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoImporter
{
    public class PhotoImporter
    {
        private static readonly Regex r = new Regex(":");
        private static readonly int OriginalDateTimePropertyId = 0x9003;
        private static readonly List<string> ImageExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
        private static readonly object dirLock = new object();

        private readonly IFolderConfiguration _config;

        /// <summary>
        /// Constructor that takes a folder config
        /// </summary>
        /// <param name="config"></param>
        public PhotoImporter(IFolderConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException("config");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Import()
        {
            var foldersCreated = 0;

            foreach (var inputFile in Directory.GetFiles(_config.Source, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                    f.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                    f.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                    f.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) ||
                    f.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase)).AsParallel())
            {

                string fileName = new FileInfo(inputFile).Name;
                DateTime? dateTaken = null;

                // My Cameras / phones name photos with 2 conventions:
                //  IMG_yyyyMMdd_hhmmss.jpg
                //  yyyyMMdd_hhmmss.jpg
                // We'll try and pull the date taken from the filename and if not available, fall back to EXIF data
                // because pulling EXIF data is slow.

                var fileNameTokens = fileName.Split('_');

                if (fileNameTokens.Length > 0)
                {
                    var dateToken = fileNameTokens[0] == "IMG" 
                        ? fileNameTokens[1] 
                        : fileNameTokens[0];

                    if (DateTime.TryParseExact(dateToken, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTakenFromFileName))
                    {
                        dateTaken = dateTakenFromFileName;
                    }
                }

                if (!dateTaken.HasValue && TryGetDateTakenFromImageProperties(inputFile, out DateTime dateTakenFromImageProperties))
                {
                    // Couldn't find date taken from filename - try the exif data
                    dateTaken = dateTakenFromImageProperties;
                }
                

                if (!dateTaken.HasValue)
                {
                    // Photos could be downloaded images (e.g. synced using IFTTT from facebook etc)
                    dateTaken = File.GetLastWriteTime(inputFile);
                }
                
                // Create destination folder based on the photos date taken in a yyyy-MM/dd structure
                var outputFolder = Path.Combine(
                    _config.Destination,
                    dateTaken.Value.ToString("yyyy-MM"),
                    dateTaken.Value.ToString("dd"));

                lock (dirLock) // multiple threads could all try and create the same dir
                {
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                        foldersCreated++;
                    }
                }

                var outputFile = Path.Combine(outputFolder, fileName);

                try
                {
                    File.Move(inputFile, outputFile);
                    Console.WriteLine("Moved {0} ---> {1}", inputFile, outputFile);
                }
                catch (IOException e)
                {
                    if (e.Message == "Cannot create a file when that file already exists.\r\n")
                    {
                        File.Delete(inputFile);
                    }
                    Console.WriteLine("ERROR {0} ---> {1} :::: {2}", inputFile, outputFile, e.Message);
                }
                finally
                {
                    // If the directory is now empty, then we can delete it to clean up
                    var directoryPath = Path.GetDirectoryName(inputFile);
                    if (!Directory.EnumerateFileSystemEntries(directoryPath).Any(f => f != "Thumbs.db"))
                    {
                        Directory.Delete(directoryPath);
                    }
                }
            }
            Console.WriteLine("Created {0} folders", foldersCreated);
            Console.ReadLine();
        }
        //retrieves the datetime WITHOUT loading the whole image
        /// <summary>
        /// Pulls the Original Date Time property from the EXIF data in the image.
        /// 0x9003 comes from this List: https://msdn.microsoft.com/en-us/library/system.drawing.imaging.propertyitem.id(v=vs.110).aspx
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="dateTaken"></param>
        /// <returns></returns>
        public static bool TryGetDateTakenFromImageProperties(string inputFile, out DateTime dateTaken)
        {
            dateTaken = DateTime.MinValue;

            try
            {
                if (ImageExtensions.Contains(Path.GetExtension(inputFile).ToLower()))
                {
                    using (var fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                    using (var myImage = Image.FromStream(fs, false, false))
                    {
                        if (myImage.PropertyIdList.Any(x => x == OriginalDateTimePropertyId))
                        {
                            var propItem = myImage.GetPropertyItem(OriginalDateTimePropertyId);
                            var dateTakenString = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                            return DateTime.TryParse(dateTakenString, out dateTaken);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured processing {inputFile}: {e.Message}");
            }
            return false;
        }
    }
}