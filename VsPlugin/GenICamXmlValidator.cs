////////////////////////////////////////////////////////////////////////////////
// File: GenICamXmlValidator.cs
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Camera Gen<i>Cam XML validation plugin for Visual Studio 2015, 2017
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Author: Nikolay Bitkin
// Created:	12-JUNE-2019
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Copyright (C) 2018-2019 Imperx, Inc. All rights reserved. 
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE;
using EnvDTE80;
using Task = System.Threading.Tasks.Task;

namespace GenICamXmlValidator
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenICamXmlValidator
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a3febda4-a49a-4c3a-bdea-fe3ccdf5faa1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenICamXmlValidator"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenICamXmlValidator(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenICamXmlValidator Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in GenICamXmlValidator's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new GenICamXmlValidator(package, commandService);


        }


        /// <summary>
        /// This method is used to run the XML validator utility
        /// </summary>
        /// <param name="xmlFileName">GenICam XML file name</param>
        /// <param name="gcXmlErrors">Errors</param>
        private bool RunValidator(string xmlFileName, ref string gcXmlErrors)
        {
			// Validator EXE file
			string strValidatorExe = Assembly.GetExecutingAssembly().Location;
			strValidatorExe = strValidatorExe.Replace("GenICamXmlValidator.dll", "GcXmlValidator.exe");

			// Output file name
			//string outFileName = System.IO.Path.GetTempFileName();

			// Run validator utility 
			System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = strValidatorExe;
            pProcess.StartInfo.Arguments = xmlFileName;

            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true; // do not display a windows
            pProcess.Start();
            gcXmlErrors = pProcess.StandardOutput.ReadToEnd(); //The output result
            pProcess.WaitForExit();

            // Return true, if ExitCode is 0, i.e. no errors found
            if (pProcess.ExitCode==0)
                return true;

            // Return false, since ExitCode!=0
            return false;
        }

        public const int xmlErrMax = 100;

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            // Get current opened file 
            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Document document = dte2.ActiveDocument;

            // Return, if no documents opened
            if (document == null)
                return;

            document.Save();

            // Run Validator utility
            string xmlErrors="";
            bool validationOK = RunValidator(document.FullName, ref xmlErrors);

            // Output results
            IVsOutputWindowPane windowPane = (IVsOutputWindowPane)Package.GetGlobalService(typeof(SVsGeneralOutputWindowPane));
            if (null != windowPane)
            {
                windowPane.Clear();
                if (validationOK)
                {
                    // Output "completed OK" banner
                    windowPane.OutputString(xmlErrors);
                    windowPane.OutputString("========== Gen<i>Cam Validation completed OK ========== \n");
                    windowPane.Activate();
                }
                else
                {
                    // Output errors to "Error List" pane
                    string[] xmlErrorsArray = xmlErrors.Split('\n');
                    int errNum = xmlErrorsArray.GetLength(0)-1;
                    for (int i = 0; i < errNum; ++i)
                    {
                        // Parse error string to extract the error line number
                        string strTmp = xmlErrorsArray[i];
                        strTmp = strTmp.Replace(document.FullName, "");
                        strTmp = strTmp.TrimPrefix("(");
                        strTmp = strTmp.Remove(strTmp.IndexOf(")"));
                        uint nLine = UInt32.Parse(strTmp)-1;

                        // Output the error info
                        windowPane.OutputTaskItemString(xmlErrorsArray[i],
                            VSTASKPRIORITY.TP_HIGH,
                            VSTASKCATEGORY.CAT_BUILDCOMPILE,
                            "Error",
                            (int)Microsoft.VisualStudio.Shell.Interop._vstaskbitmap.BMP_COMPILE,
                            document.FullName,
                            nLine,
                            xmlErrorsArray[i]);

                    }

                    // Output "FAILED" banner
                    windowPane.OutputString("========== Gen<i>Cam Validation FAILED ========== \n");
                    windowPane.FlushToTaskList();
                }

            }
            else
            {
                // We have no IVsOutputWindowPane, just show message box
                VsShellUtilities.ShowMessageBox(
                this.package,
                validationOK ? "Gen<i>Cam validation OK": "Gen<i>Cam validation FAILED",
                "Gen<i>Cam validation",
                validationOK ? OLEMSGICON.OLEMSGICON_INFO : OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
