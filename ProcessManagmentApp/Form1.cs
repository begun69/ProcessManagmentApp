using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using Microsoft.VisualBasic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace ProcessManagmentApp
{
    public partial class Form1 : Form
    {
        private List<Process> processes = null;

        private ListViewItemComparer comparer = null;
       
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtResumeProcess(IntPtr processHandle);
        public Form1()
        {
            InitializeComponent();
        }


        private void GetProcesses()
        {
            processes.Clear();
            processes = Process.GetProcesses().ToList<Process>();
        }

        private void RefreshProcessesList(List<Process> processes, string keyword)
        {
            try
            {
                listView1.Items.Clear();
                foreach (Process p in processes)
                {
                    if (p != null)
                    {
                        double memSize = 0;
                        PerformanceCounter pc = new PerformanceCounter
                        {
                            CategoryName = "Process",
                            CounterName = "Working Set - Private",
                            InstanceName = p.ProcessName
                        };

                        memSize = (double)pc.NextValue() / (1000 * 1000);

                        // Get the priority and CPU time
                        string priority = p.PriorityClass.ToString();
                        string cpuTime = p.TotalProcessorTime.ToString(@"hh\:mm\:ss");  // Format the CPU time as hours, minutes, and seconds

                        // Include ProcessName, Memory Size, PID, Priority, and CPU Time
                        string[] row = new string[] {
                        p.ProcessName.ToString(),
                        Math.Round(memSize, 1).ToString() + "MB",
                        p.Id.ToString(),
                        priority,
                        cpuTime
                };
                        listView1.Items.Add(new ListViewItem(row));

                        pc.Close();
                        pc.Dispose();
                    }
                }

                Text = $"Running Processes '{keyword}': " + processes.Count.ToString();
            }
            catch (Exception) { }
        }



        private void RefreshProcessesList()
        {
            listView1.Items.Clear();

            foreach (Process p in processes)
            {
                if (p != null)
                {
                    try
                    {
                        double memSize = 0;
                        PerformanceCounter pc = new PerformanceCounter
                        {
                            CategoryName = "Process",
                            CounterName = "Working Set - Private",
                            InstanceName = p.ProcessName
                        };

                        memSize = (double)pc.NextValue() / (1000 * 1000);

                        // Safely retrieve Priority and CPU Time
                        string priority = "N/A";
                        string cpuTime = "N/A";
                        string status = p.HasExited ? "Exited" : (p.Responding ? "Running" : "Suspended");

                        // Try to get PriorityClass safely
                        try
                        {
                            priority = p.PriorityClass.ToString();
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            // Log or silently skip this process if "Access is denied"
                            Console.WriteLine($"Access denied for PriorityClass of process {p.ProcessName} (PID: {p.Id}): {ex.Message}");
                        }

                        // Try to get CPU Time safely
                        try
                        {
                            cpuTime = p.TotalProcessorTime.ToString(@"hh\:mm\:ss");
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            // Log or silently skip this process if "Access is denied"
                            Console.WriteLine($"Access denied for CPU Time of process {p.ProcessName} (PID: {p.Id}): {ex.Message}");
                        }

                        
                        string[] row = new string[] {
                        p.ProcessName.ToString(),
                        Math.Round(memSize, 1).ToString() + "MB",
                        p.Id.ToString(),
                        priority,
                        cpuTime,
                        status
                                };
                        listView1.Items.Add(new ListViewItem(row));

                        pc.Close();
                        pc.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Handle any other process access exceptions here (e.g., memory, etc.)
                        Console.WriteLine($"Error accessing process {p.ProcessName} (PID: {p.Id}): {ex.Message}");
                    }
                }
            }

            Text = $"Running Processes: " + processes.Count.ToString();
        }


       

        private void KillProcess(Process process)
        {
            process.Kill();
            process.WaitForExit();
        }

       
        private void KillTreeOfProcesses(int pid)
        {
            if (pid == 0)
            {
                return;
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);

            ManagementObjectCollection objectCollection = searcher.Get();

            foreach (ManagementObject obj in objectCollection)
            {
                KillTreeOfProcesses(Convert.ToInt32(obj["ProcessID"]));
            }

            try
            {
                Process p = Process.GetProcessById(pid);
                p.Kill();
                p.WaitForExit();
            }
            catch (ArgumentException) { }
        }

        private int GetParentProcessId(Process p)
        {
            int parentID = 0;
            try
            {
                ManagementObject managementObject = new ManagementObject("win32_process.handle='" + p.Id + "'");
                parentID = Convert.ToInt32(managementObject["ParentProcessId"]);
            }
            catch (Exception) { }
            Console.WriteLine(parentID);
            return parentID;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            GetProcesses();
            RefreshProcessesList();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    Process processesToKill = processes.Where((x) => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
                    KillProcess(processesToKill);

                    GetProcesses();
                    RefreshProcessesList();
                }
            } catch (Exception) { }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    Process processesToKill = processes.Where((x) => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
                    KillTreeOfProcesses(GetParentProcessId(processesToKill));

                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        private void changeProrityStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[4] != null)
                {
                    Process processesToKill = processes.Where((x) => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
                    KillProcess(processesToKill);

                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            processes = new List<Process>();
            GetProcesses();
            RefreshProcessesList();

            comparer = new ListViewItemComparer();
            comparer.ColumnIndex = 0;
        }

        private void SetAfinittyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    Process processesToKill = processes.Where((x) => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
                    KillTreeOfProcesses(GetParentProcessId(processesToKill));

                    GetProcesses();
                    RefreshProcessesList();
                }
            }
            catch (Exception) { }
        }

        private void runNewProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Interaction.InputBox("Enter name of program", "Starting new process");
            try
            {
                Process.Start(path);
            }catch(Exception) { }
        }


        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            GetProcesses();
            List<Process> filteredProcesses = processes.Where((x) => x.ProcessName.ToLower().Contains(toolStripTextBox1.Text.ToLower())).ToList<Process>();
            RefreshProcessesList(filteredProcesses, toolStripTextBox1.Text);
        }

       
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            comparer.ColumnIndex = e.Column;
            comparer.SortDirection = comparer.SortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

            listView1.ListViewItemSorter = comparer;
            listView1.Sort();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Created by itshorik");

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems[0] != null)
                {
                    Process selectedProcess = processes.Where(x => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
                    int affinity = int.Parse(Interaction.InputBox("Enter CPU core mask (in hex, e.g., 0x1 for core 1, 0x3 for cores 1 & 2)", "Set Processor Affinity", "0x1"));
                    selectedProcess.ProcessorAffinity = (IntPtr)affinity;

                    MessageBox.Show($"Processor Affinity set to {affinity.ToString("X")}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting processor affinity: " + ex.Message);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = @"C:\Users\bogda\AppData\Roaming\Zoom\bin\Zoom.exe";
            Process.Start(filepath);
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void cmdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = @"C:\Windows\System32\cmd.exe";
            Process.Start(filepath);
        }

        private void tabulateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = @"X:\Lesson\Tabulate\bin\Debug\net6.0\Tabulate.exe";
            Process.Start(filepath);
        }

        private void findWordByMaxLengthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = @"X:\Lesson\Longest_word\bin\Debug\net6.0\Longest_word.exe";
            Process.Start(filepath);
        }

        private void terminateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process processesToKill = processes.Where((x) => x.ProcessName == listView1.SelectedItems[0].SubItems[0].Text).ToList()[0];
            KillTreeOfProcesses(GetParentProcessId(processesToKill));

            GetProcesses();
            RefreshProcessesList();
        }
        private void SuspendProcess(Process process)
        {
            try
            {
                var processHandle = process.Handle;
                NtSuspendProcess(processHandle);
                MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) suspended.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to suspend process: {ex.Message}");
            }
        }
        private void ResumeProcess(Process process)
        {
            try
            {
                var processHandle = process.Handle;
                NtResumeProcess(processHandle);
                MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) resumed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to resume process: {ex.Message}");
            }
        }
        private void suspendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                    var processName = listView1.SelectedItems[0].SubItems[0].Text;
                    var process = processes.Find(p => p.ProcessName == processName);
                    if (process != null)
                    {
                        SuspendProcess(process);
                    }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error suspending process: {ex.Message}");
            }
        }

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
             try
            {
                var processName = listView1.SelectedItems[0].SubItems[0].Text;
                var process = processes.Find(p => p.ProcessName == processName);
                var processHandle = process.Handle;
                NtResumeProcess(processHandle);
                MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) resumed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to resume process: {ex.Message}");
            }
        }
    }
}
