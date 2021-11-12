# ![CObs COVID Observational Model](https://github.com/lmw-gh-2020/CObs/blob/assets/CObs-Banner.png)

![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/lmw-gh-2020/CObs)
![GitHub top language](https://img.shields.io/github/languages/top/lmw-gh-2020/CObs)

## CObs COVID Observational Model

* [Platform Notes](#platform-notes)
* [Introduction](#introduction)
* [Project Scope](#project-scope)
* [Model Features](#model-features)
* [Sample Results (with screenshots)](#sample-results-with-screenshots)
* [Model Performance](#model-performance)
* [Results Quick Start](#results-quick-start)
* [Workflow Quick Start](#workflow-quick-start)
* [Developer Quick Start](#developer-quick-start)
* [Contributions and Extensions](#contributions-and-extensions)
* [Technical Notes](#technical-notes)
* [License](#license)

### Platform Notes

CObs is implemented in C# for the .NET Framework, (it may also be built for .NET Core), uses Apache
Open Office for data visualization, and is run through [codefactor.io](https://codefactor.io) for
automated code quality analysis and review.

### Introduction

The CObs data pipeline produces a model of estimated real-world SARS-CoV-2 transmission in a target
region, via empirical fit under SEIR-type assumptions, from a stream of public health data that
incorporates:

- Mortality
- Hospital Occupancy/Admissions
- Reported Cases
- Deployed Test Capacity/Test Positivity Percentage

The results feed into an integrated dashboard for data visualization and review.

Unlike most dashboards, which do no modeling and present only raw "live" feeds from disparate
sources, CObs accounts properly for the time-intervals between transmission, and the various types
of reporting event.

Low, baseline and high [transmission estimates](#transmission-b117) are provided by running the
model over a large collection of scenarios, generated from a range of input parameters for both
the various time-intervals, and epidemiological parameters such as IFR (Infection Fatality Ratio)
and Median Serial Interval between transmission events.

CObs also extracts and in some cases plots, transmission metrics such as [R-effective](#r-eff-2020),
and doubling time.

CObs was initially run on public health data published by the Government of Hungary, with
Hungary as the target region, and the releases include a bundle of historical data generated
for Hungary for the period from 2020-10-04 to 2021-05-03.

To start viewing these results straight away, jump to [Results Quick Start](#results-quick-start).

### Project Scope

The initial purpose of the CObs pipeline and integrated dashboard, was to provide as near real-time
as possible monitoring of local SARS-CoV-2 transmission in Hungary during the period 2020-2021, as
a means of obtaining general situational awareness regarding the state of the pandemic in Hungary.

The model is, broadly speaking, observational rather then predictive. It covers the time period up
to the most recent day having data.

The transmission estimates and associated metrics are based on messy real-world input data, that
feed into scenarios based on uncertain, wide-ranged epidemiological parameters. The aim is to
provide a practical, first-order handle on the approximate magnitude and growth rate of the
transmission curve, as means of gauging **"where are we now"** that is a marked step up from
staring at raw or plainly graphed hospitalizations, cases and test positivity.

CObs makes loose assumptions about the broad stability of IFR and the immunological homogeneity
of the population of the target region. It is best utilised for as near real-time as possible
monitoring of the state of the epidemic in the target region during the phase where the
population is immunologically naïve, (or equally exposed) to the pathogen to a first order
approximation.

For example, the workflow for the bundled historical data for Hungary was retired in May 2021
due to the onset of real-world decoupling between cases, admissions and mortality, driven by
age-stratified vaccination roll-out and immunity acquired from transmission.

Currently CObs is somewhat inflexible regarding the format of the CSV file that feeds in the source
data, and uses hardcoded epidemiological parameter ranges and population size, rather than configurable
ones.

These restrictions on its usage can potentially be remedied easily enough, (c.f.
[Contributions and Extensions](#contributions-and-extensions)) rendering it suitable for
configuration and use for monitoring any sufficiently sizeable, broadly IFR-stable epidemic, for
any pathogen, in any target region. Some extra degree of flexibility over the type and format
of source data pipeline inputs could also be added. If extended in such a way, the project should
probably be renamed EObs.

Note however, that thus far CObs is a single author volunteer project.

### Model Features

* [Overview](#overview)
* [Scenario Internals](#scenario-internals)
* [Inferred Admissions](#inferred-admissions)
* [Aggregates](#aggregates)

#### Overview

The model combines the signal from three primary sources that are indicative of transmission levels
into a single empirical fit, namely:

- **Mortality:** with cases inferred from IFR. The most reliable signal in terms of the absolute
magnitude of real-world transmission, with the highest lag and lowest time resolution.
- **Hospital Admissions:** with the inferred admissions-to-cases ratio anchored to mortality and IFR.
A less reliable signal than mortality, with lower lag and higher time resolution.
- **Reported Cases:** with the inferred reported-to-real ratio anchored to admissions, mortality and IFR.
The least reliable signal, with the lowest lag and highest time resolution.

Note that CObs observes real-world transmission via inference from these signals, rather than
simulating or predicting it, (at least mostly, c.f. [Scenario Internals](#scenario-internals)
below).

Each model pipeline execution pass runs approximately 2000 observed transmission scenarios with
combinations of different values of the following 7 parameters:

- **IFR:** Infection Fatality Ratio, i.e. the ratio of mortality to cases.
- **MedianTimeFromExposureToTest:** Median time from exposure to a report of a positive test result.
- **MedianTimeToAdmission:** Median time from exposure to hospital admission.
- **MedianHospitalizationDuration:** Median duration of hospital occupancy, (used to infer admissions
from absolute bed occupancy in the absence of direct admissions data).
- **MedianTimeToMortality:** Median time from exposure to mortality.
- **MedianSerialInterval:** Median time from exposure to onward transmission.
- **TransmissibilityWindowToSI:** Ratio of median duration of transmissibility to median serial interval.

It should be noted that each of these parameters can only be considered constant to a first order
approximation. For instance under real world conditions IFR is sensitive to available healthcare capacity,
and time from exposure to reported test result is sensitive to conditions in the testing logistics train.

After the scenarios have been run, low, baseline (i.e. median), and high transmission values and their
associated metrics are selected for each day. A final round of extraction is then performed to obtain some
current and cumulative aggregates of note.

The computed daily transmission metrics for each scenario are: **growth rate**, **R-effective** and
**doubling time**.

The parameter ranges currently used by CObs were obtained via an informal, (non-systematic) survey of the
literature, to which the author has unfortunately not kept references.

#### Scenario Internals

Each scenario accounts for the lags inherent in the three primary signals by dividing the timeline
of the run into five regions with their own distinct inference logic.

The signals from mortality and admissions are given equal weight where possible, the signal from reported
cases is ignored until absolutely necessary, (i.e. when the other two have both gone offline) as it is
deemed inherently unreliable.

The scenario timeline regions are handled as follows:
- **Mortality Only Run Up Period:** Transmission for the time period from one median-time-to-mortality
prior to the first source day until the first source day itself is inferred from the mortality
signal only, by direct extrapolation from IFR. Technically the admissions signal comes online
during the later part of this period, but it makes little difference to the overall results and
is ignored.
- **Mortality And Admissions Based Period:** Transmission for the time period from the first source
day until one median-time-to-mortality prior to the current day is inferred from mortality and
admissions with equal weight. The admissions-to-cases ratio is continually recalibrated,
(re-anchored to mortality and IFR) to reflect its dependence on healthcare capacity pressures
during different phases of the epidemic. A wide rolling window is used for re-anchoring so as
not to drown out the higher resolution signal from admissions.
- **Admissions Only Based Period:** Transmission for the time period from one median-time-to-mortality
prior to the current day until one median-time-to-admission prior to the current day is inferred
from admissions only, with the admissions-to-cases ratio now fixed at the last seen value.
- **Test Results Only Based Period:** Transmission for the time period from one median-time-to-admission
prior to the current day until one median-time-from-exposure-to-test prior to the current day
is inferred from reported cases, now that the other two signals are offline. A handover
reported-to-real ratio is calculated at the transition boundary, (based on smoothed rolling
averages around the boundary).
- **Retrodicted Transmission:** Transmission for the time period from one median-time-from-exposure-to-test
until the current day is simulated, now that all three signals are offline and we are flying blind.
The simulated values are based on the last seen magnitude of transmission and last seen growth exponent,
(or rather suitably smoothed rolling average versions thereof).

During the test-results-only based period, signal unreliability due to drift in deployed test
capacity and test positivity is compensated for, by adjusting the reported-to-real ratio
accordingly. For an extreme example that illustrates why this is important, were deployed
test capacity to suddenly drop to one tenth of yesterday's reported cases with 100% positivity,
it doesn't mean real-world transmission has suddenly dropped by an order of magnitude!

For these purposes, deployed capacity and positivity are treated as de facto (loosely coupled)
pseudo-independent variables with a time-local linear relationship to reported cases, and
linear correlation coefficients to adjust the reported-to-real ratio are inferred. In doing
so we are assuming that non-linearities in the underlying real-world logistical relationship
between deployed test capacity and case reporting can be ignored on short enough time scales.

Clearly, scenario implementation is contingent on some fairly ad hoc design decisions and
modelling assumptions, particularly in regard to the relative weighting of signal strengths
and the autocalibration and boundary transition logic. It's fair to say that scenario runs
have a certain approximate "finger in the air" quality to them.

Nonetheless the assumptions are reasonable enough, and the design decisions sensible enough,
to provide first-order transmission estimates commensurate with [project scope](#project-scope).

#### Inferred Admissions

In many regions of interest, including Hungary, public authorities do not publish a feed of
daily hospitalizations, instead publishing only absolute bed occupancy numbers.

This accounts for the introduction of median hospitalization duration as a scenario parameter,
as it can be used to infer admissions from occupancy, by assuming a rate of churn, (discharge)
based on reported occupancy levels over the prior median hospitalization duration period.

Specifically, churn is considered proportional to the integral of occupancy with respect to
time, from one median stay duration prior to a given day until that day. This yields a churn
delta that can be summed with the observed raw occupancy delta to give an admittedly crude
estimated admissions for the day.

There would be scope to improve the inference if it transpired that a reasonable range of
possible values for variance of stay duration were also known, however the simple formula
used is sufficient in our decidedly first-order context.

If admissions data were directly available, this step could be skipped entirely, reducing the
number of scenarios by two thirds, and improving the resolution and accuracy of the model.

#### Aggregates

Once all scenarios have run, and low, baseline and high transmission estimates and associated
metrics have been determined, the following current and cumulative model aggregates are extracted:

- **Current R-effective**
- **Current Doubling Time**
- **Total Projected Mortality**
- **Cumulative Seroprevalence**

Ranged estimates, (low, baseline and high) are provided for each aggregate.

### Sample Results (with screenshots)

Here for illustrative purposes are some sample results, with annotations, from the Hungary
2020-2021 CObs run.

#### Transmission

CObs provided timely warning of the arrival of the B.1.1.7 (Alpha) SARS-CoV-2 VOC
(Variant Of Concern) in Hungary in early February 2021, by quickly attaining a clear local
minimum and subsequent uptick in estimated transmission. Even under relatively stringent
NPIs, (Non Pharmaceutical Interventions), once the surge took off the results were dramatic.

<a id="transmission-b117">![B.1.1.7 Transmission Surge](https://github.com/lmw-gh-2020/CObs/blob/assets/Transmission-Surge-B117.png)</a>

Note that CObs currently operates from a fixed set of epidemiological scenario parameters, so
IFR and other related parameters were not tweaked to reflect the altered characteristics of
the ecologically dominant pathogen, which will have lead to some degree of overestimation of
transmission in the latter part of the run, (by back-of-the-envelope reasoning, probably
around a third).

As the main purpose of CObs is to gain a rapid first-order handle on the magnitude and direction
of travel of the epidemic under observation, this was perfectly acceptable in terms of
[project scope](#project-scope).

#### R-effective

While the model is agnostic as to whether a drop in transmission is due to persons acquiring
strong sterilizing immunity to the pathogen or because they have reduced their exposure
via social distancing, the correspondence in the Hungary 2020-2021 CObs run between
computed baseline R-effective and dramatic on-the-ground logistical changes in the stringency
of NPIs was striking, both in terms of timely indication of changes, and in overview.

Here we can see how inferred R-effective tracks closely with the underlying dynamics of
societal pandemic response as occurred in Hungary in the second half of 2020:

- **Summer 2020**: A period of almost entirely uncontrolled rapid cryptic transmission at
low incidence is observable in the second half of the summer, with R-effective close to the
R-0 of the original wild-type SARS-CoV-2 variant. This corresponds to the "summer
tourism reopening" period of relatively unrestricted travel and tourism in Hungary.
- **Autumn 2020**: A period of increasingly severe NPIs were instituted over the autumn
at medium and high incidence, (including a test capacity surge with aggressive home
quarantine orders, mask mandates, and increasingly strict but partial school and business
closures) that resulted in a considerably reduced R-effective still above 1, amounting to
a failed attempt at resurgence control.
- **Winter 2020**: Mounting healthcare capacity saturation led to the institution of a full
lockdown at high incidence. R-effective dropped rapidly and unambiguously below 1.

<a id="r-eff-2020">![R-effective in 2020 related to level of NPIs](https://github.com/lmw-gh-2020/CObs/blob/assets/R-Eff-Annotated.png)</a>

Pointing out this correspondence is not intended to serve as a political critique of the
desirability or otherwise of NPIs as a response to this or any other epidemic, though the
author notes that if they are going to be instituted, then best to move fast at low
incidence, as delayed responses often result in "worst of both worlds" outcomes.

### Model Performance

Some notes on model performance as observed during the Hungary 2020-2021 CObs run:

- The parameters were well chosen, with the baseline transmission curve fluctuating little over
time, and yielding cumulative and projected aggregates commensurate with later outcomes.
- The model is responsive, with trends such as changes in the stringency of NPIs and the emergence
of variants with new epidemiological characteristics appearing in the result sets on the order of
a week to ten days, loosely speaking.
- Sometimes the model fluctuates wildly for a brief time while passing through inflection points
in transmission, due to temporarily wide spread in extracted transmission metrics.
- Extracting meaningful doubling times in a situation where the dynamics change from week to week
is hard. R-effective is a much more useful metric to track. Given the good performance
characteristics of baseline transmission estimates, narrower scenario input parameter ranges would
have been justified and would have yielded more stable doubling times.
- The model tends to produce markedly truncated transmission peaks at high incidence, which the author
strongly suspects to be an artifact both of logistical saturation in real world reporting systems,
and of an effective cap on admissions that comes into effect while emergency triage conditions pertain
during periods of healthcare capacity strain.
- The model also briefly fluctuates wildly around some pretty odd artifacts in the reporting of
deployed test capacity. Even with a clean modelling pipeline, garbage in begets garbage out.

Overall the model served well as a first-order, as close to real time as possible monitoring tool
for the basic magnitude and direction of travel of transmission during the epidemic in the target
region, as intended.

### Results Quick Start

To start viewing the bundled historical results data, go to [Releases](https://github.com/lmw-gh-2020/CObs/releases)
and under **Assets** download `ResultsOnly.zip`, (or change branch to `results` and choose
Code -> Download Zip). The historical results are in the folder `ResultsOnly\`.

#### Requirements

Requires Apache Open Office `4.1.7` or later, running on any operating system that supports Open
Office, (Windows, Linux, Mac et al.) to render the historical results. Data visualization is
performed in the spreadsheet program Open Office Calc, with both the live results viewer and
historical results stored as `.ods` files.

![Apache Open Office](https://github.com/lmw-gh-2020/CObs/blob/assets/Open-Office-Logo.png)

Apache Open Office is open source, lightweight and easy to download and install. You can
download a version for your operating system here:

[https://www.openoffice.org/download/](https://www.openoffice.org/download/)

The spreadsheet files might work in older versions of Open Office Calc. Microsoft Excel also has
the capability to import `.ods` files, though this hasn't been tested and may well wreak havoc
with formulas, charts, formatting etc.

#### Dashboard Sections

The integrated dashboard historical results snapshots and live results viewer each contain 8
sections implemented as Open Office Calc worksheets:

- **Totals:** The headline results of Projected Total Mortality and Cumulative Seroprevalence.
- **Transmission:** Graph of low, baseline and high estimated transmissions per day, with current metrics.
- **REff:** Graph of the extracted R-eff associated with baseline transmission.
- **Hospitalizations:** Plot of 5-day rolling average hospitalizations, classic dashboard style.
- **TestPositivity:** Plot of 5-day rolling average test positivity, classic dashboard style.
- **TestCapacity:** Plot of 5-day rolling average test capacity, classic dashboard style.
- **ResultsData:** The imported results data set, (raw).
- **Aggregates:** The imported current and cumulative aggregates, (raw).

### Workflow Quick Start

To start running the model pipeline on suitably formatted CSV source data files, go to
[Releases](https://github.com/lmw-gh-2020/CObs/releases) and under **Assets** download `Workflow.zip`,
(or change branch to `workflow` and choose Code -> Download Zip). The pipeline is launched via
the executable `CObs.exe` in the folder `Workflow\`, either by double click in any suitable
File Explorer such as Windows Explorer, or by console invocation, and expects to find a correctly
formatted CSV file named `SourceData.txt` in the same working folder as `CObs.exe`.

Results from a single model pipeline execution pass will be written to the CSV files
`ResultsData.txt` and `Aggregates.txt` in the same working folder as `CObs.exe`, and will
additionally be copied to the sub-folder `CObsResults\`, (any previous results will be overwritten).

Results may then be visualized by opening the bundled live results viewer `CObsMain.ods` in the
same working folder as `CObs.exe`, answering `Yes` to the questions "**Update all links?**" and
"**This file contains links to other files. Should they be updated?**" as and when they appear.

To produce a historical snapshot `.ods` file with no live links, (e.g. in the `CObsResults\` folder
alongside the produced copies of `ResultsData.txt` and `Aggregates.txt`) copy `CObsMain.ods` to
a suitably named file with some kind of datestamp in the filename. Then open the copy, select
"**Edit -> Links...**" from the Calc menu bar, choose "**Break Link**" for both linked files,
and then save the changes.

There is currently no macro to automate this step, (as Apache Open Office macros tend to be
somewhat tricky to set up a working environment for).

#### Requirements

The bundled version of `CObs.exe` runs on any version of Microsoft Windows having .NET Framework
`4.6.1` or higher installed, (though it is possible for developers to build a version that runs
as a console application on any operating system with .NET Core `2.0` or higher installed,
including Windows, Linux, Mac, et al.). The bundled version of the live results viewer `CObsMain.ods`
requires a functioning installation of Apache Open Office `4.1.7` or later.

As the workflow bundle contains an unsigned built executable `CObs.exe`, you may prefer to build
the program from source yourself, depending on your environment.

### Developer Quick Start

By default `CObs.exe` prompts the user "Press any key to exit.", as the most common use case
is that the user double clicked on the executable in a File Explorer, launching a console that
automatically closes on program exit, and making it hard for the user to visually track
model pipeline build progress.

`CObs.exe` accepts the commandline argument `nokey` and may be invoked from the console or
via script as `CObs.exe nokey`. This is to facilitate uninterrupted batch processing of multiple
data sources.

The simplest way to build `CObs` as a .NET Core console application is to create a blank
.NET Core console application project in an appropriate development environment, add the files
from `master`:

- `Base.cs`
- `Build.cs`
- `Parameters.cs`
- `Program.cs`
- `Scenario.cs`

into the project at root level, and build.

#### Requirements

A functioning build environment for .NET Framework `4.6.1` or greater, or .NET Core `2.0` or greater.
You know what to do.

### Contributions and Extensions

Note that CObs is a single author volunteer project.

In terms of implementing new features or extensions, the author may be prepared to spend limited time on
implementation and/or reviewing pull requests from contributors, particularly if intended for serious enough
routine usage by third parties in a formal research or public health context.

Low hanging fruit that would render CObs suitable for use for any sufficiently sizeable, broadly IFR-stable
epidemic in an immunologically homogeneous population, for any pathogen, for any target region would include:

- Taking scenario population size and epidemiological parameter ranges from external configuration.
- Adding a mode to bypass inference of admissions from occupancy, in case admissions data is readily available.
- More flexibility over the format of source data CSV files.
- Polite checking and error reporting regarding IO sanity when reading and writing files.
- Performance tuning the extraction phase (c.f. [Machine Performance](#machine-performance) notes below).

CObs is open source, so anyone is free to experiment with it and potentially fork it as they see fit.

### Technical Notes

#### Machine Performance

CObs.exe takes about 30 seconds to process a year's worth of data on a standard i8 laptop. Currently
scenario day slice selection and sorting in the results extraction phase is performed by untuned .NET
LINQ expressions, causing this to slow down to approximately 15 minutes on a standard i8 laptop for a
large six year test data set. In principle performance should be linear on the size of the source data,
as all computations are time-local apart from a few trivial linear sums to extract cumulative aggregates.
Day slice selection via a sensible flat indexing mechanism would help considerably here, though the
current performance characteristics are perfectly fine given the use requirements.

#### Why Apache Open Office?

An open source visualizer of some kind was desired, ruling out Microsoft Excel, (also the author did
not have a personal copy of Excel to hand at the time). A web-based visualization package would clearly
be a desirable feature set for publishing and sharing results. However, compared to the feature rich
code-free out-of-the-box capabilities of a fully fledged spreadsheet package, achieving a similar result
with a web-based graphing and presentation layer would have been a lot of extra work to implement.

#### Testing and Design

Historically, CObs was initially thrown together and used in production to start generating crude
results as piece of one-shot data analysis for distribution to a small offline audience, i.e. it
began as an exploratory coding spike in Agile development terms.

Since then it has been run routinely, deemed good enough to keep, and iterated over and refactored
a few times. The class design is relatively clean, with clear contracts and limited redundant
dependency between the classes, (though the classes are a little overloaded with responsibilities,
and could use being split out some more, and scenarios would be cleaner if refactored to be
stateless).

While the author is generally an advocate of class structures informed and refined by TDD/BDD in the
right context, CObs has not currently been brought under unit test. Partly this is due to the manner
in which development history interacted with project resource constraints. Given that the program is
relatively small, and has simple internal contracts between classes and few external dependencies,
the potential value of unit testing in also providing meaningful regression tests is fairly limited.

In terms of regression testing for consistency, unit tests would do little more than verify that
the program indeed reads a correctly formatted CSV file and outputs two CSV files in the right
format, and that various internal public methods do indeed read and return certain lists of expected
row structures.

Regression testing for mathematical model correctness would be a huge undertaking involving
the careful and painstaking independent preparation of comprehensive test data source and result
sets, almost tantamount to developing and running a fully independent reference implementation.
Such an effort would be appropriate were CObs being integrated into a fully fledged SEIR modelling
lab product of some kind, or being used in some industrial setting with high-reliability
requirements, but is clearly outside the current scope of the project.

CObs has received large quantities of smoke testing for consistency and smell testing for
correctness. While unit testing would still potentially be useful to provide a basic test-harness
and inform any future design, there are no plans to bring CObs under unit test at this time.

Do not adapt CObs for the real-time monitoring of potential runaway conditions in nuclear reactors,
(c.f. the limitation of liability and disclaimer of warranty clauses in the [software license](#license)
below).

### License

All code, assets and results data are licensed under the [Apache License, Version 2.0](https://opensource.org/licenses/Apache-2.0).

The included sample source data in `Workflow\SourceData.txt` is Public Domain, courtesy of the Government of Hungary.
