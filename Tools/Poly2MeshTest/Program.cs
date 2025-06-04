using Newtonsoft.Json;
using Poly2Tri;

/*
To reproduce, call tcx.PrepareTriangulation(poly)+Triangulate(tcx) with the following polygon:
poly = Poly2Tri.Polygon(...points...): [{"X":4599.296875,"Y":-3661.617919921875},{"X":4597.75048828125,"Y":-3659.434814453125},{"X":4597.18505859375,"Y":-3659.940185546875},{"X":4597.4033203125,"Y":-3661.771728515625},{"X":4596.89794921875,"Y":-3660.601806640625},{"X":4595.970703125,"Y":-3661.0966796875},{"X":4596.1474609375,"Y":-3662.20654296875},{"X":4598.880859375,"Y":-3665.3857421875},{"X":4598.880859375,"Y":-3665.459228515625},{"X":4598.64892578125,"Y":-3667.198974609375},{"X":4599.92578125,"Y":-3667.947998046875},{"X":4599.68798828125,"Y":-3666.437744140625},{"X":4599.68994140625,"Y":-3666.41796875},{"X":4600.04736328125,"Y":-3665.21240234375},{"X":4599.85595703125,"Y":-3664.1513671875},{"X":4599.82568359375,"Y":-3664.067138671875},{"X":4599.017578125,"Y":-3662.76953125},{"X":4599.1884765625,"Y":-3661.61328125}]
poly.AddHole(...points...): [{"X":4599.1884765625,"Y":-3661.61328125},{"X":4599.1884765625,"Y":-3661.61328125},{"X":4599.1884765625,"Y":-3661.607421875},{"X":4599.1884765625,"Y":-3661.61328125}]
*/

class Program
{
    private const string LastVerticesJsonInputFile = "last_vertices.json";
    private const string LastHolesJsonInputFile = "last_holes.json";

    static volatile bool running = true;
    static int lastReturnCode = 0;

    private static int Main()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            running = false;
        };
        while (running)
        {
            // Place your main logic here (e.g., the current Main body)
            // Optionally, break or return if you want to exit after one run
            // For demonstration, call your logic and then wait for Ctrl+C
            lastReturnCode = RunTriangulation();
            Console.WriteLine("Press Ctrl+C to exit or Enter to run again.");
            if (Console.ReadLine() == null) break;
        }

        Console.WriteLine("Exiting with return code: " + lastReturnCode);
        return lastReturnCode;
    }

    private static int RunTriangulation()
    {
        var vertices = AskVertices();
        var poly = new Poly2Tri.Polygon(vertices);
        if (vertices.Count < 3)
        {
            Console.WriteLine("A polygon must have at least 3 vertices.");
            return 1;
        }
        AskHoles(poly);
        // Triangulate it!  Note that this may throw an exception if the data is bogus.
        try
        {
            DTSweepContext tcx = new DTSweepContext();
            tcx.PrepareTriangulation(poly);
            DTSweep.Triangulate(tcx);
            tcx = null;
        }
        catch (System.Exception e)
        {
            //Profiler.Exit(profileID);
            Console.WriteLine(e);
            return 1;
        }
        Console.WriteLine("Triangulation completed successfully. Number of triangles: " + poly.Triangles.Count);
        Console.WriteLine("Triangles:");
        foreach (DelaunayTriangle t in poly.Triangles)
        {
            Console.WriteLine(t);
        }
        return 0;
    }

    private static List<List<PolygonPoint>> AskHoles(Poly2Tri.Polygon poly)
    {
        var holes = new List<List<PolygonPoint>>();
        var lastHolesInput = LoadLastHolesJsonInput();
        if (lastHolesInput.Count > 0)
        {
            Console.WriteLine("Last input holes (json):\n" + string.Join("\n\n", lastHolesInput));
            Console.Write("Use previous input? (y/n) [y]: ");
            var answer = ConsoleReadLine();
            if (answer == null || answer.Trim().Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (var holeInput in lastHolesInput)
                {
                    var hole = ParsePoints(holeInput);
                    holes.Add(hole);
                    poly.AddHole(new Poly2Tri.Polygon(ParsePoints(holeInput)));
                }
                return holes;
            }
        }

        var holeInputs = new List<string>();
        while (true)
        {
            Console.WriteLine("Add hole, enter vertices (json) (or just press Enter to skip):");
            var holeInput = ConsoleReadLine();
            if (null == holeInput)
                break;
            var hole = ParsePoints(holeInput);
            if (hole.Count < 3)
            {
                Console.WriteLine("A polygon hole must have at least 3 vertices. Found: " + hole.Count);
                continue;
            }
            holes.Add(hole);
            holeInputs.Add(holeInput);
            poly.AddHole(new Poly2Tri.Polygon(hole));
        }
        if (holeInputs.Count > 0)
        {
            SaveLastHolesJsonInput(holeInputs);
        }

        return holes;
    }

    private static List<PolygonPoint> AskVertices()
    {
        List<PolygonPoint>? vertices = null;

        var lastVerticesInput = LoadLastVerticesJsonInput();
        if (null != lastVerticesInput)
        {
            Console.WriteLine("Last input vertices (json):\n" + lastVerticesInput);
            Console.Write("Use previous input? (y/n) [y]: ");
            var answer = ConsoleReadLine();
            if (answer == null || answer.Trim().Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                vertices = ParsePoints(lastVerticesInput);
                return vertices;
            }
        }

        while (vertices == null || vertices.Count == 0)
        {
            Console.WriteLine("Enter vertices (json):");
            var verticesInput = ConsoleReadLine();
            if (verticesInput == null)
            {
                Console.WriteLine("Input cannot be empty. Please try again.");
                continue;
            }
            try
            {
                vertices = ParsePoints(verticesInput);
                if (vertices.Count < 3) {
                    Console.WriteLine("A polygon hole must have at least 3 vertices. Found: " + vertices.Count);
                }
            }
            catch
            {
                Console.WriteLine("Invalid format. Please enter valid json.");
                vertices = null;
            }
            SaveLastVerticesJsonInput(verticesInput);
        }

        return vertices;
    }

    private static string? ConsoleReadLine()
    {
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    static List<PolygonPoint> ParsePoints(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];
        return JsonConvert.DeserializeObject<List<PolygonPoint>>(input) ?? throw new InvalidOperationException();
    }

    private static string? LoadLastVerticesJsonInput()
    {
        var input = "";
        if (File.Exists(LastVerticesJsonInputFile))
            input = File.ReadAllText(LastVerticesJsonInputFile).Trim();
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    private static void SaveLastVerticesJsonInput(string input)
    {
        File.WriteAllText(LastVerticesJsonInputFile, input);
    }

    private static List<string> LoadLastHolesJsonInput()
    {
        var input = "";
        if (File.Exists(LastHolesJsonInputFile))
            input = File.ReadAllText(LastHolesJsonInputFile).Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }
        // explode the input into a list of strings using character #
        return input.Split(['#'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
    }

    private static void SaveLastHolesJsonInput(List<string> holes)
    {
        var input = string.Join("#", holes);
        File.WriteAllText(LastHolesJsonInputFile, input);
    }
}
