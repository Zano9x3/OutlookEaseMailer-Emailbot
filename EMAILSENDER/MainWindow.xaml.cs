using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace EmailSender
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if Outlook process is running
                if (!IsOutlookRunning())
                {
                    MessageBox.Show("Microsoft Outlook is not running. Please open Outlook before sending emails.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string recipients = RecipientTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(recipients))
                {
                    MessageBox.Show("At least one recipient email address is required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string subject = SubjectTextBox.Text;
                string body = BodyTextBox.Text.Replace(Environment.NewLine, "%0D%0A");

                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                {
                    MessageBox.Show("Subject and body are mandatory fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string[] recipientArray = recipients.Split(',');
                foreach (var recipient in recipientArray)
                {
                    string psScriptContent = $@"
                        Start-Process 'mailto:{recipient.Trim()}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}'
                    ";

                    string psScriptFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "email_sender.ps1");

                    // Write the PowerShell script content to a file
                    File.WriteAllText(psScriptFilePath, psScriptContent);

                    // Execute the PowerShell script
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{psScriptFilePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    await Task.Delay(10000); // Wait for 10 seconds before sending the next email

                    // Execute the SendKeys PowerShell script
                    await Task.Run(() =>
                    {
                        string psSecondScriptContent = @"
                            # Import the necessary namespace
                            Add-Type -AssemblyName System.Windows.Forms

                            # Create a new SendKeys object
                            $sendKeys = [System.Windows.Forms.SendKeys]

                            try {
                                # Send CTRL+ENTER
                                $sendKeys::SendWait('^~')
                                Start-Sleep -Milliseconds 500 # wait for 500ms
                            } catch {
                                Write-Error ""Error sending CTRL+ENTER: $_""
                            }
                        ";

                        string psSecondScriptFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "hit_send.ps1");

                        // Write the second PowerShell script content to a file
                        File.WriteAllText(psSecondScriptFilePath, psSecondScriptContent);

                        // Execute the second PowerShell script
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -File \"{psSecondScriptFilePath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsOutlookRunning()
        {
            Process[] processes = Process.GetProcessesByName("olk");
            return processes.Length > 0;
        }

        private void AddRecipientButton_Click(object sender, RoutedEventArgs e)
        {
            RecipientTextBox.Text += ",";
        }
    }
}
