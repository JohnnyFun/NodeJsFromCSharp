namespace Services {
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;

  public class NodeJsService {
    public static async Task<string> RunNodeJsAsync(string data) {
      // call nodejs directly hydrate the webhook body expression
      // edge.js doesn't support .net core yet and it freezes up if you call it from here to a fullframework dll: https://github.com/agracio/edge-js/issues/92. And their INodeServices is basically not unit-testable.
      var result = new StringBuilder();
      var error = new StringBuilder();
      using var proc = new Process {
        StartInfo = new ProcessStartInfo {
          FileName = "node",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          RedirectStandardInput = true,
          CreateNoWindow = true
        }
      };
      proc.ErrorDataReceived += (s, e) => error.Append('\n').Append(e.Data);
      proc.OutputDataReceived += (s, e) => result.Append('\n').Append(e.Data);
      // The .proj file is configured to copy this js file to the output dir, so it should be at the runtime location of the c# code.
      // For my actual needs, I only passed a json string to the process to use as data.
      // If you really need to run dynamic js, just be careful to not allow injection of malicious code--for instance, code that could sensitive setting data from your server.
      // I just wanted to show how you could achieve the same result as the example code in my request.
      // An alternative to `eval` would be to create a .js file in c# and then run that file, fwiw.
      proc.StartInfo.ArgumentList.Add(Path.Combine("./ServerJs/run-dynamic-js-script.js"));
      proc.Start();
      proc.BeginOutputReadLine();
      proc.BeginErrorReadLine();
      var messageEnder = "--END-OF-DYNAMIC-JS--"; // nodejs needs to know when it has the full dynamic js script to run.
      await proc.StandardInput.WriteLineAsync(data + messageEnder);

      if (!proc.WaitForExit(5000)) throw new Exception($"Your node script took too long to finish running. Result so far:\n{result}.\n\nError so far:\n{error}");

      // Error message could contain stack trace, so depending on who will see it, you may want to strip that out.
      var errorMsg = error.ToString();
      if (proc.ExitCode != 0 || !string.IsNullOrWhiteSpace(errorMsg)) throw new Exception(errorMsg);
      var rawOutput = result.ToString();

      // if you know your dynamic js script will not console.log, you can simply return rawOutput at this point.
      var resultDelimeter = "--RESULT-DELIMITER--";
      var resultMatch = new Regex($@"{resultDelimeter}\n(.+)\n{resultDelimeter}", RegexOptions.Multiline).Match(rawOutput);
      var dynamicJsResult = resultMatch.Groups[1].Value;
      if (string.IsNullOrWhiteSpace(dynamicJsResult))
        throw new Exception($"body was not empty, but the result was empty. Raw nodejs output: {rawOutput}");
      return dynamicJsResult;
    }
  }
}