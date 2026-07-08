// <copyright file="ConversionJob_Markdown.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.IO;
    using System.Text;

    using Markdig;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    using QuestPDF.Fluent;
    using QuestPDF.Helpers;
    using QuestPDF.Infrastructure;

    using FileConverter.Diagnostics;

    public class ConversionJob_Markdown : ConversionJob
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public ConversionJob_Markdown() : base()
        {
        }

        public ConversionJob_Markdown(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Local, offline acknowledgement of the QuestPDF Community license. No network call.
            QuestPDF.Settings.License = LicenseType.Community;

            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }
        }

        protected override void Convert()
        {
            if (this.ConversionPreset == null)
            {
                throw new Exception("The conversion preset must be valid.");
            }

            this.UserState = Properties.Resources.ConversionStateReadDocument;

            string markdownContent = File.ReadAllText(this.InputFilePath);
            MarkdownDocument document = Markdig.Markdown.Parse(markdownContent, Pipeline);

            this.Progress = 0.3f;
            this.UserState = Properties.Resources.ConversionStateConversion;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(style => style.FontSize(11));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        foreach (Block block in document)
                        {
                            RenderBlock(column, block);
                        }
                    });
                });
            }).GeneratePdf(this.OutputFilePath);

            this.Progress = 0.95f;

            Debug.Log($"Write markdown to pdf succeed (output: {this.OutputFilePath}).");
        }

        private static void RenderBlock(ColumnDescriptor column, Block block)
        {
            if (block is HeadingBlock heading)
            {
                RenderHeading(column, heading);
            }
            else if (block is Markdig.Extensions.Tables.Table table)
            {
                RenderTable(column, table);
            }
            else if (block is ListBlock list)
            {
                RenderList(column, list, 0);
            }
            else if (block is QuoteBlock quote)
            {
                RenderQuote(column, quote);
            }
            else if (block is FencedCodeBlock fencedCode)
            {
                RenderCodeBlock(column, fencedCode);
            }
            else if (block is CodeBlock code)
            {
                RenderCodeBlock(column, code);
            }
            else if (block is ParagraphBlock paragraph)
            {
                column.Item().Text(text => RenderInlines(text, paragraph.Inline, false, false));
            }
            else if (block is ThematicBreakBlock)
            {
                column.Item().PaddingVertical(4).Height(1).Background(Colors.Grey.Lighten2);
            }
            else if (block is ContainerBlock container)
            {
                // Fallback for any container block type not handled explicitly above: render its children.
                foreach (Block child in container)
                {
                    RenderBlock(column, child);
                }
            }
        }

        private static void RenderHeading(ColumnDescriptor column, HeadingBlock heading)
        {
            float fontSize;
            switch (heading.Level)
            {
                case 1:
                    fontSize = 24f;
                    break;

                case 2:
                    fontSize = 20f;
                    break;

                case 3:
                    fontSize = 17f;
                    break;

                case 4:
                    fontSize = 14.5f;
                    break;

                case 5:
                    fontSize = 12.5f;
                    break;

                default:
                    fontSize = 11.5f;
                    break;
            }

            column.Item().PaddingTop(heading.Level == 1 ? 0 : 6).Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(fontSize).Bold());
                RenderInlines(text, heading.Inline, false, false);
            });
        }

        private static void RenderList(ColumnDescriptor column, ListBlock list, int nestingLevel)
        {
            int orderedIndex = 1;

            foreach (Block itemBlock in list)
            {
                ListItemBlock item = (ListItemBlock)itemBlock;
                string bulletText = list.IsOrdered ? orderedIndex + "." : "•";
                orderedIndex++;

                column.Item().Row(row =>
                {
                    row.ConstantItem(14 * (nestingLevel + 1));
                    row.ConstantItem(20).Text(bulletText);
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Spacing(4);

                        foreach (Block child in item)
                        {
                            if (child is ListBlock nestedList)
                            {
                                RenderList(inner, nestedList, nestingLevel + 1);
                            }
                            else if (child is ParagraphBlock paragraph)
                            {
                                inner.Item().Text(text => RenderInlines(text, paragraph.Inline, false, false));
                            }
                            else
                            {
                                RenderBlock(inner, child);
                            }
                        }
                    });
                });
            }
        }

        private static void RenderQuote(ColumnDescriptor column, QuoteBlock quote)
        {
            column.Item().BorderLeft(2).BorderColor(Colors.Grey.Darken1).PaddingLeft(10).Column(inner =>
            {
                inner.Spacing(4);

                foreach (Block child in quote)
                {
                    RenderBlock(inner, child);
                }
            });
        }

        private static void RenderCodeBlock(ColumnDescriptor column, LeafBlock codeBlock)
        {
            string code = codeBlock.Lines.ToString();

            column.Item().Background(Colors.Grey.Lighten3).Padding(8).Text(text =>
            {
                text.DefaultTextStyle(style => style.FontFamily("Consolas").FontSize(9.5f));
                text.Span(code);
            });
        }

        private static void RenderTable(ColumnDescriptor column, Markdig.Extensions.Tables.Table table)
        {
            int columnCount = table.ColumnDefinitions.Count;
            if (columnCount == 0 && table.Count > 0)
            {
                columnCount = ((Markdig.Extensions.Tables.TableRow)table[0]).Count;
            }

            if (columnCount == 0)
            {
                return;
            }

            column.Item().PaddingVertical(4).Table(questTable =>
            {
                questTable.ColumnsDefinition(columns =>
                {
                    for (int index = 0; index < columnCount; index++)
                    {
                        columns.RelativeColumn();
                    }
                });

                foreach (Block rowBlock in table)
                {
                    Markdig.Extensions.Tables.TableRow row = (Markdig.Extensions.Tables.TableRow)rowBlock;

                    foreach (Block cellBlock in row)
                    {
                        Markdig.Extensions.Tables.TableCell cell = (Markdig.Extensions.Tables.TableCell)cellBlock;

                        questTable.Cell()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten1)
                            .Background(row.IsHeader ? Colors.Grey.Lighten3 : Colors.White)
                            .Padding(4)
                            .Text(text =>
                            {
                                if (row.IsHeader)
                                {
                                    text.DefaultTextStyle(style => style.Bold());
                                }

                                foreach (Block cellContent in cell)
                                {
                                    if (cellContent is ParagraphBlock paragraph)
                                    {
                                        RenderInlines(text, paragraph.Inline, false, false);
                                    }
                                }
                            });
                    }
                }
            });
        }

        private static void RenderInlines(TextDescriptor text, ContainerInline container, bool bold, bool italic)
        {
            if (container == null)
            {
                return;
            }

            for (Inline inline = container.FirstChild; inline != null; inline = inline.NextSibling)
            {
                if (inline is LiteralInline literal)
                {
                    AddSpan(text, literal.Content.ToString(), bold, italic, false);
                }
                else if (inline is CodeInline code)
                {
                    AddSpan(text, code.Content, bold, italic, true);
                }
                else if (inline is EmphasisInline emphasis)
                {
                    bool nestedBold = bold || emphasis.DelimiterCount >= 2;
                    bool nestedItalic = italic || emphasis.DelimiterCount == 1;
                    RenderInlines(text, emphasis, nestedBold, nestedItalic);
                }
                else if (inline is LineBreakInline)
                {
                    text.Line(string.Empty);
                }
                else if (inline is LinkInline link)
                {
                    RenderLink(text, link, bold, italic);
                }
                else if (inline is ContainerInline nestedContainer)
                {
                    RenderInlines(text, nestedContainer, bold, italic);
                }
            }
        }

        private static void RenderLink(TextDescriptor text, LinkInline link, bool bold, bool italic)
        {
            string linkText = ExtractText(link);

            if (link.IsImage)
            {
                // Images are not embedded (no image download/decoding pipeline for this job); render a text reference instead.
                AddSpan(text, "[" + (string.IsNullOrEmpty(linkText) ? link.Url : linkText) + "]", bold, italic, false);
                return;
            }

            if (string.IsNullOrEmpty(linkText))
            {
                linkText = link.Url ?? string.Empty;
            }

            if (string.IsNullOrEmpty(link.Url))
            {
                AddSpan(text, linkText, bold, italic, false);
                return;
            }

            text.Hyperlink(linkText, link.Url).Underline().FontColor(Colors.Blue.Darken1);
        }

        private static string ExtractText(ContainerInline container)
        {
            StringBuilder builder = new StringBuilder();

            for (Inline inline = container.FirstChild; inline != null; inline = inline.NextSibling)
            {
                if (inline is LiteralInline literal)
                {
                    builder.Append(literal.Content.ToString());
                }
                else if (inline is ContainerInline nested)
                {
                    builder.Append(ExtractText(nested));
                }
            }

            return builder.ToString();
        }

        private static void AddSpan(TextDescriptor text, string content, bool bold, bool italic, bool code)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            if (code)
            {
                if (bold && italic)
                {
                    text.Span(content).FontFamily("Consolas").BackgroundColor(Colors.Grey.Lighten3).Bold().Italic();
                }
                else if (bold)
                {
                    text.Span(content).FontFamily("Consolas").BackgroundColor(Colors.Grey.Lighten3).Bold();
                }
                else if (italic)
                {
                    text.Span(content).FontFamily("Consolas").BackgroundColor(Colors.Grey.Lighten3).Italic();
                }
                else
                {
                    text.Span(content).FontFamily("Consolas").BackgroundColor(Colors.Grey.Lighten3);
                }

                return;
            }

            if (bold && italic)
            {
                text.Span(content).Bold().Italic();
            }
            else if (bold)
            {
                text.Span(content).Bold();
            }
            else if (italic)
            {
                text.Span(content).Italic();
            }
            else
            {
                text.Span(content);
            }
        }
    }
}
