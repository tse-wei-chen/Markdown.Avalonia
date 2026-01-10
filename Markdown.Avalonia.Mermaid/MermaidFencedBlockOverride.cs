using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media;
using ColorDocument.Avalonia;
using ColorDocument.Avalonia.DocumentElements;
using Markdown.Avalonia.Parsers;
using Markdown.Avalonia.Plugins;

namespace Markdown.Avalonia.Mermaid
{
    public class MermaidFencedBlockOverride : BlockOverride2
    {
        private readonly IBlockOverride? _inner;
        private readonly MermaidBlockHandler _handler;

        public MermaidFencedBlockOverride(IBlockOverride? inner, MermaidBlockHandler handler)
            : base("CodeBlocksWithLangEvaluator")
        {
            _inner = inner;
            _handler = handler;
        }

        public override IEnumerable<DocumentElement>? Convert2(string text, Match firstMatch, ParseStatus status, IMarkdownEngine2 engine, out int parseTextBegin, out int parseTextEnd)
        {
            string lang = firstMatch.Groups[2].Value.Trim();

            if (string.Equals(lang, "mermaid", StringComparison.OrdinalIgnoreCase))
            {
                var closeTagPattern = new Regex($"\\n[ ]*{Regex.Escape(firstMatch.Groups[1].Value)}[ ]*\\n");
                var closeTagMatch = closeTagPattern.Match(text, firstMatch.Index + firstMatch.Length);

                int codeEndIndex;
                if (closeTagMatch.Success)
                {
                    codeEndIndex = closeTagMatch.Index;
                    parseTextEnd = closeTagMatch.Index + closeTagMatch.Length;
                }
                else
                {
                    parseTextEnd = text.Length;
                    codeEndIndex = text.Length;
                }

                parseTextBegin = firstMatch.Index;
                string code = text.Substring(firstMatch.Index + firstMatch.Length, codeEndIndex - (firstMatch.Index + firstMatch.Length));

                string assetPath = engine.AssetPathRoot ?? "";
                
                var border = _handler.ProvideControl(assetPath, "mermaid", code);
                if (border != null)
                {
                    return new[] { new UnBlockElement(border) };
                }
            }

            if (_inner is BlockOverride2 override2)
            {
                return override2.Convert2(text, firstMatch, status, engine, out parseTextBegin, out parseTextEnd);
            }

            return FallbackToDefaultCodeBlock(text, firstMatch, out parseTextBegin, out parseTextEnd);
        }

        private IEnumerable<DocumentElement>? FallbackToDefaultCodeBlock(string text, Match firstMatch, out int parseTextBegin, out int parseTextEnd)
        {
            var closeTagPattern = new Regex($"\\n[ ]*{Regex.Escape(firstMatch.Groups[1].Value)}[ ]*\\n");
            var closeTagMatch = closeTagPattern.Match(text, firstMatch.Index + firstMatch.Length);

            int codeEndIndex;
            if (closeTagMatch.Success)
            {
                codeEndIndex = closeTagMatch.Index;
                parseTextEnd = closeTagMatch.Index + closeTagMatch.Length;
            }
            else
            {
                parseTextBegin = -1;
                parseTextEnd = -1;
                return null;
            }

            parseTextBegin = firstMatch.Index;
            string code = text.Substring(firstMatch.Index + firstMatch.Length, codeEndIndex - (firstMatch.Index + firstMatch.Length));

            var ctxt = new TextBlock() { Text = code, TextWrapping = TextWrapping.NoWrap };
            ctxt.Classes.Add("CodeBlock");
            var scrl = new ScrollViewer();
            scrl.Classes.Add("CodeBlock");
            scrl.Content = ctxt;
            scrl.HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
            var border = new Border();
            border.Classes.Add("CodeBlock");
            border.Child = scrl;

            return new[] { new UnBlockElement(border) };
        }
    }
}
