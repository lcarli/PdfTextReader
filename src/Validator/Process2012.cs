﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Validator
{
    class Process2012 : IRunner
    {
        int _totalProcessed = 0;

        public string FilePattern => "*.pdf";

        public void Run(File file, string outputname)
        {
            string inputFolder = file.Folder;
            string basename = file.Filename;

            string folderOutput = FileList.CreateOutputFolder(outputname, basename);

            PdfTextReader.ProgramValidator2012.Process(basename, inputFolder, folderOutput);
            _totalProcessed++;


        }






    }
}
