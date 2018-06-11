# Redox-Data-Model-Builder
A JSON schema to .Net Class library converter for Redox Data Model

This library can be configured to download the latest Redox schema ZIP file from redoxengine.com, unzip it, and use the .json schema files to create a complete or partial .Net Class Library for use in Redox API integrations.

The solution contains 2 projects, RedoxDataModelBuilder (Class Library DLL) and RedoxBuild (Wrapper Console Application).
You can:
  1. Build your own wrapper UI around the RedoxDataModelBuilder.dll library.
  2. Use the included wrapper to automate your build process.
  
After the file is generated, simply include it in your project in references and use the namespace you built the dll/cs file with! 
Simple as that.


The RedoxBuild Application supports command-line usage:

Usage:  
/showwin:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;show/hide Console window ("true" or "false")  
/outputtype:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;The output file library Type ("DLL" or "CS")  
/outputfile:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;The full file output path  
/url:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;The URL of the Redox Schema ZIP file  
/namespace:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;The top level namespace for the generated classes  
/models:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;A Comma-separated list of Models to use, OR "all":

                all
                claim
                clinicaldecisions
                clinicalsummary
                device
                financial
                flowsheet
                inventory
                media
                notes
                order
                patientadmin
                patientsearch
                provider
                referral
                results
                scheduling
                sso
                surgicalscheduling
                vaccination
