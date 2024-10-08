﻿using System;
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
        private Dictionary<int, bool> suspendedProcesses = new Dictionary<int, bool>();



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
                        string cpuTime = p.TotalProcessorTime.ToString(@"hh\:mm\:ss");
                        string status = p.HasExited ? "Exited" : (p.Responding ? "Running" : "Suspended");

                        // Include ProcessName, Memory Size, PID, Priority, and CPU Time
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
                        if (suspendedProcesses.ContainsKey(p.Id) && suspendedProcesses[p.Id])
                        {
                            status = "Suspended";
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
                        
                        Console.WriteLine($"Error accessing process {p.ProcessName} (PID: {p.Id}): {ex.Message}");
                    }
                }
            }

            Text = $"Running Processes: " + processes.Count.ToString();
        }




        private void KillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                    MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) terminated.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error terminating process: {ex.Message}");
            }
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

       /* private void SetAfinittyToolStripMenuItem_Click(object sender, EventArgs e)
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
        }*/

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

        

        private void tabulateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filepath = @"X:\Lesson\Tabulate\bin\Debug\net6.0\Tabulate.exe";
            Process.Start(filepath);
        }

        private void terminateToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }
        private void SuspendProcess(Process process)
        {
            try
            {
                NtSuspendProcess(process.Handle);
                suspendedProcesses[process.Id] = true;  // Mark process as suspended
                MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) suspended.");
                RefreshProcessesList();  // Ensure the list is refreshed after suspending
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
                NtResumeProcess(process.Handle);
                suspendedProcesses[process.Id] = false;  // Mark process as resumed
                MessageBox.Show($"Process {process.ProcessName} (PID: {process.Id}) resumed.");
                RefreshProcessesList();  // Ensure the list is refreshed after resuming
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to resume process: {ex.Message}");
            }
        }
       
        private void SetProcessPriority(Process process, ProcessPriorityClass priorityClass)
        {
            try
            {
                process.PriorityClass = priorityClass;
                MessageBox.Show($"Priority of {process.ProcessName} set to {priorityClass}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set priority: {ex.Message}");
            }
        }
        private void ChangePriority(ProcessPriorityClass priorityClass)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    string processName = listView1.SelectedItems[0].SubItems[0].Text;
                    Process process = processes.FirstOrDefault(p => p.ProcessName == processName);

                    if (process != null)
                    {
                        SetProcessPriority(process, priorityClass);
                    }
                    else
                    {
                        MessageBox.Show("Process not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing priority: {ex.Message}");
            }
        }
        private void ProcessRefreshTimer_Tick(object sender, EventArgs e)
        {
            GetProcesses(); // Refresh the list of processes
            RefreshProcessesList(); // Update the ListView with current process information
        }
       
        //
        private void idleToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.Idle);
        }

        private void belowNormalToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.BelowNormal);
        }

        private void normalToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.Normal);
        }

        private void aboveNormalToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.AboveNormal);
        }

        private void highToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.High);
        }

        private void realTimeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangePriority(ProcessPriorityClass.RealTime);
        }

        private void suspendToolStripMenuItem1_Click(object sender, EventArgs e)
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

        private void resumeToolStripMenuItem1_Click(object sender, EventArgs e)
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

        private void terminateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    string selectedProcessName = listView1.SelectedItems[0].SubItems[0].Text;
                    Process processToKill = processes.FirstOrDefault(p => p.ProcessName.Equals(selectedProcessName));

                    if (processToKill != null)
                    {
                        KillProcess(processToKill);
                        GetProcesses();  // Refresh process list after killing
                        RefreshProcessesList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error terminating process: {ex.Message}");
            }
        }
    }
}
