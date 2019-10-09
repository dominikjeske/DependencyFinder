using Alba.CsConsoleFormat;
using DependencyFinder.Core;
using DependencyFinder.Utils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.ConsoleColor;

namespace DependencyFinder
{
    public class ReferencesRunner
    {
        public async Task Run(ReferencesOptions no)
        {
            if (!Directory.Exists(no.RootPath))
            {
                ConsoleEx.WriteErrorLine($"Path {no.RootPath} not exists");
                return;
            }

            var sm = new SolutionManager();
            var result = await sm.FindAllReferences();

            var headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);
            var doc = new Document(new Span("CommonFull.TestClass:") { Color = Yellow }, "\n",
                                   new Grid
                                   {
                                       Color = Gray,
                                       Columns = { GridLength.Star(1), GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                                       Children = {
                                            new Cell("Project") { Stroke = headerThickness},
                                            new Cell("File") { Stroke = headerThickness },
                                            new Cell("Block") { Stroke = headerThickness },
                                            new Cell("Namespace") { Stroke = headerThickness },
                                            new Cell("Class") { Stroke = headerThickness },
                                            new Cell("Line") { Stroke = headerThickness },
                                            result.Select(item => new[]
                                            {
                                                new Cell(item.ProjectName),
                                                new Cell(item.FileName),
                                                new Cell(item.Block),
                                                new Cell(item.Namespace),
                                                new Cell(item.ClassName),
                                                new Cell(item.LineNumber),
                                            })
                                       }
                                   }
                               );

            ConsoleRenderer.RenderDocument(doc);

        }
    }
}