﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PdfTextReader
{
    class Pipeline
    {
        // Pipeline Definition: 
        //   * TableListener -> ProcessTable
        //   * TextListener  -> Remove Tables -> ProcessColumns -> BreakColumns -> MergeBlocks
        //
        public void Run(string basename)
        {
            var user = new UserWriter2();

            // run the listener 1
            // process blocksets
            user.ProcessBlockExtra($"bin/{basename}.pdf", $"bin/{basename}-table-output.pdf");

            var tablesFound = user.ActiveTables;

            // run the listener 2
            // process blocksets
            user.ProcessBlock($"bin/{basename}.pdf", $"bin/{basename}-output.pdf");

            var texts = user.ActiveTexts;

            // TODO: transform into blocklines

            // transform into textLines
        }
    }
}