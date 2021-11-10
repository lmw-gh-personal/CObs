using System;
using System.Linq;

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
        static void reportValidationError(SourceValidationStatus pStatus)
        {
            if (!pStatus.SourceOK)
            {
                string error;

                Console.WriteLine(
                    "CObs build: validation error reading SourceData.txt on line "
                  + pStatus.RowNumber
                  + "."
                );

                switch (pStatus.RowStatus)
                {
                    case SourceRowValidationStatus.WrongNumberOfColumns:
                        error = "had wrong number of columns";
                        break;
                    case SourceRowValidationStatus.DateUnreadable:
                        error = "had unreadable date";
                        break;
                    case SourceRowValidationStatus.DateNotContiguous:
                        error = "date was not contiguous with previous row";
                        break;
                    case SourceRowValidationStatus.DNCUnreadable:
                        error = "had unreadable DNC";
                        break;
                    case SourceRowValidationStatus.DNCNegative:
                        error = "had negative DNC";
                        break;
                    case SourceRowValidationStatus.TestsUnreadable:
                        error = "had unreadable Tests";
                        break;
                    case SourceRowValidationStatus.TestsNegative:
                        error = "had negative Tests";
                        break;
                    case SourceRowValidationStatus.PositivityUnreadable:
                        error = "had unreadable Positivity";
                        break;
                    case SourceRowValidationStatus.PositivityNotBetweenZeroAndOneHundred:
                        error = "had Positivity not between 0 and 100";
                        break;
                    case SourceRowValidationStatus.MortalityUnreadable:
                        error = "had unreadable Mortality";
                        break;
                    case SourceRowValidationStatus.MortalityNegative:
                        error = "had negative Mortality";
                        break;
                    case SourceRowValidationStatus.HospitalizationsUnreadable:
                        error = "had unreadable Hospitalizations";
                        break;
                    case SourceRowValidationStatus.HospitalizationsNegative:
                        error = "had negative Hospitalizations";
                        break;
                    default:
                        error = "had unknown error";
                        break;
                }

                Console.WriteLine("CObs build: row " + error + ".");
            }
        }

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

            SourceValidationStatus status = builder.ReadDaysRaw("SourceData.txt");

            if (
                (!status.SourceOK)
            ||  (builder.ScenarioBaseDays.DaysRaw.Count
                    <= builder.Scenarios.MedianTimeToMortalityValues.Max())
            ) {
                if (!status.SourceOK) { reportValidationError(status); }
                else
                {
                    Console.WriteLine(
                        "CObs build: "
                      + "daily data must contain more rows than max median time to mortality."
                    );

                    Console.WriteLine(
                        "CObs build: minimum number of rows is therefore currently: "
                      + (builder.Scenarios.MedianTimeToMortalityValues.Max() + 1).ToString()
                      + "."
                    );
                }
            

                if (keyToExit)
                {
                    Console.WriteLine("\nPress any key to exit.");
                    Console.ReadKey();
                }

                Environment.Exit(1);
            }

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

            Environment.Exit(0);
        }
    }
}
