using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
    public class DayRaw
    {
        public int      TimelineIndex    { get; set; }
        public DateTime Date             { get; set; }
        public int      DNC              { get; set; }
        public int      Tests            { get; set; }
        public double   Positivity       { get; set; }
        public int      Mortality        { get; set; }
        public int      Hospitalizations { get; set; }

        public DayRaw(int pIndex, List<string> pRaw)
        {
            TimelineIndex    = pIndex;

            Date             = DateTime.Parse(pRaw[0]);
            DNC              = Int32.Parse(   pRaw[1]);
            Tests            = Int32.Parse(   pRaw[2]);
            Positivity       = Double.Parse(  pRaw[3]);
            Mortality        = Int32.Parse(   pRaw[4]);
            Hospitalizations = Int32.Parse(   pRaw[5]);
        }
    }

    public class DayRolling
    {
        public DayRaw Raw                           { get; set; }
        public double Rolling5DayDNC                { get; set; }
        public double Rolling5DayTests              { get; set; }
        public double Rolling5DayPositivity         { get; set; }
        public double Rolling5DayMortality          { get; set; }
        public double Rolling5DayHospitalizations   { get; set; }
        public double Rolling101DayMortality        { get; set; }
        public double Rolling101DayHospitalizations { get; set; }

        public DayRolling(int pIndex, List<DayRaw> pDaysRaw)
        {
            /* compute rolling averages */
            int minIndexShort = (
                (pIndex - 2)  > 0
            ) ? (pIndex - 2)  : 0;

            int minIndexLong  = (
                (pIndex - 50) > 0
            ) ? (pIndex - 50) : 0;

            int maxIndexShort = (
                (pIndex + 2)  < (pDaysRaw.Count - 1)
            ) ? (pIndex + 2)  : (pDaysRaw.Count - 1);

            int maxIndexLong  = (
                (pIndex + 50) < (pDaysRaw.Count - 1)
            ) ? (pIndex + 50) : (pDaysRaw.Count - 1);

            List<DayRaw> daysShort = pDaysRaw.Where(
                raw => (raw.TimelineIndex >= minIndexShort && raw.TimelineIndex <= maxIndexShort)
            ).ToList();

            List<DayRaw> daysLong  = pDaysRaw.Where(
                raw => (raw.TimelineIndex >= minIndexLong  && raw.TimelineIndex <= maxIndexLong)
            ).ToList();

            double m = (double)daysShort.Count;
            double n = (double)daysLong.Count;

            Raw                           = pDaysRaw[pIndex];

            Rolling5DayDNC                = daysShort.Select(raw => raw.DNC             ).Sum() / m;
            Rolling5DayTests              = daysShort.Select(raw => raw.Tests           ).Sum() / m;
            Rolling5DayPositivity         = daysShort.Select(raw => raw.Positivity      ).Sum() / m;
            Rolling5DayMortality          = daysShort.Select(raw => raw.Mortality       ).Sum() / m;
            Rolling5DayHospitalizations   = daysShort.Select(raw => raw.Hospitalizations).Sum() / m;
            Rolling101DayMortality        = daysShort.Select(raw => raw.Mortality       ).Sum() / n;
            Rolling101DayHospitalizations = daysShort.Select(raw => raw.Hospitalizations).Sum() / n;
        }
    }

    public class BaseDays
    {
        public List<DayRaw>     DaysRaw     { get; private set; }
        public List<DayRolling> DaysRolling { get; private set; }

        public BaseDays()
        {
            DaysRaw     = new List<DayRaw>();
            DaysRolling = new List<DayRolling>();
        }

        public void ReadDaysRaw(string pFileName)
        {
            int index = 0;

            /* read raw source data */
            using (StreamReader sr = new StreamReader(pFileName))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    DaysRaw.Add(
                        new DayRaw(
                             index
                            ,line.Split(',').ToList().Select(entry => entry.Trim()).ToList()
                        )
                    );

                    index++;
                }
            }

            /* populate rolling averages */
            foreach (DayRaw raw in DaysRaw)
            {
                DaysRolling.Add(new DayRolling(raw.TimelineIndex, DaysRaw));
            }
        }
    }
}
