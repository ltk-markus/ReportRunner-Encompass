using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using EllieMae.Encompass.Client;
using EllieMae.Encompass.Query;
using EllieMae.Encompass.Reporting;
using EllieMae.Encompass.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http.Headers;

namespace MERSExtract
{
    class Program
    {
        /*
        // SET THE NUMBER OF RECORDS PER QUERY, MUST BE LESS THAN THE NUMBER OF LOANS IN THE EXTERNAL FILE, AND LESS THAN 900.
        // IT DOESN'T MATTER WHAT THE SIZE OF THIS IS, ALL RECORDS WILL STILL BE EXPORTED AS LONG AS IT MEETS ABOVE CRITERIA
        public static int result_div = 150;


        static void Main(string[] args)
        {
        }

        /// <summary>
        /// Function that runs the application
        /// </summary>
        private static void RunApp()
        {
            // Connect to Encompass
            StartSession();

            // RunQuery(4);
            // return;

            // RUN QUERY FOR EACH GROUP AND CREATE CSV
            for (int group = 1; group <= 3; group++)
            {
                RunQuery(group);
            }

        }

        /// <summary>

        private static void RunQuery(int group)
        {
            // DEFINE VARIABLES
            StringBuilder path = new StringBuilder();
            List<string> header_row = new List<string>();
            List<List<string>> data = new List<List<string>>();
            DateTime date_now = DateTime.Now;
            string header_code = "111111111111111111";
            string header_extract_org = "1007830";
            string header_extract_date = TranslateDate(date_now, true);
            string header_extracted_by = "1007830";
            string prefix = Reports.ReportingDatabaseCanonicalPrefix;
            string path_date = TranslateDate(date_now, false);


            // CREATE HEADER ROW
            header_row.Add(header_code);
            header_row.Add(header_extract_org);
            header_row.Add(header_extract_date);
            header_row.Add(header_extracted_by);

            // BUILD FILE NAME ACCORDING TO SPEC
            path.Append(dir);
            path.Append(header_extract_org + "_");
            path.Append("0" + group.ToString() + "_");
            path.Append(path_date + "_");
            path.Append(header_extracted_by);
            path.Append(".txt");

            // DEFINE FIELDS TO GRAB (MUST BE IN REPORTING DATABAE)
            StringList fields = new StringList();
            fields.Add(prefix + "364");             // LOAN NUMBER
            fields.Add(prefix + "Log.MS.Date.Funding");
            fields.Add(prefix + "Log.MS.Date.Purchased");
            fields.Add(prefix + "LoanFolder");
            fields.Add(prefix + "2370");

            // SORT ORDER: SORT BY LOAN NUMBER, MOST RECENT ON TOP (DECENDING)
            SortCriterionList sort = new SortCriterionList();
            sort.Add(new SortCriterion(prefix + "364", SortOrder.Descending));
            
            // ---------- MILESTONE CRITERIA ---------- 
            StringFieldCriterion criFunded = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Funding", StringFieldMatchType.Exact, true);
            StringFieldCriterion criShipping = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Shipping", StringFieldMatchType.Exact, true);
            StringFieldCriterion criPurchased = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Purchased", StringFieldMatchType.Exact, true);
            StringFieldCriterion criSettlement = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Settlement", StringFieldMatchType.Exact, true);
            StringFieldCriterion criReconciled = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Reconciled", StringFieldMatchType.Exact, true);
            StringFieldCriterion criCompleted = new StringFieldCriterion(prefix + "Log.MS.CurrentMilestone", "Completion", StringFieldMatchType.Exact, true);

            // ---------- FOLDER CRITERIA ---------- 

            // FIELD CRITERION: EXCLUDE TRASH FOLDER
            StringFieldCriterion criExcludeTrash = new StringFieldCriterion(prefix + "LoanFolder", "(Trash)", StringFieldMatchType.CaseInsensitive, false);

            // FIELD CRITERION: EXCLUDE TEST FOLDERS
            StringFieldCriterion criExcludeTest1 = new StringFieldCriterion(prefix + "LoanFolder", "Testing_Training", StringFieldMatchType.CaseInsensitive, false);
            StringFieldCriterion criExcludeTest2 = new StringFieldCriterion(prefix + "LoanFolder", "LTKTest", StringFieldMatchType.CaseInsensitive, false);
            StringFieldCriterion criExcludeTest3 = new StringFieldCriterion(prefix + "LoanFolder", "TOM_Testing", StringFieldMatchType.CaseInsensitive, false);
            StringFieldCriterion criExcludeTest4 = new StringFieldCriterion(prefix + "LoanFolder", "InfoTech Staging", StringFieldMatchType.CaseInsensitive, false);


            // FIELD CRITERION: ONLY INCLUDE THESE FOLDERS

            StringFieldCriterion criClosedLoans = new StringFieldCriterion(prefix + "LoanFolder", "Closed", StringFieldMatchType.CaseInsensitive, true);
            StringFieldCriterion criEmployeeClosedLoans = new StringFieldCriterion(prefix + "LoanFolder", "Employee Closed Loans", StringFieldMatchType.CaseInsensitive, true);
            StringFieldCriterion criEmployeeLoans = new StringFieldCriterion(prefix + "LoanFolder", "Employee Loans", StringFieldMatchType.CaseInsensitive, true);
            StringFieldCriterion criMyPipelineLoans = new StringFieldCriterion(prefix + "LoanFolder", "My Pipeline", StringFieldMatchType.CaseInsensitive, true);


            // ---------- OTHER CRITERIA ---------- 

            // FIELD CRITERION: EXCLUDE VINTAGE LOANS
            StringFieldCriterion criVintage1 = new StringFieldCriterion(prefix + 364, "VIP011686", StringFieldMatchType.CaseInsensitive, false);
            StringFieldCriterion criVintage2 = new StringFieldCriterion(prefix + 364, "VIP011685", StringFieldMatchType.CaseInsensitive, false);

            // FIELD CRITERION: Only Retails Loans
            StringFieldCriterion criRetail = new StringFieldCriterion(prefix + "2626", "Banked - Retail", StringFieldMatchType.CaseInsensitive, true);

            QueryCriterion generalCriteria = (criClosedLoans.Or(criEmployeeClosedLoans).Or(criEmployeeLoans).Or(criMyPipelineLoans)).And(criVintage1).And(criVintage2);


            /* ---------- MERS CRITERIA ---------- *

            // FIELD CRITERION: ONLY GRAB LOANS THAT ARE ON OR BEFORE 9/4/2020
            DateFieldCriterion criFundingAfter = new DateFieldCriterion(prefix + "MS.FUN", DateTime.Parse("4/1/2018"), OrdinalFieldMatchType.GreaterThanOrEquals, DateFieldMatchPrecision.Day);

            // FIELD CRITERION: PURCHASE ADVICE DATE MUST BE EMPTY
            DateFieldCriterion criPADate = new DateFieldCriterion(prefix + "2370", DateFieldCriterion.EmptyDate, OrdinalFieldMatchType.Equals, DateFieldMatchPrecision.Exact);

            // FIELD CRITERION: EXCLUDE LOANS THAT HAVE A CENLAR DATE
            DateFieldCriterion criCenlarDate = new DateFieldCriterion(prefix + "CX.CEN.TRANSFERDATE", DateFieldCriterion.EmptyDate, OrdinalFieldMatchType.Equals, DateFieldMatchPrecision.Exact);

            // FIELD CRITERION: INCLUDE FIRST LIENS
            StringFieldCriterion criFirstLien = new StringFieldCriterion(prefix + "420", "First Lien", StringFieldMatchType.CaseInsensitive, true);

            // FIELD CRITERION: INCLUDE SECOND LIENS
            StringFieldCriterion criSecondLien = new StringFieldCriterion(prefix + "420", "Second Lien", StringFieldMatchType.CaseInsensitive, true);


            // FIELD CRITERION: Exclude investores containing "BAC"
            StringFieldCriterion criExcludeInvestorBAC = new StringFieldCriterion(prefix + "VEND.X263", "BAC", StringFieldMatchType.Contains, false);

            // FIELD CRITERION: Virgina Housing Development Investor
            StringFieldCriterion criInvestorVirginiaHousing = new StringFieldCriterion(prefix + "VEND.X263", "Virginia Housing Development", StringFieldMatchType.Contains, true);

            // FIELD CRITERION: Exclude Reverse Mortgages
            StringFieldCriterion criExcludeReverse = new StringFieldCriterion(prefix + "HMDA.X56", "Reverse mortgage", StringFieldMatchType.CaseInsensitive, false);

            QueryCriterion criGeneral = (criFunded.Or(criShipping).Or(criPurchased).Or(criSettlement).Or(criReconciled).Or(criCompleted)).And(criClosedLoans.Or(criEmployeeClosedLoans).Or(criEmployeeLoans).Or(criMyPipelineLoans)).And(criExcludeTrash);


            // BUILD CRITERIA
            QueryCriterion criteria = criGeneral.And((criPADate).And(criCenlarDate).And(criFundingAfter).And(criExcludeReverse).And(criExcludeInvestorBAC).And(criFirstLien)).Or((criPADate).And(criInvestorVirginiaHousing).And(criFundingAfter).And(criSecondLien));

            List<QueryCriterion> feb_criteria = new List<QueryCriterion>();
            feb_criteria.Add(criteria);
            // feb_criteria = CreateCriteria();


            switch (group)
            {
                case 1:
                    // GROUP 1 FIELDS
                    fields.Add(prefix + "1051");    // MERS #
                    fields.Add(prefix + "420");     // LIEN TYPE
                    fields.Add(prefix + "2");       // NOTE AMOUNT
                    fields.Add(prefix + "L770");    // NOTE DATE
                    fields.Add(prefix + "ORGID");   // ORG ID 
                    fields.Add(prefix + "VEND.X954"); // SERVICER ORG ID
                    fields.Add(prefix + "11");      // STREET ADDRESS 
                    fields.Add(prefix + "12");      // CITY 
                    fields.Add(prefix + "14");      // STATE
                    fields.Add(prefix + "15");      // ZIP
                    fields.Add(prefix + "2288");    // INVESTOR LOAN NUMBER
                    fields.Add(prefix + "1040");    // FHA/VA/MI LOAN NUMBER 
                    fields.Add(prefix + "1859");    // NAME OF TRUST

                    // GROUP 1 REPORT CURSOR
                    // LoanReportCursor cur = s.Reports.OpenReportCursor(fields, criteria, sort);

                    foreach (QueryCriterion qc in feb_criteria)
                    {
                        LoanReportCursor cur = s.Reports.OpenReportCursor(fields, qc, sort);
                        L("CURSOR 1: " + cur.Count.ToString() + " LOANS FOUND");
                        Console.ReadKey();
                        // ADD DATA TO MASTER LIST
                        foreach (LoanReportData d in cur)
                        {
                            List<string> item = new List<string>();
                            string mers = "";
                            string lientype = "";
                            string noteamount = "";
                            string notedate = "";
                            string orgid = "";
                            string servicerorgid = "";
                            string street = "";
                            string street_num = "";// 
                            StringBuilder street_name = new StringBuilder();
                            string city = "";
                            string state = "";
                            string zip = "";
                            string investorloannumber = "";
                            string fhavamiloannumber = "";
                            string nameoftrust = "";

                            try { mers = d[prefix + "1051"].ToString().Trim(); } catch { }
                            try { lientype = d[prefix + "420"].ToString().Trim(); } catch { }
                            switch (lientype.ToUpper())
                            {
                                case "FIRST LIEN":
                                    lientype = "01";
                                    break;
                                case "SECOND LIEN":
                                    lientype = "02";
                                    break;
                                default:
                                    lientype = "99";
                                    break;
                            }
                            try { noteamount = d[prefix + "2"].ToString().Trim(); } catch { }
                            decimal noteamount_d = 0;
                            try { noteamount_d = Math.Round(Decimal.Parse(noteamount), 2); } catch { }
                            noteamount = noteamount_d.ToString();
                            try { 
                                notedate = d[prefix + "L770"].ToString().Trim(); 
                            } catch (Exception ex){
                                Console.WriteLine(ex.Message);
                            }
                            DateTime notedate_date = DateTime.MinValue;
                            try { notedate_date = DateTime.Parse(notedate); } catch { }
                            if (notedate_date != DateTime.MinValue)
                            {
                                notedate = TranslateDate(notedate_date, true);
                            }
                            else
                            {
                                notedate = "";
                            }
                            try { orgid = d[prefix + "ORGID"].ToString().Trim(); } catch { }
                            orgid = "1007830";
                            try { servicerorgid = d[prefix + "VEND.X954"].ToString().Trim(); } catch { }
                            try { street = d[prefix + "11"].ToString().Trim(); } catch { }

                            // SPLIT STREET ADDRESS BY SPACES
                            string[] address = street.Split(' ');

                            // GET STREET NUMBER AND STREET NAME OUT OF STREET ADDRESS FIELD AND PLUG THEM INTO THEIR OWN VARIABLES
                            int x = 0;
                            foreach (string add in address)
                            {
                                x++;
                                if (x == 1)
                                {
                                    street_num = add;
                                }
                                else
                                {
                                    street_name.Append(add);
                                    if (address.Length != x)
                                    {
                                        street_name.Append(" ");
                                    }
                                }
                            }

                            try { city = d[prefix + "12"].ToString().Trim(); } catch { }
                            try { state = d[prefix + "14"].ToString().Trim(); } catch { }
                            try { zip = d[prefix + "15"].ToString().Trim(); } catch { }
                            try { investorloannumber = d[prefix + "2288"].ToString().Trim(); } catch { }
                            try { fhavamiloannumber = d[prefix + "1040"].ToString().Trim(); } catch { }
                            try { nameoftrust = d[prefix + "1859"].ToString().Trim(); } catch { }

                            item.Add(mers);
                            item.Add(lientype);
                            item.Add(noteamount);
                            item.Add(notedate);
                            item.Add(orgid);
                            item.Add("Highland Residential Mortgage");  // Original Note Holder – [Highland Residential Mortgage]
                            item.Add(servicerorgid);
                            item.Add("1007830");    // Subservicer Org ID - Should always be -1007830
                            item.Add("1007830");    // Investor Org ID [2278] - Should always be - 1007830
                            item.Add("");           // Property Preservation Company 1 Org ID – Confirmed by Kim this field is Conditional and does not need a value.
                            item.Add(street_num);
                            item.Add(street_name.ToString());
                            item.Add("");           // Filler
                            item.Add("");           // Property Unit Number
                            item.Add(city);
                            item.Add(state);
                            item.Add(zip);
                            item.Add("Y");          // MOM Indicator – Should always be "Y"
                            item.Add(investorloannumber);
                            item.Add(fhavamiloannumber);
                            item.Add(nameoftrust);
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Filler
                            item.Add("");           // Confirmed by Kim this field is Conditional and does not need a value.
                            data.Add(item);
                        }

                        cur.Close();
                        L("CURSOR 1 CLOSED");

                    }
                    break;
                case 2:
                    // GROUP 2 FIELDS
                    fields.Add(prefix + "1051");    // MERS #
                    fields.Add(prefix + "4002");    // BORROWER LAST NAME
                    fields.Add(prefix + "4000");    // BORROWER FIRST NAME
                    fields.Add(prefix + "4001");    // BORROWER MIDDLE NAME

                    // GROUP 2 REPORT CURSOR
                    //LoanReportCursor cur2 = s.Reports.OpenReportCursor(fields, criteria, sort);

                    foreach (QueryCriterion qc in feb_criteria)
                    {
                        LoanReportCursor cur2 = s.Reports.OpenReportCursor(fields, qc, sort);
                        L("CURSOR 2: " + cur2.Count.ToString() + " LOANS FOUND");

                        // ADD DATA TO MASTER LIST
                        foreach (LoanReportData d in cur2)
                        {
                            List<string> item = new List<string>();
                            string mers = "";
                            string borr_last = "";
                            string borr_first = "";
                            string borr_middle = "";

                            try { mers = d[prefix + "1051"].ToString(); } catch { }
                            try { borr_last = d[prefix + "4002"].ToString(); } catch { }
                            try { borr_first = d[prefix + "4000"].ToString(); } catch { }
                            try { borr_middle = d[prefix + "4001"].ToString(); } catch { }

                            item.Add(mers);
                            item.Add(borr_last);
                            item.Add(borr_first);
                            item.Add(borr_middle);
                            item.Add("");
                            data.Add(item);
                        }
                        cur2.Close();
                        L("CURSOR 2 CLOSED");

                    }
                    break;


                case 3:
                    // GROUP 3 FIELDS
                    fields.Add(prefix + "1051");    // MERS #
                    fields.Add(prefix + "13");      // COUNTY

                    // GROUP 3 REPORT CURSOR
                    // LoanReportCursor cur3 = s.Reports.OpenReportCursor(fields, criteria, sort);

                    foreach (QueryCriterion qc in feb_criteria)
                    {
                        LoanReportCursor cur3 = s.Reports.OpenReportCursor(fields, qc, sort);
                        L(cur3.Count.ToString() + " LOANS FOUND");

                        // ADD DATA TO MASTER LIST
                        foreach (LoanReportData d in cur3)
                        {
                            List<string> item = new List<string>();
                            string mers = "";
                            string county = "";

                            try { mers = d[prefix + "1051"].ToString(); } catch { }
                            try { county = d[prefix + "13"].ToString(); } catch { }

                            item.Add(mers);
                            item.Add(county);
                            data.Add(item);
                        }
                        cur3.Close();
                        L("CURSOR 3 CLOSED");

                    }
                    break;
                case 4:

                    //LoanReportCursor cur4 = s.Reports.OpenReportCursor(fields, criteria, sort);

                    foreach (QueryCriterion qc in feb_criteria)
                    {
                        LoanReportCursor cur4 = s.Reports.OpenReportCursor(fields, qc, sort);
                        L(cur4.Count.ToString() + " LOANS FOUND");


                        foreach (LoanReportData d in cur4)
                        {
                            List<string> item = new List<string>();
                            string loan_num = "";
                            string loan_folder = "";
                            string funding = "";
                            string purchased = "";

                            try { loan_num = d[prefix + "364"].ToString(); } catch { }
                            try { loan_folder = d[prefix + "LoanFolder"].ToString(); } catch { }
                            try { funding = d[prefix + "Log.MS.Date.Funding"].ToString(); } catch { }
                            try { purchased = d[prefix + "Log.MS.Date.Purchased"].ToString(); } catch { }

                            item.Add(loan_num);
                            item.Add(loan_folder);
                            item.Add(funding);
                            item.Add(purchased);
                            data.Add(item);

                        }
                        cur4.Close();
                        L("CURSOR 4 CLOSED");
                    }
                    break;
            }


            // WRITE TO CSV
            if (group != 4)
            {
                CreateCSV(header_row, fields, data, path.ToString(), false, "\t", Environment.NewLine, true);
            }
            else
            {
                CreateCSV(new List<string>() { "LOAN NUMBER", "LOAN FOLDER", "FUNDING DATE", "PURCHASED DATE" }, fields, data, path.ToString().Substring(0, path.ToString().Length - 3) + "csv", true, ",", Environment.NewLine, false);
            }

        }

        private static void CreateCSV(List<string> header_row, StringList fields, List<List<string>> loans, string path, bool wrapped_in_quotes, string delimiter, string terminate_row_character, bool include_hrm_trailer)
        {
            StringBuilder csv = new StringBuilder();
            int i = 0;

            // ADD HEADER ROW
            foreach (string cell in header_row)
            {
                // COUNT WHICH CELL WE ARE ON
                i++;

                // BEGIN QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }

                // APPEND CELL DATA
                csv.Append(cell);

                // CLOSE QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }

                // CHECK IF WE ARE ON THE LAST CELL 
                if (i != header_row.Count)
                {
                    // ADD CELL DELIMITER IF NOT LAST CELL
                    csv.Append(delimiter);
                }
                else
                {
                    // ADD ROW TERMINATER IF WE'RE ON THE LAST CELL
                    csv.Append(terminate_row_character);
                }
            }

            // LOOP THROUGH EACH LOAN
            foreach (List<string> loan in loans)
            {
                // RESET CELL NUMBER TO ZERO
                i = 0;

                // LOOP THROUGH EACH CELL
                foreach (string cell in loan)
                {
                    i++;
                    // BEGIN QUOTES IF wrapped_in_quotes IS SET TO TRUE
                    if (wrapped_in_quotes) { csv.Append("\""); }

                    // APPEND CELL DATA
                    csv.Append(cell);

                    // CLOSE QUOTES IF wrapped_in_quotes IS SET TO TRUE
                    if (wrapped_in_quotes) { csv.Append("\""); }

                    // CHECK IF WE ARE ON THE LAST CELL 
                    if (i != loan.Count)
                    {
                        // ADD CELL DELIMITER IF NOT LAST CELL
                        csv.Append(delimiter);
                    }
                    else
                    {
                        // ADD ROW TERMINATER IF WE'RE ON THE LAST CELL
                        csv.Append(terminate_row_character);
                    }
                }
            }


            // FILE TRAILER
            if (include_hrm_trailer)
            {
                // BEGIN QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }

                // APPEND CELL DATA
                csv.Append("999999999999999999");

                // TERMINATE QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }

                // ADD DELIMITER
                csv.Append(delimiter);

                // BEGIN QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }

                // APPEND NUMBER OF RECORDS WITH UP TO 9 LEADING ZEROS
                csv.Append(loans.Count.ToString("D9"));

                // TERMINATE QUOTES IF wrapped_in_quotes IS SET TO TRUE
                if (wrapped_in_quotes) { csv.Append("\""); }
            }
            // WRITE TO CSV
            File.WriteAllText(path, csv.ToString());

            // OPEN CSV FILE
            Process.Start(path);

        }

        /// <summary>
        /// Logs a message to the console and/or an external file.
        /// </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="timestamp">true - include a timestamp with message</param>
        /// <param name="show_on_console">true - show in console</param>
        /// <param name="log_path">Path to file where you want message logged</param>
        public static void L(string msg, bool timestamp = true, bool show_on_console = true, string log_path = "")
        {
            string outmsg = "";
            if (timestamp)
            {
                outmsg = "[" + DateTime.Now.ToString() + "] ";
            }
            outmsg += msg;

        }

        /// <summary>
        /// Translate Date
        /// </summary>
        /// <param name="d">DateTime you want translated</param>
        /// <param name="add_slashes">True - Add slashes to date</param>
        /// <returns></returns>
        public static string TranslateDate(DateTime d, bool add_slashes)
        {
            string full_date = "";
            string month = "";
            string day = "";
            string year = d.Year.ToString();

            // PREPEND ZERO TO BEGININNG OF MONTH IF LESS THAN 10
            if (d.Month < 10)
            {
                month = "0" + d.Month.ToString();
            }
            else
            {
                month = d.Month.ToString();
            }

            // PREPEND ZERO TO BEGININNG OF DAY IF LESS THAN 10
            if (d.Day < 10)
            {
                day = "0" + d.Day.ToString();
            }
            else
            {
                day = d.Day.ToString();
            }

            if (add_slashes)
            {
                // BUILD DATE WITH SLASHES
                full_date = month + "/" + day + "/" + year;
            }
            else
            {
                // BUILD DATE WITHOUT SLASHES
                full_date = month + day + year;
            }

            return full_date;
        }

        /// <summary>
        /// Grabs a single columned file and returns each row as a line item in a list
        /// </summary>
        /// <param name="file_path">Path to file</param>
        /// <returns>List<string></returns>
        public static List<string> GetFileLines(string file_path)
        {
            // GET INDIVIDUAL EMAIL OPT OUTS FROM LO'S
            List<string> output = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(File.OpenRead(file_path)))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        output.Add(line);

                    }
                }
            }
            catch (Exception ex)
            {
                L(ex.Message);
            }
            return output;

        }

        /// <summary>
        /// Creates the criteria for the export from an external file
        /// </summary>
        /// <returns></returns>
        public static List<QueryCriterion> CreateCriteria()
        {
            List<QueryCriterion> cri_list = new List<QueryCriterion>();

            StringFieldCriterion nothing = new StringFieldCriterion(); 
            nothing.FieldName = Reports.ReportingDatabaseCanonicalPrefix + "364";
            nothing.Value = "randomloannumberblah";
            nothing.Include = true;
            nothing.MatchType = StringFieldMatchType.Exact;

            QueryCriterion criteria = nothing;

            List<string> nums = GetFileLines(@"C:\EncompassLog\mers_numbers.csv");

            int i = 0;
            decimal max = nums.Count / result_div;

            decimal num_queries = Math.Ceiling(max);

            decimal left_over = nums.Count - (result_div * (num_queries - 1));

            int query_count = 1;
            foreach (string num in nums)
            {
                i++;
                StringFieldCriterion cri = new StringFieldCriterion();
                cri.FieldName = Reports.ReportingDatabaseCanonicalPrefix + "1051";
                cri.Value = num;
                cri.Include = true;
                cri.MatchType = StringFieldMatchType.CaseInsensitive;
                criteria = criteria.Or(cri);
                L("[" + query_count.ToString() + "] [" + i.ToString() + "] " + num);

                if (i % result_div == 0 && query_count != num_queries)
                {
                    // Console.ReadKey();
                    cri_list.Add(criteria);
                    criteria = nothing;
                    L("Criteria #" + query_count.ToString() + " Added.");
                    query_count++;
                    i = 0;
                }
                else if (query_count == num_queries && i == left_over)
                {
                    // Console.ReadKey();
                    cri_list.Add(criteria);
                    criteria = nothing;
                    L("Criteria #" + query_count.ToString() + " Added (Final).");
                }

            }

            return cri_list;
    }    
*/
    
    }
}
