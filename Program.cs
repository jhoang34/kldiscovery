// <copyright file="Sample.cs" company="My Company Name">
// Copyright (c) 2020 All Rights Reserved
// </copyright>
// <author>Jimmy Hoang</author>
// <date>04/22/2020</date>
// <summary>Code submission for KLDiscovery</summary>
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace csharp
{

    class Program
    {

        // Defining file types and their signature here to add to easily
        static readonly Dictionary<string, string>  validFileTypes = new Dictionary<string, string>() 
        {
            // Cheated some and took a hex value as a string, should be 0x[value]
            {"PDF", "25504446"},
            {"JPG", "FFD8"}
        };
            
        static void Main(string[] args)
        {

            string directoryToAnalyze;
            string directoryOutput;

            // Handle the input of the input directory, don't allow progress on invalid directory
            while(true) 
            {
                Console.WriteLine("Directory to analyzed (default ./input): ");
                directoryToAnalyze = Console.ReadLine();
                if (directoryToAnalyze == "") 
                {
                    directoryToAnalyze = "input";
                    Console.WriteLine($"Using default: {directoryToAnalyze}");
                    break;                 
                } else if (ValidateDirectory(directoryToAnalyze)) 
                {
                    break;
                }
            }
            
            // Handle the input of the output file, don't allow progress on invalid directory
            while(true) 
            {
                Console.WriteLine("Full path to output file (default ./output/output_<timestamp>.csv): ");
                directoryOutput = Console.ReadLine();
                if (directoryOutput == "") 
                {
                    var timeStamp = GetTimestamp(DateTime.Now);
                    directoryOutput = $"output/output_{timeStamp}.csv";
                    Console.WriteLine($"Using default: {directoryOutput}");
                    break;
                } else if (ValidateOutputFile(directoryOutput)) 
                {
                    break;
                }
            }

            // Handle the input of the subdirectories flag, assume y|Y is True and everything else is False
            Console.WriteLine("Include Subdirectories: (y/n default y): ");
            var strIsSubdirectories = Console.ReadLine();
            if (strIsSubdirectories == "") strIsSubdirectories = "y";
            var isSubdirectories = strIsSubdirectories.Substring(0,1).ToLower() == "y";

            // This could go into a log file or [Conditional("DEBUG")]
            Console.WriteLine("=====================================================");
            Console.WriteLine("Processing with:");
            Console.WriteLine($"     Directory to Analyze: {directoryToAnalyze}");
            Console.WriteLine($"     Output Path:          {directoryOutput}");
            Console.WriteLine($"     Include Subdir:       {isSubdirectories}");
            Console.WriteLine("=====================================================");

            List<ExportRow> exportRows = new List<ExportRow>();
            GetFilesInDirectory(directoryToAnalyze, exportRows, isSubdirectories);
            WriteOutput(directoryOutput, exportRows);
        }


        static void GetFilesInDirectory(string path, List<ExportRow> exportRows, bool recursive = false) {

            foreach(string file in Directory.GetFiles(path))
            {

                // Read the first few bytes of the file
                byte[] buffer = new byte[128];
                using(FileStream fs = new FileStream(file, FileMode.Open)) 
                {
                    if (fs.Length >= 128)
                        fs.Read(buffer, 0, 128);
                    else
                        fs.Read(buffer, 0, (int)fs.Length);
                }
                var bufferString = BitConverter.ToString(buffer).Replace("-", "");

                foreach(KeyValuePair<string, string>fileType in Program.validFileTypes) 
                {
                    if (bufferString.StartsWith(fileType.Value)) 
                    {
                        ExportRow row = new ExportRow { FilePath = Path.GetFullPath(file),
                                                        FileType = fileType.Key, 
                                                        Hash = GetHashOfFile(file )};
                        exportRows.Add(row);
                    }
                }
            }

            if (recursive) // Handles the deeper dive into subdirectories
            {
                foreach(string directory in Directory.GetDirectories(path))
                {
                    GetFilesInDirectory(directory, exportRows, recursive);
                }
            }
        }

        static void WriteOutput(string outputPath, List<ExportRow> exportRows)  
        {    
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach(ExportRow row in exportRows) 
                {
                    writer.WriteLine(row.FilePath + "," + row.FileType + "," + row.Hash);
                }
            }   
        }

        static string GetHashOfFile(string fullFilePath) 
        {

            using(var md5 = MD5.Create())
            {
                using(var stream = File.OpenRead(fullFilePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        static bool ValidateDirectory(string directory, bool showError = true) {

            if (!Directory.Exists(directory)) {
                if (showError) {
                    Console.WriteLine($"ERROR: Directory ({directory}) does not exist!");
                }
                return false;
            }
            return true;
        }

        static bool ValidateOutputFile(string outputPath) {

            var isValid = true;

            if (ValidateDirectory(outputPath, false)) {
                Console.WriteLine($"ERROR: Output file exists, cannot overwrite ({outputPath})");
                return false;
            }

            var fileName = Path.GetFileName(outputPath);
            var fullPath = Path.GetFullPath(outputPath);
            var filePathDirectory = Path.GetDirectoryName(fullPath);

            if (!ValidateDirectory(filePathDirectory)) {
                Console.WriteLine($"ERROR: Path to output file is not valid ({fullPath})");
                isValid = false;
            }

            // Check extension?

            return isValid;
        }

        static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
    }

    // Allows for this object to be expanded and passed around to other serivces when exporting data
    class ExportRow
    {
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string Hash { get; set; }
    }

}
