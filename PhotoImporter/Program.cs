using System;
using System.IO;
using Yaclp;
using Yaclp.Attributes;

namespace PhotoImporter
{
    internal static class Program
    {

        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine("       W A R N I N G              W A R N I NG              W A R N I N G       ");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.WriteLine(" THIS IS A HIGHLY PERSONALISED PROGRAM THAT WILL MOVE AND DELETE FILES!         ");
                Console.WriteLine(" DO NOT CONTINUE UNLESS YOU HAVE READ AND UNDERSTOOD THE CODE AND ARE WILLING   ");
                Console.WriteLine(" TO RISK LOSING YOUR FILES.                                                     ");
                Console.WriteLine();
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.Write("DO YOU WISH TO CONTINUE [Y/N]: ");
                var confirmationChar = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (confirmationChar != 'y' && confirmationChar != 'Y')
                {
                    Console.WriteLine("Exiting without doing anything.");
                    return;
                }
                

                var configuration = DefaultParser.ParseOrExitWithUsageMessage<CommandLineParameters>(args);

                configuration.Source = PromptForFolder("Source", configuration.Source);
                configuration.Destination = PromptForFolder("Destination", configuration.Destination);

                new PhotoImporter(configuration).Import();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed with error: {e.Message}");
            }
            finally
            {
#if DEBUG
                Console.Write("Press any key to exit...");
                Console.ReadKey();
#endif
            }

        }

        /// <summary>
        /// Lets the program either take command line params or prompt for any missing parameters.
        /// </summary>
        /// <param name="folderType"></param>
        /// <param name="defaultFolder"></param>
        /// <returns></returns>
        private static string PromptForFolder(string folderType, string defaultFolder)
        {
            if (Directory.Exists(defaultFolder)) return defaultFolder;

            Console.Write("Enter {0} Folder: ", folderType);
            var folderInput = Console.ReadLine();

            return Directory.Exists(folderInput)
                ? folderInput
                : null;
        }
    }

    public interface IFolderConfiguration
    {
        string Source { get; set; }
        string Destination { get; set; }
    }

    internal class CommandLineParameters : IFolderConfiguration
    {
        [ParameterIsOptional]
        [ParameterDescription("The path to import photos from.")]
        public string Source { get; set; }

        [ParameterIsOptional]
        [ParameterDescription("The destination to import to.")]
        public string Destination { get; set; }

    }
}