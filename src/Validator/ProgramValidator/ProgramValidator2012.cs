﻿using System;
using System.Collections.Generic;
using System.Text;
using PdfTextReader.Base;
using PdfTextReader.Parser;
using PdfTextReader.ExecutionStats;
using PdfTextReader.TextStructures;
using PdfTextReader.Execution;
using PdfTextReader.PDFText;
using System.Drawing;
using PdfTextReader.PDFCore;
using System.Linq;

namespace PdfTextReader
{
    public class ProgramValidator2012
    {
        public static void Process(string basename, string inputfolder, string outputfolder)
        {
            PdfReaderException.ContinueOnException();

            var conteudos = GetTextLines(basename, inputfolder, outputfolder, out Execution.Pipeline pipeline)
                             .ConvertText<CreateTextLineIndex, TextLine>()
                                .Log<AnalyzeLines>($"{outputfolder}/{basename}-lines.txt")
                                
                                .Log<AnalyzeLinesCenterRight>($"{outputfolder}/{basename}-;lines-center-right.txt")                                
                            .ConvertText<CreateStructures, TextStructure>()
                                .Log<AnalyzeStructuresCentral>($"{outputfolder}/{basename}-central.txt")
                            //.PrintAnalytics($"bin/{basename}-print-analytics.txt")
                            .ConvertText<CreateTextSegments, TextSegment>()
                                .Log<AnalyzeSegmentTitles>($"{outputfolder}/{basename}-tree.txt")
                                .Log<AnalyzeSegmentStats>($"{outputfolder}/{basename}-segments-stats.txt")
                                .Log<AnalyzeSegments2>($"{outputfolder}/{basename}-segments.csv")
                            .ConvertText<CreateTreeSegments, TextSegment>()
                                .Log<AnalyzeTreeStructure>($"{outputfolder}/{basename}-tree-hier.txt")
                             .ConvertText<TransformConteudo, Conteudo>()
                            .ToList();

            //Create XML
            var createArticle = new TransformArtigo();
            var artigos = createArticle.Create(conteudos);
            createArticle.CreateXML(artigos, outputfolder, basename);


            var validation = pipeline.Statistics.Calculate<ValidateFooter, StatsPageFooter>();
            var layout = (ValidateLayout)pipeline.Statistics.Calculate<ValidateLayout, StatsPageLayout>();
            var overlap = (ValidateOverlap)pipeline.Statistics.Calculate<ValidateOverlap, StatsBlocksOverlapped>();

            var pagesLayout = layout.GetPageErrors().ToList();
            var pagesOverlap = overlap.GetPageErrors().ToList();
            var pages = pagesLayout.Concat(pagesOverlap).Distinct().OrderBy(t => t).ToList();

            if (pages.Count > 0)
            {
                ExtractPages($"{outputfolder}/{basename}-output", $"{outputfolder}/{basename}-parser-errors", pages);
            }

            pipeline.Done();
        }

        public static void Validate(string basename, string inputfolder, string outputfolder)
        {
            var validator = new ProgramValidatorXML();
            validator.ValidateArticle($"{inputfolder}");
        }   
        
        static PipelineText<TextLine> GetTextLines(string basename, string inputfolder, string outputfolder, out Execution.Pipeline pipeline)
        {
            pipeline = new Execution.Pipeline();

            var result =
            pipeline.Input($"{inputfolder}/{basename}.pdf")
                    .Output($"{outputfolder}/{basename}-output.pdf")
                    .AllPagesExcept<CreateTextLines>(new int[] { }, page =>
                              page.ParsePdf<PreProcessTables>()
                                  .ParseBlock<IdentifyTables>()
                              .ParsePdf<PreProcessImages>()
                                  .ParseBlock<RemoveOverlapedImages>()
                              .ParsePdf<ProcessPdfText>()
                                  .Validate<RemoveSmallFonts>().ShowErrors(p => p.ShowText(Color.Green))
                                  .ParseBlock<RemoveSmallFonts>()
                                  .ParseBlock<MergeTableText>()
                                  .ParseBlock<HighlightTextTable>()
                                  .ParseBlock<RemoveTableText>()
                                  .ParseBlock<ReplaceCharacters>()
                                  .ParseBlock<GroupLines>()
                                  .ParseBlock<RemoveTableDotChar>()
                                      .Show(Color.Yellow)
                                      .Validate<RemoveHeaderImage>().ShowErrors(p => p.Show(Color.Purple))
                                  .ParseBlock<RemoveHeaderImage>()
                                  .ParseBlock<FindInitialBlocksetWithRewind>()
                                      .Show(Color.Gray)
                                  .ParseBlock<BreakColumnsLight>()
                                  //.ParseBlock<BreakColumns>()
                                  .ParseBlock<AddTableSpace>()
                                  .ParseBlock<RemoveTableOverImage>()
                                  .ParseBlock<RemoveImageTexts>()
                                  .ParseBlock<AddImageSpace>()
                                      .Validate<RemoveFooter>().ShowErrors(p => p.Show(Color.Purple))
                                      .ParseBlock<RemoveFooter>()
                                  .ParseBlock<BreakInlineElements>()
                                  .ParseBlock<ResizeBlocksets>()
                                      .Validate<ResizeBlocksets>().ShowErrors(p => p.Show(Color.Gray))
                                  .ParseBlock<OrderBlocksets>()
                                  .Show(Color.Orange)
                                  .ShowLine(Color.Black)
                                  .ParseBlock<OrganizePageLayout>()
                                  .ParseBlock<CheckOverlap>()
                                  .Validate<ValidatePositiveCoordinates>().ShowErrors(p => p.Show(Color.Red))
                    );

            return result;
        }
        static void ExtractPages(string basename, string outputname, IList<int> pages)
        {
            var pipeline = new Execution.Pipeline();

            pipeline.Input($"{basename}.pdf")
                    .ExtractPages($"{outputname}.pdf", pages);
        }
    }
}
