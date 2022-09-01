using EllieMae.Encompass.Client;
using EllieMae.Encompass.Collections;
using EllieMae.Encompass.Query;
using EllieMae.Encompass.Reporting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReportRunnerSupreme
{
    class Program
    {
        public static bool wrappedInQuotes = true;
        public static string pfx = Reports.ReportingDatabaseCanonicalPrefix;

        private static List<string> fieldsIDs = new List<string>();

        private static EncompassCredentials NorthernMortgage = new EncompassCredentials("BE11139150", "mbosker", "PASSWORD");

        private static EncompassCredentials LTK = new EncompassCredentials("BE11215009", "mguthrie3", "MarkIsCool20!");
        
        private static bool loginWithLTK = true;
        
        public static Session s;

        public static void Main()
        {
            // Initialize Encompass Runtime
            new EllieMae.Encompass.Runtime.RuntimeServices().Initialize();

            // Check for log file and directory
            Log.checkForPath(Log.log_filename);

            // Run application
            Run();

            // Check if session is still connected and disconnect from Encompass
            if (s.IsConnected)
            {
                s.End();
                Log.WriteLine("Encompass Disconnected.");
            }
            Log.WriteLine("Press any key to exit...", false, true);
            Console.ReadKey();
        }

        private static void Run()
        {
            // Connect to Encompass
            StartSession();

            if (s == null || !s.IsConnected)
            {
                Log.WriteLineError("Encompass session was not created successfully.");

                // Try connecting again
                StartSession();
                if (s == null || !s.IsConnected)    
                {
                    Log.WriteLineError("2nd connection attempt unsuccessful. Exiting application.");
                    return;
                }
            }
            ChangeReport(ReportType.INVESTOR_REPORT);

            RunQuery(ReportType.INVESTOR_REPORT);

            Log.WriteLine("Connected to Encompass.");


        }

        private static void ChangeReport(ReportType reportName = ReportType.INVESTOR_REPORT)
        {
            switch (reportName)
            {
                case ReportType.INVESTOR_REPORT:
                    fieldsIDs = new List<string>(){
                        "364",
                        "LoanTeamMember.Name.Loan Officer",
                        //"CX.BSOBRANCHCODE",
                        "4002",
                        "2",
                        "1401",
                        //"2161",
                        //"2218",
                        "761",
                        "762",
                        "Log.MS.Stage",
                        "19"
                    };
                    break;
                case ReportType.FUNDED_LOANS:
                    fieldsIDs = new List<string>(){
                        "364",
                        "LoanTeamMember.Name.Loan Officer",
                        "4002",
                        "4000",
                        "Log.MS.Date.Funding"
                    };
                    break;
                default:
                    fieldsIDs = new List<string>(){
                        "364",
                        "2"
                    };
                    break;
            }
        }

        public enum ReportType { INVESTOR_REPORT, FUNDED_LOANS, LOCKED_LOANS};

        private static void RunQuery(ReportType reportName)
        {
            StringList fields = defineFields();
            if (loginWithLTK)
            {
                fields.Clear();
                for (int i = 0; i < fieldsIDs.Count; i++)
                {
                    if (!fieldsIDs[i].StartsWith("CX."))
                    {
                        fields.Add(pfx + fieldsIDs[i]);
                    }
                }
            }

            QueryCriterion criteria = DefineCriteria();
            // SORT ORDER: SORT BY LOAN NUMBER, MOST RECENT ON TOP (DECENDING)
            SortCriterionList sort = new SortCriterionList();
            sort.Add(new SortCriterion(pfx + "364", SortOrder.Descending));
            // Date variables
            DateTime dateNow = DateTime.Now;
            string year = dateNow.Year.ToString();
            string month = dateNow.Month.ToString();
            if (dateNow.Month < 10) { month = $"0{month}"; }
            string day = dateNow.Day.ToString();
            if (dateNow.Day < 10) { day = $"0{day}"; }
            string sDate = $"{year}-{month}-{day}";
            string fileName = $"{sDate} VCM Data.csv";
            string exportPath = Log.log_dir + fileName;

            // Check for Export Directory
            Log.checkForPath(fileName);

            Log.WriteLine("Making Header Row");
            string headerRow = makeHeaderRow();
            Log.WriteLine("Getting reporting field data");
            string reportData = MakeCursor(fields, criteria, sort);
            Log.WriteLine("Exporting CSV...");
            MakeCSV(headerRow, reportData, exportPath);
        }

        private static string MakeCursor(StringList fields, QueryCriterion criteria, SortCriterionList sort)
        {
            StringBuilder reportData = new StringBuilder();
            StringBuilder row = new StringBuilder();

            try
            {
                LoanReportCursor cur = s.Reports.OpenReportCursor(fields, criteria, sort);

                foreach (LoanReportData cell in cur)
                {

                    for (int i = 0; i < fields.Count; i++)
                    {
                        if (i == 0) { row.Clear(); }
                        /*
                         *     "364",
                    "LoanTeamMember.Name.Loan Officer",
                    "CX.BSOBRANCHCODE",
                    "4002",
                    "2",
                    "1401",
                    "2161",
                    "2218",
                    "761",
                    "762",
                    "Log.MS.Stage",
                    "19"
                        */
                        try
                        {
                            if (cell[fields[i]] != null)
                            {
                                string c = cell[fields[i]].ToString().Trim();
                                if (wrappedInQuotes)
                                {
                                    row.Append("\"");
                                }

                                if ((fields.Count - 1) == i)
                                {
                                    row.Append($"{c}");
                                    if (wrappedInQuotes)
                                    {
                                        row.Append("\"");
                                    }
                                    row.Append($"{Environment.NewLine}");
                                }
                                else
                                {
                                    row.Append($"{c}");
                                    if (wrappedInQuotes) { row.Append("\""); }
                                    row.Append(",");

                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Log.WriteError($"Cell/Field data failure. Continuing on to next cell. ERROR: {ex.Message}");
                            continue;
                        }
                    }

                    reportData.Append(row.ToString());

                }
            } catch(Exception ex)
            {
                Log.WriteLineError($"Error generating report. ERROR: {ex.Message}");
                return $"MISSING REPORT DATA. ERROR: {ex.Message}";
            }

            return reportData.ToString();
        }

        private static void MakeCSV(string header, string data, string fileName)
        {
            try
            {
                StringBuilder contents = new StringBuilder();

                // Add header to report
                if (header.Trim().Length > 0)
                {
                    contents.Append(header.Trim());
                }

                // Add data to report
                if (data.Trim().Length > 0)
                {
                    contents.Append(data.Trim());
                }

                // Write data to a file
                Log.WriteToFile(fileName, contents.ToString());
            }catch(Exception ex)
            {
                Log.WriteLineError($"Error making CSV. ERROR: {ex.Message}");
            }
        }
        private static string makeHeaderRow() {
            StringBuilder row = new StringBuilder();

            if (fieldsIDs == null || fieldsIDs.Count < 1) { return "MISSING HEADER" + Environment.NewLine; }

            try
            {
                // Create Header Row of CSV Report
                for (int i = 0; i < fieldsIDs.Count; i++)
                {
                    try
                    {
                        if (wrappedInQuotes)
                        {
                            row.Append("\"");
                        }
                        row.Append(fieldsIDs[i].Trim().ToUpper());

                        if (wrappedInQuotes)
                        {
                            row.Append("\"");
                        }

                        if (i == (fieldsIDs.Count - 1))
                        {
                            row.Append(Environment.NewLine);
                        }
                        else
                        {
                            row.Append(",");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLineError($"Field data error on header row. ERROR: {ex.Message}");
                        continue;
                    }
                }
            }catch(Exception ex)
            {
                Log.WriteLineError($"Error generating header row. Skipping. ERROR: {ex.Message}");
                return $"MISSING HEADER {Environment.NewLine}";
            }

            Log.WriteLine($"HEADER ROW GENERATED: {row.ToString()}");
            return row.ToString();
        }

        private static StringList defineFields()
        {
            // DEFINE FIELDS TO GRAB (MUST BE IN REPORTING DATABAE)
            StringList fields = new StringList();

            if (fieldsIDs == null || fieldsIDs.Count < 1) { return fields; }

            for (int i = 0; i < fieldsIDs.Count; i++) {
                string f = String.Concat(pfx, fieldsIDs[i].ToString().Trim());
                try
                {
                    // Add field to stringlist
                    if (!fields.Contains(f))
                    { 
                        fields.Add(f);
                    }
                }catch(Exception ex)
                {
                    Log.WriteLine(ex.Message);                
                }
            }
            return fields;
        }

        private static QueryCriterion DefineCriteria(ReportType reportName = ReportType.INVESTOR_REPORT)
        {
            QueryCriterion qryCriteria = null;

            switch (reportName)
            {
                case ReportType.INVESTOR_REPORT:

                    try
                    {
                        StringFieldCriterion criNotBrokered = new StringFieldCriterion();
                        criNotBrokered.FieldName = pfx + "CX.BSOINVESTORS";
                        criNotBrokered.Value = "BROKERED";
                        criNotBrokered.Include = false;
                        criNotBrokered.MatchType = StringFieldMatchType.CaseInsensitive;

                        if (criNotBrokered == null) { throw new Exception("criNotBrokered is null"); }

                        StringFieldCriterion criNotReverse = new StringFieldCriterion(pfx + "CX.BSOINVESTORS", "REVERSE", StringFieldMatchType.CaseInsensitive, false);

                        // FIELD CRITERION: EXCLUDE TRASH FOLDER
                        StringFieldCriterion criExcludeTrash = new StringFieldCriterion(pfx + "LoanFolder", "(Trash)", StringFieldMatchType.CaseInsensitive, false);

                        StringFieldCriterion criExcludeTesting1 = new StringFieldCriterion(pfx + "LoanFolder", "Test", StringFieldMatchType.StartsWith, false);

                        StringFieldCriterion criActiveLoans = new StringFieldCriterion(pfx + "LoanFolder", "Active Loans", StringFieldMatchType.CaseInsensitive, true);

                        StringFieldCriterion criClosedLoans = new StringFieldCriterion(pfx + "LoanFolder", "Closed Loans", StringFieldMatchType.CaseInsensitive, true);
                        StringFieldCriterion criClosedPreviousYear = new StringFieldCriterion(pfx + "LoanFolder", "Closed Loans - Prev Year", StringFieldMatchType.CaseInsensitive, true);
                        StringFieldCriterion criEmployeeLoans = new StringFieldCriterion(pfx + "LoanFolder", "Employee Loans", StringFieldMatchType.CaseInsensitive, true);
                        StringFieldCriterion criPendingAdverseAction = new StringFieldCriterion(pfx + "LoanFolder", "Pending Adverse Action", StringFieldMatchType.CaseInsensitive, true);
                        StringFieldCriterion criWithdrawn = new StringFieldCriterion(pfx + "LoanFolder", "Withdrawn Denied", StringFieldMatchType.CaseInsensitive, true);


                        QueryCriterion qryFolders = criExcludeTesting1.And(criExcludeTrash).And((criActiveLoans).Or(criClosedLoans).Or(criClosedPreviousYear).Or(criEmployeeLoans).Or(criPendingAdverseAction).Or(criWithdrawn));

                        qryCriteria = criNotBrokered.And(criNotReverse).And(qryFolders);

                        StringFieldCriterion criLTK = new StringFieldCriterion(pfx + "4002", "test", StringFieldMatchType.CaseInsensitive, true);

                        if (loginWithLTK)
                        {
                            Log.WriteLine("Switching to LTK Criteria");
                            qryCriteria = criLTK.And(criExcludeTrash);
                        }

                        if (qryCriteria == null) { throw new Exception("Query is null."); }

                        /*
                        CRITERIA:

                         IS NOT ANY OF THESE: BROKERED, REVERSE

                        FOLDERS: Active Loans
                            Closed Loans
                            Closed Loans - Prev Year
                            Employdee Loans
                            Pending Adverse Action
                            Withdrawn Denied

                        MILESTONES: ALL
                        */
                    }
                    catch (Exception ex)
                    {
                        Log.WriteError(ex.Message);
                    }
                    break;
            }
            Log.WriteLine("Criteria generated");
            return qryCriteria;

        }

    

        /// Function that connects tw Encompass session
        /// </summary>
        private static void StartSession()
        {
            // Start new session
            s = new Session();

            try
            {
                if (loginWithLTK)
                {
                    // Connect to Encompass
                    s.Start(LTK.URL, LTK.UserName, LTK.Password);
                }
                else
                {
                    s.Start(NorthernMortgage.URL, NorthernMortgage.UserName, LTK.Password);
                }

                if (s.IsConnected) { 
                    Log.WriteLine("Connected to Encompass."); 
                }
                else
                {
                    throw new Exception("Login Error");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("ERROR: Unable to connect to Encompass: " + ex.Message);
            }
        }

    }
}
