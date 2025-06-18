using System;
using System.Collections.ObjectModel;
using System.Management.Automation; // Added NuGet package
using System.Text;
using System.Threading.Tasks;

namespace AICommandPrompt.Services
{
    public class PowerShellService
    {
        public async Task<PowerShellExecutionResult> ExecuteCommandAsync(string command)
        {
            var result = new PowerShellExecutionResult
            {
                StandardOutput = string.Empty,
                ErrorOutput = string.Empty,
                HadErrors = false
            };

            try
            {
                // Using PowerShell.Create() ensures it runs in a separate runspace.
                using (PowerShell ps = PowerShell.Create())
                {
                    ps.AddScript(command);

                    // Invoke the command asynchronously on a background thread
                    Collection<PSObject> psOutput = await Task.Run(() => ps.Invoke());

                    // Process standard output
                    var stdOutputBuilder = new StringBuilder();
                    if (psOutput != null)
                    {
                        foreach (PSObject outputItem in psOutput)
                        {
                            if (outputItem != null)
                            {
                                stdOutputBuilder.AppendLine(outputItem.ToString());
                            }
                        }
                    }
                    result.StandardOutput = stdOutputBuilder.ToString();

                    // Process errors
                    var errOutputBuilder = new StringBuilder();
                    if (ps.Streams.Error != null && ps.Streams.Error.Count > 0)
                    {
                        result.HadErrors = true;
                        foreach (ErrorRecord errorRecord in ps.Streams.Error)
                        {
                            if (errorRecord != null)
                            {
                                // You can format this more extensively if needed
                                errOutputBuilder.AppendLine($"Error: {errorRecord.ToString()}");
                                if (errorRecord.Exception != null)
                                {
                                    errOutputBuilder.AppendLine($"Exception: {errorRecord.Exception.Message}");
                                    // For more detail: errOutputBuilder.AppendLine($"StackTrace: {errorRecord.Exception.StackTrace}");
                                }
                            }
                        }
                    }
                    result.ErrorOutput = errOutputBuilder.ToString();

                    // Check if there were any errors that didn't get written to the stream but PowerShell instance indicates failure
                    if (ps.HadErrors && !result.HadErrors) // ps.HadErrors is true if any error stream had data.
                    {
                        result.HadErrors = true;
                        if (string.IsNullOrEmpty(result.ErrorOutput)) // If Error stream was empty but ps.HadErrors is true
                        {
                             result.ErrorOutput = "An unspecified error occurred during PowerShell command execution.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch exceptions from PowerShell.Create(), AddScript(), or critical Invoke issues
                result.ErrorOutput = $"An unexpected error occurred during PowerShell command execution: {ex.Message}";
                result.HadErrors = true;
            }

            return result;
        }
    }
}
