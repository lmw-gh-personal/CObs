using System;

/*
*
* Author:  lmw.gh.2020@gmail.com, all rights reserved, October 2020
* License: Apache License, Version 2.0
*
* https://opensource.org/licenses/Apache-2.0 
*
*/

namespace CObs
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
                Process command line arguments and run build.
            */
            Builder builder   = new Builder();
            bool    keyToExit = !((args.Length > 0) && (args[0] == "nokey"));

            Console.WriteLine(
                "CObs build: reading daily data and computing rolling averages..."
            );

            builder.ReadDaysRaw("SourceData.txt");

            Console.WriteLine(
                "CObs build: generating scenarios..."
            );

            builder.GenerateScenarios();

            Console.WriteLine(
                "CObs build: running scenarios..."
            );

            builder.RunScenarios();

            Console.WriteLine(
                "CObs build: extracting result days..."
            );

            builder.ExtractResultDays();

            Console.WriteLine(
                "CObs build: extracting global aggregates..."
            );

            builder.ExtractAggregates();

            Console.WriteLine(
                "CObs build: writing results..."
            );

            builder.WriteResults();

            Console.WriteLine("CObs build: Done.");

            if (keyToExit)
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }
        }
    }
}
