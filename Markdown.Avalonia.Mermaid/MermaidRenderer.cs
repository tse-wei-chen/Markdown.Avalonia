using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Markdown.Avalonia.Mermaid
{
    /// <summary>
    /// Handles the generation of HTML for Mermaid diagrams and capturing them as images.
    /// </summary>
    public class MermaidRenderer
    {
        private readonly float _deviceScaleFactor;

        public MermaidRenderer(float deviceScaleFactor = 2.0f)
        {
            _deviceScaleFactor = deviceScaleFactor;
        }

        public async Task<byte[]?> RenderPngAsync(IBrowserContext context, string code, string theme, string bgColor)
        {
            var page = await context.NewPageAsync();

			try
			{
				string html = GenerateHtml(code, theme, bgColor);
				await page.SetContentAsync(html);
				await page.WaitForFunctionAsync("() => window.__MERMAID_RENDER_DONE === true", 
					new PageWaitForFunctionOptions { Timeout = 10000 });

				var element = await page.QuerySelectorAsync("#m svg");
				element ??= await page.QuerySelectorAsync("#m");

				if (element == null) return null;

				bool omitBg = bgColor == "transparent";
				return await element.ScreenshotAsync(new ElementHandleScreenshotOptions { OmitBackground = omitBg });
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				await page.CloseAsync();
			}
        }

        private string GenerateHtml(string code, string theme, string bgColor)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                    <head>
                        <meta charset='utf-8'/>
                        <script src='https://cdn.jsdelivr.net/npm/mermaid@11.12.2/dist/mermaid.min.js'></script>
                        <style> 
                            body {{ margin:0; padding:8px; background:transparent; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }} 
                        </style>
                    </head>
                    <body>
                        <div class='mermaid' id='m'>{System.Net.WebUtility.HtmlEncode(code)}</div>
                        <script>
                        mermaid.initialize({{ 
                            startOnLoad: false, // Turn off auto-load
                            securityLevel: 'loose', 
                            theme: '{theme}', 
                            themeVariables: {{ 
                                'background': '{bgColor}',
                                'fontFamily': 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif'
                            }},
                            flowchart: {{ useMaxWidth: false, htmlLabels: true }},
                            sequence: {{ useMaxWidth: false, htmlLabels: true }},
                            gantt: {{ useMaxWidth: false, htmlLabels: true }},
                            class: {{ useMaxWidth: false, htmlLabels: true }},
                            state: {{ useMaxWidth: false, htmlLabels: true }},
                            er: {{ useMaxWidth: false, htmlLabels: true }},
                            journey: {{ useMaxWidth: false, htmlLabels: true }},
                            mindmap: {{ useMaxWidth: false, htmlLabels: true }},
                            gitGraph: {{ useMaxWidth: false, htmlLabels: true }}
                        }});
                        
                        document.addEventListener('DOMContentLoaded', async () => {{
                            await document.fonts.ready;
                            // Explicitly run render
                            await mermaid.run();
                            window.__MERMAID_RENDER_DONE = true;
                        }});
                        </script>
                    </body>
                </html>";
        }
    }
}
